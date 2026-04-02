using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using DigiMenuAPI.UnitTests.TestInfrastructure.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

using NameTranslationInput = DigiMenuAPI.Application.DTOs.Create.NameTranslationInput;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de TagService — etiquetas del catálogo global de una Company.
///
/// IDs reservados por seed:
///   Tag IDs 1-6 → Company 1
///   Tags de test: IDs >= 100
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class TagServiceTests : ServiceTestBase
{
    private TagService CreateService()
        => new(Db, Mapper, TenantService, CacheService);

    // ── 1. Aislamiento Multi-Tenant ───────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentTenantTags()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);

        Db.Tags.AddRange(
            new TagBuilder().WithId(100).WithCompanyId(100).WithTranslation("es", "Vegano").Build(),
            new TagBuilder().WithId(101).WithCompanyId(101).WithTranslation("es", "Picante").Build(),
            new TagBuilder().WithId(102).WithCompanyId(100).WithTranslation("es", "Sin gluten").Build()
        );
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(t => t.CompanyId).Should().AllBeEquivalentTo(100);
    }

    [Fact]
    public async Task GetById_TagBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        Db.Tags.Add(new TagBuilder().WithId(110).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(110);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.TagNotFound);
    }

    [Fact]
    public async Task Delete_TagBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        Db.Tags.Add(new TagBuilder().WithId(115).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(115);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Update_TagBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        Db.Tags.Add(new TagBuilder().WithId(120).WithCompanyId(101).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new TagUpdateDto
        {
            Id           = 120,
            Color        = "#ff0000",
            Translations = []
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 2. CRUD Correcto ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTags()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingTag_ReturnsOkWithData()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100).WithColor("#4CAF50")
                .WithTranslation("es", "Vegano")
                .WithTranslation("en", "Vegan")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(100);

        result.Success.Should().BeTrue();
        result.Data!.Color.Should().Be("#4CAF50");
        result.Data.Translations.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_AssignsTenantCompanyId_AndPersistsTranslations()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var dto = new TagCreateDto
        {
            Color        = "#ff5722",
            Translations =
            [
                new NameTranslationInput { LanguageCode = "es", Name = "Picante" },
                new NameTranslationInput { LanguageCode = "en", Name = "Spicy"   },
            ]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.CompanyId.Should().Be(100);
        result.Data.Translations.Should().HaveCount(2);
        result.Data.Color.Should().Be("#ff5722");
    }

    [Fact]
    public async Task Create_SkipsTranslationsWithEmptyName()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var dto = new TagCreateDto
        {
            Color        = "#ffffff",
            Translations =
            [
                new NameTranslationInput { LanguageCode = "es", Name = "Válido" },
                new NameTranslationInput { LanguageCode = "en", Name = "   " },
                new NameTranslationInput { LanguageCode = "fr", Name = "" },
            ]
        };

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.Translations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_ExistingTag_SetsSoftDelete()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(new TagBuilder().WithId(100).WithCompanyId(100).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.Tags.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == 100);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_SoftDeletedTag_NotReturnedByGetAll()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(new TagBuilder().WithId(100).WithCompanyId(100).Deleted().Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonexistentId_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Delete(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.TagNotFound);
    }

    [Fact]
    public async Task Update_UpdatesColorAndTranslations()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100).WithColor("#ffffff")
                .WithTranslation("es", "Viejo")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new TagUpdateDto
        {
            Id    = 100,
            Color = "#111111",
            Translations =
            [
                new NameTranslationInput { LanguageCode = "es", Name = "Actualizado" },
                new NameTranslationInput { LanguageCode = "en", Name = "Updated"     },
            ]
        });

        result.Success.Should().BeTrue();

        var tag = await Db.Tags.Include(t => t.Translations)
            .FirstAsync(t => t.Id == 100);
        tag.Color.Should().Be("#111111");
        tag.Translations.Should().HaveCount(2);
        tag.Translations.Should().Contain(t => t.LanguageCode == "es" && t.Name == "Actualizado");
    }

    [Fact]
    public async Task Update_RemovesDeletedTranslations()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Español")
                .WithTranslation("en", "English")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        // Solo enviamos "es" → "en" debe eliminarse
        await CreateService().Update(new TagUpdateDto
        {
            Id           = 100,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Solo Español" }]
        });

        var translations = await Db.TagTranslations
            .Where(t => t.TagId == 100).ToListAsync();
        translations.Should().HaveCount(1);
        translations.Should().NotContain(t => t.LanguageCode == "en");
    }

    // ── 3. Resolución de idioma ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithLangParam_ReturnsCorrectTranslation()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Vegano")
                .WithTranslation("en", "Vegan")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(lang: "en");

        result.Data!.First().Name.Should().Be("Vegan");
    }

    [Fact]
    public async Task GetAll_WithUnavailableLang_FallsBackToFirst()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Vegano")
                .Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(lang: "fr");

        result.Data!.First().Name.Should().Be("Vegano");
    }

    // ── 4. Cache eviction ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        await CreateService().Create(new TagCreateDto
        {
            Color        = "#ffffff",
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Tag" }]
        });

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(new TagBuilder().WithId(100).WithCompanyId(100).Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        await CreateService().Delete(100);

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_CallsCacheEviction()
    {
        await SeedCompanyAsync(100);
        Db.Tags.Add(
            new TagBuilder().WithId(100).WithCompanyId(100)
                .WithTranslation("es", "Tag").Build());
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        await CreateService().Update(new TagUpdateDto
        {
            Id           = 100,
            Translations = [new NameTranslationInput { LanguageCode = "es", Name = "Nuevo" }]
        });

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }
}
