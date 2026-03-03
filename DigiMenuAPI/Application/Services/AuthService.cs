using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;
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
        private readonly IConfiguration      _config;
        private readonly ITenantService      _tenantService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration config,
            ITenantService tenantService)
        {
            _context       = context;
            _config        = config;
            _tenantService = tenantService;
        }

        // ── REGISTER COMPANY ────────────────────────────────────────
        public async Task<OperationResult<AuthResultDto>> RegisterCompany(RegisterCompanyDto dto)
        {
            var slug  = dto.Slug.ToLower().Trim();
            var email = dto.AdminEmail.Trim().ToLower();

            if (await _context.Companies.AnyAsync(c => c.Slug == slug))
                return OperationResult<AuthResultDto>.Fail("El slug ya está en uso. Elige otro identificador.");

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
                return OperationResult<AuthResultDto>.Fail("El email del administrador ya está registrado.");

            // Crear empresa
            var company = new Company
            {
                Name        = dto.CompanyName.Trim(),
                Slug        = slug,
                Email       = dto.CompanyEmail.Trim().ToLower(),
                Phone       = dto.CompanyPhone?.Trim(),
                CountryCode = dto.CountryCode?.ToUpper().Trim(),
                IsActive    = true
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Configuración por defecto (Setting 1:1)
            var setting = new Setting
            {
                CompanyId            = company.Id,
                BusinessName         = dto.CompanyName.Trim(),
                PrimaryColor         = "#E63946",
                SecondaryColor       = "#457B9D",
                PageBackgroundColor  = "#F1FAEE",
                HeaderBackgroundColor = "#FFFFFF",
                HeaderTextColor      = "#1D3557",
                TabBackgroundColor   = "#1D3557",
                TabTextColor         = "#FFFFFF",
                PrimaryTextColor     = "#FFFFFF",
                TitlesColor          = "#1D3557",
                TextColor            = "#1D3557",
                BrowserThemeColor    = "#FFFFFF",
                ShowProductDetails   = true,
                ProductDisplay       = 1,
                CountryCode          = dto.CountryCode ?? "CO",
                Currency             = "USD",
                CurrencyLocale       = "en-US",
                Language             = "ES",
                TimeZone             = "America/Bogota",
                Decimals             = 2
            };
            _context.Settings.Add(setting);

            // Primer admin
            var admin = new AppUser
            {
                FullName     = dto.AdminFullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
                Role         = 2,
                CompanyId    = company.Id,
                IsActive     = true
            };
            _context.Users.Add(admin);

            await _context.SaveChangesAsync();

            var token = GenerateJwt(admin, company);
            return OperationResult<AuthResultDto>.Ok(BuildResult(admin, company, token));
        }

        // ── LOGIN ───────────────────────────────────────────────────
        public async Task<OperationResult<AuthResultDto>> Login(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return OperationResult<AuthResultDto>.Fail("Credenciales incorrectas.");

            if (!user.IsActive)
                return OperationResult<AuthResultDto>.Fail("Tu cuenta está desactivada.");

            if (!user.Company.IsActive)
                return OperationResult<AuthResultDto>.Fail("Tu empresa está desactivada. Contacta al soporte.");

            var token = GenerateJwt(user, user.Company);
            return OperationResult<AuthResultDto>.Ok(BuildResult(user, user.Company, token));
        }

        // ── REGISTER USER (admin crea staff) ────────────────────────
        public async Task<OperationResult<bool>> RegisterUser(RegisterUserDto dto)
        {
            var companyId = _tenantService.GetCompanyId();
            var email     = dto.Email.Trim().ToLower();

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
                return OperationResult<bool>.Fail("El email ya está registrado.");

            // Solo Admin puede crear usuarios, y no puede crear SuperAdmins
            if (dto.Role == 1)
                return OperationResult<bool>.Fail("No puedes asignar el rol SuperAdmin.");

            var user = new AppUser
            {
                FullName     = dto.FullName.Trim(),
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role         = dto.Role,
                CompanyId    = companyId,
                IsActive     = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── JWT ─────────────────────────────────────────────────────
        private string GenerateJwt(AppUser user, Company company)
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var hours   = double.Parse(_config["Jwt:ExpiresHours"] ?? "8");
            var expires = DateTime.UtcNow.AddHours(hours);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim("companyId",   company.Id.ToString()),
                new Claim("companySlug", company.Slug),
                new Claim("role",        user.Role.ToString()),
                new Claim("fullName",    user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer:            _config["Jwt:Issuer"],
                audience:          _config["Jwt:Audience"],
                claims:            claims,
                expires:           expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static AuthResultDto BuildResult(AppUser user, Company company, string token) =>
            new(
                Token:       token,
                FullName:    user.FullName,
                Email:       user.Email,
                CompanyId:   company.Id,
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                Role:        user.Role,
                ExpiresAt:   DateTime.UtcNow.AddHours(8)
            );
    }
}
