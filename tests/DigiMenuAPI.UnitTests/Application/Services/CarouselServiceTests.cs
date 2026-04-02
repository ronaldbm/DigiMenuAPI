using AppCore.Application.Common;
using AppCore.Application.Common.Enums;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de CarouselService — items del carrusel del menú público.
///
/// FakeTenantService.ResolveBySlugAsync retorna (_branchId, _companyId) configurados
/// → SetTenant con branchId para que la resolución encuentre la branch.
///
/// Aspectos críticos:
///   1. BranchId nulo (slug no encontrado) retorna NotFound.
///   2. Solo eventos activos, con ShowPromotionalModal=true y fecha >= hoy.
///   3. Solo promociones activas, con ShowInCarousel=true y dentro del rango de fechas.
///   4. Eventos aparecen antes que las promociones en el resultado.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CarouselServiceTests : ServiceTestBase
{
    private CarouselService CreateService()
        => new(Db, TenantService);

    // ── 1. BranchId no resuelto ───────────────────────────────────────────

    [Fact]
    public async Task GetCarouselItems_BranchNotResolved_ReturnsNotFound()
    {
        // FakeTenantService con branchId=null → ResolveBySlugAsync retorna null
        SetTenant(companyId: 100, branchId: null);

        var result = await CreateService().GetCarouselItems("test-co", "main");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 2. Eventos en el carrusel ─────────────────────────────────────────

    [Fact]
    public async Task GetCarouselItems_OnlyIncludesActiveEventsWithModal()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        var today = DateTime.UtcNow.Date;
        var futureDate = today.AddDays(5);

        // Evento activo + modal → debe aparecer
        Db.BranchEvents.Add(new BranchEvent
        {
            Id                   = 100,
            BranchId             = 100,
            Title                = "Evento Activo Modal",
            EventDate            = futureDate,
            EndDate              = futureDate,
            IsActive             = true,
            ShowPromotionalModal = true,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        });

        // Evento activo pero sin modal → NO debe aparecer
        Db.BranchEvents.Add(new BranchEvent
        {
            Id                   = 101,
            BranchId             = 100,
            Title                = "Evento Sin Modal",
            EventDate            = futureDate,
            EndDate              = futureDate,
            IsActive             = true,
            ShowPromotionalModal = false,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        });

        // Evento inactivo + modal → NO debe aparecer
        Db.BranchEvents.Add(new BranchEvent
        {
            Id                   = 102,
            BranchId             = 100,
            Title                = "Evento Inactivo",
            EventDate            = futureDate,
            EndDate              = futureDate,
            IsActive             = false,
            ShowPromotionalModal = true,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        });

        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100);
        var result = await CreateService().GetCarouselItems("test-co", "main");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be("Evento Activo Modal");
    }

    [Fact]
    public async Task GetCarouselItems_ExcludesPastEvents()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        var pastDate = DateTime.UtcNow.Date.AddDays(-2);
        Db.BranchEvents.Add(new BranchEvent
        {
            Id                   = 100,
            BranchId             = 100,
            Title                = "Evento Pasado",
            EventDate            = pastDate,
            EndDate              = pastDate, // ya expiró
            IsActive             = true,
            ShowPromotionalModal = true,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100);
        var result = await CreateService().GetCarouselItems("test-co", "main");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ── 3. Promociones en el carrusel ─────────────────────────────────────

    [Fact]
    public async Task GetCarouselItems_OnlyIncludesActivePromoInCarousel()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        // Promo activa + carrusel, sin fechas → siempre visible
        Db.BranchPromotions.Add(new BranchPromotion
        {
            Id                  = 100,
            BranchId            = 100,
            Title               = "Promo Activa",
            IsActive            = true,
            ShowInCarousel      = true,
            DisplayOrder        = 1,
            PromoObjectFit      = "cover",
            PromoObjectPosition = "50% 50%",
        });

        // Promo inactiva → NO debe aparecer
        Db.BranchPromotions.Add(new BranchPromotion
        {
            Id                  = 101,
            BranchId            = 100,
            Title               = "Promo Inactiva",
            IsActive            = false,
            ShowInCarousel      = true,
            DisplayOrder        = 2,
            PromoObjectFit      = "cover",
            PromoObjectPosition = "50% 50%",
        });

        // Promo activa pero NOT en carrusel → NO debe aparecer
        Db.BranchPromotions.Add(new BranchPromotion
        {
            Id                  = 102,
            BranchId            = 100,
            Title               = "Promo No Carrusel",
            IsActive            = true,
            ShowInCarousel      = false,
            DisplayOrder        = 3,
            PromoObjectFit      = "cover",
            PromoObjectPosition = "50% 50%",
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100);
        var result = await CreateService().GetCarouselItems("test-co", "main");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Title.Should().Be("Promo Activa");
    }

    // ── 4. Orden: eventos antes que promociones ───────────────────────────

    [Fact]
    public async Task GetCarouselItems_EventsBeforePromotions()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        var futureDate = DateTime.UtcNow.Date.AddDays(3);
        Db.BranchEvents.Add(new BranchEvent
        {
            Id                   = 100,
            BranchId             = 100,
            Title                = "Evento",
            EventDate            = futureDate,
            EndDate              = futureDate,
            IsActive             = true,
            ShowPromotionalModal = true,
            FlyerObjectFit       = "cover",
            FlyerObjectPosition  = "50% 50%",
        });

        Db.BranchPromotions.Add(new BranchPromotion
        {
            Id                  = 100,
            BranchId            = 100,
            Title               = "Promoción",
            IsActive            = true,
            ShowInCarousel      = true,
            DisplayOrder        = 1,
            PromoObjectFit      = "cover",
            PromoObjectPosition = "50% 50%",
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, branchId: 100);
        var result = await CreateService().GetCarouselItems("test-co", "main");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().Title.Should().Be("Evento");  // eventos primero
        result.Data.Last().Title.Should().Be("Promoción");
    }
}
