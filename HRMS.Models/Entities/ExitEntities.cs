namespace HRMS.Models.Entities;

public class SeparationRequest
{
    public int SeparationId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? WorkEmail { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal BaseSalary { get; set; }
    public string? BranchName { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public string SeparationNumber { get; set; } = string.Empty;
    public string SeparationType { get; set; } = "Resignation";
    public string ReasonCategory { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public int NoticePeriodDays { get; set; }
    public DateTime RequestedLastWorkingDay { get; set; }
    public DateTime? ApprovedLastWorkingDay { get; set; }
    public bool IsNoticeWaived { get; set; }
    public int NoticeShortfallDays { get; set; }
    public string? EmployeeComments { get; set; }
    public string? ManagerComments { get; set; }
    public string? HrComments { get; set; }
    public string SeparationStatus { get; set; } = "Submitted";
    public bool IsRehireEligible { get; set; } = true;
    public DateTime? ManagerApprovedDate { get; set; }
    public DateTime? HrApprovedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Summary projections for list view
    public int ClearedItemsCount { get; set; }
    public int TotalClearanceItemsCount { get; set; }
    public int ActiveAssetsCount { get; set; }
    public int ActiveLoansCount { get; set; }
    public bool HasExitInterview { get; set; }
    public string? SettlementStatus { get; set; }
    public decimal? SettlementNetAmount { get; set; }
}

public class ExitClearanceTemplate
{
    public int TemplateId { get; set; }
    public int CompanyId { get; set; }
    public string DepartmentScope { get; set; } = "IT";
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class ExitClearanceItem
{
    public int ClearanceItemId { get; set; }
    public int CompanyId { get; set; }
    public int SeparationId { get; set; }
    public string DepartmentScope { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string ClearanceStatus { get; set; } = "Pending";
    public int? ClearedBy { get; set; }
    public DateTime? ClearedDate { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string? Remarks { get; set; }
}

public class ExitInterview
{
    public int ExitInterviewId { get; set; }
    public int CompanyId { get; set; }
    public int SeparationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string SeparationNumber { get; set; } = string.Empty;
    public string SeparationType { get; set; } = string.Empty;
    public DateTime InterviewDate { get; set; }
    public int OverallSatisfactionRating { get; set; }
    public int ManagerSupportRating { get; set; }
    public int WorkLifeBalanceRating { get; set; }
    public int CompensationRating { get; set; }
    public bool WouldRecommendCompany { get; set; }
    public bool WouldConsiderRejoining { get; set; }
    public string PrimaryLeavingReason { get; set; } = string.Empty;
    public string? LikedMost { get; set; }
    public string? ImprovementSuggestions { get; set; }
    public string? InterviewerNotes { get; set; }
    public bool IsConfidential { get; set; } = true;
}

public class FinalSettlement
{
    public int SettlementId { get; set; }
    public int CompanyId { get; set; }
    public int SeparationId { get; set; }
    public int EmployeeId { get; set; }
    public string SettlementNumber { get; set; } = string.Empty;
    public decimal MonthlyBaseSalary { get; set; }
    public int WorkedDaysInFinalMonth { get; set; }
    public decimal ProratedSalaryPay { get; set; }
    public decimal EncashableLeaveDays { get; set; }
    public decimal LeaveEncashmentAmount { get; set; }
    public decimal GratuityOrSeverance { get; set; }
    public decimal BonusOrOtherEarnings { get; set; }
    public decimal OutstandingLoanRecovery { get; set; }
    public decimal AssetOrNoticeRecovery { get; set; }
    public decimal TaxAndOtherDeductions { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPayableAmount { get; set; }
    public string SettlementStatus { get; set; } = "Draft";
    public string? PaymentReference { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
