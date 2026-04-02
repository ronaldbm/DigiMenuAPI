using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de SettingService — configuración de empresa y sucursal.
///
/// Datos requeridos (IDs >= 100 para no colisionar con la empresa master):
///   - CompanyInfo, CompanyTheme, CompanySeo: CompanyId 100
///   - BranchLocale: BranchId 100
///
/// Aspectos críticos:
///   1. GetCompanySettings falla si falta alguna entidad de config.
///   2. UpdateCompanyInfo actualiza BusinessName y evicta caché.
///   3. UpdateCompanyTheme actualiza colores y evicta caché.
///   4. UpdateCompanySeo actualiza metadatos y evicta caché.
///   5. UpdateBranchLocale actualiza localización y evicta caché de Branch.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SettingServiceTests : ServiceTestBase
{
    private SettingService CreateService()
        => new(Db, Mapper, TenantService, FileStorage, CacheService, ModuleGuard);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task SeedCompanyInfoAsync(int companyId = 100, int id = 100)
    {
        Db.CompanyInfos.Add(new CompanyInfo
        {
            Id           = id,
            CompanyId    = companyId,
            BusinessName = $"Test Business {companyId}",
        });
        await Db.SaveChangesAsync();
    }

    private async Task SeedCompanyThemeAsync(int companyId = 100, int id = 100)
    {
        Db.CompanyThemes.Add(new CompanyTheme
        {
            Id                    = id,
            CompanyId             = companyId,
            PrimaryColor          = "#FF0000",
            PrimaryTextColor      = "#FFFFFF",
            SecondaryColor        = "#00FF00",
            PageBackgroundColor   = "#FFFFFF",
            HeaderBackgroundColor = "#000000",
            HeaderTextColor       = "#FFFFFF",
            TabBackgroundColor    = "#EEEEEE",
            TabTextColor          = "#333333",
            TitlesColor           = "#111111",
            TextColor             = "#222222",
            BrowserThemeColor     = "#FF0000",
            IsDarkMode            = false,
            HeaderStyle           = 1,
            MenuLayout            = 1,
            ProductDisplay        = 1,
            ShowProductDetails    = true,
            FilterMode            = 0,
            ShowContactButton     = true,
            ShowModalProductInfo  = false,
            ShowMapInMenu         = true,
        });
        await Db.SaveChangesAsync();
    }

    private async Task SeedCompanySeoAsync(int companyId = 100, int id = 100)
    {
        Db.CompanySeos.Add(new CompanySeo
        {
            Id              = id,
            CompanyId       = companyId,
            MetaTitle       = "Test Meta",
            MetaDescription = "Test Description",
        });
        await Db.SaveChangesAsync();
    }

    private async Task SeedBranchLocaleAsync(int branchId = 100, int id = 100)
    {
        Db.BranchLocales.Add(new BranchLocale
        {
            Id             = id,
            BranchId       = branchId,
            CountryCode    = "CR",
            PhoneCode      = "+506",
            Currency       = "CRC",
            CurrencyLocale = "es-CR",
            Language       = "es",
            TimeZone       = "America/Costa_Rica",
            Decimals       = 0,
        });
        await Db.SaveChangesAsync();
    }

    // ── 1. GetCompanySettings ─────────────────────────────────────────────

    [Fact]
    public async Task GetCompanySettings_AllEntitiesExist_ReturnsDto()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyInfoAsync(companyId: 100);
        await SeedCompanyThemeAsync(companyId: 100);
        await SeedCompanySeoAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetCompanySettings();

        result.Success.Should().BeTrue();
        result.Data!.Info.Should().NotBeNull();
        result.Data.Theme.Should().NotBeNull();
        result.Data.Seo.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanySettings_MissingInfo_ReturnsFail()
    {
        await SeedCompanyAsync(100);
        // NO sembrar CompanyInfo → falla
        await SeedCompanyThemeAsync(companyId: 100);
        await SeedCompanySeoAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetCompanySettings();

        result.Success.Should().BeFalse();
    }

    // ── 2. GetCompanyInfo ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyInfo_Exists_ReturnsDto()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyInfoAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetCompanyInfo();

        result.Success.Should().BeTrue();
        result.Data!.BusinessName.Should().Be("Test Business 100");
    }

    [Fact]
    public async Task GetCompanyInfo_NotFound_ReturnsFail()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetCompanyInfo();

        result.Success.Should().BeFalse();
    }

    // ── 3. UpdateCompanyInfo ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateCompanyInfo_ValidData_UpdatesNameAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyInfoAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateCompanyInfo(new CompanyInfoUpdateDto(
            BusinessName: "Nombre Actualizado",
            Tagline:      "Slogan actualizado",
            Logo:         null,
            Favicon:      null,
            BackgroundImage: null));

        result.Success.Should().BeTrue();
        result.Data!.BusinessName.Should().Be("Nombre Actualizado");

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCompanyInfo_NotFound_ReturnsFail()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().UpdateCompanyInfo(new CompanyInfoUpdateDto(
            BusinessName: "X", Tagline: null, Logo: null, Favicon: null, BackgroundImage: null));

        result.Success.Should().BeFalse();
    }

    // ── 4. UpdateCompanyTheme ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateCompanyTheme_ValidData_UpdatesPrimaryColorAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyThemeAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateCompanyTheme(new CompanyThemeUpdateDto(
            IsDarkMode:            true,
            PageBackgroundColor:   "#111111",
            HeaderBackgroundColor: "#222222",
            HeaderTextColor:       "#FFFFFF",
            TabBackgroundColor:    "#333333",
            TabTextColor:          "#EEEEEE",
            PrimaryColor:          "#BLUE01",
            PrimaryTextColor:      "#FFFFFF",
            SecondaryColor:        "#GREEN1",
            TitlesColor:           "#444444",
            TextColor:             "#555555",
            BrowserThemeColor:     "#BLUE01",
            HeaderStyle:           2,
            MenuLayout:            1,
            ProductDisplay:        1,
            ShowProductDetails:    true,
            FilterMode:            0,
            ShowContactButton:     false,
            ShowModalProductInfo:  true,
            ShowMapInMenu:         false));

        result.Success.Should().BeTrue();
        result.Data!.PrimaryColor.Should().Be("#BLUE01");

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCompanyTheme_NotFound_ReturnsFail()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().UpdateCompanyTheme(new CompanyThemeUpdateDto(
            false, "#FFF", "#000", "#FFF", "#EEE", "#333", "#F00", "#FFF",
            "#0F0", "#111", "#222", "#F00", 1, 1, 1, true, 0, true, false, true));

        result.Success.Should().BeFalse();
    }

    // ── 5. UpdateCompanySeo ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateCompanySeo_ValidData_UpdatesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedCompanySeoAsync(companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateCompanySeo(new CompanySeoUpdateDto(
            MetaTitle:       "Nuevo Título",
            MetaDescription: "Nueva Descripción",
            GoogleAnalyticsId: "GA-12345",
            FacebookPixelId:  null));

        result.Success.Should().BeTrue();
        result.Data!.MetaTitle.Should().Be("Nuevo Título");

        await CacheService.Received(1).EvictMenuByCompanyAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 6. GetBranchLocale / UpdateBranchLocale ───────────────────────────

    [Fact]
    public async Task GetBranchLocale_Exists_ReturnsDto()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchLocaleAsync(branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetBranchLocale(100);

        result.Success.Should().BeTrue();
        result.Data!.CountryCode.Should().Be("CR");
    }

    [Fact]
    public async Task UpdateBranchLocale_ValidData_UpdatesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchLocaleAsync(branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().UpdateBranchLocale(new BranchLocaleUpdateDto(
            BranchId:      100,
            CountryCode:   "MX",
            PhoneCode:     "+52",
            Currency:      "MXN",
            CurrencyLocale: "es-MX",
            Language:      "es",
            TimeZone:      "America/Mexico_City",
            Decimals:      2));

        result.Success.Should().BeTrue();
        result.Data!.CountryCode.Should().Be("MX");
        result.Data.Currency.Should().Be("MXN");

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBranchLocale_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().UpdateBranchLocale(new BranchLocaleUpdateDto(
            BranchId: 110, CountryCode: "MX", PhoneCode: "+52",
            Currency: "MXN", CurrencyLocale: "es-MX",
            Language: "es", TimeZone: "America/Mexico_City", Decimals: 2));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
