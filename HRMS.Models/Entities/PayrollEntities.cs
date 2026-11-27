namespace HRMS.Models.Entities;

public class SalaryComponent
{
    public int ComponentId { get; set; }
    public int CompanyId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "Earning"; // Earning, Deduction, EmployerContribution
    public string CalculationType { get; set; } = "Fixed"; // Fixed, PercentOfBasic
    public decimal DefaultValue { get; set; }
    public bool IsTaxable { get; set; } = true;
    public bool IsStatutory { get; set; }
    public bool IsSystemComponent { get; set; }
    public int DisplayOrder { get; set; } = 10;
    public bool IsActive { get; set; } = true;
}

public class TaxSlab
{
    public int TaxSlabId { get; set; }
    public int CompanyId { get; set; }
    public int TaxYear { get; set; } = DateTime.Today.Year;
    public string SlabName { get; set; } = string.Empty;
    public decimal MinAnnualIncome { get; set; }
    public decimal? MaxAnnualIncome { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal FixedTaxAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PayrollRun
{
    public int PayrollRunId { get; set; }
    public int CompanyId { get; set; }
    public int PayrollYear { get; set; }
    public int PayrollMonth { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime? PayDate { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalEmployerContributions { get; set; }
    public decimal TotalTaxDeducted { get; set; }
    public int ProcessedEmployees { get; set; }
    public string PayrollStatus { get; set; } = "Draft"; // Draft, Approved, Locked, Paid
    public string? Notes { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? LockedDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Payslip
{
    public int PayslipId { get; set; }
    public int CompanyId { get; set; }
    public int PayrollRunId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal WorkingDaysInMonth { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasicPay { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal LeaveEncashmentPay { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal GrossPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal StatutoryDeductions { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal UnpaidAbsenceDeduction { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal EmployerContributions { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingCode { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
}

public class PayslipLine
{
    public long PayslipLineId { get; set; }
    public int CompanyId { get; set; }
    public int PayslipId { get; set; }
    public int? ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DisplayOrder { get; set; }
}
