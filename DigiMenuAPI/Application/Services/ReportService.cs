using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenant;

        public ReportService(ApplicationDbContext context, ITenantService tenant)
        {
            _context = context;
            _tenant = tenant;
        }

        public async Task<OperationResult<AccountKpiDto>> GetAccountKpis(
            int? branchId, DateTime? from, DateTime? to)
        {
            var companyId = _tenant.GetCompanyId();
            var today = DateTime.UtcNow.Date;

            var query = _context.Accounts
                .AsNoTracking()
                .Include(a => a.Items)
                .Include(a => a.Discounts)
                .Where(a => a.Branch.CompanyId == companyId);

            if (branchId.HasValue)
                query = query.Where(a => a.BranchId == branchId.Value);

            // All accounts for "current state" KPIs
            var allAccounts = await query.ToListAsync();

            // Period filter for revenue calculations
            var periodFrom = from ?? today.AddDays(-30);
            var periodTo = (to ?? today).AddDays(1); // inclusive end

            var periodAccounts = allAccounts
                .Where(a => a.CreatedAt >= periodFrom && a.CreatedAt < periodTo)
                .ToList();

            var todayAccounts = allAccounts
                .Where(a => a.CreatedAt >= today && a.CreatedAt < today.AddDays(1))
                .ToList();

            // Open accounts (current)
            int openAccounts = allAccounts.Count(a => a.Status == AccountStatus.Open);

            // Tab accounts (PendingPayment — current)
            var tabAccounts = allAccounts.Where(a => a.Status == AccountStatus.PendingPayment).ToList();
            int tabCount = tabAccounts.Count;
            decimal tabTotal = tabAccounts.Sum(a => CalculateTotal(a));

            // Revenue today (closed accounts today)
            var closedToday = todayAccounts.Where(a => a.Status == AccountStatus.Closed).ToList();
            decimal revenueToday = closedToday.Sum(a => CalculateTotal(a));
            int accountsClosedToday = closedToday.Count;

            // Revenue period (closed accounts in period)
            var closedPeriod = periodAccounts.Where(a => a.Status == AccountStatus.Closed).ToList();
            decimal revenuePeriod = closedPeriod.Sum(a => CalculateTotal(a));
            int accountsClosedPeriod = closedPeriod.Count;

            // Pending discounts (across all open/tab accounts)
            int pendingDiscounts = allAccounts
                .Where(a => a.Status == AccountStatus.Open || a.Status == AccountStatus.PendingPayment)
                .SelectMany(a => a.Discounts)
                .Count(d => d.Status == AccountDiscountStatus.PendingApproval);

            // Total discounts applied in period
            decimal totalDiscountsApplied = closedPeriod.Sum(a => CalculateDiscountTotal(a));

            // Average ticket in period
            decimal averageTicket = accountsClosedPeriod > 0
                ? revenuePeriod / accountsClosedPeriod
                : 0m;

            return OperationResult<AccountKpiDto>.Ok(new AccountKpiDto(
                OpenAccounts: openAccounts,
                TabAccounts: tabCount,
                TabTotal: tabTotal,
                RevenueToday: revenueToday,
                RevenuePeriod: revenuePeriod,
                PendingDiscounts: pendingDiscounts,
                TotalDiscountsApplied: totalDiscountsApplied,
                AccountsClosedToday: accountsClosedToday,
                AccountsClosedPeriod: accountsClosedPeriod,
                AverageTicket: Math.Round(averageTicket, 2)));
        }

        public async Task<OperationResult<PagedResult<AccountReportRowDto>>> GetAccountReport(
            int? branchId, AccountStatus? status, DateTime? from, DateTime? to,
            string? sortBy, bool sortDesc, int page, int pageSize)
        {
            var companyId = _tenant.GetCompanyId();

            var query = _context.Accounts
                .AsNoTracking()
                .Include(a => a.Branch)
                .Include(a => a.Items)
                .Include(a => a.Discounts)
                .Include(a => a.Customer)
                .Where(a => a.Branch.CompanyId == companyId);

            if (branchId.HasValue)
                query = query.Where(a => a.BranchId == branchId.Value);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt < to.Value.AddDays(1));

            var total = await query.CountAsync();

            // Sorting
            query = (sortBy?.ToLowerInvariant()) switch
            {
                "client"   => sortDesc ? query.OrderByDescending(a => a.ClientIdentifier) : query.OrderBy(a => a.ClientIdentifier),
                "status"   => sortDesc ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "branch"   => sortDesc ? query.OrderByDescending(a => a.Branch.Name) : query.OrderBy(a => a.Branch.Name),
                "createdat" => sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                _          => query.OrderByDescending(a => a.CreatedAt)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var rows = items.Select(a => new AccountReportRowDto(
                Id: a.Id,
                BranchId: a.BranchId,
                BranchName: a.Branch.Name,
                ClientIdentifier: a.ClientIdentifier,
                Status: a.Status,
                StatusLabel: GetStatusLabel(a.Status),
                CustomerId: a.CustomerId,
                CustomerName: a.Customer?.Name,
                Subtotal: CalculateSubtotal(a),
                TotalDiscounts: CalculateDiscountTotal(a),
                Total: CalculateTotal(a),
                ItemCount: a.Items.Sum(i => i.Quantity),
                CreatedAt: a.CreatedAt,
                ClosedAt: a.Status == AccountStatus.Closed ? a.ModifiedAt : null
            )).ToList();

            return OperationResult<PagedResult<AccountReportRowDto>>.Ok(
                new PagedResult<AccountReportRowDto>
                {
                    Items = rows,
                    Total = total,
                    Page = page,
                    PageSize = pageSize
                });
        }

        public async Task<OperationResult<CustomerStatementDto>> GetCustomerStatement(
            int customerId, DateTime? from, DateTime? to)
        {
            var companyId = _tenant.GetCompanyId();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
                return OperationResult<CustomerStatementDto>.NotFound("Cliente no encontrado.", errorKey: ErrorKeys.CustomerNotFound);

            var periodFrom = from ?? DateTime.UtcNow.Date.AddDays(-30);
            var periodTo = (to ?? DateTime.UtcNow.Date).AddDays(1);

            var accounts = await _context.Accounts
                .AsNoTracking()
                .Include(a => a.Items)
                .Include(a => a.Discounts)
                .Where(a => a.CustomerId == customerId
                    && a.CreatedAt >= periodFrom
                    && a.CreatedAt < periodTo)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var lines = accounts.Select(a => new CustomerStatementLineDto(
                AccountId: a.Id,
                ClientIdentifier: a.ClientIdentifier,
                Status: a.Status,
                StatusLabel: GetStatusLabel(a.Status),
                Total: CalculateTotal(a),
                CreatedAt: a.CreatedAt,
                ClosedAt: a.Status == AccountStatus.Closed ? a.ModifiedAt : null
            )).ToList();

            decimal totalSpentPeriod = accounts
                .Where(a => a.Status == AccountStatus.Closed)
                .Sum(a => CalculateTotal(a));

            return OperationResult<CustomerStatementDto>.Ok(new CustomerStatementDto(
                CustomerId: customer.Id,
                CustomerName: customer.Name,
                CreditLimit: customer.CreditLimit,
                CurrentBalance: customer.CurrentBalance,
                TotalSpentPeriod: totalSpentPeriod,
                Lines: lines));
        }

        public async Task<OperationResult<AccountReceiptDto>> GetAccountReceipt(int accountId)
        {
            var companyId = _tenant.GetCompanyId();

            var account = await _context.Accounts
                .AsNoTracking()
                .Include(a => a.Branch)
                .Include(a => a.Items)
                .Include(a => a.Discounts).ThenInclude(d => d.BranchDiscount)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == accountId && a.Branch.CompanyId == companyId);

            if (account is null)
                return OperationResult<AccountReceiptDto>.NotFound("Cuenta no encontrada.", errorKey: ErrorKeys.AccountNotFound);

            var companyInfo = await _context.CompanyInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            // Currency from BranchLocale
            var locale = await _context.BranchLocales
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.BranchId == account.BranchId);

            var subtotal = CalculateSubtotal(account);
            var discountTotal = CalculateDiscountTotal(account);
            var total = Math.Max(0, subtotal - discountTotal);

            var items = account.Items.Select(i => new ReceiptItemDto(
                ProductName: i.ProductName,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                LineTotal: i.Quantity * i.UnitPrice
            )).ToList();

            var approvedDiscounts = account.Discounts
                .Where(d => d.Status == AccountDiscountStatus.Approved)
                .ToList();

            var discounts = approvedDiscounts.Select(d =>
            {
                decimal amount;
                if (d.AccountItemId != null)
                {
                    var item = account.Items.FirstOrDefault(i => i.Id == d.AccountItemId);
                    amount = d.DiscountType == DiscountType.Percentage && item != null
                        ? item.Quantity * item.UnitPrice * d.DiscountValue / 100m
                        : d.DiscountValue;
                }
                else
                {
                    amount = d.DiscountType == DiscountType.Percentage
                        ? subtotal * d.DiscountValue / 100m
                        : d.DiscountValue;
                }

                var desc = d.DiscountType == DiscountType.Percentage
                    ? $"{d.DiscountValue}%"
                    : $"-{d.DiscountValue:N2}";

                return new ReceiptDiscountDto(
                    Name: d.BranchDiscount?.Name ?? d.Reason,
                    Description: desc,
                    Amount: amount);
            }).ToList();

            return OperationResult<AccountReceiptDto>.Ok(new AccountReceiptDto(
                BusinessName: companyInfo?.BusinessName ?? "DigiMenu",
                BranchName: account.Branch.Name,
                BranchAddress: account.Branch.Address,
                BranchPhone: account.Branch.Phone,
                AccountId: account.Id,
                ClientIdentifier: account.ClientIdentifier,
                CustomerName: account.Customer?.Name,
                StatusLabel: GetStatusLabel(account.Status),
                CreatedAt: account.CreatedAt,
                ClosedAt: account.Status == AccountStatus.Closed ? account.ModifiedAt : null,
                Items: items,
                Discounts: discounts,
                Subtotal: subtotal,
                TotalDiscounts: discountTotal,
                Total: total,
                CurrencySymbol: locale?.Currency ?? "CRC"));
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static decimal CalculateSubtotal(Infrastructure.Entities.Account a)
            => a.Items.Sum(i => i.Quantity * i.UnitPrice);

        private static decimal CalculateDiscountTotal(Infrastructure.Entities.Account a)
        {
            var subtotal = CalculateSubtotal(a);
            var approvedDiscounts = a.Discounts
                .Where(d => d.Status == AccountDiscountStatus.Approved)
                .ToList();

            // Item-level discounts
            decimal itemDiscTotal = 0m;
            foreach (var item in a.Items)
            {
                var itemDiscounts = approvedDiscounts
                    .Where(d => d.AccountItemId == item.Id);
                foreach (var d in itemDiscounts)
                {
                    itemDiscTotal += d.DiscountType == DiscountType.Percentage
                        ? item.Quantity * item.UnitPrice * d.DiscountValue / 100m
                        : d.DiscountValue;
                }
            }

            // Account-level discounts
            decimal acctDiscTotal = 0m;
            var acctDiscounts = approvedDiscounts.Where(d => d.AccountItemId == null);
            foreach (var d in acctDiscounts)
            {
                acctDiscTotal += d.DiscountType == DiscountType.Percentage
                    ? (subtotal - itemDiscTotal) * d.DiscountValue / 100m
                    : d.DiscountValue;
            }

            return itemDiscTotal + acctDiscTotal;
        }

        private static decimal CalculateTotal(Infrastructure.Entities.Account a)
            => Math.Max(0, CalculateSubtotal(a) - CalculateDiscountTotal(a));

        private static string GetStatusLabel(AccountStatus status) => status switch
        {
            AccountStatus.Open           => "Abierta",
            AccountStatus.PendingPayment => "Tab / Pendiente de pago",
            AccountStatus.Closed         => "Cerrada",
            AccountStatus.Cancelled      => "Cancelada",
            _                            => status.ToString()
        };
    }
}
