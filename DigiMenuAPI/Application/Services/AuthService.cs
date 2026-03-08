using BCrypt.Net;
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

            // Slug único globalmente a nivel de Company (define el subdominio)
            if (await _context.Companies.AnyAsync(c => c.Slug == companySlug))
                return OperationResult<LoginResponseDto>.Fail(
                    "El slug ya está en uso. Elige otro identificador.");

            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email))
                return OperationResult<LoginResponseDto>.Fail(
                    "El email del administrador ya está registrado.");

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

            // 2. Crear Branch principal.
            //    SlugHelper.GenerateUnique garantiza unicidad dentro de la Company.
            //    La Company recién creada no tiene branches, pero se usa el helper
            //    por consistencia y para reutilizar la misma lógica en BranchService.
            var existingBranchSlugs = await _context.Branches
                .Where(b => b.CompanyId == company.Id)
                .Select(b => b.Slug)
                .ToListAsync();

            var branchSlug = SlugHelper.GenerateUnique("principal", existingBranchSlugs);

            var branch = new Branch
            {
                CompanyId = company.Id,
                Name = dto.Name.Trim(),
                Slug = branchSlug,     // → {companySlug}.digimenu.cr/principal
                IsActive = true
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // 3. Crear BranchInfo — identidad del negocio
            _context.BranchInfos.Add(new BranchInfo
            {
                BranchId = branch.Id,
                BusinessName = dto.Name.Trim()
                // Tagline, Logo, Favicon, BackgroundImage → el admin los completa luego
            });

            // 4. Crear BranchTheme — tema visual con colores por defecto de DigiMenu
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

            // 5. Crear BranchLocale — configuración regional derivada del registro
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

            // 6. Crear BranchSeo — vacío, el admin lo completa luego
            _context.BranchSeos.Add(new BranchSeo
            {
                BranchId = branch.Id
                // Todos los campos son opcionales
            });

            // BranchReservationForm NO se crea aquí.
            // Se crea cuando el CompanyAdmin activa el módulo RESERVATIONS
            // desde el panel de módulos (ModuleService).

            // 7. Crear CompanyAdmin (BranchId = null → gestiona toda la empresa)
            var admin = new AppUser
            {
                FullName = "Admin " + dto.Name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
                Role = 1,    // CompanyAdmin
                CompanyId = company.Id,
                BranchId = null, // CompanyAdmin no pertenece a una Branch específica
                IsActive = true
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
                return OperationResult<LoginResponseDto>.Fail("Credenciales incorrectas.");

            if (!user.IsActive)
                return OperationResult<LoginResponseDto>.Fail(
                    "Tu cuenta está desactivada.");

            if (!user.Company.IsActive)
                return OperationResult<LoginResponseDto>.Fail(
                    "Tu empresa está desactivada. Contacta al soporte.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwt(user, user.Company);
            return OperationResult<LoginResponseDto>.Ok(BuildResult(user, user.Company, token));
        }

        // ── REGISTER USER (CompanyAdmin crea staff) ───────────────────
        public async Task<OperationResult<bool>> RegisterUser(AppUserCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();
            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == email))
                return OperationResult<bool>.ValidationError("El email ya está registrado.", errorKey: ErrorKeys.EmailAlreadyExists);

            if (dto.Role == 255)
                return OperationResult<bool>.Forbidden("No puedes asignar el rol SuperAdmin.", errorKey: ErrorKeys.CannotAssignSuperAdmin);

            // BranchAdmin y Staff deben tener BranchId
            if (dto.Role is 2 or 3 && dto.BranchId is null)
                return OperationResult<bool>.ValidationError(
                    "BranchAdmin y Staff deben estar asignados a una sucursal.", errorKey: ErrorKeys.ValidationFailed);

            // Validar que la Branch pertenece a la empresa del admin
            if (dto.BranchId.HasValue)
            {
                var branchBelongs = await _context.Branches
                    .AnyAsync(b => b.Id == dto.BranchId.Value && b.CompanyId == companyId);

                if (!branchBelongs)
                    return OperationResult<bool>.NotFound(
                        "La sucursal indicada no pertenece a tu empresa.", errorKey: ErrorKeys.BranchNotFound);
            }

            var user = new AppUser
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                CompanyId = companyId,
                BranchId = dto.BranchId,
                IsActive = true
            };
            _context.Users.Add(user);
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

            // branchId solo para BranchAdmin (2) y Staff (3)
            if (user.BranchId.HasValue)
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
                ExpiresAt: DateTime.UtcNow.AddHours(8)
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
                _ => "+506"  // CR por defecto
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