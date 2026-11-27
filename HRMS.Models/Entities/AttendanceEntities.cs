namespace HRMS.Models.Entities;

public class AttendanceRecord
{
    public long AttendanceId { get; set; }
    public int CompanyId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public int? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "Present";
    public int LateMinutes { get; set; }
    public int EarlyExitMinutes { get; set; }
    public int WorkMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Source { get; set; } = "Web";
    public string? Remarks { get; set; }

    public string WorkHoursFormatted => WorkMinutes > 0 ? $"{WorkMinutes / 60}h {WorkMinutes % 60:D2}m" : "-";
}

public class ShiftAssignment
{
    public int AssignmentId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public string StartTimeText { get; set; } = string.Empty;
    public string EndTimeText { get; set; } = string.Empty;
    public string WeeklyOffDays { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class Holiday
{
    public int HolidayId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "Public";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class AttendanceRegularization
{
    public int RegularizationId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public DateTime? RequestedCheckIn { get; set; }
    public DateTime? RequestedCheckOut { get; set; }
    public string RequestedStatus { get; set; } = "Present";
    public string Reason { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Pending";
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OvertimeRequest
{
    public int OvertimeRequestId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime OvertimeDate { get; set; }
    public decimal RequestedHours { get; set; }
    public decimal ApprovedHours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Pending";
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}
