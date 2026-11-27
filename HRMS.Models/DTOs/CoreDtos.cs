namespace HRMS.Models.DTOs;

public class AuthenticatedUserDto
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class OperationResult
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();

    public static OperationResult Success(string message = "Operation completed successfully.", int? entityId = null) =>
        new() { Succeeded = true, Message = message, EntityId = entityId };

    public static OperationResult Failure(string message, Dictionary<string, string>? errors = null) =>
        new() { Succeeded = false, Message = message, Errors = errors ?? new() };
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static OperationResult<T> Success(T data, string message = "Operation completed successfully.") =>
        new() { Succeeded = true, Data = data, Message = message };

    public new static OperationResult<T> Failure(string message, Dictionary<string, string>? errors = null) =>
        new() { Succeeded = false, Message = message, Errors = errors ?? new() };
}

public class GlobalSearchResultDto
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-search";
}
