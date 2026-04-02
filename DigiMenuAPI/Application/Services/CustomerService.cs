using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CustomerService(
            ApplicationDbContext context,
            ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // ── LIST ──────────────────────────────────────────────────────────
        public async Task<OperationResult<PagedResult<CustomerReadDto>>> GetAll(
            string? search, int page, int pageSize)
        {
            var companyId = _tenantService.GetCompanyId();

            var query = _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .Where(c => c.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Phone != null && c.Phone.Contains(term)) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = customers.Select(MapToSummary).ToList();

            return OperationResult<PagedResult<CustomerReadDto>>.Ok(
                PagedResult<CustomerReadDto>.Create(dtos, total, page, pageSize));
        }

        // ── DETAIL ────────────────────────────────────────────────────────
        public async Task<OperationResult<CustomerDetailReadDto>> GetById(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer is null)
                return OperationResult<CustomerDetailReadDto>.NotFound(
                    "Cliente no encontrado.", ErrorKeys.CustomerNotFound);

            ValidateCompanyOwnership(customer);

            return OperationResult<CustomerDetailReadDto>.Ok(MapToDetail(customer));
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<OperationResult<CustomerDetailReadDto>> Create(CustomerCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var customer = new Customer
            {
                CompanyId    = companyId,
                Name         = dto.Name.Trim(),
                Phone        = dto.Phone?.Trim(),
                Email        = dto.Email?.Trim(),
                Notes        = dto.Notes?.Trim(),
                CreditLimit  = dto.CreditLimit,
                MaxOpenTabs  = dto.MaxOpenTabs,
                MaxTabAmount = dto.MaxTabAmount,
                IsActive     = true,
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var loaded = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .FirstAsync(c => c.Id == customer.Id);

            return OperationResult<CustomerDetailReadDto>.Ok(
                MapToDetail(loaded), "Cliente creado correctamente.");
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<OperationResult<CustomerDetailReadDto>> Update(CustomerUpdateDto dto)
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == dto.Id);

            if (customer is null)
                return OperationResult<CustomerDetailReadDto>.NotFound(
                    "Cliente no encontrado.", ErrorKeys.CustomerNotFound);

            ValidateCompanyOwnership(customer);

            customer.Name         = dto.Name.Trim();
            customer.Phone        = dto.Phone?.Trim();
            customer.Email        = dto.Email?.Trim();
            customer.Notes        = dto.Notes?.Trim();
            customer.CreditLimit  = dto.CreditLimit;
            customer.MaxOpenTabs  = dto.MaxOpenTabs;
            customer.MaxTabAmount = dto.MaxTabAmount;
            customer.IsActive     = dto.IsActive;

            await _context.SaveChangesAsync();

            return OperationResult<CustomerDetailReadDto>.Ok(
                MapToDetail(customer), "Cliente actualizado correctamente.");
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────────
        public async Task<OperationResult<CustomerDetailReadDto>> ToggleActive(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer is null)
                return OperationResult<CustomerDetailReadDto>.NotFound(
                    "Cliente no encontrado.", ErrorKeys.CustomerNotFound);

            ValidateCompanyOwnership(customer);

            customer.IsActive = !customer.IsActive;
            await _context.SaveChangesAsync();

            return OperationResult<CustomerDetailReadDto>.Ok(
                MapToDetail(customer),
                customer.IsActive ? "Cliente activado." : "Cliente desactivado.");
        }

        // ── CUSTOMER ACCOUNTS ─────────────────────────────────────────────
        public async Task<OperationResult<PagedResult<AccountReadDto>>> GetCustomerAccounts(
            int customerId, int? status, int page, int pageSize)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer is null)
                return OperationResult<PagedResult<AccountReadDto>>.NotFound(
                    "Cliente no encontrado.", ErrorKeys.CustomerNotFound);

            ValidateCompanyOwnership(customer);

            var query = _context.Accounts
                .AsNoTracking()
                .Include(a => a.Items)
                .Include(a => a.Discounts)
                .Include(a => a.Customer)
                .Where(a => a.CustomerId == customerId);

            if (status.HasValue)
                query = query.Where(a => (int)a.Status == status.Value);

            var total = await query.CountAsync();

            var accounts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = accounts.Select(MapAccountToSummary).ToList();

            return OperationResult<PagedResult<AccountReadDto>>.Ok(
                PagedResult<AccountReadDto>.Create(dtos, total, page, pageSize));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void ValidateCompanyOwnership(Customer customer)
        {
            var companyId = _tenantService.GetCompanyId();
            if (customer.CompanyId != companyId)
                throw new UnauthorizedAccessException("El cliente no pertenece a esta empresa.");
        }

        private static CustomerReadDto MapToSummary(Customer c)
        {
            return new CustomerReadDto(
                Id:                c.Id,
                CompanyId:         c.CompanyId,
                Name:              c.Name,
                Phone:             c.Phone,
                Email:             c.Email,
                Notes:             c.Notes,
                CreditLimit:       c.CreditLimit,
                CurrentBalance:    c.CurrentBalance,
                MaxOpenTabs:       c.MaxOpenTabs,
                MaxTabAmount:      c.MaxTabAmount,
                IsActive:          c.IsActive,
                OpenAccountsCount: c.Accounts.Count(a => a.Status == AccountStatus.Open),
                TabAccountsCount:  c.Accounts.Count(a => a.Status == AccountStatus.PendingPayment),
                CreatedAt:         c.CreatedAt
            );
        }

        private static CustomerDetailReadDto MapToDetail(Customer c)
        {
            var closedAccounts = c.Accounts
                .Where(a => a.Status == AccountStatus.Closed)
                .ToList();

            return new CustomerDetailReadDto(
                Id:                 c.Id,
                CompanyId:          c.CompanyId,
                Name:               c.Name,
                Phone:              c.Phone,
                Email:              c.Email,
                Notes:              c.Notes,
                CreditLimit:        c.CreditLimit,
                CurrentBalance:     c.CurrentBalance,
                MaxOpenTabs:        c.MaxOpenTabs,
                MaxTabAmount:       c.MaxTabAmount,
                IsActive:           c.IsActive,
                OpenAccountsCount:  c.Accounts.Count(a => a.Status == AccountStatus.Open),
                TabAccountsCount:   c.Accounts.Count(a => a.Status == AccountStatus.PendingPayment),
                TotalHistoricSpend: 0, // Calculated from closed accounts if needed
                CreatedAt:          c.CreatedAt,
                ModifiedAt:         c.ModifiedAt
            );
        }

        private static AccountReadDto MapAccountToSummary(Account a)
        {
            var subtotal = a.Items.Sum(i => i.Quantity * i.UnitPrice);

            return new AccountReadDto(
                Id:                    a.Id,
                BranchId:              a.BranchId,
                ClientIdentifier:      a.ClientIdentifier,
                Status:                a.Status,
                StatusLabel:           GetStatusLabel(a.Status),
                Notes:                 a.Notes,
                TabAuthorizedByUserId: a.TabAuthorizedByUserId,
                CustomerId:            a.CustomerId,
                CustomerName:          a.Customer?.Name,
                CreatedAt:             a.CreatedAt,
                TotalAmount:           subtotal,
                ItemCount:             a.Items.Count
            );
        }

        private static string GetStatusLabel(AccountStatus status) => status switch
        {
            AccountStatus.Open           => "Abierta",
            AccountStatus.PendingPayment => "Tab / Pendiente",
            AccountStatus.Closed         => "Cerrada",
            AccountStatus.Cancelled      => "Cancelada",
            _                            => "Desconocido"
        };
    }
}
