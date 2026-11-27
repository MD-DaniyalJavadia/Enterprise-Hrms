using HRMS.Business.Interfaces;
using HRMS.Business.Security;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Admin;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class SystemAdminService : ISystemAdminService
{
    private static readonly HashSet<string> ValidWorkflowActions = new(StringComparer.OrdinalIgnoreCase)
        { "Approved", "Rejected", "Escalated", "Delegated", "Cancelled" };

    private readonly ISystemAdminRepository _repository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;

    public SystemAdminService(
        ISystemAdminRepository repository,
        IDepartmentRepository departmentRepository,
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
        _branchRepository = branchRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
    }

    public async Task<NotificationInboxPageViewModel> GetNotificationInboxAsync(
        int companyId, int userId, bool? filterRead, string? notificationType, string? moduleName, string? search, bool canManage)
    {
        var items = await _repository.GetNotificationInboxAsync(companyId, userId, filterRead, notificationType, moduleName, search);
        var model = new NotificationInboxPageViewModel
        {
            FilterRead = filterRead,
            NotificationType = notificationType,
            ModuleName = moduleName,
            Search = search,
            CanManageNotifications = canManage,
            Notifications = items
        };

        if (canManage)
        {
            model.Templates = await _repository.GetNotificationTemplatesAsync(companyId);
            var (roles, _, _) = await _repository.GetRolesAndPermissionsAsync(companyId);
            model.Roles = roles;
            model.Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList();
            model.Users = await _repository.GetUsersAsync(companyId);
        }

        return model;
    }

    public async Task<OperationResult<int>> SendBroadcastNotificationAsync(
        int companyId, BroadcastNotificationFormViewModel model, int senderUserId, string senderUsername)
    {
        if (!string.IsNullOrWhiteSpace(model.ActionUrl) && !model.ActionUrl.TrimStart().StartsWith('/'))
            return OperationResult<int>.Failure("Action URL must be a relative in-app route starting with '/'.");

        if (model.TargetScope == "Role" && !model.TargetRoleId.HasValue)
            return OperationResult<int>.Failure("Please select a target role for Role-scoped broadcast.");
        if (model.TargetScope == "Department" && !model.TargetDepartmentId.HasValue)
            return OperationResult<int>.Failure("Please select a target department for Department-scoped broadcast.");
        if (model.TargetScope == "SpecificUser" && !model.TargetUserId.HasValue)
            return OperationResult<int>.Failure("Please select a target user account.");

        var count = await _repository.SendBroadcastNotificationAsync(companyId, model, senderUserId);
        if (count == 0)
            return OperationResult<int>.Failure("No active recipients matched the selected target audience.");

        await _auditLogService.LogAsync(companyId, senderUserId, senderUsername, AuditActionType.Create.ToString(),
            "Notifications", "Broadcast", model.TargetScope,
            $"Dispatched '{model.Title}' ({model.Priority}) to {count} recipient(s).");

        return OperationResult<int>.Success(count, $"Notification sent to {count} recipient(s).");
    }

    public async Task<OperationResult> DeleteNotificationAsync(int companyId, long notificationId, int userId)
    {
        await _repository.DeleteNotificationAsync(companyId, notificationId, userId);
        return OperationResult.Success("Notification removed.");
    }

    public async Task<OperationResult<int>> SaveNotificationTemplateAsync(
        int companyId, NotificationTemplateFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _repository.SaveNotificationTemplateAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.TemplateId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Notifications", "NotificationTemplate", id.ToString(),
                $"Saved notification template '{model.TemplateCode}'.");
            return OperationResult<int>.Success(id, "Notification template saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteNotificationTemplateAsync(int companyId, int templateId, int userId, string username)
    {
        await _repository.DeleteNotificationTemplateAsync(companyId, templateId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Notifications", "NotificationTemplate", templateId.ToString(),
            $"Deleted notification template #{templateId}.");
        return OperationResult.Success("Notification template deleted.");
    }

    public async Task<WorkflowsPageViewModel> GetWorkflowsPageAsync(
        int companyId, string? moduleFilter, string? statusFilter, bool canManage)
    {
        var (definitions, steps) = await _repository.GetWorkflowDefinitionsAsync(companyId);
        var (instances, logs) = await _repository.GetWorkflowInstancesAsync(companyId, moduleFilter, statusFilter);
        var (roles, _, _) = await _repository.GetRolesAndPermissionsAsync(companyId);

        return new WorkflowsPageViewModel
        {
            ModuleFilter = moduleFilter,
            StatusFilter = statusFilter,
            CanManageWorkflows = canManage,
            Definitions = definitions,
            Steps = steps,
            Instances = instances,
            ActionLogs = logs,
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList(),
            Roles = roles,
            Employees = (await _employeeRepository.GetAllAsync(companyId, null, null, null, null)).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveWorkflowDefinitionAsync(
        int companyId, WorkflowDefinitionFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _repository.SaveWorkflowDefinitionAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.WorkflowId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Workflows", "WorkflowDefinition", id.ToString(),
                $"Saved workflow '{model.WorkflowCode}' ({model.ModuleName}).");
            return OperationResult<int>.Success(id, "Workflow definition saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteWorkflowDefinitionAsync(int companyId, int workflowId, int userId, string username)
    {
        try
        {
            await _repository.DeleteWorkflowDefinitionAsync(companyId, workflowId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Workflows", "WorkflowDefinition", workflowId.ToString(),
                $"Deleted workflow definition #{workflowId}.");
            return OperationResult.Success("Workflow definition deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveWorkflowStepAsync(
        int companyId, WorkflowStepFormViewModel model, int userId, string username)
    {
        if (model.ApproverType == "RoleBased" && !model.ApproverRoleId.HasValue)
            return OperationResult<int>.Failure("Please select an Approver Role for Role-Based steps.");
        if (model.ApproverType == "SpecificEmployee" && !model.ApproverEmployeeId.HasValue)
            return OperationResult<int>.Failure("Please select a Specific Employee for this approval step.");

        try
        {
            var id = await _repository.SaveWorkflowStepAsync(companyId, model);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.WorkflowStepId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Workflows", "WorkflowStep", id.ToString(),
                $"Saved step #{model.StepOrder} '{model.StepName}' on workflow #{model.WorkflowId}.");
            return OperationResult<int>.Success(id, "Workflow step saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteWorkflowStepAsync(int companyId, int workflowStepId, int userId, string username)
    {
        await _repository.DeleteWorkflowStepAsync(companyId, workflowStepId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Workflows", "WorkflowStep", workflowStepId.ToString(),
            $"Removed workflow step #{workflowStepId}.");
        return OperationResult.Success("Workflow step deleted.");
    }

    public async Task<OperationResult<long>> InitiateWorkflowInstanceAsync(
        int companyId, InitiateWorkflowInstanceFormViewModel model, int userId, int? employeeId, string actorName)
    {
        try
        {
            var id = await _repository.InitiateWorkflowInstanceAsync(companyId, model, userId, employeeId, actorName);
            await _auditLogService.LogAsync(companyId, userId, actorName, AuditActionType.Create.ToString(),
                "Workflows", "WorkflowInstance", id.ToString(),
                $"Initiated workflow instance '{model.ReferenceNumber}' on workflow #{model.WorkflowId}.");
            return OperationResult<long>.Success(id, $"Workflow instance {model.ReferenceNumber} initiated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<long>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> AdvanceWorkflowInstanceAsync(
        int companyId, AdvanceWorkflowFormViewModel model, int userId, string actorName)
    {
        if (!ValidWorkflowActions.Contains(model.ActionTaken))
            return OperationResult.Failure("Unsupported workflow transition action.");

        var normalizedAction = ValidWorkflowActions.First(x => x.Equals(model.ActionTaken, StringComparison.OrdinalIgnoreCase));
        model.ActionTaken = normalizedAction;

        try
        {
            await _repository.AdvanceWorkflowInstanceAsync(companyId, model, userId, actorName);
            await _auditLogService.LogAsync(companyId, userId, actorName,
                normalizedAction == "Rejected" ? AuditActionType.Reject.ToString() : AuditActionType.Approve.ToString(),
                "Workflows", "WorkflowInstance", model.InstanceId.ToString(),
                $"Executed workflow action '{normalizedAction}' on instance #{model.InstanceId}.");
            return OperationResult.Success($"Workflow instance marked {normalizedAction}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<AuditLogsPageViewModel> GetAuditLogsPageAsync(
        int companyId, string? moduleName, string? actionType, string? username, DateTime? fromDate, DateTime? toDate, string? search, bool canManage)
    {
        var effTo = toDate?.Date ?? DateTime.Today;
        var effFrom = fromDate?.Date ?? effTo.AddDays(-29);
        if (effFrom > effTo) (effFrom, effTo) = (effTo, effFrom);

        var (audits, logins) = await _repository.SearchAuditLogsAsync(
            companyId, moduleName, actionType, username, effFrom, effTo, search);

        return new AuditLogsPageViewModel
        {
            ModuleName = moduleName,
            ActionType = actionType,
            Username = username,
            FromDate = effFrom,
            ToDate = effTo,
            Search = search,
            CanManageAdmin = canManage,
            AuditLogs = audits,
            LoginHistory = logins
        };
    }

    public async Task<SystemSettingsPageViewModel> GetSystemSettingsPageAsync(int companyId, string? category, bool canManage)
    {
        return new SystemSettingsPageViewModel
        {
            Category = category,
            CanManageAdmin = canManage,
            Settings = await _repository.GetSystemSettingsAsync(companyId, category)
        };
    }

    public async Task<OperationResult> UpsertSystemSettingAsync(int companyId, SystemSettingFormViewModel model, int userId, string username)
    {
        if (model.DataType == "Number" && !decimal.TryParse(model.SettingValue, out _))
            return OperationResult.Failure("Setting value must be a valid number.");

        if (model.DataType == "Boolean" && !bool.TryParse(model.SettingValue, out _))
            return OperationResult.Failure("Setting value must be 'true' or 'false'.");

        await _repository.UpsertSystemSettingAsync(companyId, model, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
            "Administration", "SystemSetting", model.SettingKey,
            $"Updated system setting '{model.SettingKey}' = '{model.SettingValue}'.");
        return OperationResult.Success($"Setting '{model.SettingKey}' saved.");
    }

    public async Task<AccessControlPageViewModel> GetAccessControlPageAsync(int companyId, bool canManage)
    {
        var (roles, permissions, links) = await _repository.GetRolesAndPermissionsAsync(companyId);
        var users = await _repository.GetUsersAsync(companyId);

        return new AccessControlPageViewModel
        {
            CanManageAdmin = canManage,
            Roles = roles,
            Permissions = permissions,
            RolePermissions = links,
            Users = users,
            Branches = (await _branchRepository.GetAllAsync(companyId)).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId, null, null, null, null)).ToList()
        };
    }

    public async Task<OperationResult> SaveRolePermissionsAsync(int companyId, SaveRolePermissionsFormViewModel model, int userId, string username)
    {
        var csv = string.Join(",", model.PermissionIds.Distinct());
        try
        {
            await _repository.SaveRolePermissionsAsync(companyId, model.RoleId, csv, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Administration", "RolePermissions", model.RoleId.ToString(),
                $"Updated {model.PermissionIds.Distinct().Count()} permission(s) for Role #{model.RoleId}.");
            return OperationResult.Success("Role permissions updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveUserAccountAsync(int companyId, AdminUserFormViewModel model, int userId, string username)
    {
        string? passwordHash = null;
        if (model.UserId == 0)
        {
            if (string.IsNullOrWhiteSpace(model.InitialPassword) || model.InitialPassword.Length < 8)
                return OperationResult<int>.Failure("Initial password must be at least 8 characters long.");
            passwordHash = _passwordHasher.HashPassword(model.InitialPassword);
        }

        try
        {
            var id = await _repository.CreateOrUpdateUserAsync(companyId, model, passwordHash, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.UserId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Administration", "UserAccount", id.ToString(),
                $"Saved user account '{model.Username}' (RoleId: {model.RoleId}).");
            return OperationResult<int>.Success(id, model.UserId == 0 ? "User account created." : "User account updated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ToggleUserStatusAsync(
        int companyId, int targetUserId, string action, string? newPassword, int userId, string username)
    {
        string? newHash = null;
        if (string.Equals(action, "ResetPassword", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return OperationResult.Failure("New temporary password must be at least 8 characters long.");
            newHash = _passwordHasher.HashPassword(newPassword);
        }

        try
        {
            await _repository.ToggleUserStatusAsync(companyId, targetUserId, action, newHash, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Administration", "UserAccount", targetUserId.ToString(),
                $"Executed '{action}' on user account #{targetUserId}.");
            return OperationResult.Success($"User account action '{action}' completed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public Task<SystemHealthDiagnosticsViewModel?> CheckDatabaseHealthAsync()
        => _repository.GetSystemHealthAsync(0);

    public async Task<SystemHealthDiagnosticsViewModel> GetSystemHealthPageAsync(int companyId, bool canManage)
    {
        var diagnostics = await _repository.GetSystemHealthAsync(companyId) ?? new SystemHealthDiagnosticsViewModel
        {
            Status = "Unavailable",
            UtcTimestamp = DateTime.UtcNow
        };
        diagnostics.CanManageAdmin = canManage;
        return diagnostics;
    }

    public async Task<OperationResult<MaintenanceCleanupResultViewModel>> RunMaintenanceCleanupAsync(
        int companyId, MaintenanceCleanupFormViewModel model, int userId, string username)
    {
        if (model.NotificationRetentionDays < 7)
            return OperationResult<MaintenanceCleanupResultViewModel>.Failure("Notification retention period must be at least 7 days.");

        if (model.LoginHistoryRetentionDays < 30)
            return OperationResult<MaintenanceCleanupResultViewModel>.Failure("Login history retention period must be at least 30 days.");

        try
        {
            var summary = await _repository.RunMaintenanceCleanupAsync(
                companyId, model.NotificationRetentionDays, model.LoginHistoryRetentionDays, userId);

            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Administration", "SystemMaintenance", companyId.ToString(),
                $"Executed retention cleanup: removed {summary.DeletedNotifications} expired/read notifications (> {model.NotificationRetentionDays}d) and {summary.DeletedLoginHistoryRows} login history rows (> {model.LoginHistoryRetentionDays}d).");

            return OperationResult<MaintenanceCleanupResultViewModel>.Success(
                summary,
                $"Cleanup completed: {summary.DeletedNotifications} notification(s) and {summary.DeletedLoginHistoryRows} login history record(s) removed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<MaintenanceCleanupResultViewModel>.Failure(ex.Message);
        }
    }
}
