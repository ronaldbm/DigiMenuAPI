using DigiMenuAPI.Application.Common;
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

        public AuthService(
            ApplicationDbContext context,
            IConfiguration config,
            ITenantService tenantService)
        {
            _context = context;
            _config = config;
            _tenantService = tenantService;
        }

        // ── REGISTER COMPANY ─────────────────────────────────────────
        public async Task<OperationResult<LoginResponseDto>> RegisterCompany(CompanyCreateDto dto)
        {
            var companySlug = dto.Slug.ToLower().Trim();
            var email = dto.Email.Trim().ToLower();

            // Slug único a nivel de Company
            if (await _context.Companies.AnyAsync(c => c.Slug == companySlug))
                return OperationResult<LoginResponseDto>.Fail("El slug ya está en uso. Elige otro identificador.");

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
                return OperationResult<LoginResponseDto>.Fail("El email del administrador ya está registrado.");

            // 1. Crear Company
            var company = new Company
            {
                Name = dto.Name.Trim(),
                Slug = companySlug,
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone?.Trim(),
                CountryCode = dto.CountryCode?.ToUpper().Trim(),
                IsActive = true
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // 2. Crear Branch principal (el slug de la branch es igual al de la company al inicio)
            //    Cada Branch tiene su propio slug para la URL pública del menú.
            var branchSlug = companySlug; // el admin puede cambiarlo luego si necesita
            if (await _context.Branches.AnyAsync(b => b.Slug == branchSlug))
                branchSlug = $"{companySlug}-1"; // fallback si ya existe

            var branch = new Branch
            {
                CompanyId = company.Id,
                Name = dto.Name.Trim(),
                Slug = branchSlug,
                IsActive = true
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // 3. Crear Setting para la Branch principal (1:1 con Branch, usa BranchId)
            var setting = new Setting
            {
                BranchId = branch.Id,
                BusinessName = dto.Name.Trim(),
                PrimaryColor = "#E63946",
                SecondaryColor = "#457B9D",
                PageBackgroundColor = "#F1FAEE",
                HeaderBackgroundColor = "#FFFFFF",
                HeaderTextColor = "#1D3557",
                TabBackgroundColor = "#1D3557",
                TabTextColor = "#FFFFFF",
                PrimaryTextColor = "#FFFFFF",
                TitlesColor = "#1D3557",
                TextColor = "#1D3557",
                BrowserThemeColor = "#FFFFFF",
                ShowProductDetails = true,
                ProductDisplay = 1,
                CountryCode = dto.CountryCode?.ToUpper() ?? "CR",
                PhoneCode = "+506",
                Currency = "CRC",
                CurrencyLocale = "es-CR",
                Language = "es",
                TimeZone = "America/Costa_Rica",
                Decimals = 2
            };
            _context.Settings.Add(setting);

            // 4. Crear CompanyAdmin (BranchId = null → gestiona toda la empresa)
            var admin = new AppUser
            {
                FullName = "Admin " + dto.Name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
                Role = 1,      // CompanyAdmin
                CompanyId = company.Id,
                BranchId = null,   // CompanyAdmin no pertenece a una Branch específica
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
                return OperationResult<LoginResponseDto>.Fail("Tu cuenta está desactivada.");

            if (!user.Company.IsActive)
                return OperationResult<LoginResponseDto>.Fail("Tu empresa está desactivada. Contacta al soporte.");

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

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
                return OperationResult<bool>.Fail("El email ya está registrado.");

            if (dto.Role == 255)
                return OperationResult<bool>.Fail("No puedes asignar el rol SuperAdmin.");

            // BranchAdmin y Staff deben tener BranchId
            if (dto.Role is 2 or 3 && dto.BranchId is null)
                return OperationResult<bool>.Fail("BranchAdmin y Staff deben estar asignados a una sucursal.");

            // Validar que la Branch pertenece a la empresa del admin
            if (dto.BranchId.HasValue)
            {
                var branchBelongs = await _context.Branches
                    .AnyAsync(b => b.Id == dto.BranchId.Value && b.CompanyId == companyId);
                if (!branchBelongs)
                    return OperationResult<bool>.Fail("La sucursal indicada no pertenece a tu empresa.");
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
            var expires = DateTime.UtcNow.AddHours(hours);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new("userId",        user.Id.ToString()),
                new("companyId",     company.Id.ToString()),
                new("companySlug",   company.Slug),
                new("role",          user.Role.ToString()),
                new("fullName",      user.FullName)
            };

            // branchId solo se agrega para BranchAdmin (2) y Staff (3)
            if (user.BranchId.HasValue)
                claims.Add(new Claim("branchId", user.BranchId.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static LoginResponseDto BuildResult(AppUser user, Company company, string token) =>
            new LoginResponseDto(
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
    }
}