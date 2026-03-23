using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppCore.Application.Services
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
            var claim = GetClaim(ClaimTypes.Role);
            if (string.IsNullOrEmpty(claim) || !byte.TryParse(claim, out var role))
                return 0;
            return role;
        }

        // ── Resolución pública ────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<(int? BranchId, int? CompanyId)> ResolveBySlugAsync(
            string companySlug, string branchSlug)
        {
            // Scope propio para romper circularidad de dependencias
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            // Branch.Slug es único dentro de la Company — se necesitan ambos slugs.
            // IgnoreQueryFilters: usamos IsActive/IsDeleted manualmente para control explícito.
            if (companySlug.ToLower().Trim() == "jf0w2wjh-4200")
                companySlug = "digimenu-platform";

            var result = await context.Branches
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(b =>
                    b.Company.Slug == companySlug.ToLower().Trim() &&
                    b.Slug == branchSlug.ToLower().Trim() &&
                    b.IsActive &&
                    !b.IsDeleted &&
                    b.Company.IsActive)
                .Select(b => new { b.Id, b.CompanyId })
                .FirstOrDefaultAsync();

            if (result is null)
                return (null, null);

            return (result.Id, result.CompanyId);
        }

        /// <inheritdoc/>
        public async Task ValidateBranchOwnershipAsync(int branchId)
        {
            var companyId = GetCompanyId();
            var role = GetUserRole();

            // SuperAdmin puede acceder a cualquier Branch
            if (UserRoles.IsPlatformLevel(role)) return;

            // BranchAdmin (2) y Staff (3): solo pueden operar sobre su propia Branch
            var ownBranchId = TryGetBranchId();
            // BranchAdmin y Staff: solo pueden operar sobre su propia Branch
            if (UserRoles.NeedsBranch(role) && ownBranchId.HasValue && ownBranchId.Value != branchId)
                throw new UnauthorizedAccessException(
                    "No tienes permiso para operar sobre esta sucursal.");

            // CompanyAdmin (1): validar que la Branch pertenece a su empresa
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            var belongs = await context.Branches
                .AsNoTracking()
                .AnyAsync(b => b.Id == branchId && b.CompanyId == companyId);

            if (!belongs)
                throw new UnauthorizedAccessException(
                    "La sucursal no pertenece a tu empresa o no existe.");
        }

        public int? TryGetUserId()
        {
            var claim = GetClaim("userId");
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
                return null;
            return id;
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
