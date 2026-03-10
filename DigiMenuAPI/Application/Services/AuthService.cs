using BCrypt.Net;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Email;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Utils;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using DigiMenuIC.Application.Interfaces;
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
        private readonly IEmailQueueService _emailQueue;

        private string AppUrl => _config["Email:AppUrl"] ?? "https://app.digimenu.cr";

        public AuthService(
            ApplicationDbContext context,
            IConfiguration config,
            ITenantService tenantService,
            IEmailQueueService emailQueue)
        {
            _context = context;
            _config = config;
            _tenantService = tenantService;
            _emailQueue = emailQueue;
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
            //    SlugHelper.GenerateUnique garantiza unicidad dentro de la Company
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

            // 4. Crear BranchTheme — valores por defecto de la paleta DigiMenu
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

            // 5. Crear BranchLocale — valores derivados del país registrado
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

            // 6. Crear BranchSeo — vacío, el admin lo completa después
            _context.BranchSeos.Add(new BranchSeo
            {
                BranchId = branch.Id
            });

            // BranchReservationForm NO se crea aquí.
            // Se crea cuando el CompanyAdmin activa el módulo RESERVATIONS
            // desde el panel de módulos (ModuleService).

            // 7. Crear CompanyAdmin
            // MustChangePassword = false — el admin eligió su propia contraseña
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

            // 8. Encolar email de bienvenida
            await _emailQueue.QueueWelcomeAsync(new WelcomeEmailDto(
                ToEmail: email,
                AdminFullName: dto.AdminFullName.Trim(),
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                LoginUrl: $"{AppUrl}/login"
            ), company.Id);

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
                MustChangePassword = true  // forzar cambio en primer login
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Cargar nombre de la empresa para el email
            var company = await _context.Companies
                .AsNoTracking()
                .FirstAsync(c => c.Id == companyId);

            // Encolar email con contraseña temporal
            await _emailQueue.QueueTemporaryPasswordAsync(new TemporaryPasswordEmailDto(
                ToEmail: email,
                FullName: dto.FullName.Trim(),
                CompanyName: company.Name,
                TemporaryPassword: temporaryPassword,
                LoginUrl: $"{AppUrl}/login"
            ), companyId);

            return OperationResult<bool>.Ok(true);
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

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return OperationResult<bool>.ValidationError(
                    "La contraseña actual es incorrecta.",
                    ErrorKeys.IncorrectPassword);

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
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── FORGOT PASSWORD ───────────────────────────────────────────
        public async Task<OperationResult<bool>> ForgotPassword(ForgotPasswordDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

            // Siempre devuelve Ok — nunca confirmamos si el email existe
            // Esto previene enumeration attacks
            if (user is null || !user.IsActive || !user.Company.IsActive)
                return OperationResult<bool>.Ok(true);

            // Invalidar tokens anteriores pendientes del mismo usuario
            var previousTokens = await _context.PasswordResetRequests
                .Where(r => r.UserId == user.Id && !r.IsUsed && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var t in previousTokens)
                t.IsUsed = true;

            // Crear nuevo token
            var token = Guid.NewGuid().ToString("N"); // 32 chars sin guiones
            var resetRequest = new PasswordResetRequest
            {
                CompanyId = user.CompanyId,
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            _context.PasswordResetRequests.Add(resetRequest);
            await _context.SaveChangesAsync();

            // Encolar email con link de recuperación
            var resetUrl = $"{AppUrl}/reset-password?token={token}";
            await _emailQueue.QueueForgotPasswordAsync(new ForgotPasswordEmailDto(
                ToEmail: user.Email,
                FullName: user.FullName,
                ResetUrl: resetUrl,
                ExpiresAt: resetRequest.ExpiresAt
            ), user.CompanyId);

            return OperationResult<bool>.Ok(true);
        }

        // ── VALIDATE RESET TOKEN ──────────────────────────────────────
        public async Task<OperationResult<bool>> ValidateResetToken(string token)
        {
            var request = await _context.PasswordResetRequests
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Token == token);

            if (request is null || request.IsUsed || request.ExpiresAt < DateTime.UtcNow)
                return OperationResult<bool>.ValidationError(
                    "El enlace de recuperación es inválido o ha expirado.",
                    ErrorKeys.InvalidResetToken);

            return OperationResult<bool>.Ok(true);
        }

        // ── RESET PASSWORD ────────────────────────────────────────────
        public async Task<OperationResult<bool>> ResetPassword(ResetPasswordDto dto)
        {
            var request = await _context.PasswordResetRequests
                .IgnoreQueryFilters()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == dto.Token);

            if (request is null || request.IsUsed || request.ExpiresAt < DateTime.UtcNow)
                return OperationResult<bool>.ValidationError(
                    "El enlace de recuperación es inválido o ha expirado.",
                    ErrorKeys.InvalidResetToken);

            var passwordError = PasswordValidator.Validate(dto.NewPassword);
            if (passwordError is not null)
                return OperationResult<bool>.ValidationError(
                    passwordError, ErrorKeys.WeakPassword);

            request.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            request.User.MustChangePassword = false;
            request.IsUsed = true; // invalidar token — no reutilizable

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

            // BranchId solo para roles que requieren Branch asignada
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
                MustChangePassword: user.MustChangePassword
            );

        // ── Helpers de localización por país ──────────────────────────
        // Valores por defecto razonables para los países objetivo del SaaS.
        // El admin puede ajustar desde BranchLocale en cualquier momento.

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