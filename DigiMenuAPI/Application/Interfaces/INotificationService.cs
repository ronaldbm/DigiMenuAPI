using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(int companyId, string type, string message,
        int? branchId = null, int? targetUserId = null,
        string? relatedEntity = null, int? relatedEntityId = null);

    Task<OperationResult<PagedResult<NotificationReadDto>>> GetMine(int page, int pageSize);

    Task<OperationResult<int>> GetUnreadCount();

    Task<OperationResult<bool>> MarkAsRead(int id);

    Task<OperationResult<bool>> MarkAllAsRead();
}
