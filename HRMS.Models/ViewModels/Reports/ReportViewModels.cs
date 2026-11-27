using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Reports;

public class ExecutiveAnalyticsKpiViewModel
{
    public int ActiveHeadcount { get; set; }
    public int ProbationCount { get; set; }
    public decimal TurnoverRatePercent { get; set; }
    public decimal MonthlyPayrollCost { get; set; }
    public decimal AttendanceCompliancePercent { get; set; }
    public int OpenRequisitionsCount { get; set; }
    public decimal ApprovedLeaveDaysInPeriod { get; set; }
    public decimal OutstandingLoanPortfolio { get; set; }
    public decimal AppraisalCompletionPercent { get; set; }
    public decimal ActiveAssetPortfolioValue { get; set; }
    public int ActiveSeparationCases { get; set; }
}

public class DepartmentWorkforceAnalyticsViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ActiveHeadcount { get; set; }
    public decimal AnnualBaseSalaryTotal { get; set; }
    public decimal LatestMonthlyNetPay { get; set; }
    public decimal AvgTenureYears { get; set; }
    public int OpenPositions { get; set; }
}

public class MonthlyFinancialTrendViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalTaxDeducted { get; set; }
    public decimal ApprovedExpenseAmount { get; set; }
}

public class AttritionBreakdownViewModel
{
    public string ReasonCategory { get; set; } = string.Empty;
    public int CaseCount { get; set; }
    public decimal AvgSatisfaction { get; set; }
}

public class ReportsHubPageViewModel
{
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-29);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public bool CanExport { get; set; }
    public ExecutiveAnalyticsKpiViewModel Kpi { get; set; } = new();
    public List<DepartmentWorkforceAnalyticsViewModel> DepartmentStats { get; set; } = new();
    public List<MonthlyFinancialTrendViewModel> FinancialTrends { get; set; } = new();
    public List<AttritionBreakdownViewModel> AttritionBreakdown { get; set; } = new();
    public List<SavedReportTemplate> SavedTemplates { get; set; } = new();
    public List<ReportExportLog> RecentExports { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class WorkforceReportRowViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string JobGrade { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public decimal TenureYears { get; set; }
    public string EmploymentStatusLabel { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; }
}

public class WorkforceReportPageViewModel
{
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public byte? EmploymentStatus { get; set; }
    public string? Search { get; set; }
    public bool CanExport { get; set; }
    public List<WorkforceReportRowViewModel> Rows { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class AttendanceLeaveReportRowViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public int OnLeaveDays { get; set; }
    public decimal TotalWorkHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public decimal ApprovedLeaveDays { get; set; }
    public decimal RemainingLeaveBalance { get; set; }
}

public class AttendanceLeaveReportPageViewModel
{
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public bool CanExport { get; set; }
    public List<AttendanceLeaveReportRowViewModel> Rows { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class PayrollCostReportRowViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public int PayrollYear { get; set; }
    public int PayrollMonth { get; set; }
    public string PayrollStatus { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public decimal WorkingDaysInMonth { get; set; }
    public decimal PaidDays { get; set; }
    public decimal BasicPay { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal LeaveEncashmentPay { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal GrossPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal StatutoryDeductions { get; set; }
    public decimal OtherAndAbsenceDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal EmployerContributions { get; set; }
    public decimal CostToCompany { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class PayrollCostReportPageViewModel
{
    public int? PayrollYear { get; set; }
    public int? PayrollMonth { get; set; }
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public bool CanExport { get; set; }
    public List<PayrollCostReportRowViewModel> Rows { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class RecruitmentFunnelRowViewModel
{
    public int RequisitionId { get; set; }
    public string RequisitionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int OpeningsCount { get; set; }
    public string RequisitionStatus { get; set; } = string.Empty;
    public DateTime? TargetHireDate { get; set; }
    public int TotalApplicants { get; set; }
    public int InterviewStageCount { get; set; }
    public int OfferStageCount { get; set; }
    public int HiredCount { get; set; }
}

public class TalentPerformanceRowViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string LatestCycleName { get; set; } = string.Empty;
    public string AppraisalStatus { get; set; } = string.Empty;
    public decimal? SelfRating { get; set; }
    public decimal? ManagerRating { get; set; }
    public decimal? FinalRating { get; set; }
    public string RatingBand { get; set; } = "-";
    public int CompletedTrainings { get; set; }
    public int TotalEnrolledTrainings { get; set; }
    public decimal AvgTrainingScore { get; set; }
}

public class TalentRecruitmentReportPageViewModel
{
    public int? DepartmentId { get; set; }
    public bool CanExport { get; set; }
    public List<RecruitmentFunnelRowViewModel> FunnelRows { get; set; } = new();
    public List<TalentPerformanceRowViewModel> TalentRows { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class LoanExpenseSummaryRowViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int ActiveLoansCount { get; set; }
    public decimal TotalLoanPrincipal { get; set; }
    public decimal RemainingLoanBalance { get; set; }
    public int ExpenseClaimCount { get; set; }
    public decimal TotalClaimedAmount { get; set; }
    public decimal ApprovedExpenseAmount { get; set; }
}

public class AssetExitComplianceRowViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int AssignedAssetCount { get; set; }
    public decimal AssignedAssetValue { get; set; }
    public int AcknowledgedPoliciesCount { get; set; }
    public int MandatoryPoliciesCount { get; set; }
    public string SeparationNumber { get; set; } = "-";
    public string SeparationStatus { get; set; } = "ActiveEmployee";
    public string SettlementStatus { get; set; } = "-";
    public decimal? SettlementNetAmount { get; set; }
}

public class FinancialComplianceReportPageViewModel
{
    public int? DepartmentId { get; set; }
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-89);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public bool CanExport { get; set; }
    public List<LoanExpenseSummaryRowViewModel> FinancialRows { get; set; } = new();
    public List<AssetExitComplianceRowViewModel> ComplianceRows { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class SaveReportTemplateFormViewModel
{
    public int TemplateId { get; set; }

    [Required, StringLength(120)]
    public string TemplateName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ReportKey { get; set; } = "Workforce";

    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }

    [Range(1, 365)]
    public int DateRangeDays { get; set; } = 30;

    [StringLength(40)]
    public string? StatusFilter { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public bool IsShared { get; set; } = true;
}
