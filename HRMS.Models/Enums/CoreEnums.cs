namespace HRMS.Models.Enums;

public enum AuditActionType
{
    Login = 1,
    Logout = 2,
    FailedLogin = 3,
    PasswordChanged = 4,
    Create = 5,
    Update = 6,
    Delete = 7,
    Export = 8,
    Approve = 9,
    Reject = 10
}

public enum NotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Danger = 4
}

public enum EmploymentStatus
{
    Active = 1,
    Probation = 2,
    OnLeave = 3,
    Resigned = 4,
    Terminated = 5,
    Inactive = 6
}
