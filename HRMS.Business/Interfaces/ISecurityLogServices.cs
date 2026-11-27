using HRMS.Models.Entities;

namespace HRMS.Business.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
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
        string? newValues = null);

    Task<IEnumerable<AuditLog>> GetRecentAsync(int companyId, int topCount = 20);
}

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int companyId, int userId, int topCount = 15);
    Task<bool> MarkAsReadAsync(long notificationId, int userId);
    Task<int> MarkAllAsReadAsync(int companyId, int userId);
}
