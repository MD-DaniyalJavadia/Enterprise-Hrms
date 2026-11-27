using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Attendance;

public class DailyAttendanceKpiViewModel
{
    public int TotalActiveEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int HalfDayCount { get; set; }
    public int OnLeaveCount { get; set; }
    public int TotalOvertimeMinutes { get; set; }
}

public class DailyAttendancePageViewModel
{
    public DateTime AttendanceDate { get; set; } = DateTime.Today;
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int? CurrentUserEmployeeId { get; set; }

    public DailyAttendanceKpiViewModel Kpi { get; set; } = new();
    public List<AttendanceRecord> Records { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class ManualAttendanceViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; } = DateTime.Today;

    [Required, StringLength(20)]
    public string Status { get; set; } = "Present";

    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }

    [Range(0, 720)]
    public int? LateMinutes { get; set; }

    [Range(0, 720)]
    public int? OvertimeMinutes { get; set; }

    [StringLength(400)]
    public string? Remarks { get; set; }
}

public class QuickPunchViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [StringLength(400)]
    public string? Remarks { get; set; }
}

public class MonthlyAttendanceReportItemViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public int LeaveDays { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }

    public double TotalWorkHours => Math.Round(TotalWorkMinutes / 60.0, 2);
    public double TotalOvertimeHours => Math.Round(TotalOvertimeMinutes / 60.0, 2);
}

public class MonthlyAttendanceReportPageViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }

    public List<MonthlyAttendanceReportItemViewModel> Items { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class EmployeeCalendarViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }

    public List<AttendanceRecord> Logs { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class ShiftAssignmentFormViewModel
{
    public int AssignmentId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ShiftId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EffectiveTo { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class ShiftRosterPageViewModel
{
    public int? FilterEmployeeId { get; set; }
    public List<ShiftAssignment> Assignments { get; set; } = new();
    public List<ShiftViewModel> Shifts { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class HolidayFormViewModel
{
    public int HolidayId { get; set; }
    public int? BranchId { get; set; }

    [Required, StringLength(120)]
    public string HolidayName { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime HolidayDate { get; set; } = DateTime.Today;

    [Required, StringLength(30)]
    public string HolidayType { get; set; } = "Public";

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class HolidayPageViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public int? BranchId { get; set; }
    public List<Holiday> Holidays { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
}

public class RegularizationSubmitViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; } = DateTime.Today;

    public TimeSpan? RequestedCheckIn { get; set; }
    public TimeSpan? RequestedCheckOut { get; set; }

    [Required, StringLength(20)]
    public string RequestedStatus { get; set; } = "Present";

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class RegularizationReviewViewModel
{
    [Required]
    public int RegularizationId { get; set; }

    [Required, StringLength(20)]
    public string ApprovalStatus { get; set; } = "Approved";

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public class OvertimeSubmitViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime OvertimeDate { get; set; } = DateTime.Today;

    [Required, Range(0.25, 24)]
    public decimal RequestedHours { get; set; } = 1.0m;

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class OvertimeReviewViewModel
{
    [Required]
    public int OvertimeRequestId { get; set; }

    [Required, StringLength(20)]
    public string ApprovalStatus { get; set; } = "Approved";

    [Range(0, 24)]
    public decimal ApprovedHours { get; set; }

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public class RegularizationsPageViewModel
{
    public string? FilterStatus { get; set; }
    public int? CurrentUserEmployeeId { get; set; }
    public List<AttendanceRegularization> Regularizations { get; set; } = new();
    public List<OvertimeRequest> OvertimeRequests { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class AttendanceImportResultViewModel
{
    public int TotalRowsProcessed { get; set; }
    public int ImportedCount { get; set; }
    public List<string> RowErrors { get; set; } = new();
}
