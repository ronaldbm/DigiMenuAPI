using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DigiMenuAPI.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        // ── Empresa ───────────────────────────────────────────────────

        public int GetCompanyId()
        {
            var value = TryGetCompanyId();
            if (value is null)
                throw new UnauthorizedAccessException(
                    "No se pudo determinar la empresa del usuario autenticado.");
            return value.Value;
        }

        public int? TryGetCompanyId()
        {
            var claim = GetClaim("companyId");
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                return null;
            return id;
        }

        // ── Sucursal ──────────────────────────────────────────────────

        public int? TryGetBranchId()
        {
            var claim = GetClaim("branchId");
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                return null;
            return id;
        }

        public int GetBranchId()
        {
            var value = TryGetBranchId();
            if (value is null)
                throw new UnauthorizedAccessException(
                    "Este endpoint requiere un usuario asignado a una sucursal específica.");
            return value.Value;
        }

        // ── Usuario ───────────────────────────────────────────────────

        public int GetUserId()
        {
            var claim = GetClaim("userId");
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException(
                    "No se pudo determinar el usuario autenticado.");
            return id;
        }

        public byte GetUserRole()
        {
            var claim = GetClaim("role");
            if (string.IsNullOrEmpty(claim) || !byte.TryParse(claim, out var role))
                return 0;
            return role;
        }

        // ── Resolución pública ────────────────────────────────────────

        public async Task<(int? BranchId, int? CompanyId)> ResolveByBranchSlugAsync(string slug)
        {
            // Scope propio para romper circularidad de dependencias
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Branch.Slug es el identificador público del menú
            var result = await context.Branches
                .AsNoTracking()
                .Where(b => b.Slug == slug.ToLower().Trim() && b.IsActive)
                .Select(b => new { b.Id, b.CompanyId })
                .FirstOrDefaultAsync();

            if (result is null)
                return (null, null);

            return (result.Id, result.CompanyId);
        }

        public async Task ValidateBranchOwnershipAsync(int branchId)
        {
            var companyId = GetCompanyId();
            var role = GetUserRole();

            // SuperAdmin puede acceder a cualquier Branch
            if (role == 255) return;

            // BranchAdmin (2) y Staff (3): solo pueden operar sobre su propia Branch
            var ownBranchId = TryGetBranchId();
            if (ownBranchId.HasValue && ownBranchId.Value != branchId)
                throw new UnauthorizedAccessException(
                    "No tienes permiso para operar sobre esta sucursal.");

            // CompanyAdmin (1): validar que la Branch pertenece a su empresa
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // IgnoreQueryFilters porque usamos IsActive en lugar de IsDeleted para branches activas
            var belongs = await context.Branches
                .AsNoTracking()
                .AnyAsync(b => b.Id == branchId && b.CompanyId == companyId);

            if (!belongs)
                throw new UnauthorizedAccessException(
                    "La sucursal no pertenece a tu empresa o no existe.");
        }

        // ── Helpers ───────────────────────────────────────────────────

        private string? GetClaim(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || user.Identity?.IsAuthenticated != true)
                return null;
            return user.FindFirstValue(claimType);
        }
    }
}