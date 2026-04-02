using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext   _context;
    private readonly ITenantService         _tenantService;
    private readonly ICacheService          _cache;
    private readonly IAccountAuditService   _audit;
    private readonly INotificationService   _notifications;

    public AccountService(
        ApplicationDbContext   context,
        ITenantService         tenantService,
        ICacheService          cache,
        IAccountAuditService   audit,
        INotificationService   notifications)
    {
        _context        = context;
        _tenantService  = tenantService;
        _cache          = cache;
        _audit          = audit;
        _notifications  = notifications;
    }

    // ── LIST ──────────────────────────────────────────────────────────────────

    public async Task<OperationResult<PagedResult<AccountReadDto>>> GetByBranch(
        int branchId, AccountStatus? status, int page, int pageSize)
    {
        await _tenantService.ValidateBranchOwnershipAsync(branchId);

        var query = _context.Accounts
            .AsNoTracking()
            .Include(a => a.Items)
            .Include(a => a.Discounts)
            .Include(a => a.Customer)
            .Where(a => a.BranchId == branchId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var total = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = accounts.Select(MapToSummary).ToList();
        return OperationResult<PagedResult<AccountReadDto>>.Ok(
            PagedResult<AccountReadDto>.Create(dtos, total, page, pageSize));
    }

    // ── DETAIL ────────────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> GetById(int id)
    {
        var account = await LoadAccountDetail(id);
        if (account is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Cuenta no encontrada.", ErrorKeys.AccountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);
        return OperationResult<AccountDetailReadDto>.Ok(MapToDetail(account));
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> Create(AccountCreateDto dto)
    {
        await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

        var account = new Account
        {
            BranchId         = dto.BranchId,
            ClientIdentifier = dto.ClientIdentifier.Trim(),
            Notes            = dto.Notes?.Trim(),
            Status           = AccountStatus.Open,
            CustomerId       = dto.CustomerId,
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(account.Id, AuditActions.Created,
            $"Cuenta creada: {account.ClientIdentifier}");

        var loaded = await LoadAccountDetail(account.Id);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Cuenta creada correctamente.");
    }

    // ── ADD ITEM ──────────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> AddItem(AccountItemCreateDto dto)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

        if (account is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Cuenta no encontrada.", ErrorKeys.AccountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);

        if (account.Status == AccountStatus.Closed || account.Status == AccountStatus.Cancelled)
            return OperationResult<AccountDetailReadDto>.Conflict(
                "No se pueden agregar ítems a una cuenta cerrada o cancelada.",
                ErrorKeys.AccountClosed);

        var branchProduct = await _context.BranchProducts
            .Include(bp => bp.Product)
                .ThenInclude(p => p.Translations)
            .FirstOrDefaultAsync(bp => bp.Id == dto.BranchProductId && !bp.IsDeleted);

        if (branchProduct is null || !branchProduct.IsVisible)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Producto no disponible.", ErrorKeys.BranchProductNotVisibleForAccount);

        var productName = branchProduct.Product?.Translations?.FirstOrDefault()?.Name
                          ?? $"Producto #{dto.BranchProductId}";

        var item = new AccountItem
        {
            AccountId       = dto.AccountId,
            BranchProductId = dto.BranchProductId,
            ProductName     = productName,
            UnitPrice       = branchProduct.OfferPrice ?? branchProduct.Price,
            Quantity        = dto.Quantity,
            Notes           = dto.Notes?.Trim(),
        };

        _context.AccountItems.Add(item);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(dto.AccountId, AuditActions.ItemAdded,
            $"Ítem agregado: {productName} x{dto.Quantity}");

        var loaded = await LoadAccountDetail(dto.AccountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Ítem agregado correctamente.");
    }

    // ── UPDATE ITEM ───────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> UpdateItem(AccountItemUpdateDto dto)
    {
        var item = await _context.AccountItems
            .Include(i => i.Account)
            .FirstOrDefaultAsync(i => i.Id == dto.Id && i.AccountId == dto.AccountId);

        if (item is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Ítem no encontrado.", ErrorKeys.AccountItemNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(item.Account.BranchId);

        if (item.Account.Status == AccountStatus.Closed || item.Account.Status == AccountStatus.Cancelled)
            return OperationResult<AccountDetailReadDto>.Conflict(
                "No se puede modificar una cuenta cerrada o cancelada.",
                ErrorKeys.AccountClosed);

        item.Quantity = dto.Quantity;
        item.Notes    = dto.Notes?.Trim();

        await _context.SaveChangesAsync();

        await _audit.LogAsync(dto.AccountId, AuditActions.ItemUpdated,
            $"Ítem actualizado: {item.ProductName} → cantidad {dto.Quantity}");

        var loaded = await LoadAccountDetail(dto.AccountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Ítem actualizado correctamente.");
    }

    // ── REMOVE ITEM ───────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> RemoveItem(int accountItemId)
    {
        var item = await _context.AccountItems
            .Include(i => i.Account)
            .FirstOrDefaultAsync(i => i.Id == accountItemId);

        if (item is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Ítem no encontrado.", ErrorKeys.AccountItemNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(item.Account.BranchId);

        if (item.Account.Status == AccountStatus.Closed || item.Account.Status == AccountStatus.Cancelled)
            return OperationResult<AccountDetailReadDto>.Conflict(
                "No se puede modificar una cuenta cerrada o cancelada.",
                ErrorKeys.AccountClosed);

        var accountId = item.AccountId;
        var removedName = item.ProductName;
        _context.AccountItems.Remove(item);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(accountId, AuditActions.ItemRemoved,
            $"Ítem eliminado: {removedName}");

        var loaded = await LoadAccountDetail(accountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Ítem eliminado correctamente.");
    }

    // ── APPLY DISCOUNT ────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> ApplyDiscount(ApplyDiscountDto dto)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

        if (account is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Cuenta no encontrada.", ErrorKeys.AccountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);

        if (account.Status == AccountStatus.Closed || account.Status == AccountStatus.Cancelled)
            return OperationResult<AccountDetailReadDto>.Conflict(
                "No se pueden aplicar descuentos a una cuenta cerrada o cancelada.",
                ErrorKeys.AccountClosed);

        var branchDiscount = await _context.BranchDiscounts
            .FirstOrDefaultAsync(d => d.Id == dto.BranchDiscountId);

        if (branchDiscount is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Descuento no encontrado.", ErrorKeys.BranchDiscountNotFound);

        if (branchDiscount.BranchId != account.BranchId)
            return OperationResult<AccountDetailReadDto>.Forbidden(
                "El descuento no pertenece a esta sucursal.", ErrorKeys.BranchDiscountNotOwned);

        if (!branchDiscount.IsActive)
            return OperationResult<AccountDetailReadDto>.Conflict(
                "El descuento no está activo.", ErrorKeys.BranchDiscountInactive);

        // Validate item-level target if provided
        if (dto.AccountItemId.HasValue)
        {
            var itemExists = await _context.AccountItems
                .AnyAsync(i => i.Id == dto.AccountItemId.Value && i.AccountId == dto.AccountId);
            if (!itemExists)
                return OperationResult<AccountDetailReadDto>.NotFound(
                    "Ítem no encontrado en esta cuenta.", ErrorKeys.AccountItemNotFound);
        }

        // Determine the caller's role to check approval & limits
        var callerRole    = _tenantService.GetUserRole();
        var discountValue = dto.DiscountValue ?? branchDiscount.DefaultValue;

        // If staff has a max limit and the value exceeds it, cap it
        if (callerRole == UserRoles.Staff && branchDiscount.MaxValueForStaff.HasValue)
        {
            if (discountValue > branchDiscount.MaxValueForStaff.Value)
                discountValue = branchDiscount.MaxValueForStaff.Value;
        }

        // Determine discount status: requires approval if configured AND caller is Staff
        var discountStatus = (branchDiscount.RequiresApproval && callerRole == UserRoles.Staff)
            ? AccountDiscountStatus.PendingApproval
            : AccountDiscountStatus.Approved;

        var accountDiscount = new AccountDiscount
        {
            AccountId        = dto.AccountId,
            BranchDiscountId = dto.BranchDiscountId,
            AccountItemId    = dto.AccountItemId,
            DiscountType     = branchDiscount.DiscountType,
            DiscountValue    = discountValue,
            AppliesTo        = dto.AccountItemId.HasValue
                                   ? DiscountAppliesTo.SpecificItem
                                   : DiscountAppliesTo.WholeAccount,
            Reason           = dto.Reason.Trim(),
            Status           = discountStatus,
        };

        _context.AccountDiscounts.Add(accountDiscount);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(dto.AccountId, AuditActions.DiscountApplied,
            $"Descuento aplicado: {branchDiscount.Name} ({discountValue}{(branchDiscount.DiscountType == DiscountType.Percentage ? "%" : " fijo")})" +
            (discountStatus == AccountDiscountStatus.PendingApproval ? " — pendiente de aprobación" : ""));

        if (discountStatus == AccountDiscountStatus.PendingApproval)
        {
            await _notifications.CreateAsync(
                _tenantService.GetCompanyId(),
                NotificationTypes.DiscountPendingApproval,
                $"Descuento \"{branchDiscount.Name}\" pendiente de aprobación en cuenta {account.ClientIdentifier}.",
                branchId: account.BranchId,
                relatedEntity: "Account",
                relatedEntityId: dto.AccountId);
        }

        var loaded = await LoadAccountDetail(dto.AccountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!),
            discountStatus == AccountDiscountStatus.PendingApproval
                ? "Descuento enviado para aprobación."
                : "Descuento aplicado correctamente.");
    }

    // ── AUTHORIZE DISCOUNT ────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDiscountReadDto>> AuthorizeDiscount(AuthorizeDiscountDto dto)
    {
        var discount = await _context.AccountDiscounts
            .Include(d => d.Account)
            .Include(d => d.BranchDiscount)
            .FirstOrDefaultAsync(d => d.Id == dto.AccountDiscountId);

        if (discount is null)
            return OperationResult<AccountDiscountReadDto>.NotFound(
                "Descuento no encontrado.", ErrorKeys.AccountDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.Account.BranchId);

        // Only BranchAdmin+ can authorize
        var callerRole = _tenantService.GetUserRole();
        if (callerRole == UserRoles.Staff)
            return OperationResult<AccountDiscountReadDto>.Forbidden(
                "No tiene permiso para autorizar descuentos.", ErrorKeys.DiscountApprovalUnauthorized);

        if (discount.Status != AccountDiscountStatus.PendingApproval)
            return OperationResult<AccountDiscountReadDto>.Conflict(
                "El descuento ya fue procesado.", ErrorKeys.AccountDiscountAlreadyProcessed);

        discount.Status             = dto.Approved ? AccountDiscountStatus.Approved : AccountDiscountStatus.Rejected;
        discount.AuthorizedByUserId = _tenantService.GetUserId();

        await _context.SaveChangesAsync();

        var auditAction = dto.Approved ? AuditActions.DiscountAuthorized : AuditActions.DiscountRejected;
        await _audit.LogAsync(discount.AccountId, auditAction,
            $"Descuento {(dto.Approved ? "aprobado" : "rechazado")}: {discount.BranchDiscount?.Name ?? "descuento"}");

        return OperationResult<AccountDiscountReadDto>.Ok(
            MapDiscountToDto(discount),
            dto.Approved ? "Descuento aprobado." : "Descuento rechazado.");
    }

    // ── REMOVE DISCOUNT ───────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> RemoveDiscount(int accountDiscountId)
    {
        var discount = await _context.AccountDiscounts
            .Include(d => d.Account)
            .FirstOrDefaultAsync(d => d.Id == accountDiscountId);

        if (discount is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Descuento no encontrado.", ErrorKeys.AccountDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.Account.BranchId);

        var accountId = discount.AccountId;
        _context.AccountDiscounts.Remove(discount);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(accountId, AuditActions.DiscountRemoved,
            "Descuento eliminado");

        var loaded = await LoadAccountDetail(accountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Descuento eliminado correctamente.");
    }

    // ── SET STATUS ────────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> SetStatus(AccountStatusUpdateDto dto)
    {
        var account = await _context.Accounts
            .Include(a => a.Items)
            .Include(a => a.Discounts)
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

        if (account is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Cuenta no encontrada.", ErrorKeys.AccountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);

        var callerRole = _tenantService.GetUserRole();

        // Staff cannot cancel accounts
        if (dto.Status == AccountStatus.Cancelled && callerRole == UserRoles.Staff)
            return OperationResult<AccountDetailReadDto>.Forbidden(
                "No tiene permiso para cancelar cuentas.", ErrorKeys.DiscountApprovalUnauthorized);

        // Tab (PendingPayment) requires TabsEnabled at company level
        if (dto.Status == AccountStatus.PendingPayment)
        {
            var companyId  = _tenantService.GetCompanyId();
            var companyInfo = await _context.CompanyInfos
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId);

            if (companyInfo is null || !companyInfo.TabsEnabled)
                return OperationResult<AccountDetailReadDto>.Forbidden(
                    "Los tabs no están habilitados en esta empresa.", ErrorKeys.TabsNotEnabled);

            // Manager approval required: Staff cannot put on tab directly
            if (companyInfo.TabRequiresManagerApproval && callerRole == UserRoles.Staff)
                return OperationResult<AccountDetailReadDto>.Forbidden(
                    "Se requiere aprobación de un gerente para poner una cuenta en tab.",
                    ErrorKeys.TabRequiresManagerApproval);

            // Calculate this account's total for limit checks
            var accountTotal = account.Items.Sum(i => i.Quantity * i.UnitPrice)
                             - account.Discounts
                                   .Where(d => d.Status == AccountDiscountStatus.Approved)
                                   .Sum(d => d.DiscountType == DiscountType.Percentage
                                       ? account.Items.Sum(i => i.Quantity * i.UnitPrice) * d.DiscountValue / 100m
                                       : d.DiscountValue);
            accountTotal = Math.Max(0, accountTotal);

            if (account.CustomerId.HasValue)
            {
                // Customer-specific limits
                var customer = await _context.Set<Customer>()
                    .Include(c => c.Accounts)
                    .FirstOrDefaultAsync(c => c.Id == account.CustomerId.Value);

                if (customer is not null)
                {
                    if (!customer.IsActive)
                        return OperationResult<AccountDetailReadDto>.Forbidden(
                            "El cliente está inactivo.", ErrorKeys.CustomerInactive);

                    // MaxOpenTabs: count current PendingPayment accounts (excluding this one)
                    var openTabs = customer.Accounts
                        .Count(a => a.Status == AccountStatus.PendingPayment && a.Id != account.Id);
                    if (openTabs >= customer.MaxOpenTabs)
                        return OperationResult<AccountDetailReadDto>.Forbidden(
                            $"El cliente ya tiene {openTabs} tab(s) abierto(s) (máximo: {customer.MaxOpenTabs}).",
                            ErrorKeys.CustomerTabLimitReached);

                    // MaxTabAmount: this account's total cannot exceed the per-tab limit
                    if (customer.MaxTabAmount > 0 && accountTotal > customer.MaxTabAmount)
                        return OperationResult<AccountDetailReadDto>.Forbidden(
                            $"El monto de la cuenta ({accountTotal:C}) excede el monto máximo por tab ({customer.MaxTabAmount:C}).",
                            ErrorKeys.CustomerMaxTabAmountReached);

                    // CreditLimit: current balance + this account cannot exceed credit limit
                    if (customer.CreditLimit > 0 && customer.CurrentBalance + accountTotal > customer.CreditLimit)
                        return OperationResult<AccountDetailReadDto>.Forbidden(
                            $"El saldo pendiente ({customer.CurrentBalance:C}) más esta cuenta ({accountTotal:C}) excede el límite de crédito ({customer.CreditLimit:C}).",
                            ErrorKeys.CustomerCreditLimitReached);
                }
            }
            else
            {
                // Company-level defaults for accounts without a customer
                if (companyInfo.DefaultMaxOpenTabs > 0)
                {
                    var branchOpenTabs = await _context.Accounts
                        .CountAsync(a => a.BranchId == account.BranchId
                                      && a.Status == AccountStatus.PendingPayment
                                      && a.CustomerId == null
                                      && a.Id != account.Id);
                    if (branchOpenTabs >= companyInfo.DefaultMaxOpenTabs)
                        return OperationResult<AccountDetailReadDto>.Forbidden(
                            $"La sucursal ya tiene {branchOpenTabs} tab(s) sin cliente (máximo: {companyInfo.DefaultMaxOpenTabs}).",
                            ErrorKeys.CompanyTabLimitReached);
                }

                if (companyInfo.DefaultMaxTabAmount > 0 && accountTotal > companyInfo.DefaultMaxTabAmount)
                    return OperationResult<AccountDetailReadDto>.Forbidden(
                        $"El monto de la cuenta ({accountTotal:C}) excede el monto máximo por tab ({companyInfo.DefaultMaxTabAmount:C}).",
                        ErrorKeys.CompanyMaxTabAmountReached);
            }

            // Increase customer balance when putting on tab
            if (account.CustomerId.HasValue)
            {
                var custForBalance = await _context.Set<Customer>()
                    .FirstOrDefaultAsync(c => c.Id == account.CustomerId.Value);
                if (custForBalance is not null)
                    custForBalance.CurrentBalance += accountTotal;
            }

            account.TabAuthorizedByUserId = _tenantService.GetUserId();
        }

        // When closing a tab account, reduce customer balance
        if (dto.Status == AccountStatus.Closed && account.Status == AccountStatus.PendingPayment
            && account.CustomerId.HasValue)
        {
            var customer = await _context.Set<Customer>()
                .FirstOrDefaultAsync(c => c.Id == account.CustomerId.Value);

            if (customer is not null)
            {
                var closingTotal = account.Items.Sum(i => i.Quantity * i.UnitPrice)
                                 - account.Discounts
                                       .Where(d => d.Status == AccountDiscountStatus.Approved)
                                       .Sum(d => d.DiscountType == DiscountType.Percentage
                                           ? account.Items.Sum(i => i.Quantity * i.UnitPrice) * d.DiscountValue / 100m
                                           : d.DiscountValue);
                customer.CurrentBalance = Math.Max(0, customer.CurrentBalance - Math.Max(0, closingTotal));
            }
        }

        var prevStatus = account.Status;
        account.Status = dto.Status;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(dto.AccountId, AuditActions.StatusChanged,
            $"Estado cambiado: {GetStatusLabel(prevStatus)} → {GetStatusLabel(dto.Status)}");

        var loaded = await LoadAccountDetail(dto.AccountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "Estado de cuenta actualizado.");
    }

    // ── CREATE SPLIT ──────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> CreateSplit(AccountSplitCreateDto dto)
    {
        var account = await _context.Accounts
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId);

        if (account is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "Cuenta no encontrada.", ErrorKeys.AccountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);

        // Validate that split quantities don't exceed item quantities
        foreach (var splitItemInput in dto.Items)
        {
            var accountItem = account.Items.FirstOrDefault(i => i.Id == splitItemInput.AccountItemId);
            if (accountItem is null)
                return OperationResult<AccountDetailReadDto>.NotFound(
                    $"Ítem {splitItemInput.AccountItemId} no encontrado en la cuenta.",
                    ErrorKeys.AccountItemNotFound);

            // Check total already allocated to other splits for this item
            var alreadyAllocated = await _context.AccountSplitItems
                .Include(si => si.AccountSplit)
                .Where(si => si.AccountItemId == splitItemInput.AccountItemId
                          && si.AccountSplit.AccountId == dto.AccountId)
                .SumAsync(si => si.Quantity);

            if (alreadyAllocated + splitItemInput.Quantity > accountItem.Quantity)
                return OperationResult<AccountDetailReadDto>.Conflict(
                    $"La cantidad asignada para '{accountItem.ProductName}' excede la cantidad del ítem.",
                    ErrorKeys.AccountSplitQtyExceedsItem);
        }

        var split = new AccountSplit
        {
            AccountId = dto.AccountId,
            SplitName = dto.SplitName.Trim(),
        };

        _context.AccountSplits.Add(split);
        await _context.SaveChangesAsync();

        var splitItems = dto.Items.Select(i => new AccountSplitItem
        {
            AccountSplitId = split.Id,
            AccountItemId  = i.AccountItemId,
            Quantity       = i.Quantity,
        }).ToList();

        _context.AccountSplitItems.AddRange(splitItems);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(dto.AccountId, AuditActions.SplitCreated,
            $"División creada: {split.SplitName} ({splitItems.Count} ítems)");

        var loaded = await LoadAccountDetail(dto.AccountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "División creada correctamente.");
    }

    // ── REMOVE SPLIT ──────────────────────────────────────────────────────────

    public async Task<OperationResult<AccountDetailReadDto>> RemoveSplit(int accountSplitId)
    {
        var split = await _context.AccountSplits
            .Include(s => s.Account)
            .FirstOrDefaultAsync(s => s.Id == accountSplitId);

        if (split is null)
            return OperationResult<AccountDetailReadDto>.NotFound(
                "División no encontrada.", ErrorKeys.AccountSplitNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(split.Account.BranchId);

        var accountId = split.AccountId;
        var splitName = split.SplitName;
        _context.AccountSplits.Remove(split);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(accountId, AuditActions.SplitRemoved,
            $"División eliminada: {splitName}");

        var loaded = await LoadAccountDetail(accountId);
        return OperationResult<AccountDetailReadDto>.Ok(
            MapToDetail(loaded!), "División eliminada correctamente.");
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────────

    private async Task<Account?> LoadAccountDetail(int id)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Items)
                .ThenInclude(i => i.Discounts)
                    .ThenInclude(d => d.BranchDiscount)
            .Include(a => a.Discounts)
                .ThenInclude(d => d.BranchDiscount)
            .Include(a => a.Discounts)
                .ThenInclude(d => d.AuthorizedByUser)
            .Include(a => a.Splits)
                .ThenInclude(s => s.Items)
                    .ThenInclude(si => si.AccountItem)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    private static AccountReadDto MapToSummary(Account a)
    {
        var approvedDiscounts = a.Discounts
            .Where(d => d.Status == AccountDiscountStatus.Approved)
            .ToList();

        var subtotal = a.Items.Sum(i => i.Quantity * i.UnitPrice);
        var itemDisc = a.Items.Sum(i =>
            i.Discounts
             .Where(d => d.Status == AccountDiscountStatus.Approved
                      && d.AppliesTo == DiscountAppliesTo.SpecificItem)
             .Sum(d => d.DiscountType == DiscountType.Percentage
                         ? i.Quantity * i.UnitPrice * d.DiscountValue / 100m
                         : d.DiscountValue));
        var acctDisc = approvedDiscounts
            .Where(d => d.AppliesTo == DiscountAppliesTo.WholeAccount
                     || d.AppliesTo == DiscountAppliesTo.Both)
            .Sum(d => d.DiscountType == DiscountType.Percentage
                        ? (subtotal - itemDisc) * d.DiscountValue / 100m
                        : d.DiscountValue);

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
            TotalAmount:           Math.Max(0, subtotal - itemDisc - acctDisc),
            ItemCount:             a.Items.Count
        );
    }

    private static AccountDetailReadDto MapToDetail(Account a)
    {
        var itemDtos = a.Items.Select(i =>
        {
            var itemDiscounts = i.Discounts
                .Where(d => d.AppliesTo == DiscountAppliesTo.SpecificItem
                         && d.Status    == AccountDiscountStatus.Approved)
                .ToList();

            var itemDiscountAmt = itemDiscounts.Sum(d =>
                d.DiscountType == DiscountType.Percentage
                    ? i.Quantity * i.UnitPrice * d.DiscountValue / 100m
                    : d.DiscountValue);

            return new AccountItemReadDto(
                Id:               i.Id,
                AccountId:        i.AccountId,
                BranchProductId:  i.BranchProductId,
                ProductName:      i.ProductName,
                UnitPrice:        i.UnitPrice,
                Quantity:         i.Quantity,
                Notes:            i.Notes,
                LineTotal:        Math.Max(0, i.Quantity * i.UnitPrice - itemDiscountAmt),
                AppliedDiscounts: i.Discounts.Select(MapDiscountToDto).ToList()
            );
        }).ToList();

        var subtotal      = itemDtos.Sum(i => (decimal)i.Quantity * i.UnitPrice);
        var itemDiscTotal = itemDtos.Sum(i => i.Quantity * i.UnitPrice - i.LineTotal);

        var acctDiscounts = a.Discounts
            .Where(d => (d.AppliesTo == DiscountAppliesTo.WholeAccount
                      || d.AppliesTo == DiscountAppliesTo.Both)
                     && d.Status == AccountDiscountStatus.Approved)
            .ToList();

        var acctDiscTotal = acctDiscounts.Sum(d =>
            d.DiscountType == DiscountType.Percentage
                ? (subtotal - itemDiscTotal) * d.DiscountValue / 100m
                : d.DiscountValue);

        var allDiscountDtos = a.Discounts.Select(MapDiscountToDto).ToList();

        var splitDtos = a.Splits.Select(s =>
        {
            var splitItemDtos = s.Items.Select(si => new AccountSplitItemReadDto(
                Id:             si.Id,
                AccountSplitId: si.AccountSplitId,
                AccountItemId:  si.AccountItemId,
                ProductName:    si.AccountItem?.ProductName ?? string.Empty,
                UnitPrice:      si.AccountItem?.UnitPrice   ?? 0m,
                Quantity:       si.Quantity,
                LineTotal:      si.Quantity * (si.AccountItem?.UnitPrice ?? 0m)
            )).ToList();

            return new AccountSplitReadDto(
                Id:         s.Id,
                AccountId:  s.AccountId,
                SplitName:  s.SplitName,
                Items:      splitItemDtos,
                SplitTotal: splitItemDtos.Sum(si => si.LineTotal)
            );
        }).ToList();

        return new AccountDetailReadDto(
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
            Items:                 itemDtos,
            Discounts:             allDiscountDtos,
            Splits:                splitDtos,
            Subtotal:              subtotal,
            TotalDiscounts:        itemDiscTotal + acctDiscTotal,
            Total:                 Math.Max(0, subtotal - itemDiscTotal - acctDiscTotal)
        );
    }

    private static AccountDiscountReadDto MapDiscountToDto(AccountDiscount d)
    {
        return new AccountDiscountReadDto(
            Id:                   d.Id,
            AccountId:            d.AccountId,
            BranchDiscountId:     d.BranchDiscountId,
            DiscountName:         d.BranchDiscount?.Name ?? string.Empty,
            AccountItemId:        d.AccountItemId,
            ProductName:          null,
            DiscountType:         d.DiscountType,
            DiscountValue:        d.DiscountValue,
            AppliesTo:            d.AppliesTo,
            Reason:               d.Reason,
            Status:               d.Status,
            StatusLabel:          GetDiscountStatusLabel(d.Status),
            AuthorizedByUserId:   d.AuthorizedByUserId,
            AuthorizedByUserName: d.AuthorizedByUser?.FullName,
            CreatedAt:            d.CreatedAt
        );
    }

    private static decimal CalculateTotal(
        List<AccountItem> items, List<AccountDiscount> discounts)
    {
        var subtotal = items.Sum(i => i.Quantity * i.UnitPrice);

        var itemDiscAmt = items.Sum(i =>
        {
            var itemDiscs = discounts
                .Where(d => d.AccountItemId == i.Id
                         && d.Status        == AccountDiscountStatus.Approved
                         && d.AppliesTo     == DiscountAppliesTo.SpecificItem)
                .ToList();
            return itemDiscs.Sum(d =>
                d.DiscountType == DiscountType.Percentage
                    ? i.Quantity * i.UnitPrice * d.DiscountValue / 100m
                    : d.DiscountValue);
        });

        var afterItemDisc = subtotal - itemDiscAmt;

        var acctDiscAmt = discounts
            .Where(d => d.AccountItemId is null
                     && d.Status == AccountDiscountStatus.Approved
                     && (d.AppliesTo == DiscountAppliesTo.WholeAccount
                      || d.AppliesTo == DiscountAppliesTo.Both))
            .Sum(d => d.DiscountType == DiscountType.Percentage
                        ? afterItemDisc * d.DiscountValue / 100m
                        : d.DiscountValue);

        return Math.Max(0, afterItemDisc - acctDiscAmt);
    }

    private static string GetStatusLabel(AccountStatus status) => status switch
    {
        AccountStatus.Open           => "Abierta",
        AccountStatus.PendingPayment => "Tab / Pendiente de pago",
        AccountStatus.Closed         => "Cerrada",
        AccountStatus.Cancelled      => "Cancelada",
        _                            => status.ToString()
    };

    private static string GetDiscountStatusLabel(AccountDiscountStatus status) => status switch
    {
        AccountDiscountStatus.Approved        => "Aprobado",
        AccountDiscountStatus.PendingApproval => "Pendiente de aprobación",
        AccountDiscountStatus.Rejected        => "Rechazado",
        _                                     => status.ToString()
    };
}
