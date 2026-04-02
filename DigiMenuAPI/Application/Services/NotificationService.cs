using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DigiMenuAPI.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService       _tenantService;
    private readonly IMemoryCache         _cache;

    private static readonly TimeSpan UnreadCountCacheDuration = TimeSpan.FromSeconds(15);

    public NotificationService(ApplicationDbContext context, ITenantService tenantService, IMemoryCache cache)
    {
        _context       = context;
        _tenantService = tenantService;
        _cache         = cache;
    }

    public async Task CreateAsync(int companyId, string type, string message,
        int? branchId = null, int? targetUserId = null,
        string? relatedEntity = null, int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            CompanyId       = companyId,
            BranchId        = branchId,
            TargetUserId    = targetUserId,
            Type            = type,
            Message         = message,
            RelatedEntity   = relatedEntity,
            RelatedEntityId = relatedEntityId,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidar el contador cacheado del destinatario
        _cache.Remove(UnreadCountKey(companyId, targetUserId, branchId));
    }

    public async Task<OperationResult<PagedResult<NotificationReadDto>>> GetMine(int page, int pageSize)
    {
        var companyId = _tenantService.GetCompanyId();
        var userId    = _tenantService.TryGetUserId();
        var branchId  = _tenantService.TryGetBranchId();

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.CompanyId == companyId)
            .Where(n => n.TargetUserId == null || n.TargetUserId == userId)
            .Where(n => n.BranchId == null || n.BranchId == branchId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationReadDto(
                n.Id, n.Type, n.Message, n.RelatedEntity, n.RelatedEntityId,
                n.IsRead, n.CreatedAt))
            .ToListAsync();

        return OperationResult<PagedResult<NotificationReadDto>>.Ok(
            PagedResult<NotificationReadDto>.Create(items, total, page, pageSize));
    }

    public async Task<OperationResult<int>> GetUnreadCount()
    {
        var companyId = _tenantService.GetCompanyId();
        var userId    = _tenantService.TryGetUserId();
        var branchId  = _tenantService.TryGetBranchId();
        var cacheKey  = UnreadCountKey(companyId, userId, branchId);

        if (_cache.TryGetValue(cacheKey, out int cached))
            return OperationResult<int>.Ok(cached);

        var count = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.CompanyId == companyId)
            .Where(n => n.TargetUserId == null || n.TargetUserId == userId)
            .Where(n => n.BranchId == null || n.BranchId == branchId)
            .Where(n => !n.IsRead)
            .CountAsync();

        _cache.Set(cacheKey, count, UnreadCountCacheDuration);

        return OperationResult<int>.Ok(count);
    }

    public async Task<OperationResult<bool>> MarkAsRead(int id)
    {
        var companyId = _tenantService.GetCompanyId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId);

        if (notification is null)
            return OperationResult<bool>.NotFound("Notificación no encontrada.", "NOTIFICATION_NOT_FOUND");

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        InvalidateUnreadCache();
        return OperationResult<bool>.Ok(true);
    }

    public async Task<OperationResult<bool>> MarkAllAsRead()
    {
        var companyId = _tenantService.GetCompanyId();
        var userId    = _tenantService.TryGetUserId();
        var branchId  = _tenantService.TryGetBranchId();

        await _context.Notifications
            .Where(n => n.CompanyId == companyId)
            .Where(n => n.TargetUserId == null || n.TargetUserId == userId)
            .Where(n => n.BranchId == null || n.BranchId == branchId)
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        _cache.Remove(UnreadCountKey(companyId, userId, branchId));
        return OperationResult<bool>.Ok(true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string UnreadCountKey(int companyId, int? userId, int? branchId)
        => $"notif-unread-{companyId}-{userId}-{branchId}";

    private void InvalidateUnreadCache()
    {
        var companyId = _tenantService.GetCompanyId();
        var userId    = _tenantService.TryGetUserId();
        var branchId  = _tenantService.TryGetBranchId();
        _cache.Remove(UnreadCountKey(companyId, userId, branchId));
    }
}
