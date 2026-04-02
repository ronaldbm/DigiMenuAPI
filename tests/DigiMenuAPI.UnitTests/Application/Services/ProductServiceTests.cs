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
/// Tests de ProductService — catálogo global de productos de una Company.
///
/// IDs reservados por seed:
///   Category IDs 1-5  → Company 1
///   Product  IDs 1-11 → Company 1
///   Tag      IDs 1-6  → Company 1
///   IDs de test: 100+
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class ProductServiceTests : ServiceTestBase
{
    private ProductService CreateService()
        => new(Db, Mapper, TenantService, FileStorage, CacheService);

    // ── 1. Aislamiento Multi-Tenant ───────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentTenantProducts()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 101, companyId: 101);

        Db.Products.AddRange(
            new ProductBuilder().WithId(100).WithCompanyId(100).WithCategoryId(100).Build(),
            new ProductBuilder().WithId(101).WithCompanyId(101).WithCategoryId(101).Build(),
            new ProductBuilder().WithId(102).WithCompanyId(100).WithCategoryId(100).Build()
        );
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Select(p => p.CategoryId).Should().AllBeEquivalentTo(100);
    }

    [Fact]
    public async Task GetById_ProductBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 101, companyId: 101);
        Db.Products.Add(new ProductBuilder().WithId(110).WithCompanyId(101).WithCategoryId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(110);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ProductNotFound);
    }

    [Fact]
    public async Task Delete_ProductBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 101, companyId: 101);
        Db.Products.Add(new ProductBuilder().WithId(120).WithCompanyId(101).WithCategoryId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(120);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 2. Create — validaciones ──────────────────────────────────────────

    [Fact]
    public async Task Create_CategoryNotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var dto = new ProductCreateDto
        {
            CategoryId   = 999, // no existe para company 100
            Translations = [new ProductTranslationInput { LanguageCode = "es", Name = "Producto" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Create_CategoryBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 101, companyId: 101); // categoría de otra empresa

        SetTenant(companyId: 100);
        var dto = new ProductCreateDto
        {
            CategoryId   = 101,
            Translations = [new ProductTranslationInput { LanguageCode = "es", Name = "Producto" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Create_ValidData_PersistsProductWithTranslations()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var dto = new ProductCreateDto
        {
            CategoryId   = 100,
            Translations =
            [
                new ProductTranslationInput { LanguageCode = "es", Name = "Hamburguesa", ShortDescription = "Clásica" },
                new ProductTranslationInput { LanguageCode = "en", Name = "Burger", ShortDescription = "Classic" },
            ]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.CompanyId.Should().Be(100);
        result.Data.Translations.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_SkipsTranslationsWithEmptyName()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var dto = new ProductCreateDto
        {
            CategoryId   = 100,
            Translations =
            [
                new ProductTranslationInput { LanguageCode = "es", Name = "Válido"  },
                new ProductTranslationInput { LanguageCode = "en", Name = ""        },
                new ProductTranslationInput { LanguageCode = "fr", Name = "   "     },
            ]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.Translations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_DefaultsImageObjectFit_ToCoverWhenNull()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var dto = new ProductCreateDto
        {
            CategoryId      = 100,
            ImageObjectFit  = null, // no se especifica → debe defaultear a "cover"
            Translations    = [new ProductTranslationInput { LanguageCode = "es", Name = "Plato" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();

        var saved = await Db.Products.FirstAsync(p => p.Id == result.Data!.Id);
        saved.ImageObjectFit.Should().Be("cover");
        saved.ImageObjectPosition.Should().Be("50% 50%");
    }

    [Fact]
    public async Task Create_OnlyLinksTagsFromOwnCompany()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedTagAsync(id: 100, companyId: 100);
        await SeedTagAsync(id: 101, companyId: 101); // tag de otra empresa

        SetTenant(companyId: 100);
        var dto = new ProductCreateDto
        {
            CategoryId   = 100,
            TagIds       = [100, 101], // 101 no pertenece a company 100
            Translations = [new ProductTranslationInput { LanguageCode = "es", Name = "Producto" }]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();

        var product = await Db.Products.Include(p => p.Tags)
            .FirstAsync(p => p.Id == result.Data!.Id);
        product.Tags.Should().HaveCount(1);
        product.Tags.First().Id.Should().Be(100);
    }

    // ── 3. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ProductNotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var result = await CreateService().Update(new ProductUpdateDto
        {
            Id           = 999,
            CategoryId   = 100,
            Translations = []
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ProductNotFound);
    }

    [Fact]
    public async Task Update_CategoryDoesNotBelongToCompany_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 101, companyId: 101);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new ProductUpdateDto
        {
            Id           = 100,
            CategoryId   = 101, // pertenece a company 101
            Translations = []
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CategoryNotFound);
    }

    [Fact]
    public async Task Update_UpdatesTranslations_UpsertPattern()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100, name: "Original");

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new ProductUpdateDto
        {
            Id           = 100,
            CategoryId   = 100,
            Translations =
            [
                new ProductTranslationInput { LanguageCode = "es", Name = "Actualizado", ShortDescription = "Desc" },
                new ProductTranslationInput { LanguageCode = "en", Name = "Updated"                                },
            ]
        });

        result.Success.Should().BeTrue();

        var translations = await Db.ProductTranslations
            .Where(t => t.ProductId == 100).ToListAsync();
        translations.Should().HaveCount(2);
        translations.Should().Contain(t => t.LanguageCode == "es" && t.Name == "Actualizado");
        translations.Should().Contain(t => t.LanguageCode == "en" && t.Name == "Updated");
    }

    // ── 4. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingProduct_SetsSoftDelete()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == 100);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NonexistentId_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Delete(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.ProductNotFound);
    }

    // ── 5. Paginación ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Paginated_ReturnsCorrectPage()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);

        for (int i = 0; i < 5; i++)
            await SeedProductAsync(id: 100 + i, companyId: 100, categoryId: 100, name: $"Producto {i}");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(page: 2, pageSize: 2);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Page.Should().Be(2);
        result.Data.TotalPages.Should().Be(3); // ceil(5/2)
    }

    // ── 6. Cache eviction ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        await CreateService().Create(new ProductCreateDto
        {
            CategoryId   = 100,
            Translations = [new ProductTranslationInput { LanguageCode = "es", Name = "Producto" }]
        });

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100);
        await CreateService().Delete(100);

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100);
        await CreateService().Update(new ProductUpdateDto
        {
            Id           = 100,
            CategoryId   = 100,
            Translations = [new ProductTranslationInput { LanguageCode = "es", Name = "Nuevo" }]
        });

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }
}
