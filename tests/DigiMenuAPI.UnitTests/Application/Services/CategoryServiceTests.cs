using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using DigiMenuAPI.UnitTests.TestInfrastructure.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

// NameTranslationInput se usa tanto en Create como en Update para categorías
using NameTranslationInput = DigiMenuAPI.Application.DTOs.Create.NameTranslationInput;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests del CategoryService.
///
/// PATRÓN DE REFERENCIA: Esta clase es la plantilla canónica para todos
/// los demás tests de servicios. Sigue este orden:
///   1. Multi-tenant isolation  (MÁXIMA PRIORIDAD)
///   2. CRUD correcto
///   3. OperationResult error codes
///   4. Edge cases
///
/// CONVENCIÓN DE IDs:
///   - CompanyId 100 = "nuestra empresa" en tests (evita colisión con Company 1 del seed)
///   - CompanyId 101 = "empresa ajena" en tests multi-tenant
///   - Category IDs >= 100 (IDs 1-5 ya están seedeados para Company 1)
///   - Product  IDs >= 100 (IDs 1-11 ya están seedeados para Company 1)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class CategoryServiceTests : ServiceTestBase
{
    private CategoryService CreateService()
        => new(Db, Mapper, TenantService, CacheService);

    // ── 1. Aislamiento Multi-Tenant (CRÍTICO) ─────────────────────────────

    [Fact]
    public async Task GetAll_WithCategoriesInMultipleCompanies_ReturnsOnlyCurrentTenantCategories()
    {
        // Arrange: dos companies con categorías distintas
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");

        Db.Categories.AddRange(
            new CategoryBuilder().WithId(100).WithCompanyId(100).WithTranslation("es", "Cat Empresa 100").Build(),
            new CategoryBuilder().WithId(101).WithCompanyId(101).WithTranslation("es", "Cat Empresa 101").Build(),
            new CategoryBuilder().WithId(102).WithCompanyId(100).WithTranslation("es", "Cat2 Empresa 100").Build()
        );
        await Db.SaveChangesAsync();

        // Act: autenticado como company 100
        SetTenant(companyId: 100);
        var service = CreateService();
        var result  = await service.GetAll();

        // Assert: solo las categorías de company 100
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(c => c.CompanyId).Should().AllBeEquivalentTo(100);
    }

    [Fact]
    public async Task GetById_CategoryBelongsToOtherTenant_ReturnsNotFound()
    {
        // Arrange: company 101 tiene la categoría id=105
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");
        Db.Categories.Add(
            new CategoryBuilder().WithId(105).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        // Act: autenticado como company 100 intenta acceder a categoría de company 101
        SetTenant(companyId: 100);
        var service = CreateService();
        var result  = await service.GetById(105);

        // Assert: no debe encontrarla (aislamiento garantizado)
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Delete_CategoryBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");
        Db.Categories.Add(
            new CategoryBuilder().WithId(110).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var service = CreateService();
        var result  = await service.Delete(110);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Update_CategoryBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");
        Db.Categories.Add(
            new CategoryBuilder().WithId(120).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var service = CreateService();
        var result  = await service.Update(new CategoryUpdateDto
        {
            Id           = 120,
            IsVisible    = false,
            Translations = []
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Reorder_IgnoresCategoryIdsFromOtherTenant()
    {
        // Arrange: company 100 tiene cat id=100, company 101 tiene cat id=101
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");
        Db.Categories.AddRange(
            new CategoryBuilder().WithId(100).WithCompanyId(100).WithDisplayOrder(1).Build(),
            new CategoryBuilder().WithId(101).WithCompanyId(101).WithDisplayOrder(1).Build()
        );
        await Db.SaveChangesAsync();

        // Act: company 100 intenta reordenar incluyendo id=101 (de company 101)
        SetTenant(companyId: 100);
        var service = CreateService();
        var result  = await service.Reorder(
        [
            new ReorderItemDto(100, 5),
            new ReorderItemDto(101, 9),  // pertenece a otro tenant
        ]);

        // Assert: la operación no falla pero id=101 no cambia
        result.Success.Should().BeTrue();

        var cat101 = await Db.Categories.FindAsync(101);
        cat101!.DisplayOrder.Should().Be(1); // sin cambios
    }

    // ── 2. CRUD Correcto ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoCategories()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);
        var service = CreateService();

        var result = await service.GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsCategoriesOrderedByDisplayOrder()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.AddRange(
            new CategoryBuilder().WithId(100).WithCompanyId(100).WithDisplayOrder(3).Build(),
            new CategoryBuilder().WithId(101).WithCompanyId(100).WithDisplayOrder(1).Build(),
            new CategoryBuilder().WithId(102).WithCompanyId(100).WithDisplayOrder(2).Build()
        );
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Data!.Select(c => c.DisplayOrder)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetById_ExistingCategory_ReturnsOkWithData()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(
            new CategoryBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Entradas")
                .WithTranslation("en", "Starters")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(100);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Translations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_NonexistentId_ReturnsNotFound()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var result = await CreateService().GetById(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Create_AssignsDisplayOrder1_WhenNoCategoriesExist()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var dto = new CategoryCreateDto
        {
            IsVisible    = true,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Nueva" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task Create_AssignsDisplayOrderMaxPlusOne_WhenCategoriesExist()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.AddRange(
            new CategoryBuilder().WithId(100).WithCompanyId(100).WithDisplayOrder(1).Build(),
            new CategoryBuilder().WithId(101).WithCompanyId(100).WithDisplayOrder(5).Build()
        );
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var dto = new CategoryCreateDto
        {
            IsVisible    = true,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Postre" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.DisplayOrder.Should().Be(6); // max(5) + 1
    }

    [Fact]
    public async Task Create_SkipsTranslationsWithEmptyName()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var dto = new CategoryCreateDto
        {
            IsVisible    = true,
            Translations =
            [
                new NameTranslationInput { LanguageCode = "es", Name = "Válida" },
                new NameTranslationInput { LanguageCode = "en", Name = "   " },  // espacio vacío
                new NameTranslationInput { LanguageCode = "fr", Name = "" },      // vacía
            ]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.Translations.Should().HaveCount(1);
        result.Data.Translations.First().LanguageCode.Should().Be("es");
    }

    [Fact]
    public async Task Create_AssignsTenantCompanyId_NotDtoCompanyId()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var dto = new CategoryCreateDto
        {
            IsVisible    = true,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Cat" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.CompanyId.Should().Be(100);
    }

    [Fact]
    public async Task Delete_ExistingCategory_SetsSoftDelete()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(new CategoryBuilder().WithId(100).WithCompanyId(100).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        // El registro sigue en BD (soft delete) — buscar ignorando el filtro global
        var deleted = await Db.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == 100);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SoftDeletedCategory_NotReturnedByGetAll()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(new CategoryBuilder().WithId(100).WithCompanyId(100).Deleted().Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty(); // el filtro global excluye IsDeleted=true
    }

    [Fact]
    public async Task Delete_NonexistentId_ReturnsNotFound()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var result = await CreateService().Delete(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Update_UpdatesIsVisibleField()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(new CategoryBuilder().WithId(100).WithCompanyId(100).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new CategoryUpdateDto
        {
            Id           = 100,
            IsVisible    = false,
            Translations = []
        });

        result.Success.Should().BeTrue();

        var updated = await Db.Categories.FindAsync(100);
        updated!.IsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task Update_DeletesRemovedTranslations_AddsNewOnes()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(
            new CategoryBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Español")
                .WithTranslation("en", "English")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        // Actualizar: quitar "en", cambiar "es", agregar "fr"
        var result = await CreateService().Update(new CategoryUpdateDto
        {
            Id        = 100,
            IsVisible = true,
            Translations =
            [
                new NameTranslationInput { LanguageCode = "es", Name = "Español Actualizado" },
                new NameTranslationInput { LanguageCode = "fr", Name = "Français" },
            ]
        });

        result.Success.Should().BeTrue();

        var translations = await Db.CategoryTranslations
            .Where(t => t.CategoryId == 100)
            .ToListAsync();

        translations.Should().HaveCount(2);
        translations.Should().Contain(t => t.LanguageCode == "es" && t.Name == "Español Actualizado");
        translations.Should().Contain(t => t.LanguageCode == "fr" && t.Name == "Français");
        translations.Should().NotContain(t => t.LanguageCode == "en");
    }

    [Fact]
    public async Task Reorder_UpdatesDisplayOrder_ForOwnedCategories()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.AddRange(
            new CategoryBuilder().WithId(100).WithCompanyId(100).WithDisplayOrder(1).Build(),
            new CategoryBuilder().WithId(101).WithCompanyId(100).WithDisplayOrder(2).Build()
        );
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Reorder(
        [
            new ReorderItemDto(100, 99),
            new ReorderItemDto(101, 1),
        ]);

        result.Success.Should().BeTrue();

        var cat100 = await Db.Categories.FindAsync(100);
        var cat101 = await Db.Categories.FindAsync(101);
        cat100!.DisplayOrder.Should().Be(99);
        cat101!.DisplayOrder.Should().Be(1);
    }

    // ── 3. Resolución de nombre por idioma ────────────────────────────────

    [Fact]
    public async Task GetAll_WithLangParam_ReturnsTranslationForRequestedLanguage()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(
            new CategoryBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Postres")
                .WithTranslation("en", "Desserts")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(lang: "en");

        result.Data!.First().Name.Should().Be("Desserts");
    }

    [Fact]
    public async Task GetAll_WithUnavailableLang_FallsBackToFirstTranslation()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(
            new CategoryBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Postres")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(lang: "fr"); // francés no disponible

        result.Data!.First().Name.Should().Be("Postres"); // fallback al primero
    }

    // ── 4. Cache eviction se llama ────────────────────────────────────────

    [Fact]
    public async Task Create_CallsCacheEviction()
    {
        await SeedCompanyAsync(); // id=100
        SetTenant(companyId: 100);

        var dto = new CategoryCreateDto
        {
            IsVisible    = true,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Cat" }]
        };

        await CreateService().Create(dto);

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_CallsCacheEviction()
    {
        await SeedCompanyAsync(); // id=100
        Db.Categories.Add(new CategoryBuilder().WithId(100).WithCompanyId(100).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        await CreateService().Delete(100);

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }
}
