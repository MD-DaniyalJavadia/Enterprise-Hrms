using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;

namespace HRMS.Business.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(
        int companyId,
        int? userId,
        string? username,
        string actionType,
        string moduleName,
        string entityName,
        string? entityId,
        string description,
        string? ipAddress = null,
        string? userAgent = null,
        string? oldValues = null,
        string? newValues = null)
    {
        var entry = new AuditLog
        {
            CompanyId = companyId,
            UserId = userId,
            Username = username,
            ActionType = actionType,
            ModuleName = moduleName,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _auditLogRepository.InsertAsync(entry);
    }

    public Task<IEnumerable<AuditLog>> GetRecentAsync(int companyId, int topCount = 20)
    {
        return _auditLogRepository.GetRecentAsync(companyId, topCount);
    }
}

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task<IEnumerable<Notification>> GetUserNotificationsAsync(int companyId, int userId, int topCount = 15)
    {
        return _notificationRepository.GetByUserIdAsync(companyId, userId, topCount);
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, int userId)
    {
        var affected = await _notificationRepository.MarkAsReadAsync(notificationId, userId);
        return affected > 0;
    }

    public Task<int> MarkAllAsReadAsync(int companyId, int userId)
    {
        return _notificationRepository.MarkAllAsReadAsync(companyId, userId);
    }
}
