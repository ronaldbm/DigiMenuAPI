using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de BranchEventService — gestión de eventos por Branch.
///
/// Aspectos críticos:
///   1. Eventos no se pueden crear con fecha pasada.
///   2. EndTime sin StartTime retorna ValidationError.
///   3. Multi-tenant: ValidateBranchOwnershipAsync lanza excepción.
///   4. Delete purga la imagen y evicta la caché.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class BranchEventServiceTests : ServiceTestBase
{
    private BranchEventService CreateService()
        => new(Db, TenantService, FileStorage, CacheService);

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<BranchEvent> SeedEventAsync(
        int id       = 100,
        int branchId = 100,
        bool isActive = true,
        DateTime? eventDate = null,
        bool showModal = false)
    {
        var today  = DateTime.UtcNow.Date;
        var date   = eventDate ?? today.AddDays(7);
        var ev = new BranchEvent
        {
            Id                   = id,
            BranchId             = branchId,
            Title                = $"Evento {id}",
            EventDate            = date,
            EndDate              = date,
            IsActive             = isActive,
            ShowPromotionalModal = showModal,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        };
        Db.BranchEvents.Add(ev);
        await Db.SaveChangesAsync();
        return ev;
    }

    // ── 1. GetEvents ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_ValidBranch_ReturnsEvents()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedEventAsync(id: 100, branchId: 100);
        await SeedEventAsync(id: 101, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetEvents(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvents_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetEvents(110);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 2. GetById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetById(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.EventNotFound);
    }

    [Fact]
    public async Task GetById_ValidEvent_ReturnsDto()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedEventAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(100);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(100);
    }

    // ── 3. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_PastDate_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new BranchEventCreateDto
        {
            BranchId  = 100,
            Title     = "Evento pasado",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)), // ayer
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.EventPastDate);
    }

    [Fact]
    public async Task Create_EndTimeWithoutStartTime_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new BranchEventCreateDto
        {
            BranchId  = 100,
            Title     = "Evento inválido",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            StartTime = null,
            EndTime   = TimeSpan.FromHours(22), // EndTime sin StartTime
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.EventStartRequiredWithEnd);
    }

    [Fact]
    public async Task Create_ValidEvent_PersistsAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7));
        var result = await CreateService().Create(new BranchEventCreateDto
        {
            BranchId             = 100,
            Title                = "Gran Inauguración",
            EventDate            = futureDate,
            ShowPromotionalModal = true,
        });

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Gran Inauguración");
        result.Data.IsActive.Should().BeTrue();

        var saved = await Db.BranchEvents.FirstAsync(e => e.Title == "Gran Inauguración");
        saved.BranchId.Should().Be(100);

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_MidnightEvent_EndDateIsNextDay()
    {
        // Evento que cruza medianoche: StartTime=20:00, EndTime=02:00 → EndDate = EventDate + 1
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5));
        var result = await CreateService().Create(new BranchEventCreateDto
        {
            BranchId  = 100,
            Title     = "Noche de Gala",
            EventDate = futureDate,
            StartTime = TimeSpan.FromHours(20),  // 20:00
            EndTime   = TimeSpan.FromHours(2),   // 02:00 → cruce medianoche
        });

        result.Success.Should().BeTrue();

        // EndDate debe ser el día siguiente
        var saved = await Db.BranchEvents.FirstAsync(e => e.Title == "Noche de Gala");
        saved.EndDate.Date.Should().Be(futureDate.ToDateTime(TimeOnly.MinValue).AddDays(1).Date);
    }

    // ── 4. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_EventNotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchEventUpdateDto
        {
            Id        = 9999,
            BranchId  = 100,
            Title     = "X",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            IsActive  = true,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Update_PastDate_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedEventAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchEventUpdateDto
        {
            Id        = 100,
            BranchId  = 100,
            Title     = "Actualizado",
            EventDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)), // pasado
            IsActive  = true,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.EventPastDate);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesTitleAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedEventAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));
        var result = await CreateService().Update(new BranchEventUpdateDto
        {
            Id        = 100,
            BranchId  = 100,
            Title     = "Nombre Actualizado",
            EventDate = futureDate,
            IsActive  = true,
        });

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Nombre Actualizado");

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 5. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_EventNotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Delete(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Delete_ValidEvent_RemovesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedEventAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.BranchEvents.FindAsync(100);
        deleted.Should().BeNull();

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }
}
