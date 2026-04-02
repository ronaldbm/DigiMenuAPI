using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services;

public class AccountAuditService : IAccountAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService       _tenantService;

    public AccountAuditService(ApplicationDbContext context, ITenantService tenantService)
    {
        _context       = context;
        _tenantService = tenantService;
    }

    public async Task LogAsync(int accountId, string action, string humanReadable, string? details = null)
    {
        var userId = _tenantService.TryGetUserId();
        string? userName = null;

        if (userId.HasValue)
        {
            userName = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }

        var entry = new AccountAuditEntry
        {
            AccountId     = accountId,
            Action        = action,
            UserId        = userId,
            UserName      = userName,
            Details       = details,
            HumanReadable = humanReadable,
            CreatedAt     = DateTime.UtcNow,
        };

        _context.AccountAuditEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<OperationResult<PagedResult<AccountAuditReadDto>>> GetByAccount(
        int accountId, int page, int pageSize)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account is null)
            return OperationResult<PagedResult<AccountAuditReadDto>>.NotFound(
                "Cuenta no encontrada.", "ACCOUNT_NOT_FOUND");

        await _tenantService.ValidateBranchOwnershipAsync(account.BranchId);

        var query = _context.AccountAuditEntries
            .AsNoTracking()
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync();

        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AccountAuditReadDto(
                e.Id,
                e.AccountId,
                e.Action,
                e.UserId,
                e.UserName,
                e.Details,
                e.HumanReadable,
                e.CreatedAt
            ))
            .ToListAsync();

        return OperationResult<PagedResult<AccountAuditReadDto>>.Ok(
            PagedResult<AccountAuditReadDto>.Create(entries, total, page, pageSize));
    }
}
