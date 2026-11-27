using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Exit;

public class ExitKpiSummaryViewModel
{
    public int TotalRequests { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ClearanceInProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal TotalPendingSettlementAmount { get; set; }
    public decimal AvgExitSatisfaction { get; set; }
}

public class ExitReadinessSnapshotViewModel
{
    public int ActiveAssetsCount { get; set; }
    public int ActiveLoansCount { get; set; }
    public decimal ActiveLoanBalance { get; set; }
    public decimal EncashableLeaveDays { get; set; }
    public decimal MonthlyBaseSalary { get; set; }
}

public class ExitIndexPageViewModel
{
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? SeparationType { get; set; }
    public string? Search { get; set; }
    public bool CanManage { get; set; }
    public bool IsManager { get; set; }
    public int? CurrentEmployeeId { get; set; }
    public ExitKpiSummaryViewModel Kpi { get; set; } = new();
    public List<SeparationRequest> Requests { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class ExitDetailsPageViewModel
{
    public SeparationRequest Request { get; set; } = new();
    public ExitReadinessSnapshotViewModel Readiness { get; set; } = new();
    public List<ExitClearanceItem> ClearanceItems { get; set; } = new();
    public ExitInterview? ExitInterview { get; set; }
    public FinalSettlement? Settlement { get; set; }
    public bool CanManage { get; set; }
    public bool CanManagerApprove { get; set; }
    public bool IsOwner { get; set; }
}

public class ExitInterviewsPageViewModel
{
    public List<ExitInterview> Interviews { get; set; } = new();
    public List<SeparationRequest> SeparationsWithoutInterview { get; set; } = new();
}

public class SeparationSubmitFormViewModel
{
    public int EmployeeId { get; set; }

    [Required, StringLength(30)]
    public string SeparationType { get; set; } = "Resignation";

    [Required, StringLength(60)]
    public string ReasonCategory { get; set; } = "BetterOpportunity";

    [Required]
    public DateTime SubmissionDate { get; set; } = DateTime.Today;

    [Range(0, 180)]
    public int NoticePeriodDays { get; set; } = 30;

    [Required]
    public DateTime RequestedLastWorkingDay { get; set; } = DateTime.Today.AddDays(30);

    public bool IsNoticeWaived { get; set; }

    [StringLength(600)]
    public string? EmployeeComments { get; set; }

    [StringLength(600)]
    public string? HrComments { get; set; }

    public bool InitiateAsHrApproved { get; set; }
}

public class SeparationReviewFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int SeparationId { get; set; }

    [Required, StringLength(25)]
    public string Action { get; set; } = "HrApprove";

    public DateTime? ApprovedLastWorkingDay { get; set; }
    public bool IsNoticeWaived { get; set; }
    public bool IsRehireEligible { get; set; } = true;

    [StringLength(600)]
    public string? Comments { get; set; }
}

public class ClearanceTemplateFormViewModel
{
    public int TemplateId { get; set; }

    [Required, StringLength(40)]
    public string DepartmentScope { get; set; } = "IT";

    [Required, StringLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsMandatory { get; set; } = true;

    [Range(1, 100)]
    public int DisplayOrder { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}

public class ClearanceItemUpdateViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int ClearanceItemId { get; set; }

    [Required, StringLength(20)]
    public string ClearanceStatus { get; set; } = "Cleared";

    [Range(0, 1000000)]
    public decimal RecoveryAmount { get; set; }

    [StringLength(400)]
    public string? Remarks { get; set; }
}

public class ExitInterviewFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int SeparationId { get; set; }

    [Required]
    public DateTime InterviewDate { get; set; } = DateTime.Today;

    [Range(1, 5)]
    public int OverallSatisfactionRating { get; set; } = 4;

    [Range(1, 5)]
    public int ManagerSupportRating { get; set; } = 4;

    [Range(1, 5)]
    public int WorkLifeBalanceRating { get; set; } = 4;

    [Range(1, 5)]
    public int CompensationRating { get; set; } = 3;

    public bool WouldRecommendCompany { get; set; } = true;
    public bool WouldConsiderRejoining { get; set; } = true;

    [Required, StringLength(200)]
    public string PrimaryLeavingReason { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LikedMost { get; set; }

    [StringLength(500)]
    public string? ImprovementSuggestions { get; set; }

    [StringLength(500)]
    public string? InterviewerNotes { get; set; }

    public bool IsConfidential { get; set; } = true;
}

public class FinalSettlementFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int SeparationId { get; set; }

    [Range(0, 31)]
    public int? WorkedDaysInFinalMonth { get; set; }

    [Range(0, 365)]
    public decimal? EncashableLeaveDays { get; set; }

    [Range(0, 10000000)]
    public decimal GratuityOrSeverance { get; set; }

    [Range(0, 10000000)]
    public decimal BonusOrOtherEarnings { get; set; }

    [Range(0, 10000000)]
    public decimal? OutstandingLoanRecovery { get; set; }

    [Range(0, 10000000)]
    public decimal? AssetOrNoticeRecovery { get; set; }

    [Range(0, 10000000)]
    public decimal TaxAndOtherDeductions { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class FinalSettlementStatusFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int SettlementId { get; set; }

    [Required, StringLength(20)]
    public string SettlementStatus { get; set; } = "Approved";

    [StringLength(80)]
    public string? PaymentReference { get; set; }

    public DateTime? PaidDate { get; set; }
}
