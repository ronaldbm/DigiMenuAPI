using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de BranchService — gestión de sucursales por Company.
///
/// Branch seeded Id=1, CompanyId=1 → usar IDs >= 100 para evitar colisiones.
/// NOTA: BranchService.Create usa NetTopologySuite Point para Location;
///       InMemory no valida tipos geométricos, por lo que no se testea Location.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class BranchServiceTests : ServiceTestBase
{
    private BranchService CreateService()
        => new(Db, TenantService, CacheService, Mapper);

    // ── 1. Aislamiento Multi-Tenant ───────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentTenantBranches()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 101);
        await SeedBranchAsync(id: 102, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(b => b.Id).Should().NotContain(101);
    }

    [Fact]
    public async Task GetById_BranchBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(110);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchNotFound);
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedBranches()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100, isActive: false);
        // Soft-delete branch 101
        var branch = await Db.Branches.FindAsync(101);
        branch!.IsDeleted = true;
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data!.Should().HaveCount(1);
        result.Data.First().Id.Should().Be(100);
    }

    // ── 2. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidBranch_AssignsTenantCompanyId()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Create(new BranchCreateDto(
            Name: "Sucursal Centro",
            Slug: "centro",
            Address: null,
            Phone: null,
            Email: null,
            Latitude: null,
            Longitude: null));

        result.Success.Should().BeTrue();

        var saved = await Db.Branches.FirstAsync(b => b.Id == result.Data!.Id);
        saved.CompanyId.Should().Be(100);
        saved.Name.Should().Be("Sucursal Centro");
    }

    [Fact]
    public async Task Create_DuplicateSlug_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100, slug: "mi-slug");

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new BranchCreateDto(
            Name: "Otra",
            Slug: "mi-slug", // ya existe
            Address: null, Phone: null, Email: null,
            Latitude: null, Longitude: null));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.BranchSlugAlreadyExists);
    }

    [Fact]
    public async Task Create_BranchLimitReached_ReturnsConflict()
    {
        var company = await SeedCompanyAsync(100);
        company.MaxBranches = 1;
        await Db.SaveChangesAsync();

        await SeedBranchAsync(id: 100, companyId: 100); // ya hay 1 branch activa

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new BranchCreateDto(
            Name: "Segunda", Slug: null,
            Address: null, Phone: null, Email: null,
            Latitude: null, Longitude: null));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.BranchLimitReached);
    }

    [Fact]
    public async Task Create_AutoGeneratesSlugFromName()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Create(new BranchCreateDto(
            Name: "Sucursal Norte",
            Slug: null, // sin slug → se genera automáticamente
            Address: null, Phone: null, Email: null,
            Latitude: null, Longitude: null));

        result.Success.Should().BeTrue();
        result.Data!.Slug.Should().NotBeNullOrWhiteSpace();
        result.Data.Slug.Should().Be("sucursal-norte");
    }

    // ── 3. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_BranchBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchUpdateDto(
            Id: 110, Name: "Hackeado",
            Address: null, Phone: null, Email: null,
            Latitude: null, Longitude: null));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchNotFound);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesName()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchUpdateDto(
            Id: 100, Name: "Nombre Nuevo",
            Address: "Calle 1", Phone: null, Email: null,
            Latitude: null, Longitude: null));

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Nombre Nuevo");

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 4. ToggleActive ───────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_TogglesIsActiveState()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100, isActive: true);

        SetTenant(companyId: 100);
        var result = await CreateService().ToggleActive(100);

        result.Success.Should().BeTrue();

        var branch = await Db.Branches.FindAsync(100);
        branch!.IsActive.Should().BeFalse();

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    // ── 5. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_BranchWithActiveUsers_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, branchId: 100, role: UserRoles.Staff);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.BranchHasActiveUsers);
    }

    [Fact]
    public async Task Delete_BranchWithNoActiveUsers_SetsSoftDelete()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.Branches.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == 100);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.IsActive.Should().BeFalse();

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_NonexistentBranch_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Delete(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchNotFound);
    }
}
