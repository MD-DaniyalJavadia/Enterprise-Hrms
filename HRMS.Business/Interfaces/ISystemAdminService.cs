using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Admin;

namespace HRMS.Business.Interfaces;

public interface ISystemAdminService
{
    Task<NotificationInboxPageViewModel> GetNotificationInboxAsync(
        int companyId, int userId, bool? filterRead, string? notificationType, string? moduleName, string? search, bool canManage);
    Task<OperationResult<int>> SendBroadcastNotificationAsync(
        int companyId, BroadcastNotificationFormViewModel model, int senderUserId, string senderUsername);
    Task<OperationResult> DeleteNotificationAsync(int companyId, long notificationId, int userId);
    Task<OperationResult<int>> SaveNotificationTemplateAsync(
        int companyId, NotificationTemplateFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteNotificationTemplateAsync(int companyId, int templateId, int userId, string username);

    Task<WorkflowsPageViewModel> GetWorkflowsPageAsync(
        int companyId, string? moduleFilter, string? statusFilter, bool canManage);
    Task<OperationResult<int>> SaveWorkflowDefinitionAsync(
        int companyId, WorkflowDefinitionFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteWorkflowDefinitionAsync(int companyId, int workflowId, int userId, string username);
    Task<OperationResult<int>> SaveWorkflowStepAsync(
        int companyId, WorkflowStepFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteWorkflowStepAsync(int companyId, int workflowStepId, int userId, string username);
    Task<OperationResult<long>> InitiateWorkflowInstanceAsync(
        int companyId, InitiateWorkflowInstanceFormViewModel model, int userId, int? employeeId, string actorName);
    Task<OperationResult> AdvanceWorkflowInstanceAsync(
        int companyId, AdvanceWorkflowFormViewModel model, int userId, string actorName);

    Task<AuditLogsPageViewModel> GetAuditLogsPageAsync(
        int companyId, string? moduleName, string? actionType, string? username, DateTime? fromDate, DateTime? toDate, string? search, bool canManage);

    Task<SystemSettingsPageViewModel> GetSystemSettingsPageAsync(int companyId, string? category, bool canManage);
    Task<OperationResult> UpsertSystemSettingAsync(int companyId, SystemSettingFormViewModel model, int userId, string username);

    Task<AccessControlPageViewModel> GetAccessControlPageAsync(int companyId, bool canManage);
    Task<OperationResult> SaveRolePermissionsAsync(int companyId, SaveRolePermissionsFormViewModel model, int userId, string username);
    Task<OperationResult<int>> SaveUserAccountAsync(int companyId, AdminUserFormViewModel model, int userId, string username);
    Task<OperationResult> ToggleUserStatusAsync(int companyId, int targetUserId, string action, string? newPassword, int userId, string username);

    Task<SystemHealthDiagnosticsViewModel?> CheckDatabaseHealthAsync();
    Task<SystemHealthDiagnosticsViewModel> GetSystemHealthPageAsync(int companyId, bool canManage);
    Task<OperationResult<MaintenanceCleanupResultViewModel>> RunMaintenanceCleanupAsync(
        int companyId, MaintenanceCleanupFormViewModel model, int userId, string username);
}
