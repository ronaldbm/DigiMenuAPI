using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public DashboardService(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<OperationResult<DashboardStatsDto>> GetStats()
        {
            var companyId = _tenantService.GetCompanyId();

            // Global query filters already exclude soft-deleted records
            var products   = await _context.Products.CountAsync(p => p.CompanyId == companyId);
            var categories = await _context.Categories.CountAsync(c => c.CompanyId == companyId);
            var tags       = await _context.Tags.CountAsync(t => t.CompanyId == companyId);
            var users      = await _context.Users.CountAsync(u => u.CompanyId == companyId);

            return OperationResult<DashboardStatsDto>.Ok(
                new DashboardStatsDto(products, categories, tags, users));
        }
    }
}
