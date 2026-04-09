using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests del BulkImportService.
///
/// Cubre:
///   1. Generación de templates (CSV headers)
///   2. Importación de categorías (validaciones + CRUD + multi-tenant)
///   3. Importación de productos (validaciones + CRUD + ZIP warnings)
///   4. Importación de BranchProducts (validaciones + reactivación + skip)
///   5. Lock de concurrencia por tenant
///
/// CONVENCIÓN DE IDs:
///   - CompanyId 100 = "nuestra empresa"
///   - CompanyId 101 = "empresa ajena"
///   - Entidades >= 100 para evitar colisión con seed de Company 1
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "BulkImport")]
public sealed class BulkImportServiceTests : ServiceTestBase
{
    private BulkImportService CreateService(ImportLockService? lockService = null)
        => new(Db, TenantService, FileStorage, CacheService, lockService ?? new ImportLockService());

    // ── Seed helpers ──────────────────────────────────────────────────────

    private async Task SeedCompanyLanguageAsync(int companyId, string code, bool isDefault, int id)
    {
        Db.CompanyLanguages.Add(new AppCore.Domain.Entities.CompanyLanguage
        {
            Id           = id,
            CompanyId    = companyId,
            LanguageCode = code,
            IsDefault    = isDefault,
        });
        await Db.SaveChangesAsync();
    }

