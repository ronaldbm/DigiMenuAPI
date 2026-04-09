using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class SuperAdminSubscriptionService : ISuperAdminSubscriptionService
    {
        private const int MasterCompanyId = 1;

        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public SuperAdminSubscriptionService(
            ApplicationDbContext context,
            ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // ── GET ALL ───────────────────────────────────────────────────
        public async Task<OperationResult<List<SubscriptionDto>>> GetAll(string? status)
        {
            var query = _context.Subscriptions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId != MasterCompanyId);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<SubscriptionStatus>(status, true, out var parsedStatus))
                query = query.Where(s => s.Status == parsedStatus);

            var subs = await query.OrderBy(s => s.EndDate).ToListAsync();
            return OperationResult<List<SubscriptionDto>>.Ok(
                subs.Select(s => SuperAdminCompanyService.MapSubscription(s, s.Company.Name, s.Company.Slug)).ToList());
        }

        // ── GET BY COMPANY ────────────────────────────────────────────
        public async Task<OperationResult<SubscriptionDto>> GetByCompany(int companyId)
        {
            var sub = await _context.Subscriptions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (sub is null)
                return OperationResult<SubscriptionDto>.NotFound(
                    "El tenant no tiene suscripción registrada.",
                    ErrorKeys.SubscriptionNotFound);

            return OperationResult<SubscriptionDto>.Ok(
                SuperAdminCompanyService.MapSubscription(sub, sub.Company.Name, sub.Company.Slug));
        }

        // ── GET EXPIRING SOON ─────────────────────────────────────────
        public async Task<OperationResult<List<SubscriptionDto>>> GetExpiringSoon(int days = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(days);

            var subs = await _context.Subscriptions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId != MasterCompanyId &&
                            s.Status == SubscriptionStatus.Active &&
                            s.EndDate <= cutoff &&
                            s.EndDate >= DateTime.UtcNow)
                .OrderBy(s => s.EndDate)
                .ToListAsync();

            return OperationResult<List<SubscriptionDto>>.Ok(
                subs.Select(s => SuperAdminCompanyService.MapSubscription(s, s.Company.Name, s.Company.Slug)).ToList());
        }

        // ── GET AT RISK ───────────────────────────────────────────────
        public async Task<OperationResult<List<SubscriptionDto>>> GetAtRisk()
        {
            var subs = await _context.Subscriptions
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId != MasterCompanyId &&
                            (s.Status == SubscriptionStatus.Expired ||
                             s.Status == SubscriptionStatus.Suspended))
                .OrderBy(s => s.Status)
                .ThenBy(s => s.EndDate)
                .ToListAsync();

            return OperationResult<List<SubscriptionDto>>.Ok(
                subs.Select(s => SuperAdminCompanyService.MapSubscription(s, s.Company.Name, s.Company.Slug)).ToList());
        }

        // ── UPDATE ────────────────────────────────────────────────────
        public async Task<OperationResult<SubscriptionDto>> Update(
            int companyId, UpdateSubscriptionDto dto)
        {
            var sub = await _context.Subscriptions
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (sub is null)
                return OperationResult<SubscriptionDto>.NotFound(
                    "El tenant no tiene suscripción registrada.",
                    ErrorKeys.SubscriptionNotFound);

            sub.Status = dto.Status;
            sub.EndDate = dto.EndDate;
            sub.NextBillingDate = dto.NextBillingDate;
            sub.TrialEndsAt = dto.TrialEndsAt;
            sub.Notes = dto.Notes;

            if (dto.Status == SubscriptionStatus.Suspended)
            {
                sub.SuspendedAt ??= DateTime.UtcNow;
                sub.SuspendedReason = dto.SuspendedReason;
            }
            else
            {
                // Al reactivar, limpiar datos de suspensión
                sub.SuspendedAt = null;
                sub.SuspendedReason = null;
            }

            await _context.SaveChangesAsync();
            return OperationResult<SubscriptionDto>.Ok(
                SuperAdminCompanyService.MapSubscription(sub, sub.Company.Name, sub.Company.Slug));
        }

        // ── GET PAYMENTS ──────────────────────────────────────────────
        public async Task<OperationResult<List<PaymentRecordDto>>> GetPayments(int companyId)
        {
            var payments = await _context.PaymentRecords
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.RecordedBy)
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            return OperationResult<List<PaymentRecordDto>>.Ok(
                payments.Select(p => SuperAdminCompanyService.MapPayment(p, p.Company.Name)).ToList());
        }

        // ── REGISTER PAYMENT ─────────────────────────────────────────
        public async Task<OperationResult<PaymentRecordDto>> RegisterPayment(
            int companyId, RegisterPaymentDto dto)
        {
            var sub = await _context.Subscriptions
                .IgnoreQueryFilters()
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (sub is null)
                return OperationResult<PaymentRecordDto>.NotFound(
                    "El tenant no tiene suscripción registrada.",
                    ErrorKeys.SubscriptionNotFound);

            var recordedById = _tenantService.GetUserId();

            var payment = new PaymentRecord
            {
                CompanyId = companyId,
                SubscriptionId = sub.Id,
                Amount = dto.Amount,
                Currency = dto.Currency.Trim().ToUpper(),
                PaidAt = dto.PaidAt,
                Method = dto.Method,
                Reference = dto.Reference?.Trim(),
                Notes = dto.Notes?.Trim(),
                Status = dto.Status,
                RecordedByUserId = recordedById
            };

            _context.PaymentRecords.Add(payment);
            await _context.SaveChangesAsync();

            // Recargar con relaciones para devolver DTO completo
            var created = await _context.PaymentRecords
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.RecordedBy)
                .FirstAsync(p => p.Id == payment.Id);

            return OperationResult<PaymentRecordDto>.Ok(
                SuperAdminCompanyService.MapPayment(created, created.Company.Name));
        }

        // ── UPDATE PAYMENT STATUS ─────────────────────────────────────
        public async Task<OperationResult<PaymentRecordDto>> UpdatePaymentStatus(
            int paymentId, PaymentStatus paymentStatus)
        {
            var payment = await _context.PaymentRecords
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.RecordedBy)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment is null)
                return OperationResult<PaymentRecordDto>.NotFound(
                    "Pago no encontrado.",
                    ErrorKeys.PaymentNotFound);

            payment.Status = paymentStatus;
            await _context.SaveChangesAsync();

            return OperationResult<PaymentRecordDto>.Ok(
                SuperAdminCompanyService.MapPayment(payment, payment.Company.Name));
        }
    }
}
