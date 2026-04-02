using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de UserService — gestión de usuarios dentro de una Company.
///
/// Aspectos críticos:
///   1. Aislamiento multi-tenant
///   2. Restricciones por rol (BranchAdmin solo ve su branch)
///   3. Jerarquía de roles (CompanyAdmin no puede crear SuperAdmin)
///   4. Unicidad de email
///   5. Pertenencia de Branch a la empresa
///   6. Autoprotección (no puede eliminarse a sí mismo)
///
/// AppUser seeded Id=1, CompanyId=1, Role=CompanyAdmin → usar IDs >= 100.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
[Trait("Category", "Security")]
public sealed class UserServiceTests : ServiceTestBase
{
    private UserService CreateService()
        => new(Db, TenantService, EmailQueue, Mapper, Configuration);

    // ── 1. Aislamiento Multi-Tenant ───────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyUsersFromCurrentTenant()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedUserAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 101, companyId: 100);
        await SeedUserAsync(id: 102, companyId: 101); // otra empresa

        SetTenant(companyId: 100, userId: 100);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data.Select(u => u.Id).Should().NotContain(102);
    }

    [Fact]
    public async Task GetById_UserBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedUserAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100, userId: 100);
        var result = await CreateService().GetById(110);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.UserNotFound);
    }

    // ── 2. BranchAdmin scope ──────────────────────────────────────────────

    [Fact]
    public async Task GetAll_BranchAdmin_OnlySeesUsersOfOwnBranch()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        await SeedUserAsync(id: 101, companyId: 100, branchId: 100, role: UserRoles.Staff);
        await SeedUserAsync(id: 102, companyId: 100, branchId: 101, role: UserRoles.Staff); // otra branch

        SetTenant(companyId: 100, branchId: 100, userId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data!.Should().HaveCount(2); // solo los de branch 100
        result.Data.Select(u => u.Id).Should().NotContain(102);
    }

    [Fact]
    public async Task GetById_BranchAdmin_CannotAccessUserOfOtherBranch()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        await SeedUserAsync(id: 105, companyId: 100, branchId: 101, role: UserRoles.Staff);

        SetTenant(companyId: 100, branchId: 100, userId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().GetById(105);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 3. Create — jerarquía de roles ────────────────────────────────────

    [Fact]
    public async Task Create_CompanyAdmin_CannotCreateSuperAdmin()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var dto = new AppUserCreateDto(
            FullName: "Super",
            Email:    "super@test.com",
            Role:     UserRoles.SuperAdmin,
            BranchId: null,
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.CannotAssignSuperAdmin);
    }

    [Fact]
    public async Task Create_BranchAdmin_CannotCreateUserInOtherBranch()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);

        SetTenant(companyId: 100, branchId: 100, userId: 100, role: UserRoles.BranchAdmin);
        var dto = new AppUserCreateDto(
            FullName: "Staff",
            Email:    "staff@test.com",
            Role:     UserRoles.Staff,
            BranchId: 101, // otra branch — no permitido
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.Forbidden);
    }

    [Fact]
    public async Task Create_BranchAdminRole_WithoutBranch_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var dto = new AppUserCreateDto(
            FullName: "Admin",
            Email:    "admin@test.com",
            Role:     UserRoles.BranchAdmin,
            BranchId: null, // falta branch
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.BranchRequiredForRole);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100); // email: user100@test.com
        await SeedUserAsync(id: 101, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 101, role: UserRoles.CompanyAdmin);
        // Staff + BranchId válido → pasan las validaciones de branch y rol antes de la de email
        var dto = new AppUserCreateDto(
            FullName: "Duplicado",
            Email:    "user100@test.com", // ya existe
            Role:     UserRoles.Staff,
            BranchId: 100,
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.EmailAlreadyExists);
    }

    [Fact]
    public async Task Create_BranchDoesNotBelongToCompany_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101); // branch de otra empresa
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var dto = new AppUserCreateDto(
            FullName: "Nuevo",
            Email:    "nuevo@test.com",
            Role:     UserRoles.BranchAdmin,
            BranchId: 110, // de otra empresa
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchNotFound);
    }

    [Fact]
    public async Task Create_ValidCompanyAdmin_PersistsUserWithMustChangePassword()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var dto = new AppUserCreateDto(
            FullName: "Nuevo Staff",
            Email:    "staff.nuevo@test.com",
            Role:     UserRoles.Staff,
            BranchId: 100,
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeTrue();
        result.Data!.CompanyId.Should().Be(100);

        var saved = await Db.Users.FindAsync(result.Data.Id);
        saved!.MustChangePassword.Should().BeTrue();
        saved.PasswordHash.Should().NotBeNullOrEmpty();
    }

    // ── 4. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingUser_SetsSoftDelete()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);
        await SeedUserAsync(id: 101, companyId: 100);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var result = await CreateService().Delete(101);

        result.Success.Should().BeTrue();

        var deleted = await Db.Users.FindAsync(101);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_Self_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var result = await CreateService().Delete(100); // eliminarse a sí mismo

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.CannotModifySelf);
    }

    [Fact]
    public async Task Delete_NonexistentUser_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var result = await CreateService().Delete(999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.UserNotFound);
    }

    // ── 5. ToggleActive ───────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_TogglesIsActiveState()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);
        await SeedUserAsync(id: 101, companyId: 100); // IsActive=true por defecto

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var result = await CreateService().ToggleActive(101);

        result.Success.Should().BeTrue();

        var user = await Db.Users.FindAsync(101);
        user!.IsActive.Should().BeFalse(); // se desactivó
    }

    [Fact]
    public async Task ToggleActive_Self_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        var result = await CreateService().ToggleActive(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.CannotModifySelf);
    }

    // ── 6. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_DuplicateEmail_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);
        await SeedUserAsync(id: 101, companyId: 100); // email: user101@test.com

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        // Intentar cambiar email de user100 al email de user101
        var result = await CreateService().Update(new AppUserUpdateDto(
            Id:       100,
            FullName: "CompanyAdmin",
            Email:    "user101@test.com", // ya existe
            BranchId: null,
            AdminLang: null));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.EmailAlreadyExists);
    }

    [Fact]
    public async Task Update_SameEmail_DoesNotConflict()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        // Actualizar con el mismo email → no debe conflicto
        var result = await CreateService().Update(new AppUserUpdateDto(
            Id:       100,
            FullName: "Nombre Actualizado",
            Email:    "user100@test.com", // mismo email
            BranchId: null,
            AdminLang: null));

        result.Success.Should().BeTrue();
        result.Data!.FullName.Should().Be("Nombre Actualizado");
    }

    // ── 7. Plan limit ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_UserLimitReached_ReturnsConflict()
    {
        // Company con MaxUsers=1 ya tiene 1 usuario activo → no puede crear más
        var company = await SeedCompanyAsync(100);
        company.MaxUsers = 1;
        await Db.SaveChangesAsync();

        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedUserAsync(id: 100, companyId: 100, role: UserRoles.CompanyAdmin);

        SetTenant(companyId: 100, userId: 100, role: UserRoles.CompanyAdmin);
        // Staff + BranchId válido → pasan las validaciones de rol/branch antes de llegar al límite
        var dto = new AppUserCreateDto(
            FullName: "Excedente",
            Email:    "extra@test.com",
            Role:     UserRoles.Staff,
            BranchId: 100,
            AdminLang: null);

        var result = await CreateService().Create(dto);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.UserLimitReached);
    }
}
