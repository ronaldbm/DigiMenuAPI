using AppCore.Application.Common;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Application.Services
{
    /// <summary>
    /// Implementación de ICacheService usando OutputCache de ASP.NET Core.
    ///
    /// Garantiza que la invalidación sea siempre por tenant:
    ///   - Datos de Branch → invalida solo esa Branch (menu-branch:{id})
    ///   - Datos de catálogo global → invalida toda la Company (menu-company:{id})
    ///
    /// Nunca invalida el cache de otros tenants, independientemente de quién
    /// haga el cambio. Esto es crítico para la escalabilidad multitenant.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IOutputCacheStore _store;

        public CacheService(IOutputCacheStore store)
        {
            _store = store;
        }

        /// <inheritdoc/>
        public async Task EvictMenuByBranchAsync(int branchId, CancellationToken ct = default)
            => await _store.EvictByTagAsync(CacheKeys.MenuBranch(branchId), ct);

        /// <inheritdoc/>
        public async Task EvictMenuByCompanyAsync(int companyId, CancellationToken ct = default)
            => await _store.EvictByTagAsync(CacheKeys.MenuCompany(companyId), ct);
    }
}