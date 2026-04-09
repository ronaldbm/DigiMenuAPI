using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DigiMenuAPI.Application.Services
{
    /// <summary>
    /// Gestión segura de tokens de impersonación.
    ///
    /// Flujo completo:
    ///   1. SuperAdmin llama POST /api/superadmin/impersonate/{companyId}
    ///   2. Se genera un token de 32 bytes aleatorios (criptográficamente seguro)
    ///   3. Se guarda SHA-256(token) en ImpersonationSession, nunca el token en claro
    ///   4. Se retorna el token en claro al SuperAdmin (única vez que existe en claro)
    ///   5. DigiMenuAdmin abre DigiMenuWeb con el token en el URL fragment (#imp_token=...)
    ///   6. DigiMenuWeb llama POST /api/auth/impersonate/exchange con el token en body
    ///   7. Se valida: hash coincide + no expirado + no usado
    ///   8. Se marca UsedAt en transacción atómica (one-time use garantizado)
    ///   9. Se emite JWT del CompanyAdmin del tenant con claim "imp_by"
    /// </summary>
    public class SuperAdminImpersonationService : ISuperAdminImpersonationService
    {
        private const int MasterCompanyId = 1;
        private const int TokenTtlMinutes = 30;

        private readonly ApplicationDbContext _context;
        private readonly AppCore.Application.Interfaces.ITenantService _tenantService;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _http;

        public SuperAdminImpersonationService(
            ApplicationDbContext context,
            AppCore.Application.Interfaces.ITenantService tenantService,
            IConfiguration config,
            IHttpContextAccessor http)
        {
            _context = context;
            _tenantService = tenantService;
            _config = config;
            _http = http;
        }

        // ── CREATE TOKEN ──────────────────────────────────────────────
        public async Task<OperationResult<ImpersonationTokenDto>> CreateToken(int companyId)
        {
            if (companyId == MasterCompanyId)
                return OperationResult<ImpersonationTokenDto>.Forbidden(
                    "No se puede impersonar la empresa maestra.",
                    ErrorKeys.Forbidden);

            var superAdminId = _tenantService.GetUserId();

            // Obtener el primer CompanyAdmin activo del tenant
            var targetUser = await _context.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(u => u.Company)
                .Where(u => u.CompanyId == companyId &&
                             u.Role == UserRoles.CompanyAdmin &&
                             u.IsActive &&
                             !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (targetUser is null)
                return OperationResult<ImpersonationTokenDto>.NotFound(
                    "No se encontró un CompanyAdmin activo en el tenant.",
                    ErrorKeys.NoCompanyAdminAvailable);

            // Generar token criptográficamente seguro (32 bytes = 256 bits)
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var tokenPlain = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe Base64

            var tokenHash = ComputeSha256(tokenPlain);
            var now = DateTime.UtcNow;
            var ip = GetClientIp();

            var session = new ImpersonationSession
            {
                SuperAdminUserId = superAdminId,
                TargetCompanyId  = companyId,
                TargetUserId     = targetUser.Id,
                TokenHash        = tokenHash,
                IssuedAt         = now,
                ExpiresAt        = now.AddMinutes(TokenTtlMinutes),
                UsedAt           = null,
                IpAddress        = ip,
                CreatedAt        = now
            };

            _context.ImpersonationSessions.Add(session);
            await _context.SaveChangesAsync();

            return OperationResult<ImpersonationTokenDto>.Ok(new ImpersonationTokenDto(
                Token: tokenPlain,
                TargetCompanyId: companyId,
                TargetCompanyName: targetUser.Company.Name,
                TargetCompanySlug: targetUser.Company.Slug,
                ExpiresAt: session.ExpiresAt
            ));
        }

        // ── EXCHANGE TOKEN ────────────────────────────────────────────
        public async Task<OperationResult<LoginResponseDto>> ExchangeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return OperationResult<LoginResponseDto>.Forbidden(
                    "Token de impersonación inválido.",
                    ErrorKeys.ImpersonationTokenInvalid);

            var tokenHash = ComputeSha256(token.Trim());
            var now = DateTime.UtcNow;

            // Cargar la sesión con todas las relaciones necesarias en una sola consulta
            var session = await _context.ImpersonationSessions
                .Include(s => s.TargetUser)
                    .ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash);

            // Respuesta deliberadamente genérica para no revelar por qué falló
            static OperationResult<LoginResponseDto> Invalid() =>
                OperationResult<LoginResponseDto>.Forbidden(
                    "El token de acceso no es válido o ya fue utilizado.",
                    ErrorKeys.ImpersonationTokenInvalid);

            if (session is null)             return Invalid();
            if (session.UsedAt.HasValue)     return Invalid(); // one-time use
            if (now > session.ExpiresAt)     return Invalid(); // expirado

            // Marcar como usado (transacción atómica — si SaveChanges falla, no se emite JWT)
            session.UsedAt = now;
            await _context.SaveChangesAsync();

            var targetUser = session.TargetUser;
            var company    = targetUser.Company;

            // Emitir JWT del CompanyAdmin del tenant con claim "imp_by"
            var jwt = GenerateImpersonationJwt(targetUser, company, session.SuperAdminUserId);

            return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto(
                Token: jwt,
                UserId: targetUser.Id,
                FullName: targetUser.FullName,
                Email: targetUser.Email,
                CompanyId: company.Id,
                CompanyName: company.Name,
                CompanySlug: company.Slug,
                BranchId: null,
                BranchName: null,
                Role: targetUser.Role,
                ExpiresAt: now.AddMinutes(TokenTtlMinutes),
                MustChangePassword: false,
                AdminLang: targetUser.AdminLang
            ));
        }

        // ── Helpers ───────────────────────────────────────────────────

        private string GenerateImpersonationJwt(AppUser user, Company company, int impersonatedBy)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new("userId",      user.Id.ToString()),
                new("companyId",   company.Id.ToString()),
                new("companySlug", company.Slug),
                new("role",        user.Role.ToString()),
                new("fullName",    user.FullName),
                // Claim que identifica esta como sesión de impersonación
                // El frontend DigiMenuWeb lo usa para mostrar el banner de soporte
                new("imp_by",      impersonatedBy.ToString())
            };

            var jwtToken = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                // El JWT de impersonación dura exactamente 30 minutos, no renovable
                expires:  DateTime.UtcNow.AddMinutes(TokenTtlMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant(); // 64 chars hex
        }

        private string GetClientIp()
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return "unknown";

            // Respetar proxy headers (X-Forwarded-For en entornos con load balancer)
            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',').First().Trim();

            return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
