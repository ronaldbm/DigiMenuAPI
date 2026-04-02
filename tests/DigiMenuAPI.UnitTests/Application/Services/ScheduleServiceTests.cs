using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de ScheduleService — horarios semanales y días especiales por Branch.
///
/// Notas de seed:
///   - BranchSchedule IDs 1-7 para Branch 1 → usar BranchId >= 100, IDs >= 100.
///   - Para testear UpdateScheduleDay se deben sembrar manualmente 7 filas
///     de BranchSchedule para la Branch de test.
///
/// Aspectos críticos:
///   1. InvalidScheduleDay (DayOfWeek > 6) retorna ValidationError.
///   2. IsOpen=true sin OpenTime/CloseTime retorna ValidationError.
///   3. SpecialDay con fecha pasada retorna ValidationError.
///   4. SpecialDay duplicado retorna Conflict.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ScheduleServiceTests : ServiceTestBase
{
    private ScheduleService CreateService()
        => new(Db, TenantService);

    // ── Helpers locales ───────────────────────────────────────────────────

    /// <summary>Siembra 7 filas de BranchSchedule para la Branch dada (como hace el seed real).</summary>
    private async Task SeedSchedulesForBranchAsync(int branchId, int startId = 100)
    {
        var open  = new TimeSpan(9,  0, 0);
        var close = new TimeSpan(22, 0, 0);

        for (byte day = 0; day <= 6; day++)
        {
            Db.BranchSchedules.Add(new BranchSchedule
            {
                Id        = startId + day,
                BranchId  = branchId,
                DayOfWeek = day,
                IsOpen    = day != 0, // Domingo cerrado
                OpenTime  = day != 0 ? open  : null,
                CloseTime = day != 0 ? close : null,
            });
        }
        await Db.SaveChangesAsync();
    }

    private async Task<BranchSpecialDay> SeedSpecialDayAsync(
        int id       = 100,
        int branchId = 100,
        DateTime? date = null)
    {
        var sd = new BranchSpecialDay
        {
            Id       = id,
            BranchId = branchId,
            Date     = (date ?? DateTime.UtcNow.Date.AddDays(7)).Date,
            IsClosed = true,
            Reason   = "Feriado test",
        };
        Db.BranchSpecialDays.Add(sd);
        await Db.SaveChangesAsync();
        return sd;
    }

    // ── 1. GetSchedule ────────────────────────────────────────────────────

    [Fact]
    public async Task GetSchedule_ReturnsSeven_DaysSortedByDayOfWeek()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSchedulesForBranchAsync(branchId: 100, startId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetSchedule(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(7);

        // Orden: Lunes(1) primero, Domingo(0) al final
        result.Data!.First().DayOfWeek.Should().Be(1); // Lunes
        result.Data.Last().DayOfWeek.Should().Be(0);   // Domingo
    }

    [Fact]
    public async Task GetSchedule_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetSchedule(110);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 2. UpdateScheduleDay ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateScheduleDay_InvalidDay_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 7,   // inválido — fuera de rango 0-6
            IsOpen:    true,
            OpenTime:  TimeSpan.FromHours(9),
            CloseTime: TimeSpan.FromHours(22)));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.InvalidScheduleDay);
    }

    [Fact]
    public async Task UpdateScheduleDay_IsOpenWithoutOpenTime_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSchedulesForBranchAsync(branchId: 100, startId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 1,    // Lunes
            IsOpen:    true,
            OpenTime:  null, // falta OpenTime → error
            CloseTime: TimeSpan.FromHours(22)));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.ScheduleOpenTimeRequired);
    }

    [Fact]
    public async Task UpdateScheduleDay_IsOpenWithoutCloseTime_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSchedulesForBranchAsync(branchId: 100, startId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 1,
            IsOpen:    true,
            OpenTime:  TimeSpan.FromHours(9),
            CloseTime: null)); // falta CloseTime → error

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.ScheduleCloseTimeRequired);
    }

    [Fact]
    public async Task UpdateScheduleDay_IsClosedDay_ClearsTimesAndPersists()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSchedulesForBranchAsync(branchId: 100, startId: 100);

        SetTenant(companyId: 100);
        // Cerrar el Lunes (DayOfWeek=1)
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 1,
            IsOpen:    false,
            OpenTime:  null,
            CloseTime: null));

        result.Success.Should().BeTrue();
        result.Data!.IsOpen.Should().BeFalse();
        result.Data.OpenTime.Should().BeNull();
        result.Data.CloseTime.Should().BeNull();

        var updated = await Db.BranchSchedules.FirstAsync(s => s.BranchId == 100 && s.DayOfWeek == 1);
        updated.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateScheduleDay_ValidOpenDay_PersistsHours()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSchedulesForBranchAsync(branchId: 100, startId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 1,
            IsOpen:    true,
            OpenTime:  TimeSpan.FromHours(8),
            CloseTime: TimeSpan.FromHours(20)));

        result.Success.Should().BeTrue();
        result.Data!.OpenTime.Should().Be(TimeSpan.FromHours(8));
        result.Data.CloseTime.Should().Be(TimeSpan.FromHours(20));
    }

    [Fact]
    public async Task UpdateScheduleDay_ScheduleNotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        // NO sembrar schedules → DayOfWeek=1 no existe

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateScheduleDay(new BranchScheduleUpdateDto(
            BranchId:  100,
            DayOfWeek: 1,
            IsOpen:    false,
            OpenTime:  null,
            CloseTime: null));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ScheduleNotFound);
    }

    // ── 3. GetSpecialDays ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSpecialDays_ReturnsDaysForBranch()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSpecialDayAsync(id: 100, branchId: 100);
        await SeedSpecialDayAsync(id: 101, branchId: 100,
            date: DateTime.UtcNow.Date.AddDays(14));

        SetTenant(companyId: 100);
        var result = await CreateService().GetSpecialDays(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    // ── 4. CreateSpecialDay ───────────────────────────────────────────────

    [Fact]
    public async Task CreateSpecialDay_PastDate_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().CreateSpecialDay(new BranchSpecialDayCreateDto(
            BranchId: 100,
            Date:     DateTime.UtcNow.Date.AddDays(-1), // ayer
            IsClosed: true,
            OpenTime: null,
            CloseTime: null,
            Reason:   "Test"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.SpecialDayPastDate);
    }

    [Fact]
    public async Task CreateSpecialDay_DuplicateDate_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        var futureDate = DateTime.UtcNow.Date.AddDays(7);
        await SeedSpecialDayAsync(id: 100, branchId: 100, date: futureDate);

        SetTenant(companyId: 100);
        var result = await CreateService().CreateSpecialDay(new BranchSpecialDayCreateDto(
            BranchId: 100,
            Date:     futureDate, // misma fecha ya registrada
            IsClosed: false,
            OpenTime: TimeSpan.FromHours(10),
            CloseTime: TimeSpan.FromHours(18),
            Reason:   "Duplicado"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.SpecialDayDuplicate);
    }

    [Fact]
    public async Task CreateSpecialDay_NotClosedMissingOpenTime_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().CreateSpecialDay(new BranchSpecialDayCreateDto(
            BranchId:  100,
            Date:      DateTime.UtcNow.Date.AddDays(7),
            IsClosed:  false,
            OpenTime:  null,  // falta → error
            CloseTime: TimeSpan.FromHours(18),
            Reason:    "Horario especial"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.SpecialDayHoursRequired);
    }

    [Fact]
    public async Task CreateSpecialDay_ClosedDay_PersistsWithNullHours()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var futureDate = DateTime.UtcNow.Date.AddDays(7);
        var result = await CreateService().CreateSpecialDay(new BranchSpecialDayCreateDto(
            BranchId:  100,
            Date:      futureDate,
            IsClosed:  true,
            OpenTime:  null,
            CloseTime: null,
            Reason:    "Feriado Nacional"));

        result.Success.Should().BeTrue();
        result.Data!.IsClosed.Should().BeTrue();
        result.Data.OpenTime.Should().BeNull();

        var saved = await Db.BranchSpecialDays.FirstAsync(d => d.BranchId == 100);
        saved.Reason.Should().Be("Feriado Nacional");
    }

    [Fact]
    public async Task CreateSpecialDay_OpenWithHours_PersistsCorrectly()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().CreateSpecialDay(new BranchSpecialDayCreateDto(
            BranchId:  100,
            Date:      DateTime.UtcNow.Date.AddDays(10),
            IsClosed:  false,
            OpenTime:  TimeSpan.FromHours(10),
            CloseTime: TimeSpan.FromHours(14),
            Reason:    "Horario reducido"));

        result.Success.Should().BeTrue();
        result.Data!.OpenTime.Should().Be(TimeSpan.FromHours(10));
        result.Data.CloseTime.Should().Be(TimeSpan.FromHours(14));
    }

    // ── 5. UpdateSpecialDay ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateSpecialDay_NotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var result = await CreateService().UpdateSpecialDay(new BranchSpecialDayUpdateDto(
            Id: 9999, BranchId: 100, IsClosed: true, OpenTime: null, CloseTime: null, Reason: "X"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.SpecialDayNotFound);
    }

    [Fact]
    public async Task UpdateSpecialDay_ValidData_UpdatesReason()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSpecialDayAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateSpecialDay(new BranchSpecialDayUpdateDto(
            Id: 100, BranchId: 100, IsClosed: true, OpenTime: null, CloseTime: null,
            Reason: "Razón actualizada"));

        result.Success.Should().BeTrue();
        result.Data!.Reason.Should().Be("Razón actualizada");
    }

    // ── 6. DeleteSpecialDay ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteSpecialDay_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().DeleteSpecialDay(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task DeleteSpecialDay_ValidDay_RemovesPhysically()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedSpecialDayAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().DeleteSpecialDay(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.BranchSpecialDays.FindAsync(100);
        deleted.Should().BeNull(); // eliminación física
    }
}
