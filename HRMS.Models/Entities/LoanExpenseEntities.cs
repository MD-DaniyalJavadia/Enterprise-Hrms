namespace HRMS.Models.Entities;

public class LoanType
{
    public int LoanTypeId { get; set; }
    public int CompanyId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Category { get; set; } = "Loan";
    public decimal MaxAmount { get; set; }
    public decimal DefaultInterestRate { get; set; }
    public int MaxTenureMonths { get; set; }
    public bool RequiresGuarantor { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int LoansCount { get; set; }
}

public class EmployeeLoan
{
    public int LoanId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LoanTypeId { get; set; }
    public string LoanTypeCode { get; set; } = string.Empty;
    public string LoanTypeName { get; set; } = string.Empty;
    public string LoanCategory { get; set; } = "Loan";
    public string LoanNumber { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRatePercent { get; set; }
    public int TenureMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalPayableAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? Purpose { get; set; }
    public DateTime RepaymentStartDate { get; set; }
    public string LoanStatus { get; set; } = "Pending";
    public DateTime? ApprovedDate { get; set; }
    public DateTime? DisbursedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LoanRepaymentInstallment
{
    public int ScheduleId { get; set; }
    public int CompanyId { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }
    public int? PayrollRunId { get; set; }
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public int CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal? MaxLimitPerClaim { get; set; }
    public bool RequiresReceipt { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int ClaimsCount { get; set; }
}

public class ExpenseClaim
{
    public int ClaimId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string MerchantOrVendor { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? Description { get; set; }
    public string? ReceiptFileName { get; set; }
    public string? ReceiptStoredName { get; set; }
    public string ClaimStatus { get; set; } = "Submitted";
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime? ReimbursedDate { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedDate { get; set; }
}
