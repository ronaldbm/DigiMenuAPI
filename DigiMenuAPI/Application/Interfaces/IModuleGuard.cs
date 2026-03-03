namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Verifica si una empresa tiene un módulo premium activo.
    /// Usado tanto en filtros de acción como en servicios.
    /// </summary>
    public interface IModuleGuard
    {
        /// <summary>
        /// Devuelve true si la empresa tiene el módulo activo y no expirado.
        /// </summary>
        Task<bool> HasModuleAsync(int companyId, string moduleCode);

        /// <summary>
        /// Lanza ModuleNotActiveException si la empresa no tiene el módulo.
        /// </summary>
        Task AssertModuleAsync(int companyId, string moduleCode);
    }
}