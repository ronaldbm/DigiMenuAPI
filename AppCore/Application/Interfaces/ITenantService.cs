namespace AppCore.Application.Interfaces
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
        /// Resuelve BranchId y CompanyId a partir de companySlug + branchSlug.
        /// Usado en endpoints públicos sin JWT (menú público, reservas públicas).
        ///
        /// El slug de Branch es único dentro de la Company, no globalmente.
        /// Por eso se necesitan ambos slugs para resolver sin ambigüedad.
        /// </summary>
        Task<(int? BranchId, int? CompanyId)> ResolveBySlugAsync(string companySlug, string branchSlug);

        /// <summary>
        /// Verifica que una Branch pertenece a la empresa del usuario autenticado.
        /// BranchAdmin/Staff adicionalmente verifican que sea su propia Branch asignada.
        /// Lanza UnauthorizedAccessException si no pasa la validación.
        /// </summary>
        Task ValidateBranchOwnershipAsync(int branchId);

        /// <summary>
        /// Devuelve el UserId del JWT o null si no hay sesión activa.
        /// Para contextos donde el usuario puede ser anónimo.
        /// </summary>
        int? TryGetUserId();
    }
}
