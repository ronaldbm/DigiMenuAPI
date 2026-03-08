namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Abstracción del cache de invalidación de OutputCache.
    ///
    /// Todos los servicios que modifiquen datos visibles en el menú público
    /// deben usar este contrato para invalidar el cache del tenant afectado,
    /// nunca el cache global.
    ///
    /// Regla de uso:
    ///   - Cambios en Branch (Setting, FooterLinks, BranchProducts) → EvictMenuByBranchAsync
    ///   - Cambios en catálogo global (Category, Product, Tag)      → EvictMenuByCompanyAsync
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Invalida el menú público de una Branch específica.
        /// No afecta el cache de otras Branches ni de otras Companies.
        /// </summary>
        Task EvictMenuByBranchAsync(int branchId, CancellationToken ct = default);

        /// <summary>
        /// Invalida el menú público de todas las Branches de una Company.
        /// Usar cuando cambia el catálogo global (Category, Product, Tag).
        /// </summary>
        Task EvictMenuByCompanyAsync(int companyId, CancellationToken ct = default);
    }
}