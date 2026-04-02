using AppCore.Application.Common;
using FluentAssertions;

namespace AppCore.UnitTests.Application.Common;

[Trait("Category", "Unit")]
public sealed class UserRolesTests
{
    // ── Constantes ────────────────────────────────────────────────────────

    [Fact]
    public void RoleConstants_HaveExpectedValues()
    {
        UserRoles.SuperAdmin.Should().Be(255);
        UserRoles.SuperAdminCompany.Should().Be(254);
        UserRoles.CompanyAdmin.Should().Be(1);
        UserRoles.BranchAdmin.Should().Be(2);
        UserRoles.Staff.Should().Be(3);
    }

    // ── IsPlatformLevel ───────────────────────────────────────────────────

    [Theory]
    [InlineData(UserRoles.SuperAdmin,        true)]
    [InlineData(UserRoles.SuperAdminCompany, true)]
    [InlineData(UserRoles.CompanyAdmin,      false)]
    [InlineData(UserRoles.BranchAdmin,       false)]
    [InlineData(UserRoles.Staff,             false)]
    public void IsPlatformLevel_ReturnsExpectedResult(byte role, bool expected)
        => UserRoles.IsPlatformLevel(role).Should().Be(expected);

    // ── NeedsBranch ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(UserRoles.SuperAdmin,        false)]
    [InlineData(UserRoles.SuperAdminCompany, false)]
    [InlineData(UserRoles.CompanyAdmin,      false)]
    [InlineData(UserRoles.BranchAdmin,       true)]
    [InlineData(UserRoles.Staff,             true)]
    public void NeedsBranch_ReturnsExpectedResult(byte role, bool expected)
        => UserRoles.NeedsBranch(role).Should().Be(expected);

    // ── CanAssign ─────────────────────────────────────────────────────────

    [Fact]
    public void SuperAdmin_CanAssign_AnyRoleExceptSuperAdmin()
    {
        UserRoles.CanAssign(UserRoles.SuperAdmin, UserRoles.SuperAdminCompany).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.SuperAdmin, UserRoles.CompanyAdmin).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.SuperAdmin, UserRoles.BranchAdmin).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.SuperAdmin, UserRoles.Staff).Should().BeTrue();

        // No puede asignarse a sí mismo
        UserRoles.CanAssign(UserRoles.SuperAdmin, UserRoles.SuperAdmin).Should().BeFalse();
    }

    [Fact]
    public void SuperAdminCompany_CanAssign_CompanyAdminBranchAdminStaff()
    {
        UserRoles.CanAssign(UserRoles.SuperAdminCompany, UserRoles.CompanyAdmin).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.SuperAdminCompany, UserRoles.BranchAdmin).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.SuperAdminCompany, UserRoles.Staff).Should().BeTrue();

        UserRoles.CanAssign(UserRoles.SuperAdminCompany, UserRoles.SuperAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.SuperAdminCompany, UserRoles.SuperAdminCompany).Should().BeFalse();
    }

    [Fact]
    public void CompanyAdmin_CanAssign_BranchAdminAndStaff_Only()
    {
        UserRoles.CanAssign(UserRoles.CompanyAdmin, UserRoles.BranchAdmin).Should().BeTrue();
        UserRoles.CanAssign(UserRoles.CompanyAdmin, UserRoles.Staff).Should().BeTrue();

        UserRoles.CanAssign(UserRoles.CompanyAdmin, UserRoles.SuperAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.CompanyAdmin, UserRoles.SuperAdminCompany).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.CompanyAdmin, UserRoles.CompanyAdmin).Should().BeFalse();
    }

    [Fact]
    public void BranchAdmin_CanAssign_StaffOnly()
    {
        UserRoles.CanAssign(UserRoles.BranchAdmin, UserRoles.Staff).Should().BeTrue();

        UserRoles.CanAssign(UserRoles.BranchAdmin, UserRoles.SuperAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.BranchAdmin, UserRoles.SuperAdminCompany).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.BranchAdmin, UserRoles.CompanyAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.BranchAdmin, UserRoles.BranchAdmin).Should().BeFalse();
    }

    [Fact]
    public void Staff_CannotAssign_AnyRole()
    {
        UserRoles.CanAssign(UserRoles.Staff, UserRoles.SuperAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.Staff, UserRoles.SuperAdminCompany).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.Staff, UserRoles.CompanyAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.Staff, UserRoles.BranchAdmin).Should().BeFalse();
        UserRoles.CanAssign(UserRoles.Staff, UserRoles.Staff).Should().BeFalse();
    }

    [Fact]
    public void UnknownRole_CannotAssign_Anything()
    {
        // Rol 0 (desconocido) no puede asignar nada
        UserRoles.CanAssign(0, UserRoles.Staff).Should().BeFalse();
        UserRoles.CanAssign(0, UserRoles.CompanyAdmin).Should().BeFalse();
    }

    // ── Sets de roles ─────────────────────────────────────────────────────

    [Fact]
    public void RequireBranch_ContainsOnlyBranchAdminAndStaff()
    {
        UserRoles.RequireBranch.Should().BeEquivalentTo(
            new[] { UserRoles.BranchAdmin, UserRoles.Staff });
    }

    [Fact]
    public void PlatformLevel_ContainsOnlySuperAdmins()
    {
        UserRoles.PlatformLevel.Should().BeEquivalentTo(
            new[] { UserRoles.SuperAdmin, UserRoles.SuperAdminCompany });
    }
}
