using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Models.ViewModels.Portal;

public sealed class EssEmployeeSummaryViewModel
{
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string WorkEmail { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public byte EmploymentStatus { get; set; }
    public string? PhotoPath { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingCode { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? TaxFilingStatus { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public string? ReportingManagerEmail { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string ShiftStartTime { get; set; } = "09:00";
    public string ShiftEndTime { get; set; } = "18:00";
    public string WeeklyOffDays { get; set; } = "Saturday,Sunday";
    public string CurrencyCode { get; set; } = "USD";
}

public sealed class EssTodayAttendanceViewModel
{
    public long AttendanceId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "NotMarked";
    public int LateMinutes { get; set; }
    public int EarlyExitMinutes { get; set; }
    public int WorkMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    public string WorkHoursFormatted => WorkMinutes > 0 ? $"{WorkMinutes / 60}h {WorkMinutes % 60:D2}m" : "-";
}

public sealed class EssPayslipItemViewModel
{
    public int PayslipId { get; set; }
    public int PayrollRunId { get; set; }
    public int PayrollYear { get; set; }
    public int PayrollMonth { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime? PayDate { get; set; }
    public decimal WorkingDaysInMonth { get; set; }
    public decimal PaidDays { get; set; }
    public decimal BasicPay { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string PayrollStatus { get; set; } = "Draft";
    public string CurrencyCode { get; set; } = "USD";
}

public sealed class EssDashboardViewModel
{
    public bool HasEmployeeProfile { get; set; }
    public bool IsSuperAdminPreview { get; set; }
    public int? SelectedEmployeeId { get; set; }
    public EssEmployeeSummaryViewModel? Profile { get; set; }
    public EssTodayAttendanceViewModel? TodayAttendance { get; set; }
    public List<LeaveBalance> LeaveBalances { get; set; } = new();
    public List<LeaveApplication> RecentLeaves { get; set; } = new();
    public List<EssPayslipItemViewModel> RecentPayslips { get; set; } = new();
    public List<EmployeeLoan> ActiveLoans { get; set; } = new();
    public List<ExpenseClaim> RecentExpenses { get; set; } = new();
    public List<PerformanceAppraisal> Appraisals { get; set; } = new();
    public List<PerformanceGoal> Goals { get; set; } = new();
    public List<TrainingEnrollment> Enrollments { get; set; } = new();
    public List<ProfileUpdateRequest> ProfileRequests { get; set; } = new();
    public List<Holiday> UpcomingHolidays { get; set; } = new();

    // Quick modal dropdown lookups
    public List<LeaveType> LeaveTypes { get; set; } = new();
    public List<LoanType> LoanTypes { get; set; } = new();
    public List<ExpenseCategory> ExpenseCategories { get; set; } = new();
    public List<EmployeeListItemViewModel> PreviewEmployees { get; set; } = new();
}

public sealed class EssMyProfilePageViewModel
{
    public bool HasEmployeeProfile { get; set; }
    public bool IsSuperAdminPreview { get; set; }
    public EmployeeProfileViewModel? FullProfile { get; set; }
    public List<ProfileUpdateRequest> ProfileRequests { get; set; } = new();
    public List<EmployeeListItemViewModel> PreviewEmployees { get; set; } = new();
}

public sealed class EssAttendancePageViewModel
{
    public bool HasEmployeeProfile { get; set; }
    public bool IsSuperAdminPreview { get; set; }
    public int? EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public EssEmployeeSummaryViewModel? Profile { get; set; }
    public EssTodayAttendanceViewModel? TodayAttendance { get; set; }
    public List<AttendanceRecord> Logs { get; set; } = new();
    public List<AttendanceRegularization> Regularizations { get; set; } = new();
    public List<OvertimeRequest> OvertimeRequests { get; set; } = new();
    public List<EmployeeListItemViewModel> PreviewEmployees { get; set; } = new();
}

public sealed class EssPayslipsPageViewModel
{
    public bool HasEmployeeProfile { get; set; }
    public bool IsSuperAdminPreview { get; set; }
    public int? EmployeeId { get; set; }
    public int? Year { get; set; }
    public EssEmployeeSummaryViewModel? Profile { get; set; }
    public List<EssPayslipItemViewModel> Payslips { get; set; } = new();
    public List<EmployeeListItemViewModel> PreviewEmployees { get; set; } = new();
}

public sealed class ProfileUpdateRequestFormViewModel
{
    [Required]
    [StringLength(40)]
    public string RequestType { get; set; } = "ContactInfo";

    [Required(ErrorMessage = "Please provide a brief summary of your requested change.")]
    [StringLength(200)]
    public string FieldSummary { get; set; } = string.Empty;

    [StringLength(30)]
    public string? RequestedPhone { get; set; }

    [StringLength(30)]
    public string? RequestedMobilePhone { get; set; }

    [EmailAddress]
    [StringLength(120)]
    public string? RequestedPersonalEmail { get; set; }

    [StringLength(300)]
    public string? RequestedAddress { get; set; }

    [StringLength(80)]
    public string? RequestedCity { get; set; }

    [StringLength(80)]
    public string? RequestedCountry { get; set; }

    [StringLength(120)]
    public string? RequestedBankName { get; set; }

    [StringLength(120)]
    public string? RequestedBankAccountTitle { get; set; }

    [StringLength(50)]
    public string? RequestedBankAccountNumber { get; set; }

    [StringLength(40)]
    public string? RequestedBankRoutingCode { get; set; }

    [StringLength(120)]
    public string? RequestedEmergencyContactName { get; set; }

    [StringLength(50)]
    public string? RequestedEmergencyRelationship { get; set; }

    [StringLength(30)]
    public string? RequestedEmergencyPhone { get; set; }

    [StringLength(500)]
    public string? EmployeeNotes { get; set; }
}

public sealed class ProfileUpdateReviewViewModel
{
    [Required]
    public int RequestId { get; set; }

    [Required]
    public string ApprovalStatus { get; set; } = "Approved";

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public sealed class ManagerKpiSummaryViewModel
{
    public int TeamHeadcount { get; set; }
    public int PresentTodayCount { get; set; }
    public int LateTodayCount { get; set; }
    public int OnLeaveTodayCount { get; set; }
    public int AbsentTodayCount { get; set; }
    public int PendingLeaveCount { get; set; }
    public int PendingRegularizationCount { get; set; }
    public int PendingOvertimeCount { get; set; }
    public int PendingLoanCount { get; set; }
    public int PendingExpenseCount { get; set; }
    public int PendingProfileRequestCount { get; set; }
    public int PendingAppraisalReviewCount { get; set; }

    public int TotalPendingApprovals =>
        PendingLeaveCount + PendingRegularizationCount + PendingOvertimeCount +
        PendingLoanCount + PendingExpenseCount + PendingProfileRequestCount;
}

public sealed class ManagerTeamMemberViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime HireDate { get; set; }
    public byte EmploymentStatus { get; set; }
    public int ReportingLevel { get; set; }
    public string? ManagerName { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string TodayStatus { get; set; } = "NotMarked";
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int LateMinutes { get; set; }
    public int WorkMinutes { get; set; }
    public string? PhotoPath { get; set; }
    public int OpenGoalsCount { get; set; }
    public int PendingLeaveCount { get; set; }

    public string WorkHoursFormatted => WorkMinutes > 0 ? $"{WorkMinutes / 60}h {WorkMinutes % 60:D2}m" : "-";
}

public sealed class ManagerPendingApprovalItemViewModel
{
    public string Category { get; set; } = string.Empty; // Leave, Regularization, Overtime, Loan, Expense, ProfileUpdate
    public int RequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal NumericValue { get; set; }
    public bool HasAttachment { get; set; }
    public DateTime SubmittedDate { get; set; }
}

public sealed class ManagerDeptStatViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public int PresentCount { get; set; }
    public int OnLeaveCount { get; set; }
}

public sealed class ManagerUpcomingLeaveViewModel
{
    public int LeaveApplicationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#2563eb";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string ApprovalStatus { get; set; } = "Approved";
}

public sealed class ManagerDashboardViewModel
{
    public bool IsCompanyWideView { get; set; }
    public ManagerKpiSummaryViewModel Kpis { get; set; } = new();
    public List<ManagerTeamMemberViewModel> TeamToday { get; set; } = new();
    public List<ManagerPendingApprovalItemViewModel> PendingApprovals { get; set; } = new();
    public List<PerformanceAppraisal> PendingAppraisals { get; set; } = new();
    public List<ManagerDeptStatViewModel> DepartmentStats { get; set; } = new();
    public List<ManagerUpcomingLeaveViewModel> UpcomingLeaves { get; set; } = new();
}

public sealed class ManagerTeamPageViewModel
{
    public bool IsCompanyWideView { get; set; }
    public int? FilterDepartmentId { get; set; }
    public string? FilterSearch { get; set; }
    public List<ManagerTeamMemberViewModel> Members { get; set; } = new();
    public List<HRMS.Models.ViewModels.Organization.DepartmentListItemViewModel> Departments { get; set; } = new();
}

public sealed class ManagerApprovalsPageViewModel
{
    public bool IsCompanyWideView { get; set; }
    public string? FilterCategory { get; set; }
    public List<ManagerPendingApprovalItemViewModel> Items { get; set; } = new();
    public List<PerformanceAppraisal> PendingAppraisals { get; set; } = new();
}

public sealed class ManagerApprovalActionViewModel
{
    [Required]
    [StringLength(30)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public int RequestId { get; set; }

    [Required]
    public string Decision { get; set; } = "Approved"; // Approved or Rejected

    public decimal? NumericOverride { get; set; } // Optional ApprovedHours (Overtime) or ApprovedAmount (Expense)

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}
