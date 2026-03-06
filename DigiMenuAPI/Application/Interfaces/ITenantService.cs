namespace DigiMenuAPI.Application.Interfaces
{
    public interface ITenantService
    {
        // ── Empresa ───────────────────────────────────────────────────

        /// <summary>Devuelve CompanyId del JWT. Lanza UnauthorizedAccessException si no hay claim.</summary>
        int GetCompanyId();

        /// <summary>Devuelve CompanyId o null. Para endpoints que pueden ser públicos.</summary>
        int? TryGetCompanyId();

        // ── Sucursal ──────────────────────────────────────────────────

        /// <summary>
        /// Devuelve BranchId del JWT o null.
        /// CompanyAdmin (role=1) y SuperAdmin (role=255) → null.
        /// BranchAdmin (role=2) y Staff (role=3) → siempre tienen valor.
        /// </summary>
        int? TryGetBranchId();

        /// <summary>
        /// Devuelve BranchId del JWT. Lanza UnauthorizedAccessException si no existe.
        /// Usar solo en endpoints exclusivos de BranchAdmin/Staff.
        /// </summary>
        int GetBranchId();

        // ── Usuario ───────────────────────────────────────────────────

        /// <summary>Devuelve el UserId del JWT autenticado. Lanza si no existe.</summary>
        int GetUserId();

        /// <summary>Devuelve el rol del usuario autenticado (255=SuperAdmin, 1=CompanyAdmin, 2=BranchAdmin, 3=Staff).</summary>
        byte GetUserRole();

        // ── Resolución pública ────────────────────────────────────────

        /// <summary>
        /// Resuelve el CompanyId a partir del slug de la Branch.
        /// Usado en endpoints públicos sin JWT (ej: menú público, reservas públicas).
        /// </summary>
        Task<(int? BranchId, int? CompanyId)> ResolveByBranchSlugAsync(string slug);

        /// <summary>
        /// Verifica que una Branch pertenece a la empresa del usuario autenticado.
        /// BranchAdmin/Staff adicionalmente verifican que sea su propia Branch asignada.
        /// Lanza UnauthorizedAccessException si no pasa la validación.
        /// </summary>
        Task ValidateBranchOwnershipAsync(int branchId);
    }
}