using HRMS.Models.Entities;

namespace HRMS.Data.Interfaces;

public interface IAuditLogRepository
{
    Task<long> InsertAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int companyId, int topCount = 20);
}

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int companyId, int userId, int topCount = 15);
    Task<int> MarkAsReadAsync(long notificationId, int userId);
    Task<int> MarkAllAsReadAsync(int companyId, int userId);
}
