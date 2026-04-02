using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de CompanyLanguageService — gestión de idiomas habilitados por empresa.
///
/// Datos seedeados:
///   - SupportedLanguages: es, en, pt, fr (IDs de cadena — no int)
///   - CompanyLanguages: Company 1, idioma "es", IsDefault = true
///
/// Aspectos críticos:
///   1. AddLanguage falla si el idioma no está en SupportedLanguages.
///   2. Primer idioma agregado se convierte en default automáticamente.
///   3. RemoveLanguage falla si es el único idioma o es el default.
///   4. SetDefault intercambia el idioma por defecto.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CompanyLanguageServiceTests : ServiceTestBase
{
    private CompanyLanguageService CreateService()
        // ApplicationDbContext extiende CoreDbContext — compatible con el constructor
        => new(Db, TenantService, CacheService);

    // ── Helpers locales ───────────────────────────────────────────────────

    /// <summary>Agrega un CompanyLanguage para la company dada (sin pasar por el service).</summary>
    private async Task SeedCompanyLanguageAsync(
        int companyId,
        string code,
        bool isDefault = false,
        int id = 100)
    {
        Db.CompanyLanguages.Add(new CompanyLanguage
        {
            Id           = id,
            CompanyId    = companyId,
            LanguageCode = code,
            IsDefault    = isDefault,
        });
        await Db.SaveChangesAsync();
    }

    // ── 1. GetSupportedLanguages ──────────────────────────────────────────

    [Fact]
    public async Task GetSupportedLanguages_ReturnsFourSupportedLanguages()
    {
        // SupportedLanguages es, en, pt, fr están seedeados en el contexto de test
        SetTenant(companyId: 100);
        var result = await CreateService().GetSupportedLanguages();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetSupportedLanguages_MarksSelectedLanguage_WhenCompanyHasIt()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetSupportedLanguages();

        result.Success.Should().BeTrue();
        var es = result.Data!.First(l => l.Code == "es");
        es.IsSelected.Should().BeTrue();
        es.IsDefault.Should().BeTrue();

        var en = result.Data.First(l => l.Code == "en");
        en.IsSelected.Should().BeFalse();
    }

    // ── 2. GetCompanyLanguages ────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyLanguages_ReturnsOnlyCompanyLanguages()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "en", isDefault: false, id: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().GetCompanyLanguages();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Any(l => l.IsDefault && l.Code == "es").Should().BeTrue();
    }

    // ── 3. AddLanguage ────────────────────────────────────────────────────

    [Fact]
    public async Task AddLanguage_UnsupportedCode_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().AddLanguage("xx"); // no existe

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddLanguage_AlreadyEnabled_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().AddLanguage("es"); // ya habilitado

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AddLanguage_FirstLanguage_BecomesDefault()
    {
        await SeedCompanyAsync(100);
        // Company 100 no tiene idiomas → primer AddLanguage debe ser IsDefault=true
        SetTenant(companyId: 100);

        var result = await CreateService().AddLanguage("es");

        result.Success.Should().BeTrue();
        result.Data!.Single().IsDefault.Should().BeTrue();

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddLanguage_SecondLanguage_NotDefault()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().AddLanguage("en");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);

        var en = result.Data!.FirstOrDefault(l => l.Code == "en");
        en.Should().NotBeNull();
        en!.IsDefault.Should().BeFalse();
    }

    // ── 4. RemoveLanguage ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveLanguage_NotEnabled_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().RemoveLanguage("en"); // no habilitado

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveLanguage_OnlyLanguage_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().RemoveLanguage("es"); // único idioma

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("al menos un idioma");
    }

    [Fact]
    public async Task RemoveLanguage_DefaultLanguage_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "en", isDefault: false, id: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().RemoveLanguage("es"); // es el default

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("defecto");
    }

    [Fact]
    public async Task RemoveLanguage_NonDefaultLanguage_RemovesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "en", isDefault: false, id: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().RemoveLanguage("en");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.Single().Code.Should().Be("es");

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 5. SetDefault ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_NotEnabled_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().SetDefault("fr"); // no habilitado

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefault_AlreadyDefault_ReturnsOkWithNoChange()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true, id: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().SetDefault("es"); // ya es el default

        result.Success.Should().BeTrue();
        await CacheService.DidNotReceive().EvictMenuByCompanyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefault_ValidLanguage_SwitchesDefault()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "es", isDefault: true,  id: 100);
        await SeedCompanyLanguageAsync(companyId: 100, code: "en", isDefault: false, id: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().SetDefault("en");

        result.Success.Should().BeTrue();
        result.Data!.First(l => l.Code == "en").IsDefault.Should().BeTrue();
        result.Data.First(l => l.Code == "es").IsDefault.Should().BeFalse();

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }
}
