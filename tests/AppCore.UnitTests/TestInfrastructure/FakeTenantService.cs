using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace AppCore.UnitTests.TestInfrastructure;

/// <summary>
/// Implementación de ITenantService para tests.
/// Elimina la dependencia de HttpContext/JWT para que los tests sean
/// puramente unitarios y configurables mediante constructor.
/// </summary>
public sealed class FakeTenantService : ITenantService
{
    private readonly int _companyId;
    private readonly int? _branchId;
    private readonly int _userId;
    private readonly byte _role;

    // DbContext para ValidateBranchOwnershipAsync (opcional — si es null, se usa lógica sin BD)
    private readonly CoreDbContext? _context;

    public FakeTenantService(
        int companyId = 1,
        int? branchId = null,
        int userId = 1,
        byte role = UserRoles.CompanyAdmin,
        CoreDbContext? context = null)
    {
        _companyId = companyId;
        _branchId  = branchId;
        _userId    = userId;
        _role      = role;
        _context   = context;
    }

    // ── Empresa ────────────────────────────────────────────────────────────

    public int GetCompanyId() => _companyId;

    public int? TryGetCompanyId() => _companyId;

    // ── Sucursal ───────────────────────────────────────────────────────────

    public int? TryGetBranchId() => _branchId;

    public int GetBranchId()
    {
        if (_branchId is null)
            throw new UnauthorizedAccessException(
                "Este endpoint requiere un usuario asignado a una sucursal específica.");
        return _branchId.Value;
    }

    // ── Usuario ────────────────────────────────────────────────────────────

    public int GetUserId() => _userId;

    public int? TryGetUserId() => _userId;

    public byte GetUserRole() => _role;

    // ── Resolución pública ─────────────────────────────────────────────────

    public Task<(int? BranchId, int? CompanyId)> ResolveBySlugAsync(
        string companySlug, string branchSlug)
    {
        // En tests unitarios no se usa este método — devuelve los valores configurados.
        return Task.FromResult<(int? BranchId, int? CompanyId)>((_branchId, _companyId));
    }

    // ── Validación de propiedad de Branch ──────────────────────────────────

    /// <summary>
    /// Replica el comportamiento exacto de TenantService.ValidateBranchOwnershipAsync:
    ///   - PlatformLevel (SuperAdmin/SuperAdminCompany): siempre permite.
    ///   - BranchAdmin/Staff: solo permite si el branchId coincide con el suyo.
    ///   - CompanyAdmin: valida que la Branch pertenezca a su Company (en BD si hay context,
    ///     o lanza si no hay context y la branch no coincide con companyId).
    /// </summary>
    public async Task ValidateBranchOwnershipAsync(int branchId)
    {
        // SuperAdmin/SuperAdminCompany → siempre OK
        if (UserRoles.IsPlatformLevel(_role)) return;

        // BranchAdmin/Staff → solo su propia branch
        if (UserRoles.NeedsBranch(_role))
        {
            if (_branchId.HasValue && _branchId.Value != branchId)
                throw new UnauthorizedAccessException(
                    "No tienes permiso para operar sobre esta sucursal.");
            return;
        }

        // CompanyAdmin → la branch debe pertenecer a la company
        if (_context is not null)
        {
            var belongs = await _context.Branches
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(b => b.Id == branchId && b.CompanyId == _companyId);

            if (!belongs)
                throw new UnauthorizedAccessException(
                    "La sucursal no pertenece a tu empresa o no existe.");
        }
    }
}
