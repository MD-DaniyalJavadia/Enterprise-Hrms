using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Payroll;

public class PayrollKpiSummaryViewModel
{
    public int TotalRuns { get; set; }
    public int DraftCount { get; set; }
    public int FinalizedCount { get; set; }
    public decimal LatestNetPay { get; set; }
    public decimal YearToDateGross { get; set; }
    public decimal YearToDateTax { get; set; }
}

public class PayrollRunsPageViewModel
{
    public int? FilterYear { get; set; }
    public PayrollKpiSummaryViewModel Kpi { get; set; } = new();
    public List<PayrollRun> Runs { get; set; } = new();
}

public class PayrollGenerateViewModel
{
    [Required, Range(2020, 2050)]
    public int PayrollYear { get; set; } = DateTime.Today.Year;

    [Required, Range(1, 12)]
    public int PayrollMonth { get; set; } = DateTime.Today.Month;

    [DataType(DataType.Date)]
    public DateTime? PayDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class PayrollDepartmentSummaryViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNet { get; set; }
}

public class PayrollRunDetailsViewModel
{
    public PayrollRun Run { get; set; } = new();
    public List<Payslip> Payslips { get; set; } = new();
    public List<PayrollDepartmentSummaryViewModel> DepartmentSummary { get; set; } = new();
}

public class PayslipAdjustViewModel
{
    [Required]
    public int PayslipId { get; set; }

    [Required, Range(0, 1000000)]
    public decimal BonusAmount { get; set; }

    [Required, Range(0, 1000000)]
    public decimal OtherDeductions { get; set; }

    [Required, StringLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class PayslipHeaderInfo : Payslip
{
    public string WorkEmail { get; set; } = string.Empty;
    public string? TaxIdentificationNumber { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int PayrollYear { get; set; }
    public int PayrollMonth { get; set; }
    public DateTime? PayDate { get; set; }
    public string PayrollStatus { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? CompanyAddress { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class PayslipDetailViewModel
{
    public PayslipHeaderInfo Payslip { get; set; } = new();
    public List<PayslipLine> Lines { get; set; } = new();
}

public class SalaryComponentFormViewModel
{
    public int ComponentId { get; set; }

    [Required, StringLength(20)]
    public string ComponentCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ComponentName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string ComponentType { get; set; } = "Earning";

    [Required, StringLength(20)]
    public string CalculationType { get; set; } = "Fixed";

    [Required, Range(0, 1000000)]
    public decimal DefaultValue { get; set; }

    public bool IsTaxable { get; set; } = true;
    public bool IsStatutory { get; set; }

    [Range(1, 999)]
    public int DisplayOrder { get; set; } = 10;

    public bool IsActive { get; set; } = true;
}

public class TaxSlabFormViewModel
{
    public int TaxSlabId { get; set; }

    [Required, Range(2020, 2050)]
    public int TaxYear { get; set; } = DateTime.Today.Year;

    [Required, StringLength(80)]
    public string SlabName { get; set; } = string.Empty;

    [Required, Range(0, 100000000)]
    public decimal MinAnnualIncome { get; set; }

    [Range(0, 100000000)]
    public decimal? MaxAnnualIncome { get; set; }

    [Required, Range(0, 100)]
    public decimal TaxRatePercent { get; set; }

    [Required, Range(0, 10000000)]
    public decimal FixedTaxAmount { get; set; }

    public bool IsActive { get; set; } = true;
}

public class PayrollComponentsPageViewModel
{
    public int? FilterTaxYear { get; set; }
    public List<SalaryComponent> Components { get; set; } = new();
    public List<TaxSlab> TaxSlabs { get; set; } = new();
}

public class EmployeeSalaryStructureListItemViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public decimal AnnualBaseSalary { get; set; }
    public int? SalaryStructureId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public decimal BasicSalaryMonthly { get; set; }
    public decimal HourlyOvertimeRate { get; set; }
    public string TaxRegime { get; set; } = "Standard";
    public string PaymentMethod { get; set; } = "BankTransfer";
    public bool HasCustomStructure { get; set; }
    public decimal MonthlyGrossPackage { get; set; }
    public decimal MonthlyRecurringDeductions { get; set; }
}

public class SalaryStructureItemInputViewModel
{
    public int ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "Earning";
    public string CalculationType { get; set; } = "Fixed";
    public decimal AmountOrPercent { get; set; }
    public decimal CalculatedMonthlyAmount { get; set; }
    public bool IsIncluded { get; set; }
}

public class SalaryStructureEditViewModel
{
    [Required]
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal AnnualBaseSalary { get; set; }
    public int SalaryStructureId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [Required, Range(1, 10000000)]
    public decimal BasicSalaryMonthly { get; set; }

    [Required, Range(0, 100000)]
    public decimal HourlyOvertimeRate { get; set; }

    [Required, StringLength(40)]
    public string TaxRegime { get; set; } = "Standard";

    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = "BankTransfer";

    [StringLength(300)]
    public string? Remarks { get; set; }

    public List<SalaryStructureItemInputViewModel> Items { get; set; } = new();
}

public class PayrollStructuresPageViewModel
{
    public int? DepartmentId { get; set; }
    public string? SearchTerm { get; set; }
    public List<EmployeeSalaryStructureListItemViewModel> Structures { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class BankTransferRowViewModel
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string BankAccountTitle { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal NetPay { get; set; }
}
