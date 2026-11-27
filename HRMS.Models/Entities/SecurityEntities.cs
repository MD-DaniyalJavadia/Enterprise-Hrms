namespace HRMS.Models.Entities;

public class User : BaseEntity
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public int? EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public string? LastLoginIp { get; set; }
}

public class Role : BaseEntity
{
    public int RoleId { get; set; }
    public int CompanyId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
}

public class Permission
{
    public int PermissionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int CompanyId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Notification
{
    public long NotificationId { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "Info";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
}
