using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Leave;

public class LeaveKpiSummaryViewModel
{
    public int PendingCount { get; set; }
    public int ApprovedThisMonthCount { get; set; }
    public int OnLeaveTodayCount { get; set; }
    public int RejectedCount { get; set; }
}

public class LeaveApplicationsPageViewModel
{
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? LeaveTypeId { get; set; }
    public string? ApprovalStatus { get; set; }
    public int? CurrentUserEmployeeId { get; set; }

    public LeaveKpiSummaryViewModel Kpi { get; set; } = new();
    public List<LeaveApplication> Applications { get; set; } = new();
    public List<LeaveType> LeaveTypes { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class LeaveApplyViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public bool IsHalfDay { get; set; }

    [StringLength(20)]
    public string? HalfDaySession { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(60)]
    public string? ContactDuringLeave { get; set; }

    public string? AttachmentFileName { get; set; }
    public string? AttachmentStoredName { get; set; }
}

public class LeaveReviewViewModel
{
    [Required]
    public int LeaveApplicationId { get; set; }

    [Required, StringLength(20)]
    public string ApprovalStatus { get; set; } = "Approved";

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public class LeaveTypeFormViewModel
{
    public int LeaveTypeId { get; set; }

    [Required, StringLength(20)]
    public string LeaveCode { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LeaveName { get; set; } = string.Empty;

    [Required, Range(0, 365)]
    public decimal AnnualQuota { get; set; }

    public bool IsPaid { get; set; } = true;
    public bool AllowCarryForward { get; set; }

    [Range(0, 365)]
    public decimal MaxCarryForwardDays { get; set; }

    public bool AllowEncashment { get; set; }
    public bool RequiresDocument { get; set; }

    [Range(1, 365)]
    public int? MaxConsecutiveDays { get; set; }

    public bool IsHalfDayAllowed { get; set; } = true;

    [StringLength(20)]
    public string? GenderRestriction { get; set; }

    [Required, StringLength(15)]
    public string ColorHex { get; set; } = "#2563eb";

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class LeaveBalancesPageViewModel
{
    public int LeaveYear { get; set; } = DateTime.Today.Year;
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }

    public List<LeaveBalance> Balances { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class LeaveBalanceAdjustViewModel
{
    [Required]
    public int LeaveBalanceId { get; set; }

    [Required, Range(0, 365)]
    public decimal OpeningBalance { get; set; }

    [Required, Range(0, 365)]
    public decimal AccruedDays { get; set; }

    [Required, Range(0, 365)]
    public decimal CarryForwardDays { get; set; }
}

public class LeaveCalendarEventViewModel
{
    public int LeaveApplicationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2563eb";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public bool IsHalfDay { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
}

public class LeaveCalendarPageViewModel
{
    public int Year { get; set; } = DateTime.Today.Year;
    public int Month { get; set; } = DateTime.Today.Month;
    public int? DepartmentId { get; set; }

    public List<LeaveCalendarEventViewModel> Events { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class LeaveEncashmentSubmitViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public int LeaveYear { get; set; } = DateTime.Today.Year;

    [Required, Range(0.5, 90)]
    public decimal DaysEncashed { get; set; } = 1.0m;

    [StringLength(300)]
    public string? Remarks { get; set; }
}

public class LeaveEncashmentReviewViewModel
{
    [Required]
    public int EncashmentId { get; set; }

    [Required, StringLength(20)]
    public string ApprovalStatus { get; set; } = "Approved";

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public class LeaveEncashmentPageViewModel
{
    public string? FilterStatus { get; set; }
    public int? CurrentUserEmployeeId { get; set; }
    public List<LeaveEncashment> Encashments { get; set; } = new();
    public List<LeaveType> EncashableTypes { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}
