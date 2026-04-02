using AppCore.Application.Common;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de AuthService — registro, login, cambio de contraseña, reset.
///
/// Aspectos críticos (seguridad):
///   1. Credenciales inválidas siempre retornan Forbidden (no revela si el email existe)
///   2. Empresa/usuario inactivo no puede iniciar sesión
///   3. Slug y email únicos en registro
///   4. Validación de complejidad de contraseña
///   5. ForgotPassword no revela existencia de cuentas (anti-enumeration)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Security")]
public sealed class AuthServiceTests : ServiceTestBase
{
    private AuthService CreateService()
        => new(Db, Configuration, TenantService, EmailQueue);

    // ── Helpers ───────────────────────────────────────────────────────────

    private static CompanyCreateDto ValidRegisterDto(
        string slug  = "test-company",
        string email = "admin@test.com",
        string pass  = "Password1") =>
        new(Name:          "Test Company",
            AdminFullName: "Test Admin",
            Email:         email,
            Password:      pass,
            Phone:         null,
            CountryCode:   "CR",
            PlanId:        1,
            MaxBranches:   null,
            MaxUsers:      null,
            Slug:          slug);

    /// <summary>Siembra un usuario con BCrypt real para tests de Login.</summary>
    private async Task<AppUser> SeedUserWithPasswordAsync(
        string email    = "user@test.com",
        string password = "Password1",
        bool isActive   = true,
        bool companyActive = true,
        int userId     = 100,
        int companyId  = 100)
    {
        var company = await SeedCompanyAsync(companyId);
        company.IsActive = companyActive;
        await Db.SaveChangesAsync();

        var user = new AppUser
        {
            Id           = userId,
            CompanyId    = companyId,
            FullName     = "Test User",
            Email        = email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role         = UserRoles.CompanyAdmin,
            IsActive     = isActive,
            IsDeleted    = false,
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    // ── RegisterCompany ───────────────────────────────────────────────────

    [Fact]
    public async Task RegisterCompany_WeakPassword_ReturnsValidationError()
    {
        var result = await CreateService().RegisterCompany(
            ValidRegisterDto(pass: "simple")); // sin mayúscula ni número

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.WeakPassword);
    }

    [Fact]
    public async Task RegisterCompany_DuplicateSlug_ReturnsConflict()
    {
        // Insertar company con slug exacto "test-company" (SeedCompanyAsync appendea el id)
        await SeedPlanAsync();
        Db.Companies.Add(new AppCore.Domain.Entities.Company
        {
            Id          = 200,
            Name        = "Existing Co",
            Slug        = "test-company", // slug exacto que usará el DTO
            Email       = "existing@co.com",
            IsActive    = true,
            PlanId      = 99,
            MaxBranches = -1,
            MaxUsers    = -1,
        });
        await Db.SaveChangesAsync();

        var result = await CreateService().RegisterCompany(
            ValidRegisterDto(slug: "test-company"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.SlugAlreadyExists);
    }

    [Fact]
    public async Task RegisterCompany_DuplicateEmail_ReturnsConflict()
    {
        // Email ya registrado en otro usuario
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100);

        // user100@test.com ya existe (SeedUserAsync default)
        var result = await CreateService().RegisterCompany(
            ValidRegisterDto(email: "user100@test.com"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.EmailAlreadyExists);
    }

    [Fact]
    public async Task RegisterCompany_ValidData_CreatesCompanyBranchAndAdmin()
    {
        var result = await CreateService().RegisterCompany(
            ValidRegisterDto(slug: "nueva-empresa", email: "ceo@nueva.com"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.Role.Should().Be(UserRoles.CompanyAdmin);

        // Verificar que se creó la empresa
        var company = await Db.Companies.FirstOrDefaultAsync(c => c.Slug == "nueva-empresa");
        company.Should().NotBeNull();

        // Verificar que se creó la branch principal
        var branch = await Db.Branches.FirstOrDefaultAsync(b => b.CompanyId == company!.Id);
        branch.Should().NotBeNull();

        // Verificar que se creó el usuario admin
        var admin = await Db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "ceo@nueva.com");
        admin.Should().NotBeNull();
        admin!.Role.Should().Be(UserRoles.CompanyAdmin);
        admin.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterCompany_ValidData_QueuesWelcomeEmail()
    {
        await CreateService().RegisterCompany(
            ValidRegisterDto(slug: "empresa-email", email: "welcome@test.com"));

        await EmailQueue.Received(1).QueueWelcomeAsync(
            Arg.Any<AppCore.Application.DTOs.Email.WelcomeEmailDto>(),
            Arg.Any<int>());
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        await SeedUserWithPasswordAsync(
            email: "admin@corp.com", password: "Password1");

        var result = await CreateService().Login(
            new LoginRequestDto("admin@corp.com", "Password1"));

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.Email.Should().Be("admin@corp.com");
    }

    [Fact]
    public async Task Login_EmailNotFound_ReturnsForbidden_WithGenericMessage()
    {
        var result = await CreateService().Login(
            new LoginRequestDto("nonexistent@test.com", "Password1"));

        // NUNCA debe revelar si el email existe — siempre mismo mensaje genérico
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.InvalidCredentials);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsForbidden_WithSameMessage()
    {
        await SeedUserWithPasswordAsync(
            email: "admin@corp.com", password: "Password1");

        var result = await CreateService().Login(
            new LoginRequestDto("admin@corp.com", "WrongPass99"));

        // Mismo error que email no encontrado — anti-enumeration
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.InvalidCredentials);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsForbidden()
    {
        await SeedUserWithPasswordAsync(
            email: "inactive@corp.com", password: "Password1", isActive: false);

        var result = await CreateService().Login(
            new LoginRequestDto("inactive@corp.com", "Password1"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.AccountDisabled);
    }

    [Fact]
    public async Task Login_InactiveCompany_ReturnsForbidden()
    {
        await SeedUserWithPasswordAsync(
            email: "user@corp.com", password: "Password1",
            isActive: true, companyActive: false);

        var result = await CreateService().Login(
            new LoginRequestDto("user@corp.com", "Password1"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.CompanyDisabled);
    }

    [Fact]
    public async Task Login_ValidCredentials_UpdatesLastLoginAt()
    {
        await SeedUserWithPasswordAsync(
            email: "admin@corp.com", password: "Password1");

        await CreateService().Login(
            new LoginRequestDto("admin@corp.com", "Password1"));

        var user = await Db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Email == "admin@corp.com");
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── ChangePassword ────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsValidationError()
    {
        await SeedUserWithPasswordAsync(userId: 100, password: "Password1");
        SetTenant(companyId: 100, userId: 100);

        var result = await CreateService().ChangePassword(
            new ChangePasswordDto("WrongPassword1", "NewPassword2"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.IncorrectPassword);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsValidationError()
    {
        await SeedUserWithPasswordAsync(userId: 100, password: "Password1");
        SetTenant(companyId: 100, userId: 100);

        var result = await CreateService().ChangePassword(
            new ChangePasswordDto("Password1", "weak")); // sin mayúscula ni número

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.WeakPassword);
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrentPassword_ReturnsValidationError()
    {
        await SeedUserWithPasswordAsync(userId: 100, password: "Password1");
        SetTenant(companyId: 100, userId: 100);

        var result = await CreateService().ChangePassword(
            new ChangePasswordDto("Password1", "Password1")); // misma contraseña

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.WeakPassword);
    }

    [Fact]
    public async Task ChangePassword_ValidChange_UpdatesHashAndClearsMustChange()
    {
        var user = await SeedUserWithPasswordAsync(userId: 100, password: "Password1");
        user.MustChangePassword = true;
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100, userId: 100);
        var result = await CreateService().ChangePassword(
            new ChangePasswordDto("Password1", "NewPassword2"));

        result.Success.Should().BeTrue();

        var updated = await Db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == 100);
        updated.MustChangePassword.Should().BeFalse();
        BCrypt.Net.BCrypt.Verify("NewPassword2", updated.PasswordHash).Should().BeTrue();
    }

    // ── ForgotPassword — anti-enumeration ────────────────────────────────

    [Fact]
    public async Task ForgotPassword_NonexistentEmail_ReturnsOkWithoutRevealingInfo()
    {
        var result = await CreateService().ForgotPassword(
            new ForgotPasswordDto("nobody@nowhere.com"));

        // NUNCA debe revelar si el email existe — devuelve Ok silenciosamente
        result.Success.Should().BeTrue();
        await EmailQueue.DidNotReceive().QueueForgotPasswordAsync(
            Arg.Any<AppCore.Application.DTOs.Email.ForgotPasswordEmailDto>(),
            Arg.Any<int>());
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_QueuesEmailAndCreatesResetToken()
    {
        await SeedUserWithPasswordAsync(
            email: "valid@test.com", password: "Password1");

        var result = await CreateService().ForgotPassword(
            new ForgotPasswordDto("valid@test.com"));

        result.Success.Should().BeTrue();

        var reset = await Db.PasswordResetRequests
            .FirstOrDefaultAsync(r => !r.IsUsed);
        reset.Should().NotBeNull();
        reset!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await EmailQueue.Received(1).QueueForgotPasswordAsync(
            Arg.Any<AppCore.Application.DTOs.Email.ForgotPasswordEmailDto>(),
            Arg.Any<int>());
    }

    // ── ValidateResetToken ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateResetToken_InvalidToken_ReturnsValidationError()
    {
        var result = await CreateService().ValidateResetToken("non-existent-token");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.InvalidResetToken);
    }

    [Fact]
    public async Task ValidateResetToken_ExpiredToken_ReturnsValidationError()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100);

        Db.PasswordResetRequests.Add(new PasswordResetRequest
        {
            CompanyId = 100,
            UserId    = 100,
            Token     = "expired-token-abc",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // vencido
            IsUsed    = false,
        });
        await Db.SaveChangesAsync();

        var result = await CreateService().ValidateResetToken("expired-token-abc");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Validation);
        result.ErrorKey.Should().Be(ErrorKeys.InvalidResetToken);
    }

    [Fact]
    public async Task ValidateResetToken_ValidToken_ReturnsOk()
    {
        await SeedCompanyAsync(100);
        await SeedUserAsync(id: 100, companyId: 100);

        Db.PasswordResetRequests.Add(new PasswordResetRequest
        {
            CompanyId = 100,
            UserId    = 100,
            Token     = "valid-token-xyz",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed    = false,
        });
        await Db.SaveChangesAsync();

        var result = await CreateService().ValidateResetToken("valid-token-xyz");

        result.Success.Should().BeTrue();
    }
}
