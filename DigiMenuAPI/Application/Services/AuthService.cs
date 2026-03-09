using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DigiMenuAPI.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ITenantService _tenantService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration config,
            ITenantService tenantService)
        {
            _context = context;
            _config = config;
            _tenantService = tenantService;
        }

        // ── REGISTER COMPANY ──────────────────────────────────────────
        public async Task<OperationResult<LoginResponseDto>> RegisterCompany(
            CompanyCreateDto dto)
        {
            var companySlug = SlugHelper.Slugify(dto.Slug);
            var email = dto.Email.Trim().ToLower();

            // Validar complejidad de contraseña del primer admin
            var passwordError = PasswordValidator.Validate(dto.Password);
            if (passwordError is not null)
                return OperationResult<LoginResponseDto>.ValidationError(
                    passwordError, ErrorKeys.WeakPassword);

            if (await _context.Companies.AnyAsync(c => c.Slug == companySlug))
                return OperationResult<LoginResponseDto>.Conflict(
                    "El slug ya está en uso. Elige otro identificador.",
                    ErrorKeys.SlugAlreadyExists);

            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email))
                return OperationResult<LoginResponseDto>.Conflict(
                    "El email del administrador ya está registrado.",
                    ErrorKeys.EmailAlreadyExists);

            // 1. Crear Company
            var company = new Company
            {
                Name = dto.Name.Trim(),
                Slug = companySlug,
                Email = email,
                Phone = dto.Phone?.Trim(),
                CountryCode = dto.CountryCode?.ToUpper().Trim(),
                IsActive = true
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // 2. Crear Branch principal
            var existingBranchSlugs = await _context.Branches
                .Where(b => b.CompanyId == company.Id)
                .Select(b => b.Slug)
                .ToListAsync();

            var branchSlug = SlugHelper.GenerateUnique("principal", existingBranchSlugs);

            var branch = new Branch
            {
                CompanyId = company.Id,
                Name = dto.Name.Trim(),
                Slug = branchSlug,
                IsActive = true
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // 3. Crear BranchInfo
            _context.BranchInfos.Add(new BranchInfo
            {
                BranchId = branch.Id,
                BusinessName = dto.Name.Trim()
            });

            // 4. Crear BranchTheme
            _context.BranchThemes.Add(new BranchTheme
            {
                BranchId = branch.Id,
                IsDarkMode = false,
                PageBackgroundColor = "#F1FAEE",
                HeaderBackgroundColor = "#FFFFFF",
                HeaderTextColor = "#1D3557",
                TabBackgroundColor = "#1D3557",
                TabTextColor = "#FFFFFF",
                PrimaryColor = "#E63946",
                PrimaryTextColor = "#FFFFFF",
                SecondaryColor = "#457B9D",
                TitlesColor = "#1D3557",
                TextColor = "#1D3557",
                BrowserThemeColor = "#FFFFFF",
                HeaderStyle = 1,
                MenuLayout = 1,
                ProductDisplay = 1,
                ShowProductDetails = true,
                ShowSearchButton = false,
                ShowContactButton = false
            });

            // 5. Crear BranchLocale
            _context.BranchLocales.Add(new BranchLocale
            {
                BranchId = branch.Id,
                CountryCode = dto.CountryCode?.ToUpper() ?? "CR",
                PhoneCode = ResolvePhoneCode(dto.CountryCode),
                Currency = ResolveCurrency(dto.CountryCode),
                CurrencyLocale = ResolveCurrencyLocale(dto.CountryCode),
                Language = "es",
                TimeZone = ResolveTimeZone(dto.CountryCode),
                Decimals = 2
            });

            // 6. Crear BranchSeo
            _context.BranchSeos.Add(new BranchSeo
            {
                BranchId = branch.Id
            });

            // 7. Crear CompanyAdmin
            // MustChangePassword = false — el admin registra su propia contraseña
            var admin = new AppUser
            {
                FullName = dto.AdminFullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRoles.CompanyAdmin,
                CompanyId = company.Id,
                BranchId = null,
                IsActive = true,
                MustChangePassword = false
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var token = GenerateJwt(admin, company);
            return OperationResult<LoginResponseDto>.Ok(BuildResult(admin, company, token));
        }

        // ── LOGIN ─────────────────────────────────────────────────────
        public async Task<OperationResult<LoginResponseDto>> Login(LoginRequestDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return OperationResult<LoginResponseDto>.Fail(
                    "Credenciales incorrectas.",
                    OperationResultError.Forbidden,
                    ErrorKeys.InvalidCredentials);

            if (!user.IsActive)
                return OperationResult<LoginResponseDto>.Forbidden(
                    "Tu cuenta está desactivada.",
                    ErrorKeys.AccountDisabled);

            if (!user.Company.IsActive)
                return OperationResult<LoginResponseDto>.Forbidden(
                    "Tu empresa está desactivada. Contacta al soporte.",
                    ErrorKeys.CompanyDisabled);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwt(user, user.Company);
            return OperationResult<LoginResponseDto>.Ok(BuildResult(user, user.Company, token));
            // MustChangePassword se incluye en BuildResult — el frontend lo lee y redirige
        }

        // ── REGISTER USER ─────────────────────────────────────────────
        public async Task<OperationResult<bool>> RegisterUser(AppUserCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();
            var callerRole = _tenantService.GetUserRole();
            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email))
                return OperationResult<bool>.Conflict(
                    "El email ya está registrado.",
                    ErrorKeys.EmailAlreadyExists);

            if (!UserRoles.CanAssign(callerRole, dto.Role))
                return OperationResult<bool>.Forbidden(
                    "No tienes permiso para asignar este rol.",
                    ErrorKeys.CannotAssignSuperAdmin);

            if (UserRoles.NeedsBranch(dto.Role) && dto.BranchId is null)
                return OperationResult<bool>.ValidationError(
                    "BranchAdmin y Staff deben estar asignados a una sucursal.",
                    ErrorKeys.BranchRequiredForRole);

            if (dto.BranchId.HasValue)
            {
                var branchBelongs = await _context.Branches
                    .AnyAsync(b => b.Id == dto.BranchId.Value && b.CompanyId == companyId);

                if (!branchBelongs)
                    return OperationResult<bool>.NotFound(
                        "La sucursal indicada no pertenece a tu empresa.",
                        ErrorKeys.BranchNotFound);
            }

            // Generar contraseña temporal — el usuario deberá cambiarla en su primer login
            var temporaryPassword = PasswordValidator.GenerateTemporary();

            var user = new AppUser
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                Role = dto.Role,
                CompanyId = companyId,
                BranchId = dto.BranchId,
                IsActive = true,
                MustChangePassword = true   // ← forzar cambio en primer login
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // TODO: enviar email con contraseña temporal via SendGrid
            // await _emailService.SendTemporaryPasswordAsync(user.Email, temporaryPassword);
            // Por ahora la contraseña temporal se devuelve en la respuesta para
            // que el CompanyAdmin pueda entregársela al usuario manualmente.
            // IMPORTANTE: eliminar este campo cuando se integre el email.

            return OperationResult<bool>.Ok(true, $"Usuario creado. Contraseña temporal: {temporaryPassword}");
        }

        // ── CHANGE PASSWORD ───────────────────────────────────────────
        public async Task<OperationResult<bool>> ChangePassword(ChangePasswordDto dto)
        {
            var userId = _tenantService.GetUserId();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return OperationResult<bool>.NotFound(
                    "Usuario no encontrado.",
                    ErrorKeys.Unauthorized);

            // Verificar contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return OperationResult<bool>.ValidationError(
                    "La contraseña actual es incorrecta.",
                    ErrorKeys.IncorrectPassword);

            // Validar complejidad de la nueva contraseña
            var passwordError = PasswordValidator.Validate(dto.NewPassword);
            if (passwordError is not null)
                return OperationResult<bool>.ValidationError(
                    passwordError, ErrorKeys.WeakPassword);

            // Evitar reusar la misma contraseña
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                return OperationResult<bool>.ValidationError(
                    "La nueva contraseña debe ser diferente a la actual.",
                    ErrorKeys.WeakPassword);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;  // ← limpiar flag
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── JWT ───────────────────────────────────────────────────────
        private string GenerateJwt(AppUser user, Company company)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var hours = double.Parse(_config["Jwt:ExpiresHours"] ?? "8");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new("userId",      user.Id.ToString()),
                new("companyId",   company.Id.ToString()),
                new("companySlug", company.Slug),
                new("role",        user.Role.ToString()),
                new("fullName",    user.FullName)
            };

            if (UserRoles.NeedsBranch(user.Role) && user.BranchId.HasValue)
                claims.Add(new Claim("branchId", user.BranchId.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(hours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static LoginResponseDto BuildResult(
            AppUser user, Company company, string token) =>
            new(
                Token: token,
                FullName: user.FullName,
                Email: user.Email,
                CompanyId: company.Id,
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                BranchId: user.BranchId,
                BranchName: user.BranchId.HasValue ? user.Branch?.Name : null,
                Role: user.Role,
                ExpiresAt: DateTime.UtcNow.AddHours(8),
                MustChangePassword: user.MustChangePassword  // ← nuevo campo
            );

        // ── Helpers de localización ───────────────────────────────────

        private static string ResolvePhoneCode(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "+52",
                "CO" => "+57",
                "US" => "+1",
                "GT" => "+502",
                "PA" => "+507",
                "SV" => "+503",
                "HN" => "+504",
                "NI" => "+505",
                _ => "+506"
            };

        private static string ResolveCurrency(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "MXN",
                "CO" => "COP",
                "US" => "USD",
                "GT" => "GTQ",
                "PA" => "USD",
                "SV" => "USD",
                "HN" => "HNL",
                "NI" => "NIO",
                _ => "CRC"
            };

        private static string ResolveCurrencyLocale(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "es-MX",
                "CO" => "es-CO",
                "US" => "en-US",
                "GT" => "es-GT",
                "PA" => "es-PA",
                "SV" => "es-SV",
                "HN" => "es-HN",
                "NI" => "es-NI",
                _ => "es-CR"
            };

        private static string ResolveTimeZone(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "America/Mexico_City",
                "CO" => "America/Bogota",
                "US" => "America/New_York",
                "GT" => "America/Guatemala",
                "PA" => "America/Panama",
                "SV" => "America/El_Salvador",
                "HN" => "America/Tegucigalpa",
                "NI" => "America/Managua",
                _ => "America/Costa_Rica"
            };
    }
}