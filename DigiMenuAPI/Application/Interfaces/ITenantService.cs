namespace DigiMenuAPI.Application.Interfaces
{
    public interface ITenantService
    {
        /// <summary>Devuelve CompanyId del JWT. Lanza UnauthorizedAccessException si no hay claim.</summary>
        int GetCompanyId();

        /// <summary>Devuelve CompanyId o null. Para endpoints que pueden ser públicos.</summary>
        int? TryGetCompanyId();

        /// <summary>Devuelve el rol del usuario autenticado (1=SuperAdmin, 2=Admin, 3=Staff).</summary>
        byte GetUserRole();

        /// <summary>
        /// Resuelve el CompanyId a partir del slug de la empresa.
        /// Usado en endpoints públicos donde no hay JWT (ej: reservas desde el menú público).
        /// </summary>
        Task<int?> ResolveCompanyBySlugAsync(string slug);
    }
}