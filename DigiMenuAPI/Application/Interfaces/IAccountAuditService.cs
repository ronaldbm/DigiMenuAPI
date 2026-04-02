using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces;

public interface IAccountAuditService
{
    Task LogAsync(int accountId, string action, string humanReadable, string? details = null);
    Task<OperationResult<PagedResult<AccountAuditReadDto>>> GetByAccount(int accountId, int page, int pageSize);
}