    private async Task<BranchProduct> SeedDeletedBranchProductAsync(
        int id, int branchId, int productId, int categoryId)
    {
        var bp = new BranchProduct
        {
            Id                  = id,
            BranchId            = branchId,
            ProductId           = productId,
            CategoryId          = categoryId,
            Price               = 1000m,
            IsDeleted           = true,
            DisplayOrder        = 0,
            ImageObjectFit      = "cover",
            ImageObjectPosition = "50% 50%",
        };
        Db.BranchProducts.Add(bp);
        await Db.SaveChangesAsync();
        return bp;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  1. TEMPLATES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoryTemplate_WithLanguages_ReturnsHeadersWithDefaultFirst()
    {
        // Arrange: company 100 con es (default) y en
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(100, "en", isDefault: false, id: 101);

        SetTenant(companyId: 100);

        // Act
        var result = await CreateService().GetCategoryTemplate();

        // Assert: headers deben ser nombre_es, nombre_en, visible (default primero)
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Headers.Should().ContainInOrder("nombre_es", "nombre_en", "visible");
        result.Data.Headers.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCategoryTemplate_WithNoLanguages_ReturnsValidationError()
    {
        // Arrange: company sin idiomas configurados
        await SeedCompanyAsync(id: 100);
        SetTenant(companyId: 100);

        // Act
        var result = await CreateService().GetCategoryTemplate();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorKey.Should().Be(ErrorKeys.BulkImportNoLanguages);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  2. IMPORT CATEGORIES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportCategories_ValidItems_CreatesAllCategories()
    {
        // Arrange
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);

        var dto = new BulkCategoryImportDto
        {
            Items =
            [
                new BulkCategoryImportItemDto { Names = new() { ["es"] = "Entradas" }, IsVisible = true },
                new BulkCategoryImportItemDto { Names = new() { ["es"] = "Platos Fuertes" }, IsVisible = true },
                new BulkCategoryImportItemDto { Names = new() { ["es"] = "Postres" }, IsVisible = false },
            ]
        };

        // Act
        var result = await CreateService().ImportCategories(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.CreatedCount.Should().Be(3);

        // Filtrar solo las traducciones de company 100 (la BD tiene seed data de company 1)
        var catIds = await Db.Categories
            .Where(c => c.CompanyId == 100)
            .Select(c => c.Id)
            .ToListAsync();
        var translations = await Db.CategoryTranslations
            .Where(t => catIds.Contains(t.CategoryId))
            .ToListAsync();
        translations.Should().HaveCount(3);
        translations.Should().Contain(t => t.Name == "Entradas");
        translations.Should().Contain(t => t.Name == "Platos Fuertes");
        translations.Should().Contain(t => t.Name == "Postres");
    }

    [Fact]
    public async Task ImportCategories_DuplicateNameInFile_ReturnsValidationError()
    {
        // Arrange: dos items con el mismo nombre en el mismo batch
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        SetTenant(companyId: 100);

        var dto = new BulkCategoryImportDto
        {
            Items =
            [
                new BulkCategoryImportItemDto { Names = new() { ["es"] = "Bebidas" }, IsVisible = true },
                new BulkCategoryImportItemDto { Names = new() { ["es"] = "Bebidas" }, IsVisible = true },
            ]
        };

        // Act
        var result = await CreateService().ImportCategories(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().NotBeNull();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportDuplicateRow);
    }

    [Fact]
    public async Task ImportCategories_DuplicateNameInDb_ReturnsSuccessWithWarning()
    {
        // Arrange: ya existe "Entradas" en BD
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");

        SetTenant(companyId: 100);

        var dto = new BulkCategoryImportDto
        {
            Items = [new BulkCategoryImportItemDto { Names = new() { ["es"] = "Entradas" }, IsVisible = true }]
        };

        // Act: debería importar con advertencia (no error)
        var result = await CreateService().ImportCategories(dto);

        // Assert: success con warning de duplicado en BD
        result.Success.Should().BeTrue();
        result.Data!.Warnings.Should().Contain(w => w.WarningKey == ErrorKeys.BulkImportDuplicateInDb);
    }

    [Fact]
    public async Task ImportCategories_MissingDefaultLang_ReturnsValidationError()
    {
        // Arrange: idioma default = "es", pero el item solo tiene "en"
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(100, "en", isDefault: false, id: 101);
        SetTenant(companyId: 100);

        var dto = new BulkCategoryImportDto
        {
            Items = [new BulkCategoryImportItemDto { Names = new() { ["en"] = "Starters" }, IsVisible = true }]
        };

        // Act
        var result = await CreateService().ImportCategories(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportDefaultLangRequired);
    }

    [Fact]
    public async Task ImportCategories_ExceedsMaxRows_ReturnsValidationError()
    {
        // Arrange: 501 items (máximo es 500)
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        SetTenant(companyId: 100);

        var items = Enumerable.Range(1, 501)
            .Select(i => new BulkCategoryImportItemDto
            {
                Names = new() { ["es"] = $"Categoría {i}" },
                IsVisible = true,
            })
            .ToList();

        var dto = new BulkCategoryImportDto { Items = items };

        // Act
        var result = await CreateService().ImportCategories(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorKey.Should().Be(ErrorKeys.BulkImportValidationFailed);
    }

    [Fact]
    public async Task ImportCategories_MultiTenant_OnlyAffectsCurrentCompany()
    {
        // Arrange: company 100 y 101 ambas con idioma "es"
        await SeedCompanyAsync(id: 100, slug: "company-one");
        await SeedCompanyAsync(id: 101, slug: "company-two");
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCompanyLanguageAsync(101, "es", isDefault: true, id: 101);

        // Company 101 ya tiene una categoría
        await SeedCategoryAsync(id: 101, companyId: 101, translationName: "Cat Empresa 101", langCode: "es");

        // Act: importar como company 100
        SetTenant(companyId: 100);
        var dto = new BulkCategoryImportDto
        {
            Items = [new BulkCategoryImportItemDto { Names = new() { ["es"] = "Cat Empresa 100" }, IsVisible = true }]
        };

        var result = await CreateService().ImportCategories(dto);

        // Assert: company 101 sigue teniendo exactamente 1 categoría sin cambios
        result.Success.Should().BeTrue();

        var cats101 = await Db.Categories
            .Where(c => c.CompanyId == 101)
            .ToListAsync();
        cats101.Should().HaveCount(1);
        cats101.First().Translations.Should().Contain(t => t.Name == "Cat Empresa 101");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  3. IMPORT PRODUCTS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportProducts_ValidItems_CreatesProductsWithTranslations()
    {
        // Arrange
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        SetTenant(companyId: 100);

        var dto = new BulkProductImportDto
        {
            Items =
            [
                new BulkProductImportItemDto
                {
                    CategoryName      = "Entradas",
                    Names             = new() { ["es"] = "Ceviche" },
                    ShortDescriptions = new() { ["es"] = "Clásico peruano" },
                    LongDescriptions  = new() { ["es"] = "Preparado con limón fresco" },
                    ImageFilename     = null,
                },
                new BulkProductImportItemDto
                {
                    CategoryName      = "Entradas",
                    Names             = new() { ["es"] = "Tiradito" },
                    ShortDescriptions = new(),
                    LongDescriptions  = new(),
                    ImageFilename     = null,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.CreatedCount.Should().Be(2);

        // Filtrar solo las traducciones de company 100 (la BD tiene seed data de company 1)
        var prodIds = await Db.Products
            .Where(p => p.CompanyId == 100)
            .Select(p => p.Id)
            .ToListAsync();
        var translations = await Db.ProductTranslations
            .Where(t => prodIds.Contains(t.ProductId))
            .ToListAsync();
        translations.Should().HaveCount(2);
        translations.Should().Contain(t => t.Name == "Ceviche");
        translations.Should().Contain(t => t.Name == "Tiradito");
    }

    [Fact]
    public async Task ImportProducts_CategoryNotFound_ReturnsValidationError()
    {
        // Arrange: no existe la categoría "Nonexistent"
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        SetTenant(companyId: 100);

        var dto = new BulkProductImportDto
        {
            Items =
            [
                new BulkProductImportItemDto
                {
                    CategoryName  = "Nonexistent",
                    Names         = new() { ["es"] = "Sopa del día" },
                    ShortDescriptions = new(),
                    LongDescriptions  = new(),
                },
            ]
        };

        // Act
        var result = await CreateService().ImportProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportCategoryNotFound);
    }

    [Fact]
    public async Task ImportProducts_DuplicateNameInBatch_ReturnsValidationError()
    {
        // Arrange: dos productos con el mismo nombre en el batch
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        SetTenant(companyId: 100);

        var dto = new BulkProductImportDto
        {
            Items =
            [
                new BulkProductImportItemDto
                {
                    CategoryName  = "Entradas",
                    Names         = new() { ["es"] = "Ceviche" },
                    ShortDescriptions = new(),
                    LongDescriptions  = new(),
                },
                new BulkProductImportItemDto
                {
                    CategoryName  = "Entradas",
                    Names         = new() { ["es"] = "Ceviche" }, // duplicado
                    ShortDescriptions = new(),
                    LongDescriptions  = new(),
                },
            ]
        };

        // Act
        var result = await CreateService().ImportProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportDuplicateRow);
    }

    [Fact]
    public async Task ImportProducts_NoZip_ReturnsSuccessWithImageWarnings()
    {
        // Arrange: item con imagen pero sin ZIP
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Bebidas", langCode: "es");
        SetTenant(companyId: 100);

        var dto = new BulkProductImportDto
        {
            Items =
            [
                new BulkProductImportItemDto
                {
                    CategoryName  = "Bebidas",
                    Names         = new() { ["es"] = "Limonada" },
                    ShortDescriptions = new(),
                    LongDescriptions  = new(),
                    ImageFilename = "limonada.jpg", // referencia a imagen pero no hay ZIP
                },
            ]
        };

        // Act: sin pasar ZIP
        var result = await CreateService().ImportProducts(dto, imagesZip: null);

        // Assert: success pero con advertencia de imagen
        result.Success.Should().BeTrue();
        result.Data!.Warnings.Should().Contain(w => w.WarningKey == ErrorKeys.BulkImportImageNotFound);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  4. IMPORT BRANCH PRODUCTS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportBranchProducts_NewItems_CreatesAllBranchProducts()
    {
        // Arrange
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 1500m,
                    OfferPrice   = null,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.CreatedCount.Should().Be(1);

        // Filtrar solo los BranchProducts de la branch 100 (la BD tiene seed data de company 1)
        var bps = await Db.BranchProducts
            .Where(bp => bp.BranchId == 100)
            .ToListAsync();
        bps.Should().HaveCount(1);
        bps.First().Price.Should().Be(1500m);
    }

    [Fact]
    public async Task ImportBranchProducts_SoftDeletedItem_ReactivatesWithNewPrice()
    {
        // Arrange: BranchProduct soft-deleted
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        await SeedDeletedBranchProductAsync(id: 200, branchId: 100, productId: 100, categoryId: 100);

        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 2500m, // nuevo precio
                    OfferPrice   = null,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert: reactivado, no creado de nuevo
        result.Success.Should().BeTrue();
        result.Data!.ReactivatedCount.Should().Be(1);
        result.Data.CreatedCount.Should().Be(0);

        var bp = await Db.BranchProducts
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == 200);
        bp.IsDeleted.Should().BeFalse();
        bp.Price.Should().Be(2500m);
    }

    [Fact]
    public async Task ImportBranchProducts_AlreadyActiveItem_SkipsAndReturnsWarning()
    {
        // Arrange: BranchProduct activo
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        await SeedBranchProductAsync(id: 200, branchId: 100, productId: 100, categoryId: 100, price: 1000m);

        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 3000m,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert: no se inserta duplicado, se reporta como skipped
        result.Success.Should().BeTrue();
        result.Data!.SkippedCount.Should().Be(1);
        result.Data.CreatedCount.Should().Be(0);
        result.Data.Warnings.Should().Contain(w => w.WarningKey == ErrorKeys.BulkImportBranchProductExists);

        // Solo debe existir el BP original para branch 100 (sin duplicado)
        var allBPs = await Db.BranchProducts
            .IgnoreQueryFilters()
            .Where(bp => bp.BranchId == 100)
            .ToListAsync();
        allBPs.Should().HaveCount(1);
    }

    [Fact]
    public async Task ImportBranchProducts_ProductNotFound_ReturnsValidationError()
    {
        // Arrange
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Producto Inexistente",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 1000m,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportProductNotFound);
    }

    [Fact]
    public async Task ImportBranchProducts_BranchNotFound_ReturnsValidationError()
    {
        // Arrange
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Sucursal Inexistente",
                    CategoryName = "Entradas",
                    Price        = 1000m,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportBranchNotFound);
    }

    [Fact]
    public async Task ImportBranchProducts_InvalidPrice_ReturnsValidationError()
    {
        // Arrange: precio negativo
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = -1m, // inválido
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportInvalidPrice);
    }

    [Fact]
    public async Task ImportBranchProducts_OfferPriceGreaterThanPrice_ReturnsValidationError()
    {
        // Arrange: precio_oferta >= precio (precio=1000, oferta=1500)
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 1000m,
                    OfferPrice   = 1500m, // mayor que el precio base
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportInvalidPrice);
    }

    [Fact]
    public async Task ImportBranchProducts_DuplicateComboInBatch_ReturnsValidationError()
    {
        // Arrange: misma combinación producto+sucursal dos veces en el mismo batch
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "branch-100");
        await SeedCategoryAsync(id: 100, companyId: 100, translationName: "Entradas", langCode: "es");
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Ceviche");
        SetTenant(companyId: 100);

        var dto = new BulkBranchProductImportDto
        {
            Items =
            [
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche",
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 1000m,
                    IsVisible    = true,
                },
                new BulkBranchProductImportItemDto
                {
                    ProductName  = "Ceviche", // misma combinación
                    BranchName   = "Branch 100",
                    CategoryName = "Entradas",
                    Price        = 2000m,
                    IsVisible    = true,
                },
            ]
        };

        // Act
        var result = await CreateService().ImportBranchProducts(dto, imagesZip: null);

        // Assert
        result.Success.Should().BeFalse();
        result.Data!.Errors.Should().Contain(e => e.ErrorKey == ErrorKeys.BulkImportDuplicateRow);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  5. LOCK DE CONCURRENCIA
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportCategories_ConcurrentImport_ReturnsConflict()
    {
        // Arrange: bloquear el semáforo manualmente antes de intentar importar
        await SeedCompanyAsync(id: 100);
        await SeedCompanyLanguageAsync(100, "es", isDefault: true, id: 100);
        SetTenant(companyId: 100);

        // Crear un ImportLockService compartido y adquirir el lock manualmente
        var sharedLockService = new ImportLockService();
        var semaphore = sharedLockService.GetLock(100);
        await semaphore.WaitAsync(); // ocupar el semáforo

        try
        {
            // Intentar importar usando el mismo lock service (ya bloqueado)
            var service = CreateService(sharedLockService);
            var dto = new BulkCategoryImportDto
            {
                Items = [new BulkCategoryImportItemDto { Names = new() { ["es"] = "Test" }, IsVisible = true }]
            };

            // Act
            var result = await service.ImportCategories(dto);

            // Assert: debe reportar conflicto
            result.Success.Should().BeFalse();
            result.ErrorKey.Should().Be(ErrorKeys.BulkImportAlreadyInProgress);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
