using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class SuperAdminDashboardService : ISuperAdminDashboardService
    {
        private const int MasterCompanyId = 1;

        private readonly ApplicationDbContext _context;

        public SuperAdminDashboardService(ApplicationDbContext context)
            => _context = context;

        public async Task<OperationResult<DashboardMetricsDto>> GetMetrics()
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAhead = now.AddDays(30);
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Todas las suscripciones activas (excluye empresa maestra)
            var subs = await _context.Subscriptions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId != MasterCompanyId)
                .ToListAsync();

            var totalTenants       = subs.Count;
            var activeTenants      = subs.Count(s => s.Status == SubscriptionStatus.Active);
            var trialTenants       = subs.Count(s => s.Status == SubscriptionStatus.Trial);
            var expiringIn30Days   = subs.Count(s => s.Status == SubscriptionStatus.Active &&
                                                      s.EndDate <= thirtyDaysAhead &&
                                                      s.EndDate >= now);
            var expiredTenants     = subs.Count(s => s.Status == SubscriptionStatus.Expired);
            var suspendedTenants   = subs.Count(s => s.Status == SubscriptionStatus.Suspended);
            var atRiskCount        = expiredTenants + suspendedTenants;

            // MRR: suma de precios mensuales de suscripciones activas
            var totalMrr = subs
                .Where(s => s.Status == SubscriptionStatus.Active)
                .Sum(s => s.Plan?.MonthlyPrice ?? 0m);

            // Nuevos tenants este mes (por fecha de creación de suscripción)
            var newThisMonth = subs.Count(s => s.CreatedAt >= firstOfMonth);

            // Últimos 10 pagos registrados en toda la plataforma
            var recentPayments = await _context.PaymentRecords
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.RecordedBy)
                .Where(p => p.CompanyId != MasterCompanyId)
                .OrderByDescending(p => p.PaidAt)
                .Take(10)
                .ToListAsync();

            // Tenants at-risk (vencidos o suspendidos), máximo 10
            var atRiskSubs = subs
                .Where(s => s.Status == SubscriptionStatus.Expired ||
                             s.Status == SubscriptionStatus.Suspended)
                .OrderBy(s => s.EndDate)
                .Take(10)
                .Select(s => new TenantSummaryDto(
                    s.CompanyId, s.Company.Name, s.Company.Slug,
                    s.Company.Email, s.Company.Phone, s.Company.CountryCode,
                    s.Company.IsActive, s.Company.PlanId,
                    s.Plan?.Name ?? string.Empty, s.Plan?.Code ?? string.Empty,
                    s.Company.MaxBranches, s.Company.MaxUsers,
                    0, 0, // branch/user counts omitidos en dashboard (evitar N+1)
                    s.Status, s.EndDate,
                    null, null,
                    s.Company.CreatedAt
                ))
                .ToList();

            return OperationResult<DashboardMetricsDto>.Ok(new DashboardMetricsDto(
                totalTenants,
                activeTenants,
                trialTenants,
                expiringIn30Days,
                expiredTenants,
                suspendedTenants,
                totalMrr,
                newThisMonth,
                atRiskCount,
                recentPayments.Select(p => SuperAdminCompanyService.MapPayment(p, p.Company.Name)).ToList(),
                atRiskSubs
            ));
        }
    }
}
