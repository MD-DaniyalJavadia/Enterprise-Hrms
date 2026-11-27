namespace HRMS.Models.Entities;

public class LeaveType
{
    public int LeaveTypeId { get; set; }
    public int CompanyId { get; set; }
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public decimal AnnualQuota { get; set; }
    public bool IsPaid { get; set; }
    public bool AllowCarryForward { get; set; }
    public decimal MaxCarryForwardDays { get; set; }
    public bool AllowEncashment { get; set; }
    public bool RequiresDocument { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public bool IsHalfDayAllowed { get; set; }
    public string? GenderRestriction { get; set; }
    public string ColorHex { get; set; } = "#2563eb";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2563eb";
    public int LeaveYear { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal AccruedDays { get; set; }
    public decimal CarryForwardDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal EncashedDays { get; set; }
    public decimal AvailableDays { get; set; }
}

public class LeaveApplication
{
    public int LeaveApplicationId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2563eb";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public bool IsHalfDay { get; set; }
    public string? HalfDaySession { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ContactDuringLeave { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentStoredName { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeaveEncashment
{
    public int EncashmentId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveName { get; set; } = string.Empty;
    public int LeaveYear { get; set; }
    public decimal DaysEncashed { get; set; }
    public decimal EncashmentAmount { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? Remarks { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}
