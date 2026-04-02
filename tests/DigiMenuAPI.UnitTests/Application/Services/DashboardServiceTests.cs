using AppCore.Domain.Entities;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de DashboardService — contadores de entidades para el panel de administración.
///
/// Aspectos críticos:
///   1. Solo cuenta entidades del tenant autenticado (multi-tenant isolation).
///   2. Soft-deleted records no se cuentan (query filters globales).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class DashboardServiceTests : ServiceTestBase
{
    private DashboardService CreateService()
        => new(Db, TenantService, new MemoryCache(new MemoryCacheOptions()));

    // ── 1. GetStats — aislamiento multi-tenant ────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsOnlyCurrentTenantCounts()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);

        // Tenant 100: 2 categorías, 1 tag, 1 usuario
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedCategoryAsync(id: 101, companyId: 100);
        await SeedTagAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100);

        // Tenant 101: 1 categoría, 1 usuario — NO deben contarse para tenant 100
        await SeedCategoryAsync(id: 102, companyId: 101);
        await SeedUserAsync(id: 101, companyId: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().GetStats();

        result.Success.Should().BeTrue();
        result.Data!.Categories.Should().Be(2);
        result.Data.Tags.Should().Be(1);
        result.Data.Users.Should().Be(1);
    }

    [Fact]
    public async Task GetStats_SoftDeletedCategories_NotCounted()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100, isDeleted: false);
        await SeedCategoryAsync(id: 101, companyId: 100, isDeleted: true); // borrado → no cuenta

        SetTenant(companyId: 100);
        var result = await CreateService().GetStats();

        result.Success.Should().BeTrue();
        result.Data!.Categories.Should().Be(1); // solo la no borrada
    }

    [Fact]
    public async Task GetStats_EmptyTenant_ReturnsZeroCounts()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().GetStats();

        result.Success.Should().BeTrue();
        result.Data!.Products.Should().Be(0);
        result.Data.Categories.Should().Be(0);
        result.Data.Tags.Should().Be(0);
        result.Data.Users.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_CountsProducts_ForCurrentTenant()
    {
        await SeedCompanyAsync(100);
        await SeedCategoryAsync(id: 100, companyId: 100);
        await SeedProductAsync(id: 100, companyId: 100, categoryId: 100);
        await SeedProductAsync(id: 101, companyId: 100, categoryId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetStats();

        result.Success.Should().BeTrue();
        result.Data!.Products.Should().Be(2);
    }
}
