using AppCore.Application.Common;
using AppCore.Application.DTOs.Email;
using AppCore.Application.Interfaces;
using AppCore.Application.Utils;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
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
            var plan = await _context.Plans.FindAsync(dto.PlanId);
            if (plan is null)
                return OperationResult<LoginResponseDto>.NotFound(
                    "El plan seleccionado no existe.", ErrorKeys.CompanyNotFound);

            var company = new Company
            {
                Name = dto.Name.Trim(),
                Slug = companySlug,
                Email = email,
                Phone = dto.Phone?.Trim(),
                CountryCode = dto.CountryCode?.ToUpper().Trim(),
                IsActive = true,
                PlanId = plan.Id,
                MaxBranches = dto.MaxBranches ?? plan.MaxBranches,
                MaxUsers = dto.MaxUsers ?? plan.MaxUsers
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

            // 3. Crear CompanyInfo
            _context.CompanyInfos.Add(new CompanyInfo
            {
                CompanyId = company.Id,
                BusinessName = dto.Name.Trim()
            });

            // 4. Crear CompanyTheme — valores por defecto de la paleta DigiMenu
            _context.CompanyThemes.Add(new CompanyTheme
            {
                CompanyId = company.Id,
                IsDarkMode = false,
                DarkModeAutoGenerate = true,
                ColorPalette = new ColorPaletteData
                {
                    PageBackgroundColor   = DefaultTheme.PageBackground,
                    HeaderBackgroundColor = DefaultTheme.HeaderBackground,
                    HeaderTextColor       = DefaultTheme.HeaderText,
                    TabBackgroundColor    = DefaultTheme.TabBackground,
                    TabTextColor          = DefaultTheme.TabText,
                    PrimaryColor          = DefaultTheme.Primary,
                    PrimaryTextColor      = DefaultTheme.PrimaryText,
                    SecondaryColor        = DefaultTheme.Secondary,
                    TitlesColor           = DefaultTheme.Titles,
                    TextColor             = DefaultTheme.Text,
                    BrowserThemeColor     = DefaultTheme.BrowserTheme,
                    CardBackgroundColor   = DefaultTheme.CardBackground,
                    CardBorderColor       = DefaultTheme.CardBorder,
                    FooterBackgroundColor = DefaultTheme.FooterBackground
                },
                BackgroundSettings = new BackgroundSettingsData(),
                FrameSettings      = new FrameSettingsData(),
                HeaderStyle        = 1,
                MenuLayout         = 1,
                ProductDisplay     = 1,
                CategoryHeaderStyle = 1,
                ShowProductDetails  = true,
                ShowCategoryImages  = true,
                FilterMode          = 0,
                ShowContactButton   = false
            });

            // 5. Crear CompanySeo — vacío, el admin lo completa después
            _context.CompanySeos.Add(new CompanySeo
            {
                CompanyId = company.Id
            });

            // 6. Crear BranchLocale — valores derivados del país registrado
            _context.BranchLocales.Add(
                BranchLocaleInitializer.Create(branch.Id, dto.CountryCode));

            // 7. Inicializar horario semanal de la Branch con defaults
            //    El admin puede ajustar cada día desde el panel de configuración
            _context.BranchSchedules.AddRange(
                BranchScheduleInitializer.Generate(branch.Id));

            // 8. Crear CompanyAdmin
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

            // 9. Encolar email de bienvenida
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

            // Crear nuevo token — Guid sin guiones, 32 chars, no predecible
            var token = Guid.NewGuid().ToString("N");
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
            request.IsUsed = true;

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

            var jwtToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(hours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        private static LoginResponseDto BuildResult(
            AppUser user, Company company, string token) =>
            new(
                Token: token,
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                CompanyId: company.Id,
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                BranchId: user.BranchId,
                BranchName: user.BranchId.HasValue ? user.Branch?.Name : null,
                Role: user.Role,
                ExpiresAt: DateTime.UtcNow.AddHours(8),
                MustChangePassword: user.MustChangePassword,
                AdminLang: user.AdminLang
            );
    }
}