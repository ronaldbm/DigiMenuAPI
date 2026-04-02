using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using DigiMenuAPI.UnitTests.TestInfrastructure.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de BranchProductService — activación y configuración de productos por sucursal.
///
/// Aspectos críticos:
///   1. Branch ownership: solo el tenant dueño de la branch puede operar sobre ella
///   2. Producto y Categoría deben pertenecer al mismo tenant (Company)
///   3. Un producto no puede activarse dos veces en la misma Branch
///   4. Promos activas bloquean la eliminación (sin forceDelete)
///   5. Reorder ignora IDs de BranchProducts de otras branches
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class BranchProductServiceTests : ServiceTestBase
{
    private BranchProductService CreateService()
        => new(Db, Mapper, TenantService, FileStorage, CacheService);

    // ── 1. Branch Ownership (ValidateBranchOwnershipAsync) ───────────────

    [Fact]
    public async Task GetByBranch_BranchBelongsToOtherCompany_ThrowsForbidden()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 101, companyId: 101); // branch de company 101

        SetTenant(companyId: 100, role: UserRoles.CompanyAdmin);

        var act = async () => await CreateService().GetByBranch(101);

        // ValidateBranchOwnershipAsync lanza excepción de acceso denegado
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetByBranch_OwnBranch_ReturnsProducts()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().GetByBranch(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    // ── 2. Create — validaciones de pertenencia ───────────────────────────

    [Fact]
    public async Task Create_ProductNotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var dto = new BranchProductCreateDto(
            BranchId:   100,
            ProductId:  999, // no existe
            CategoryId: 100,
            Price:      1000m,
            OfferPrice: null,
            ImageOverride: null,
            IsVisible: true);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ProductNotFound);
    }

    [Fact]
    public async Task Create_ProductBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 101, companyId: 101);
        await SeedProductAsync(id: 101, companyId: 101, categoryId: 101); // producto de otra company

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var dto = new BranchProductCreateDto(
            BranchId:   100,
            ProductId:  101, // de company 101
            CategoryId: 100,
            Price:      1000m,
            OfferPrice: null,
            ImageOverride: null,
            IsVisible: true);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ProductNotFound);
    }

    [Fact]
    public async Task Create_CategoryBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100); // categoría de company 100 para el producto
        await SeedCategoryAsync(id: 101, companyId: 101); // categoría de otra company
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var dto = new BranchProductCreateDto(
            BranchId:   100,
            ProductId:  100,
            CategoryId: 101, // de company 101
            Price:      1000m,
            OfferPrice: null,
            ImageOverride: null,
            IsVisible: true);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Create_DuplicateBranchProduct_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var dto = new BranchProductCreateDto(
            BranchId:   100,
            ProductId:  100, // ya activado
            CategoryId: 100,
            Price:      1500m,
            OfferPrice: null,
            ImageOverride: null,
            IsVisible: true);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.BranchProductAlreadyExists);
    }

    [Fact]
    public async Task Create_ValidData_DefaultsImageObjectFit()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var dto = new BranchProductCreateDto(
            BranchId:   100,
            ProductId:  100,
            CategoryId: 100,
            Price:      2500m,
            OfferPrice: null,
            ImageOverride: null,
            IsVisible: true,
            ImageObjectFit: null); // no especificado → "cover"

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();

        var saved = await Db.BranchProducts.FirstAsync(bp => bp.Id == result.Data!.Id);
        saved.ImageObjectFit.Should().Be("cover");
        saved.ImageObjectPosition.Should().Be("50% 50%");
    }

    [Fact]
    public async Task Create_CallsCacheEvictionByBranch()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        await CreateService().Create(new BranchProductCreateDto(
            BranchId: 100, ProductId: 100, CategoryId: 100,
            Price: 1000m, OfferPrice: null, ImageOverride: null, IsVisible: true));

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 3. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_OfferPriceGreaterThanPrice_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100, price: 1000m);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Update(new BranchProductUpdateDto(
            Id:         100,
            CategoryId: 100,
            Price:      1000m,
            OfferPrice: 1500m, // mayor al precio base
            ImageOverride: null,
            IsVisible: true));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.ValidationFailed);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Update(new BranchProductUpdateDto(
            Id: 999, CategoryId: 100, Price: 1000m,
            OfferPrice: null, ImageOverride: null, IsVisible: true));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchProductNotFound);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesPrice()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100, price: 1000m);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Update(new BranchProductUpdateDto(
            Id:         100,
            CategoryId: 100,
            Price:      2500m,
            OfferPrice: 2000m, // válido: menor al precio base
            ImageOverride: null,
            IsVisible: true));

        result.Success.Should().BeTrue();

        var updated = await Db.BranchProducts.FindAsync(100);
        updated!.Price.Should().Be(2500m);
        updated.OfferPrice.Should().Be(2000m);
    }

    // ── 4. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithActivePromotions_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);

        // Agregar una promoción activa vinculada al BranchProduct
        Db.BranchPromotions.Add(new DigiMenuAPI.Infrastructure.Entities.BranchPromotion
        {
            Id              = 100,
            BranchId        = 100,
            BranchProductId = 100,
            IsActive        = true,
            Title           = "Promo test",
            DisplayOrder    = 1,
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.ProductHasActivePromotions);
    }

    [Fact]
    public async Task Delete_WithActivePromotions_ForceDelete_Succeeds()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);

        Db.BranchPromotions.Add(new DigiMenuAPI.Infrastructure.Entities.BranchPromotion
        {
            Id              = 100,
            BranchId        = 100,
            BranchProductId = 100,
            IsActive        = true,
            Title           = "Promo test",
            DisplayOrder    = 1,
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Delete(100, forceDeletePromotions: true);

        result.Success.Should().BeTrue();

        // La promo también fue eliminada
        var promo = await Db.BranchPromotions.FindAsync(100);
        promo.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NoPromotions_SetsSoftDelete()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.BranchProducts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(bp => bp.Id == 100);
        deleted!.IsDeleted.Should().BeTrue();
    }

    // ── 5. ToggleVisibility ───────────────────────────────────────────────

    [Fact]
    public async Task ToggleVisibility_TogglesIsVisible()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100, isVisible: true);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().ToggleVisibility(100);

        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse(); // fue visible → ahora no visible

        var bp = await Db.BranchProducts.FindAsync(100);
        bp!.IsVisible.Should().BeFalse();
    }

    // ── 6. Reorder ────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_IgnoresIdsFromOtherBranches()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedProductAsync(id: 101, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 101, branchId: 101, productId: 101, categoryId: 100); // otra branch

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Reorder(100,
        [
            new ReorderItemDto(100, 5),
            new ReorderItemDto(101, 9), // de branch 101 → debe ignorarse
        ]);

        result.Success.Should().BeTrue();

        var bp101 = await Db.BranchProducts.FindAsync(101);
        bp101!.DisplayOrder.Should().Be(1); // no cambió
    }

    // ── 7. SetCategoryVisibility ──────────────────────────────────────────
    // NOTA: SetCategoryVisibility usa ExecuteUpdateAsync (bulk update de EF Core 7+),
    // que no es compatible con el proveedor InMemory. Este método se prueba en
    // DigiMenuAPI.IntegrationTests contra SQL Server real (Testcontainers).

    [Fact(Skip = "ExecuteUpdateAsync requires a real SQL provider — covered in IntegrationTests")]
    public async Task SetCategoryVisibility_UpdatesAllProductsInCategory()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedProductAsync(id: 101, companyId: 100, categoryId: 100);
        await SeedBranchProductAsync(id: 100, branchId: 100, productId: 100, categoryId: 100, isVisible: true);
        await SeedBranchProductAsync(id: 101, branchId: 100, productId: 101, categoryId: 100, isVisible: true);

        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().SetCategoryVisibility(
            branchId: 100,
            categoryId: 100,
            new DigiMenuAPI.Application.DTOs.Update.BranchCategoryVisibilityUpdateDto(IsVisible: false));

        result.Success.Should().BeTrue();

        var bps = await Db.BranchProducts.Where(bp => bp.BranchId == 100).ToListAsync();
        bps.Should().AllSatisfy(bp => bp.IsVisible.Should().BeFalse());
    }
}
