using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Models.ViewModels.LoanExpense;

public class LoanKpiSummaryViewModel
{
    public int ActiveLoansCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalOutstandingBalance { get; set; }
    public decimal MonthlyRecoveryTotal { get; set; }
}

public class LoansIndexPageViewModel
{
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }

    public LoanKpiSummaryViewModel Kpi { get; set; } = new();
    public List<EmployeeLoan> Loans { get; set; } = new();
    public List<LoanType> LoanTypes { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class LoanDetailsPageViewModel
{
    public EmployeeLoan Loan { get; set; } = new();
    public List<LoanRepaymentInstallment> Schedule { get; set; } = new();
}

public class LoanTypeFormViewModel
{
    public int LoanTypeId { get; set; }

    [Required, StringLength(20)]
    public string TypeCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string TypeName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Category { get; set; } = "Loan";

    [Required, Range(1, 100000000)]
    public decimal MaxAmount { get; set; } = 25000m;

    [Range(0, 100)]
    public decimal DefaultInterestRate { get; set; }

    [Required, Range(1, 120)]
    public int MaxTenureMonths { get; set; } = 24;

    public bool RequiresGuarantor { get; set; }

    [StringLength(400)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmployeeLoanFormViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LoanTypeId { get; set; }

    [Required, Range(1, 100000000)]
    public decimal PrincipalAmount { get; set; }

    [Required, Range(1, 120)]
    public int TenureMonths { get; set; } = 6;

    [Required, DataType(DataType.Date)]
    public DateTime RepaymentStartDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);

    [StringLength(400)]
    public string? Purpose { get; set; }
}

public class LoanReviewViewModel
{
    [Required]
    public int LoanId { get; set; }

    [Required, StringLength(20)]
    public string ActionStatus { get; set; } = "Active";

    [DataType(DataType.Date)]
    public DateTime? RepaymentStartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DisbursedDate { get; set; }

    [StringLength(400)]
    public string? ReviewerComments { get; set; }
}

public class RecordRepaymentViewModel
{
    [Required]
    public int ScheduleId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime PaidDate { get; set; } = DateTime.Today;

    [StringLength(80)]
    public string? PaymentReference { get; set; }
}

public class ExpenseKpiSummaryViewModel
{
    public int TotalClaimsCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedAwaitingPayoutCount { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public decimal TotalReimbursedYtd { get; set; }
}

public class ExpensesIndexPageViewModel
{
    public int? EmployeeId { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }

    public ExpenseKpiSummaryViewModel Kpi { get; set; } = new();
    public List<ExpenseClaim> Claims { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class ExpenseCategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required, StringLength(20)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(1, 100000000)]
    public decimal? MaxLimitPerClaim { get; set; }

    public bool RequiresReceipt { get; set; } = true;

    [StringLength(400)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ExpenseClaimFormViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;

    [Required, StringLength(150)]
    public string MerchantOrVendor { get; set; } = string.Empty;

    [Required, Range(0.01, 10000000)]
    public decimal ClaimAmount { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public string? ReceiptFileName { get; set; }
    public string? ReceiptStoredName { get; set; }
}

public class ExpenseReviewViewModel
{
    [Required]
    public int ClaimId { get; set; }

    [Required, StringLength(20)]
    public string ActionStatus { get; set; } = "Approved";

    [Range(0.01, 10000000)]
    public decimal? ApprovedAmount { get; set; }

    [StringLength(400)]
    public string? ReviewerComments { get; set; }

    [StringLength(80)]
    public string? PaymentReference { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ReimbursedDate { get; set; }
}
