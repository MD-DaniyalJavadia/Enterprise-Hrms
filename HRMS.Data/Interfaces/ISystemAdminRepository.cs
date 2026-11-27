using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Admin;

namespace HRMS.Data.Interfaces;

public interface ISystemAdminRepository
{
    Task<List<NotificationInboxItemViewModel>> GetNotificationInboxAsync(
        int companyId, int userId, bool? filterRead, string? notificationType, string? moduleName, string? search);
    Task<int> SendBroadcastNotificationAsync(int companyId, BroadcastNotificationFormViewModel model, int senderUserId);
    Task DeleteNotificationAsync(int companyId, long notificationId, int userId);

    Task<List<NotificationTemplate>> GetNotificationTemplatesAsync(int companyId);
    Task<int> SaveNotificationTemplateAsync(int companyId, NotificationTemplateFormViewModel model, int userId);
    Task DeleteNotificationTemplateAsync(int companyId, int templateId, int userId);

    Task<(List<WorkflowDefinition> Definitions, List<WorkflowStep> Steps)> GetWorkflowDefinitionsAsync(int companyId);
    Task<int> SaveWorkflowDefinitionAsync(int companyId, WorkflowDefinitionFormViewModel model, int userId);
    Task DeleteWorkflowDefinitionAsync(int companyId, int workflowId, int userId);
    Task<int> SaveWorkflowStepAsync(int companyId, WorkflowStepFormViewModel model);
    Task DeleteWorkflowStepAsync(int companyId, int workflowStepId);

    Task<(List<WorkflowInstance> Instances, List<WorkflowActionLog> ActionLogs)> GetWorkflowInstancesAsync(
        int companyId, string? moduleName, string? instanceStatus);
    Task<long> InitiateWorkflowInstanceAsync(
        int companyId, InitiateWorkflowInstanceFormViewModel model, int requestedByUserId, int? requestedByEmployeeId, string actorName);
    Task AdvanceWorkflowInstanceAsync(
        int companyId, AdvanceWorkflowFormViewModel model, int actorUserId, string actorName);

    Task<(List<AuditLog> AuditLogs, List<LoginHistoryEntry> LoginHistory)> SearchAuditLogsAsync(
        int companyId, string? moduleName, string? actionType, string? username, DateTime? fromDate, DateTime? toDate, string? search, int topCount = 250);

    Task<List<SystemSetting>> GetSystemSettingsAsync(int companyId, string? category = null);
    Task UpsertSystemSettingAsync(int companyId, SystemSettingFormViewModel model, int userId);

    Task<(List<RoleSummaryViewModel> Roles, List<Permission> Permissions, List<RolePermissionLinkViewModel> Links)> GetRolesAndPermissionsAsync(int companyId);
    Task SaveRolePermissionsAsync(int companyId, int roleId, string permissionIdsCsv, int userId);

    Task<List<AdminUserListItemViewModel>> GetUsersAsync(int companyId);
    Task<int> CreateOrUpdateUserAsync(int companyId, AdminUserFormViewModel model, string? passwordHash, int userId);
    Task ToggleUserStatusAsync(int companyId, int targetUserId, string action, string? newPasswordHash, int modifiedBy);

    Task<SystemHealthDiagnosticsViewModel?> GetSystemHealthAsync(int companyId);
    Task<MaintenanceCleanupResultViewModel> RunMaintenanceCleanupAsync(
        int companyId, int notificationRetentionDays, int loginHistoryRetentionDays, int actorUserId);
}
