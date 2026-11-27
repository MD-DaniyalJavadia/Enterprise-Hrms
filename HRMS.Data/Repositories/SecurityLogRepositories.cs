using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;

namespace HRMS.Data.Repositories;

public sealed class AuditLogRepository : BaseRepository, IAuditLogRepository
{
    public AuditLogRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<long> InsertAsync(AuditLog auditLog)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", auditLog.CompanyId, DbType.Int32);
        parameters.Add("@UserId", auditLog.UserId, DbType.Int32);
        parameters.Add("@Username", auditLog.Username, DbType.String, size: 60);
        parameters.Add("@ActionType", auditLog.ActionType, DbType.String, size: 50);
        parameters.Add("@ModuleName", auditLog.ModuleName, DbType.String, size: 80);
        parameters.Add("@EntityName", auditLog.EntityName, DbType.String, size: 80);
        parameters.Add("@EntityId", auditLog.EntityId, DbType.String, size: 50);
        parameters.Add("@Description", auditLog.Description, DbType.String, size: 500);
        parameters.Add("@OldValues", auditLog.OldValues, DbType.String);
        parameters.Add("@NewValues", auditLog.NewValues, DbType.String);
        parameters.Add("@IpAddress", auditLog.IpAddress, DbType.String, size: 45);
        parameters.Add("@UserAgent", auditLog.UserAgent, DbType.String, size: 500);
        parameters.Add("@AuditLogId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_AuditLog_Insert", parameters);
        return parameters.Get<long>("@AuditLogId");
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int companyId, int topCount = 20)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@TopCount", topCount, DbType.Int32);

        return await QuerySpAsync<AuditLog>("sp_AuditLog_GetRecent", parameters);
    }
}

public sealed class NotificationRepository : BaseRepository, INotificationRepository
{
    public NotificationRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int companyId, int userId, int topCount = 15)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@TopCount", topCount, DbType.Int32);

        return await QuerySpAsync<Notification>("sp_Notification_GetByUserId", parameters);
    }

    public async Task<int> MarkAsReadAsync(long notificationId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@NotificationId", notificationId, DbType.Int64);
        parameters.Add("@UserId", userId, DbType.Int32);

        return await ExecuteSpAsync("sp_Notification_MarkAsRead", parameters);
    }

    public async Task<int> MarkAllAsReadAsync(int companyId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);

        return await ExecuteSpAsync("sp_Notification_MarkAllAsRead", parameters);
    }
}
