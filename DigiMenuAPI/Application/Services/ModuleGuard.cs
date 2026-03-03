using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DigiMenuAPI.Application.Services
{
    /// <summary>
    /// Verifica acceso a módulos premium.
    /// Usa IMemoryCache de corta duración para evitar queries repetidas
    /// en la misma sesión de usuario.
    /// </summary>
    public class ModuleGuard : IModuleGuard
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ModuleGuard(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> HasModuleAsync(int companyId, string moduleCode)
        {
            var cacheKey = $"module:{companyId}:{moduleCode}";

            if (_cache.TryGetValue(cacheKey, out bool cached))
                return cached;

            var hasModule = await _context.CompanyModules
                .AsNoTracking()
                .AnyAsync(cm =>
                    cm.CompanyId == companyId &&
                    cm.PlatformModule.Code == moduleCode &&
                    cm.IsActive &&
                    (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));

            _cache.Set(cacheKey, hasModule, CacheDuration);
            return hasModule;
        }

        public async Task AssertModuleAsync(int companyId, string moduleCode)
        {
            if (!await HasModuleAsync(companyId, moduleCode))
                throw new ModuleNotActiveException(moduleCode);
        }

        /// <summary>Invalida el cache de módulos para una empresa (usar al activar/desactivar).</summary>
        public void InvalidateCache(int companyId, string moduleCode)
        {
            _cache.Remove($"module:{companyId}:{moduleCode}");
        }
    }
}
