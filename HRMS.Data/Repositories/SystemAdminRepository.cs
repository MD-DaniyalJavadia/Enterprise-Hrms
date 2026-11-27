using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Admin;

namespace HRMS.Data.Repositories;

public sealed class SystemAdminRepository : BaseRepository, ISystemAdminRepository
{
    public SystemAdminRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<List<NotificationInboxItemViewModel>> GetNotificationInboxAsync(
        int companyId, int userId, bool? filterRead, string? notificationType, string? moduleName, string? search)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@UserId", userId);
        p.Add("@FilterRead", filterRead);
        p.Add("@NotificationType", notificationType?.Trim());
        p.Add("@ModuleName", moduleName?.Trim());
        p.Add("@Search", search?.Trim());
        return (await QuerySpAsync<NotificationInboxItemViewModel>("dbo.sp_Notification_GetInbox", p)).ToList();
    }

    public async Task<int> SendBroadcastNotificationAsync(int companyId, BroadcastNotificationFormViewModel model, int senderUserId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@Title", model.Title.Trim());
        p.Add("@Message", model.Message.Trim());
        p.Add("@NotificationType", model.NotificationType);
        p.Add("@Priority", model.Priority);
        p.Add("@ModuleName", model.ModuleName);
        p.Add("@ActionUrl", string.IsNullOrWhiteSpace(model.ActionUrl) ? null : model.ActionUrl.Trim());
        p.Add("@TargetScope", model.TargetScope);
        p.Add("@TargetRoleId", model.TargetRoleId);
        p.Add("@TargetDepartmentId", model.TargetDepartmentId);
        p.Add("@TargetUserId", model.TargetUserId);
        p.Add("@SenderUserId", senderUserId);
        p.Add("@RecipientsInserted", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_Notification_SendBroadcast", p);
        return p.Get<int>("@RecipientsInserted");
    }

    public Task DeleteNotificationAsync(int companyId, long notificationId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@NotificationId", notificationId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_Notification_Delete", p);
    }

    public async Task<List<NotificationTemplate>> GetNotificationTemplatesAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        return (await QuerySpAsync<NotificationTemplate>("dbo.sp_NotificationTemplate_GetAll", p)).ToList();
    }

    public async Task<int> SaveNotificationTemplateAsync(int companyId, NotificationTemplateFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@TemplateId", model.TemplateId);
        p.Add("@CompanyId", companyId);
        p.Add("@TemplateCode", model.TemplateCode.Trim().ToUpperInvariant());
        p.Add("@TemplateName", model.TemplateName.Trim());
        p.Add("@ModuleName", model.ModuleName);
        p.Add("@EventTrigger", model.EventTrigger);
        p.Add("@SubjectTemplate", model.SubjectTemplate.Trim());
        p.Add("@BodyTemplate", model.BodyTemplate.Trim());
        p.Add("@DefaultType", model.DefaultType);
        p.Add("@DefaultPriority", model.DefaultPriority);
        p.Add("@SendInApp", model.SendInApp);
        p.Add("@SendEmailDigest", model.SendEmailDigest);
        p.Add("@IsActive", model.IsActive);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_NotificationTemplate_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteNotificationTemplateAsync(int companyId, int templateId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@TemplateId", templateId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_NotificationTemplate_Delete", p);
    }

    public async Task<(List<WorkflowDefinition> Definitions, List<WorkflowStep> Steps)> GetWorkflowDefinitionsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_WorkflowDefinition_GetAll", p, commandType: CommandType.StoredProcedure);
        var definitions = (await multi.ReadAsync<WorkflowDefinition>()).ToList();
        var steps = (await multi.ReadAsync<WorkflowStep>()).ToList();
        return (definitions, steps);
    }

    public async Task<int> SaveWorkflowDefinitionAsync(int companyId, WorkflowDefinitionFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@WorkflowId", model.WorkflowId);
        p.Add("@CompanyId", companyId);
        p.Add("@WorkflowCode", model.WorkflowCode.Trim().ToUpperInvariant());
        p.Add("@WorkflowName", model.WorkflowName.Trim());
        p.Add("@ModuleName", model.ModuleName);
        p.Add("@DepartmentId", model.DepartmentId);
        p.Add("@MinAmountThreshold", model.MinAmountThreshold);
        p.Add("@AutoEscalateHours", model.AutoEscalateHours);
        p.Add("@Description", model.Description?.Trim());
        p.Add("@IsActive", model.IsActive);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_WorkflowDefinition_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteWorkflowDefinitionAsync(int companyId, int workflowId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@WorkflowId", workflowId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_WorkflowDefinition_Delete", p);
    }

    public async Task<int> SaveWorkflowStepAsync(int companyId, WorkflowStepFormViewModel model)
    {
        var p = new DynamicParameters();
        p.Add("@WorkflowStepId", model.WorkflowStepId);
        p.Add("@CompanyId", companyId);
        p.Add("@WorkflowId", model.WorkflowId);
        p.Add("@StepOrder", model.StepOrder);
        p.Add("@StepName", model.StepName.Trim());
        p.Add("@ApproverType", model.ApproverType);
        p.Add("@ApproverRoleId", model.ApproverRoleId);
        p.Add("@ApproverEmployeeId", model.ApproverEmployeeId);
        p.Add("@EscalationRoleId", model.EscalationRoleId);
        p.Add("@SlaHours", model.SlaHours);
        p.Add("@CanDelegate", model.CanDelegate);
        p.Add("@IsMandatory", model.IsMandatory);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_WorkflowStep_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteWorkflowStepAsync(int companyId, int workflowStepId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@WorkflowStepId", workflowStepId);
        return ExecuteSpAsync("dbo.sp_WorkflowStep_Delete", p);
    }

    public async Task<(List<WorkflowInstance> Instances, List<WorkflowActionLog> ActionLogs)> GetWorkflowInstancesAsync(
        int companyId, string? moduleName, string? instanceStatus)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@ModuleName", moduleName?.Trim());
        p.Add("@InstanceStatus", instanceStatus?.Trim());

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_WorkflowInstance_GetAll", p, commandType: CommandType.StoredProcedure);
        var instances = (await multi.ReadAsync<WorkflowInstance>()).ToList();
        var logs = (await multi.ReadAsync<WorkflowActionLog>()).ToList();
        return (instances, logs);
    }

    public async Task<long> InitiateWorkflowInstanceAsync(
        int companyId, InitiateWorkflowInstanceFormViewModel model, int requestedByUserId, int? requestedByEmployeeId, string actorName)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@WorkflowId", model.WorkflowId);
        p.Add("@EntityId", model.EntityId.Trim());
        p.Add("@ReferenceNumber", model.ReferenceNumber.Trim().ToUpperInvariant());
        p.Add("@Summary", model.Summary.Trim());
        p.Add("@RequestedByUserId", requestedByUserId);
        p.Add("@RequestedByEmployeeId", requestedByEmployeeId);
        p.Add("@ActorName", actorName);
        p.Add("@NewInstanceId", dbType: DbType.Int64, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_WorkflowInstance_Initiate", p);
        return p.Get<long>("@NewInstanceId");
    }

    public Task AdvanceWorkflowInstanceAsync(
        int companyId, AdvanceWorkflowFormViewModel model, int actorUserId, string actorName)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@InstanceId", model.InstanceId);
        p.Add("@ActionTaken", model.ActionTaken);
        p.Add("@Comments", model.Comments?.Trim());
        p.Add("@ActorUserId", actorUserId);
        p.Add("@ActorName", actorName);
        return ExecuteSpAsync("dbo.sp_WorkflowInstance_Advance", p);
    }

    public async Task<(List<AuditLog> AuditLogs, List<LoginHistoryEntry> LoginHistory)> SearchAuditLogsAsync(
        int companyId, string? moduleName, string? actionType, string? username, DateTime? fromDate, DateTime? toDate, string? search, int topCount = 250)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@ModuleName", moduleName?.Trim());
        p.Add("@ActionType", actionType?.Trim());
        p.Add("@Username", username?.Trim());
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        p.Add("@Search", search?.Trim());
        p.Add("@TopCount", topCount);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_AuditLog_Search", p, commandType: CommandType.StoredProcedure);
        var audits = (await multi.ReadAsync<AuditLog>()).ToList();
        var logins = (await multi.ReadAsync<LoginHistoryEntry>()).ToList();
        return (audits, logins);
    }

    public async Task<List<SystemSetting>> GetSystemSettingsAsync(int companyId, string? category = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@Category", category?.Trim());
        return (await QuerySpAsync<SystemSetting>("dbo.sp_SystemSettings_GetAll", p)).ToList();
    }

    public Task UpsertSystemSettingAsync(int companyId, SystemSettingFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SettingKey", model.SettingKey.Trim());
        p.Add("@SettingValue", model.SettingValue.Trim());
        p.Add("@Category", model.Category);
        p.Add("@DataType", model.DataType);
        p.Add("@Description", model.Description?.Trim());
        p.Add("@ModifiedBy", userId);
        return ExecuteSpAsync("dbo.sp_SystemSettings_Upsert", p);
    }

    public async Task<(List<RoleSummaryViewModel> Roles, List<Permission> Permissions, List<RolePermissionLinkViewModel> Links)> GetRolesAndPermissionsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Admin_GetRolesAndPermissions", p, commandType: CommandType.StoredProcedure);
        var roles = (await multi.ReadAsync<RoleSummaryViewModel>()).ToList();
        var permissions = (await multi.ReadAsync<Permission>()).ToList();
        var links = (await multi.ReadAsync<RolePermissionLinkViewModel>()).ToList();
        return (roles, permissions, links);
    }

    public Task SaveRolePermissionsAsync(int companyId, int roleId, string permissionIdsCsv, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@RoleId", roleId);
        p.Add("@PermissionIdsCsv", permissionIdsCsv);
        p.Add("@ModifiedBy", userId);
        return ExecuteSpAsync("dbo.sp_Admin_SaveRolePermissions", p);
    }

    public async Task<List<AdminUserListItemViewModel>> GetUsersAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        return (await QuerySpAsync<AdminUserListItemViewModel>("dbo.sp_Admin_GetUsers", p)).ToList();
    }

    public async Task<int> CreateOrUpdateUserAsync(int companyId, AdminUserFormViewModel model, string? passwordHash, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@UserId", model.UserId);
        p.Add("@EmployeeId", model.EmployeeId);
        p.Add("@BranchId", model.BranchId);
        p.Add("@Username", model.Username.Trim());
        p.Add("@Email", model.Email.Trim());
        p.Add("@FullName", model.FullName.Trim());
        p.Add("@PasswordHash", passwordHash);
        p.Add("@RoleId", model.RoleId);
        p.Add("@IsActive", model.IsActive);
        p.Add("@MustChangePassword", model.MustChangePassword);
        p.Add("@ModifiedBy", userId);
        p.Add("@SavedUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_Admin_CreateOrUpdateUser", p);
        return p.Get<int>("@SavedUserId");
    }

    public Task ToggleUserStatusAsync(int companyId, int targetUserId, string action, string? newPasswordHash, int modifiedBy)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@UserId", targetUserId);
        p.Add("@Action", action);
        p.Add("@NewPasswordHash", newPasswordHash);
        p.Add("@ModifiedBy", modifiedBy);
        return ExecuteSpAsync("dbo.sp_Admin_ToggleUserStatus", p);
    }

    public Task<SystemHealthDiagnosticsViewModel?> GetSystemHealthAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        return QueryFirstOrDefaultSpAsync<SystemHealthDiagnosticsViewModel>("dbo.sp_System_HealthCheck", p, commandTimeout: 10);
    }

    public async Task<MaintenanceCleanupResultViewModel> RunMaintenanceCleanupAsync(
        int companyId, int notificationRetentionDays, int loginHistoryRetentionDays, int actorUserId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@NotificationRetentionDays", notificationRetentionDays);
        p.Add("@LoginHistoryRetentionDays", loginHistoryRetentionDays);
        p.Add("@ActorUserId", actorUserId);
        return await QueryFirstOrDefaultSpAsync<MaintenanceCleanupResultViewModel>("dbo.sp_System_MaintenanceCleanup", p, commandTimeout: 60)
               ?? new MaintenanceCleanupResultViewModel { ExecutedAtUtc = DateTime.UtcNow };
    }
}
