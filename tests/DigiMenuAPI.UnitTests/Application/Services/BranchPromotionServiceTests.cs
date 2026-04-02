using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de BranchPromotionService — gestión de promociones del carrusel.
///
/// Aspectos críticos:
///   1. EndDate anterior a StartDate retorna ValidationError.
///   2. EndTime anterior a StartTime en el mismo día retorna ValidationError.
///   3. Multi-tenant: ValidateBranchOwnershipAsync lanza excepción.
///   4. Reorder actualiza DisplayOrder de varias promos.
///   5. Delete limpia imagen y evicta caché.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class BranchPromotionServiceTests : ServiceTestBase
{
    private BranchPromotionService CreateService()
        => new(Db, TenantService, FileStorage, CacheService);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task<BranchPromotion> SeedPromotionAsync(
        int id           = 100,
        int branchId     = 100,
        int displayOrder = 1,
        bool isActive    = true)
    {
        var promo = new BranchPromotion
        {
            Id                  = id,
            BranchId            = branchId,
            Title               = $"Promo {id}",
            DisplayOrder        = displayOrder,
            IsActive            = isActive,
            ShowInCarousel      = true,
            PromoObjectFit      = "cover",
            PromoObjectPosition = "50% 50%",
        };
        Db.BranchPromotions.Add(promo);
        await Db.SaveChangesAsync();
        return promo;
    }

    // ── 1. GetByBranch ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBranch_ValidBranch_ReturnsPromotionsOrdered()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedPromotionAsync(id: 100, branchId: 100, displayOrder: 2);
        await SeedPromotionAsync(id: 101, branchId: 100, displayOrder: 1);

        SetTenant(companyId: 100);
        var result = await CreateService().GetByBranch(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().DisplayOrder.Should().Be(1); // orden ascendente
    }

    [Fact]
    public async Task GetByBranch_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetByBranch(110);

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
        result.ErrorKey.Should().Be(ErrorKeys.PromotionNotFound);
    }

    // ── 3. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_EndDateBeforeStartDate_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var result = await CreateService().Create(new BranchPromotionCreateDto
        {
            BranchId     = 100,
            Title        = "Promo Inválida",
            StartDate    = new DateOnly(2026, 6, 10),
            EndDate      = new DateOnly(2026, 6, 5), // antes de StartDate
            DisplayOrder = 1,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.PromotionEndBeforeStart);
    }

    [Fact]
    public async Task Create_SameDayEndTimeBeforeStartTime_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var sameDate = new DateOnly(2026, 6, 10);
        var result = await CreateService().Create(new BranchPromotionCreateDto
        {
            BranchId     = 100,
            Title        = "Promo Horario Inválido",
            StartDate    = sameDate,
            EndDate      = sameDate,
            StartTime    = new TimeOnly(20, 0),
            EndTime      = new TimeOnly(18, 0), // anterior a StartTime → error
            DisplayOrder = 1,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.PromotionEndBeforeStart);
    }

    [Fact]
    public async Task Create_ValidPromotion_PersistsAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var result = await CreateService().Create(new BranchPromotionCreateDto
        {
            BranchId      = 100,
            Title         = "2x1 en Hamburguesas",
            Label         = "2x1",
            StartDate     = new DateOnly(2026, 7, 1),
            EndDate       = new DateOnly(2026, 7, 31),
            ShowInCarousel = true,
            DisplayOrder  = 1,
        });

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("2x1 en Hamburguesas");
        result.Data.Label.Should().Be("2x1");
        result.Data.IsActive.Should().BeTrue();

        var saved = await Db.BranchPromotions.FirstAsync(p => p.Title == "2x1 en Hamburguesas");
        saved.BranchId.Should().Be(100);

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().Create(new BranchPromotionCreateDto
        {
            BranchId     = 110,
            Title        = "Hack",
            DisplayOrder = 1,
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 4. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchPromotionUpdateDto
        {
            Id           = 9999,
            BranchId     = 100,
            Title        = "X",
            DisplayOrder = 1,
            IsActive     = true,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesTitleAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedPromotionAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchPromotionUpdateDto
        {
            Id           = 100,
            BranchId     = 100,
            Title        = "Título Actualizado",
            Label        = "NUEVO",
            DisplayOrder = 2,
            IsActive     = true,
            ShowInCarousel = true,
        });

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Título Actualizado");

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 5. Reorder ────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_UpdatesDisplayOrderForMultiplePromos()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedPromotionAsync(id: 100, branchId: 100, displayOrder: 1);
        await SeedPromotionAsync(id: 101, branchId: 100, displayOrder: 2);

        SetTenant(companyId: 100);
        var result = await CreateService().Reorder(100, new List<ReorderItemDto>
        {
            new(100, 2), // mover la promo 100 al orden 2
            new(101, 1), // mover la promo 101 al orden 1
        });

        result.Success.Should().BeTrue();

        var promo100 = await Db.BranchPromotions.FindAsync(100);
        promo100!.DisplayOrder.Should().Be(2);

        var promo101 = await Db.BranchPromotions.FindAsync(101);
        promo101!.DisplayOrder.Should().Be(1);

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 6. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Delete(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Delete_ValidPromotion_RemovesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedPromotionAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.BranchPromotions.FindAsync(100);
        deleted.Should().BeNull();

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_PromotionWithImage_DeletesFileAndRecord()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        var promo = await SeedPromotionAsync(id: 100, branchId: 100);
        promo.PromoImageUrl = "promotions/promo-100.jpg";
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        // Verifica que se intentó borrar el archivo
        FileStorage.Received(1).DeleteFile("promotions/promo-100.jpg", "promotions");
    }
}
