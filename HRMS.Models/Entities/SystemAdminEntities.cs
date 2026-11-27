namespace HRMS.Models.Entities;

public class NotificationTemplate : BaseEntity
{
    public int TemplateId { get; set; }
    public int CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string EventTrigger { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string DefaultType { get; set; } = "Info";
    public string DefaultPriority { get; set; } = "Normal";
    public bool SendInApp { get; set; } = true;
    public bool SendEmailDigest { get; set; }
}

public class WorkflowDefinition : BaseEntity
{
    public int WorkflowId { get; set; }
    public int CompanyId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public decimal? MinAmountThreshold { get; set; }
    public int AutoEscalateHours { get; set; } = 48;
    public string? Description { get; set; }
    public int StepCount { get; set; }
}

public class WorkflowStep
{
    public int WorkflowStepId { get; set; }
    public int CompanyId { get; set; }
    public int WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string ApproverType { get; set; } = "ReportingManager";
    public int? ApproverRoleId { get; set; }
    public string? ApproverRoleName { get; set; }
    public int? ApproverEmployeeId { get; set; }
    public string? ApproverEmployeeName { get; set; }
    public int? EscalationRoleId { get; set; }
    public string? EscalationRoleName { get; set; }
    public int SlaHours { get; set; } = 24;
    public bool CanDelegate { get; set; } = true;
    public bool IsMandatory { get; set; } = true;
}

public class WorkflowInstance
{
    public long InstanceId { get; set; }
    public int CompanyId { get; set; }
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string WorkflowCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public int? RequestedByUserId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public int CurrentStepOrder { get; set; }
    public int TotalSteps { get; set; }
    public string CurrentStepName { get; set; } = string.Empty;
    public string InstanceStatus { get; set; } = "InProgress";
    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class WorkflowActionLog
{
    public long ActionLogId { get; set; }
    public int CompanyId { get; set; }
    public long InstanceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; }
}

public class SystemSetting
{
    public int SettingId { get; set; }
    public int CompanyId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string DataType { get; set; } = "Text";
    public bool IsSensitive { get; set; }
    public string? Description { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class LoginHistoryEntry
{
    public long LoginHistoryId { get; set; }
    public int? UserId { get; set; }
    public string UsernameOrEmail { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
}
