using AppCore.Application.Common;
using AppCore.Application.Services;
using AppCore.Domain.Entities;
using AppCore.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;

namespace AppCore.UnitTests.Application.Services;

/// <summary>
/// Tests del TenantService — extracción de claims JWT y validación de acceso.
/// Se prueba tanto con FakeTenantService (comportamiento esperado) como
/// con la implementación real TenantService (mocking de HttpContext).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
[Trait("Category", "Security")]
public sealed class TenantServiceTests : IDisposable
{
    private readonly CoreDbContextFactory _dbFactory;

    public TenantServiceTests()
    {
        _dbFactory = new CoreDbContextFactory();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static TenantService CreateServiceWithClaims(
        int? companyId, int? branchId, int? userId, byte? role,
        CoreDbContextFactory? dbFactory = null)
    {
        var claims = new List<Claim>();
        if (companyId.HasValue) claims.Add(new Claim("companyId", companyId.Value.ToString()));
        if (branchId.HasValue)  claims.Add(new Claim("branchId",  branchId.Value.ToString()));
        if (userId.HasValue)    claims.Add(new Claim("userId",    userId.Value.ToString()));
        if (role.HasValue)      claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));

        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpCtx   = new DefaultHttpContext { User = principal };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpCtx);

        var serviceProvider = Substitute.For<IServiceProvider>();
        return new TenantService(httpContextAccessor, serviceProvider);
    }

    // ── GetCompanyId ───────────────────────────────────────────────────────

    [Fact]
    public void GetCompanyId_WithValidClaim_ReturnsCompanyId()
    {
        var service = CreateServiceWithClaims(companyId: 42, branchId: null, userId: 1, role: UserRoles.CompanyAdmin);

        service.GetCompanyId().Should().Be(42);
    }

    [Fact]
    public void GetCompanyId_WithoutClaim_ThrowsUnauthorizedAccessException()
    {
        var service = CreateServiceWithClaims(companyId: null, branchId: null, userId: null, role: null);

        var act = () => service.GetCompanyId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void TryGetCompanyId_WithValidClaim_ReturnsId()
    {
        var service = CreateServiceWithClaims(companyId: 7, branchId: null, userId: 1, role: UserRoles.CompanyAdmin);

        service.TryGetCompanyId().Should().Be(7);
    }

    [Fact]
    public void TryGetCompanyId_WithoutClaim_ReturnsNull()
    {
        var service = CreateServiceWithClaims(companyId: null, branchId: null, userId: null, role: null);

        service.TryGetCompanyId().Should().BeNull();
    }

    // ── GetBranchId ────────────────────────────────────────────────────────

    [Fact]
    public void GetBranchId_WithBranchClaim_ReturnsBranchId()
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: 5, userId: 1, role: UserRoles.BranchAdmin);

        service.GetBranchId().Should().Be(5);
    }

    [Fact]
    public void GetBranchId_WithoutBranchClaim_ThrowsUnauthorizedAccessException()
    {
        // CompanyAdmin no tiene branchId en el JWT
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: 1, role: UserRoles.CompanyAdmin);

        var act = () => service.GetBranchId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void TryGetBranchId_CompanyAdmin_ReturnsNull()
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: 1, role: UserRoles.CompanyAdmin);

        service.TryGetBranchId().Should().BeNull();
    }

    // ── GetUserId ──────────────────────────────────────────────────────────

    [Fact]
    public void GetUserId_WithValidClaim_ReturnsUserId()
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: 99, role: UserRoles.CompanyAdmin);

        service.GetUserId().Should().Be(99);
    }

    [Fact]
    public void GetUserId_WithoutClaim_ThrowsUnauthorizedAccessException()
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: null, role: UserRoles.CompanyAdmin);

        var act = () => service.GetUserId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    // ── GetUserRole ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(UserRoles.SuperAdmin)]
    [InlineData(UserRoles.CompanyAdmin)]
    [InlineData(UserRoles.BranchAdmin)]
    [InlineData(UserRoles.Staff)]
    public void GetUserRole_WithValidClaim_ReturnsRole(byte expectedRole)
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: 1, role: expectedRole);

        service.GetUserRole().Should().Be(expectedRole);
    }

    [Fact]
    public void GetUserRole_WithoutClaim_Returns0()
    {
        var service = CreateServiceWithClaims(companyId: 1, branchId: null, userId: 1, role: null);

        service.GetUserRole().Should().Be(0);
    }

    // ── Unauthenticated user ───────────────────────────────────────────────

    [Fact]
    public void GetCompanyId_WithNullHttpContext_ThrowsUnauthorized()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var serviceProvider = Substitute.For<IServiceProvider>();
        var service         = new TenantService(httpContextAccessor, serviceProvider);

        var act = () => service.GetCompanyId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    // ── FakeTenantService ─────────────────────────────────────────────────
    // Verifica que FakeTenantService replica el comportamiento esperado

    [Fact]
    public void FakeTenantService_CompanyAdmin_TryGetBranchId_ReturnsNull()
    {
        var fake = new FakeTenantService(companyId: 1, branchId: null, role: UserRoles.CompanyAdmin);

        fake.TryGetBranchId().Should().BeNull();
    }

    [Fact]
    public void FakeTenantService_BranchAdmin_TryGetBranchId_ReturnsBranchId()
    {
        var fake = new FakeTenantService(companyId: 1, branchId: 3, role: UserRoles.BranchAdmin);

        fake.TryGetBranchId().Should().Be(3);
    }

    [Fact]
    public async Task FakeTenantService_SuperAdmin_ValidateBranchOwnership_AlwaysPasses()
    {
        var fake = new FakeTenantService(companyId: 1, role: UserRoles.SuperAdmin);

        // No debe lanzar excepción para ningún branchId
        var act = async () => await fake.ValidateBranchOwnershipAsync(branchId: 999);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FakeTenantService_BranchAdmin_WrongBranch_ThrowsUnauthorized()
    {
        var fake = new FakeTenantService(companyId: 1, branchId: 2, role: UserRoles.BranchAdmin);

        var act = async () => await fake.ValidateBranchOwnershipAsync(branchId: 5);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task FakeTenantService_BranchAdmin_CorrectBranch_Passes()
    {
        var fake = new FakeTenantService(companyId: 1, branchId: 2, role: UserRoles.BranchAdmin);

        var act = async () => await fake.ValidateBranchOwnershipAsync(branchId: 2);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FakeTenantService_CompanyAdmin_BranchNotInCompany_ThrowsUnauthorized()
    {
        // Arrange: branch 99 no existe en la BD (ni pertenece a company 1)
        var fake = new FakeTenantService(
            companyId: 1,
            branchId: null,
            role: UserRoles.CompanyAdmin,
            context: _dbFactory.Context);

        var act = async () => await fake.ValidateBranchOwnershipAsync(branchId: 99);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _dbFactory.Dispose();
}
