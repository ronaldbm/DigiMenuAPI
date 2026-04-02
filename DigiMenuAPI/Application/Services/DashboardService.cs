using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DigiMenuAPI.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan StatsCacheDuration = TimeSpan.FromSeconds(60);

        public DashboardService(ApplicationDbContext context, ITenantService tenantService, IMemoryCache cache)
        {
            _context = context;
            _tenantService = tenantService;
            _cache = cache;
        }

        public async Task<OperationResult<DashboardStatsDto>> GetStats()
        {
            var companyId = _tenantService.GetCompanyId();
            var cacheKey = $"dashboard-stats-{companyId}";

            if (_cache.TryGetValue(cacheKey, out DashboardStatsDto? cached) && cached is not null)
                return OperationResult<DashboardStatsDto>.Ok(cached);

            var products   = await _context.Products.AsNoTracking().CountAsync(p => p.CompanyId == companyId);
            var categories = await _context.Categories.AsNoTracking().CountAsync(c => c.CompanyId == companyId);
            var tags       = await _context.Tags.AsNoTracking().CountAsync(t => t.CompanyId == companyId);
            var users      = await _context.Users.AsNoTracking().CountAsync(u => u.CompanyId == companyId);

            var stats = new DashboardStatsDto(products, categories, tags, users);
            _cache.Set(cacheKey, stats, StatsCacheDuration);

            return OperationResult<DashboardStatsDto>.Ok(stats);
        }
    }
}
