using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Admin;

public class NotificationInboxItemViewModel
{
    public long NotificationId { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "Info";
    public string ModuleName { get; set; } = "General";
    public string Priority { get; set; } = "Normal";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public class NotificationInboxPageViewModel
{
    public bool? FilterRead { get; set; }
    public string? NotificationType { get; set; }
    public string? ModuleName { get; set; }
    public string? Search { get; set; }
    public bool CanManageNotifications { get; set; }
    public List<NotificationInboxItemViewModel> Notifications { get; set; } = new();
    public List<NotificationTemplate> Templates { get; set; } = new();
    public List<RoleSummaryViewModel> Roles { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<AdminUserListItemViewModel> Users { get; set; } = new();
}

public class BroadcastNotificationFormViewModel
{
    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string NotificationType { get; set; } = "Info";

    [Required, StringLength(20)]
    public string Priority { get; set; } = "Normal";

    [Required, StringLength(60)]
    public string ModuleName { get; set; } = "System";

    [StringLength(260)]
    public string? ActionUrl { get; set; }

    [Required, StringLength(30)]
    public string TargetScope { get; set; } = "AllUsers";

    public int? TargetRoleId { get; set; }
    public int? TargetDepartmentId { get; set; }
    public int? TargetUserId { get; set; }
}

public class NotificationTemplateFormViewModel
{
    public int TemplateId { get; set; }

    [Required, StringLength(60)]
    public string TemplateCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string TemplateName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string ModuleName { get; set; } = "System";

    [Required, StringLength(80)]
    public string EventTrigger { get; set; } = "OnApproved";

    [Required, StringLength(180)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required, StringLength(600)]
    public string BodyTemplate { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string DefaultType { get; set; } = "Info";

    [Required, StringLength(20)]
    public string DefaultPriority { get; set; } = "Normal";

    public bool SendInApp { get; set; } = true;
    public bool SendEmailDigest { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WorkflowsPageViewModel
{
    public string? ModuleFilter { get; set; }
    public string? StatusFilter { get; set; }
    public bool CanManageWorkflows { get; set; }
    public List<WorkflowDefinition> Definitions { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<WorkflowInstance> Instances { get; set; } = new();
    public List<WorkflowActionLog> ActionLogs { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<RoleSummaryViewModel> Roles { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class WorkflowDefinitionFormViewModel
{
    public int WorkflowId { get; set; }

    [Required, StringLength(40)]
    public string WorkflowCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string WorkflowName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ModuleName { get; set; } = "Leave";

    public int? DepartmentId { get; set; }

    [Range(0, 10000000)]
    public decimal? MinAmountThreshold { get; set; }

    [Range(1, 720)]
    public int AutoEscalateHours { get; set; } = 48;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkflowStepFormViewModel
{
    public int WorkflowStepId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int WorkflowId { get; set; }

    [Required, Range(1, 20)]
    public int StepOrder { get; set; } = 1;

    [Required, StringLength(100)]
    public string StepName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string ApproverType { get; set; } = "ReportingManager";

    public int? ApproverRoleId { get; set; }
    public int? ApproverEmployeeId { get; set; }
    public int? EscalationRoleId { get; set; }

    [Range(1, 720)]
    public int SlaHours { get; set; } = 24;

    public bool CanDelegate { get; set; } = true;
    public bool IsMandatory { get; set; } = true;
}

public class InitiateWorkflowInstanceFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int WorkflowId { get; set; }

    [Required, StringLength(50)]
    public string EntityId { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Summary { get; set; } = string.Empty;
}

public class AdvanceWorkflowFormViewModel
{
    [Required, Range(1, long.MaxValue)]
    public long InstanceId { get; set; }

    [Required, StringLength(30)]
    public string ActionTaken { get; set; } = "Approved";

    [StringLength(400)]
    public string? Comments { get; set; }
}

public class AuditLogsPageViewModel
{
    public string? ModuleName { get; set; }
    public string? ActionType { get; set; }
    public string? Username { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public bool CanManageAdmin { get; set; }
    public List<AuditLog> AuditLogs { get; set; } = new();
    public List<LoginHistoryEntry> LoginHistory { get; set; } = new();
}

public class SystemSettingsPageViewModel
{
    public string? Category { get; set; }
    public bool CanManageAdmin { get; set; }
    public List<SystemSetting> Settings { get; set; } = new();
}

public class SystemSettingFormViewModel
{
    [Required, StringLength(80)]
    public string SettingKey { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string SettingValue { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = "General";

    [Required, StringLength(20)]
    public string DataType { get; set; } = "Text";

    [StringLength(250)]
    public string? Description { get; set; }
}

public class RoleSummaryViewModel
{
    public int RoleId { get; set; }
    public int CompanyId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int AssignedUserCount { get; set; }
    public int PermissionCount { get; set; }
}

public class RolePermissionLinkViewModel
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
}

public class AdminUserListItemViewModel
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public string? LastLoginIp { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int PrimaryRoleId { get; set; }
    public string RoleNames { get; set; } = string.Empty;
}

public class AccessControlPageViewModel
{
    public bool CanManageAdmin { get; set; }
    public List<AdminUserListItemViewModel> Users { get; set; } = new();
    public List<RoleSummaryViewModel> Roles { get; set; } = new();
    public List<Permission> Permissions { get; set; } = new();
    public List<RolePermissionLinkViewModel> RolePermissions { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class AdminUserFormViewModel
{
    public int UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? BranchId { get; set; }

    [Required, StringLength(60)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? InitialPassword { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
}

public class SaveRolePermissionsFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    public List<int> PermissionIds { get; set; } = new();
}

public class SystemHealthDiagnosticsViewModel
{
    public string Status { get; set; } = "Healthy";
    public string DatabaseName { get; set; } = string.Empty;
    public string ProductVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public DateTime UtcTimestamp { get; set; }
    public int TableCount { get; set; }
    public int StoredProcedureCount { get; set; }
    public int ActiveCompanies { get; set; }
    public int ActiveEmployees { get; set; }
    public int ActiveUsers { get; set; }
    public int PendingWorkflows { get; set; }
    public int UnreadNotifications { get; set; }
    public int AuditLogCount { get; set; }
    public int LoginHistoryCount { get; set; }
    public bool CanManageAdmin { get; set; }
    public int NotificationRetentionDays { get; set; } = 90;
    public int LoginHistoryRetentionDays { get; set; } = 180;
}

public class MaintenanceCleanupFormViewModel
{
    [Required, Range(7, 3650, ErrorMessage = "Notification retention must be between 7 and 3650 days.")]
    public int NotificationRetentionDays { get; set; } = 90;

    [Required, Range(30, 3650, ErrorMessage = "Login history retention must be between 30 and 3650 days.")]
    public int LoginHistoryRetentionDays { get; set; } = 180;
}

public class MaintenanceCleanupResultViewModel
{
    public int DeletedNotifications { get; set; }
    public int DeletedLoginHistoryRows { get; set; }
    public DateTime ExecutedAtUtc { get; set; }
}
