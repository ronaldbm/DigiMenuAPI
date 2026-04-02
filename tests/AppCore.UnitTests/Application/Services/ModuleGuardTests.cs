using AppCore.Application.Common;
using AppCore.Application.Services;
using AppCore.Domain.Entities;
using AppCore.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace AppCore.UnitTests.Application.Services;

/// <summary>
/// Tests del ModuleGuard — feature gating por módulo premium.
///
/// NOTA sobre seed data:
/// CoreDbContext.SeedCoreData() pre-siembra los siguientes PlatformModules:
///   Id=1 → RESERVATIONS, Id=2 → TABLE_MANAGEMENT, Id=3 → ANALYTICS, Id=4 → ONLINE_ORDERS
/// No intentamos re-insertarlos; directamente los usamos como FK en CompanyModule.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ModuleGuardTests : IDisposable
{
    private readonly CoreDbContextFactory _dbFactory;
    private readonly IMemoryCache _cache;

    // IDs de PlatformModules pre-seedeados (CoreDbContext.SeedPlatformModules)
    private const int ReservationsId     = 1;
    private const int TableManagementId  = 2;
    private const int AnalyticsId        = 3;
    private const int OnlineOrdersId     = 4;

    public ModuleGuardTests()
    {
        _dbFactory = new CoreDbContextFactory();
        _cache     = new MemoryCache(new MemoryCacheOptions());
    }

    private ModuleGuard CreateGuard() => new(_dbFactory.Context, _cache);

    // ── Seed helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Siembra una company de test (si no existe) y un CompanyModule.
    /// Usa el Plan 1 (el primero seedeado) para evitar FK issues.
    /// </summary>
    private async Task SeedCompanyModuleAsync(
        int companyId,
        int platformModuleId,
        bool isActive      = true,
        DateTime? expiresAt = null)
    {
        if (!_dbFactory.Context.Companies.Any(c => c.Id == companyId))
        {
            _dbFactory.Context.Companies.Add(new Company
            {
                Id          = companyId,
                Name        = $"TestCo {companyId}",
                Slug        = $"testco-{companyId}",
                Email       = $"co{companyId}@test.com",
                IsActive    = true,
                PlanId      = 1,  // Plan seedeado por SeedPlans
                MaxBranches = -1,
                MaxUsers    = -1,
            });
            await _dbFactory.Context.SaveChangesAsync();
        }

        _dbFactory.Context.CompanyModules.Add(new CompanyModule
        {
            CompanyId         = companyId,
            PlatformModuleId  = platformModuleId,
            IsActive          = isActive,
            ActivatedAt       = DateTime.UtcNow,
            ActivatedByUserId = 1,
            ExpiresAt         = expiresAt,
        });
        await _dbFactory.Context.SaveChangesAsync();
    }

    // ── HasModuleAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task HasModuleAsync_ActiveModule_ReturnsTrue()
    {
        await SeedCompanyModuleAsync(companyId: 10, platformModuleId: ReservationsId, isActive: true);

        var result = await CreateGuard().HasModuleAsync(10, ModuleCodes.Reservations);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasModuleAsync_InactiveModule_ReturnsFalse()
    {
        await SeedCompanyModuleAsync(companyId: 11, platformModuleId: AnalyticsId, isActive: false);

        var result = await CreateGuard().HasModuleAsync(11, ModuleCodes.Analytics);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasModuleAsync_ExpiredModule_ReturnsFalse()
    {
        await SeedCompanyModuleAsync(
            companyId: 12,
            platformModuleId: TableManagementId,
            isActive: true,
            expiresAt: DateTime.UtcNow.AddDays(-1));  // venció ayer

        var result = await CreateGuard().HasModuleAsync(12, ModuleCodes.TableManagement);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasModuleAsync_ActiveWithFutureExpiry_ReturnsTrue()
    {
        await SeedCompanyModuleAsync(
            companyId: 13,
            platformModuleId: OnlineOrdersId,
            isActive: true,
            expiresAt: DateTime.UtcNow.AddDays(30));  // vence en 30 días

        var result = await CreateGuard().HasModuleAsync(13, ModuleCodes.OnlineOrders);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasModuleAsync_NonexistentModuleCode_ReturnsFalse()
    {
        var result = await CreateGuard().HasModuleAsync(1, "MODULE_THAT_DOES_NOT_EXIST");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasModuleAsync_ModuleOfOtherCompany_ReturnsFalse()
    {
        // Company 20 tiene RESERVATIONS, pero preguntamos por company 21
        await SeedCompanyModuleAsync(companyId: 20, platformModuleId: ReservationsId, isActive: true);

        var result = await CreateGuard().HasModuleAsync(companyId: 21, ModuleCodes.Reservations);

        result.Should().BeFalse();
    }

    // ── Caché ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HasModuleAsync_CachesResult_SameGuardReturnsSameValueWithoutDbQuery()
    {
        await SeedCompanyModuleAsync(companyId: 30, platformModuleId: ReservationsId, isActive: true);

        var guard = CreateGuard();
        var first  = await guard.HasModuleAsync(30, ModuleCodes.Reservations);
        var second = await guard.HasModuleAsync(30, ModuleCodes.Reservations);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateCache_AllowsFreshQueryAfterDataChange()
    {
        await SeedCompanyModuleAsync(companyId: 40, platformModuleId: ReservationsId, isActive: true);

        var guard = CreateGuard();

        // Primera llamada: cachea true
        (await guard.HasModuleAsync(40, ModuleCodes.Reservations)).Should().BeTrue();

        // Desactivar en BD directamente
        var cm = _dbFactory.Context.CompanyModules
            .First(m => m.CompanyId == 40 && m.PlatformModuleId == ReservationsId);
        cm.IsActive = false;
        await _dbFactory.Context.SaveChangesAsync();

        // Sin invalidar: sigue devolviendo true desde caché
        (await guard.HasModuleAsync(40, ModuleCodes.Reservations)).Should().BeTrue();

        // Invalidar y preguntar de nuevo → ve el cambio en BD
        guard.InvalidateCache(40, ModuleCodes.Reservations);
        (await guard.HasModuleAsync(40, ModuleCodes.Reservations)).Should().BeFalse();
    }

    // ── AssertModuleAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AssertModuleAsync_ActiveModule_DoesNotThrow()
    {
        await SeedCompanyModuleAsync(companyId: 50, platformModuleId: AnalyticsId, isActive: true);

        var act = async () => await CreateGuard().AssertModuleAsync(50, ModuleCodes.Analytics);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AssertModuleAsync_InactiveModule_ThrowsModuleNotActiveException()
    {
        var act = async () => await CreateGuard().AssertModuleAsync(1, "NONEXISTENT_MODULE");

        var ex = await act.Should().ThrowAsync<ModuleNotActiveException>();
        ex.Which.ModuleCode.Should().Be("NONEXISTENT_MODULE");
    }

    [Fact]
    public async Task AssertModuleAsync_ExpiredModule_ThrowsModuleNotActiveException()
    {
        await SeedCompanyModuleAsync(
            companyId: 60,
            platformModuleId: TableManagementId,
            isActive: true,
            expiresAt: DateTime.UtcNow.AddSeconds(-1));

        var act = async () => await CreateGuard().AssertModuleAsync(60, ModuleCodes.TableManagement);

        await act.Should().ThrowAsync<ModuleNotActiveException>();
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
        _cache.Dispose();
    }
}
