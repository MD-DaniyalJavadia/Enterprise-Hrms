namespace HRMS.Models.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? SelectedBranchId { get; set; }
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    public DashboardKpiViewModel Kpis { get; set; } = new();
    public PayrollSummaryViewModel PayrollSummary { get; set; } = new();
    public List<BranchOptionViewModel> Branches { get; set; } = new();
    public List<DepartmentStatViewModel> DepartmentStats { get; set; } = new();
    public List<GenderStatViewModel> GenderStats { get; set; } = new();
    public List<MonthlyHiringStatViewModel> MonthlyHiringStats { get; set; } = new();
    public List<AttendanceAnalyticsViewModel> AttendanceAnalytics { get; set; } = new();
    public List<UpcomingEventViewModel> UpcomingBirthdays { get; set; } = new();
    public List<UpcomingEventViewModel> UpcomingAnniversaries { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    public List<NotificationItemViewModel> Notifications { get; set; } = new();
}

public class DashboardKpiViewModel
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int NewHiresThisMonth { get; set; }
    public int ResignedEmployeesThisMonth { get; set; }
    public int EmployeesOnLeaveToday { get; set; }
    public int PresentToday { get; set; }
    public int LateArrivalsToday { get; set; }
    public int AbsentToday { get; set; }
    public int PendingLeaveRequests { get; set; }
}

public class PayrollSummaryViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public int ProcessedEmployees { get; set; }
    public string PayrollStatus { get; set; } = "Pending";
}

public class BranchOptionViewModel
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}

public class DepartmentStatViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int ActiveCount { get; set; }
}

public class GenderStatViewModel
{
    public string Gender { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyHiringStatViewModel
{
    public string MonthLabel { get; set; } = string.Empty;
    public int HiredCount { get; set; }
    public int ResignedCount { get; set; }
}

public class AttendanceAnalyticsViewModel
{
    public string DateLabel { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int OnLeaveCount { get; set; }
}

public class UpcomingEventViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int? YearsOfService { get; set; }
}

public class RecentActivityViewModel
{
    public long AuditLogId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class NotificationItemViewModel
{
    public long NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "Info";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}
