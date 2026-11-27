using System.ComponentModel.DataAnnotations;

namespace HRMS.Models.ViewModels.Organization;

public class CompanyListItemViewModel
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string TimeZoneId { get; set; } = "UTC";
    public bool IsActive { get; set; }
    public int BranchCount { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CompanyFormViewModel
{
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Company code is required.")]
    [StringLength(20)]
    [Display(Name = "Company Code")]
    public string CompanyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(150)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Legal Entity Name")]
    public string? LegalName { get; set; }

    [StringLength(50)]
    [Display(Name = "Tax / Registration ID")]
    public string? TaxId { get; set; }

    [EmailAddress]
    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [Url]
    [StringLength(150)]
    public string? Website { get; set; }

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? AddressLine1 { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency (ISO 3)")]
    public string CurrencyCode { get; set; } = "USD";

    [Required]
    [StringLength(60)]
    [Display(Name = "Time Zone")]
    public string TimeZoneId { get; set; } = "UTC";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class BranchListItemViewModel
{
    public int BranchId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class BranchFormViewModel
{
    public int BranchId { get; set; }
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch code is required.")]
    [StringLength(20)]
    [Display(Name = "Branch Code")]
    public string BranchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch name is required.")]
    [StringLength(120)]
    [Display(Name = "Branch Name")]
    public string BranchName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? AddressLine1 { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [Display(Name = "Head Office")]
    public bool IsHeadOffice { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class DepartmentListItemViewModel
{
    public int DepartmentId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ParentDepartmentId { get; set; }
    public string? ParentDepartmentName { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? CostCenterCode { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class DepartmentFormViewModel
{
    public int DepartmentId { get; set; }
    public int CompanyId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Display(Name = "Parent Department")]
    public int? ParentDepartmentId { get; set; }

    [Required(ErrorMessage = "Department code is required.")]
    [StringLength(20)]
    [Display(Name = "Department Code")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(120)]
    [Display(Name = "Department Name")]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Cost Center Code")]
    public string? CostCenterCode { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<BranchListItemViewModel> AvailableBranches { get; set; } = new();
    public List<DepartmentListItemViewModel> AvailableParentDepartments { get; set; } = new();
    public List<CostCenterViewModel> AvailableCostCenters { get; set; } = new();
}

public class DesignationListItemViewModel
{
    public int DesignationId { get; set; }
    public int CompanyId { get; set; }
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string? JobGrade { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class DesignationFormViewModel
{
    public int DesignationId { get; set; }
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Designation code is required.")]
    [StringLength(20)]
    [Display(Name = "Designation Code")]
    public string DesignationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation title is required.")]
    [StringLength(120)]
    [Display(Name = "Designation Title")]
    public string DesignationTitle { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Job Grade")]
    public string? JobGrade { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<JobGradeViewModel> AvailableJobGrades { get; set; } = new();
}

public class JobGradeViewModel
{
    public int JobGradeId { get; set; }
    public int CompanyId { get; set; }

    [Required, StringLength(20)]
    public string GradeCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string GradeName { get; set; } = string.Empty;

    [Range(1, 50)]
    public int LevelOrder { get; set; } = 1;

    [Range(0, 100000000)]
    public decimal MinSalary { get; set; }

    [Range(0, 100000000)]
    public decimal MaxSalary { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class JobPositionViewModel
{
    public int JobPositionId { get; set; }
    public int CompanyId { get; set; }

    [Required]
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    [Required]
    public int DesignationId { get; set; }
    public string? DesignationTitle { get; set; }

    public int? JobGradeId { get; set; }
    public string? GradeCode { get; set; }

    public int? ReportsToPositionId { get; set; }
    public string? ReportsToPositionTitle { get; set; }

    [Required, StringLength(25)]
    public string PositionCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string PositionTitle { get; set; } = string.Empty;

    [Range(1, 5000)]
    public int HeadcountBudget { get; set; } = 1;

    [StringLength(400)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CostCenterViewModel
{
    public int CostCenterId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    [Required, StringLength(30)]
    public string CostCenterCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string CostCenterName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkLocationViewModel
{
    public int WorkLocationId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }

    [Required, StringLength(20)]
    public string LocationCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string LocationName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? AddressLine1 { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [Required, StringLength(60)]
    public string TimeZoneId { get; set; } = "UTC";

    public bool IsRemote { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ShiftViewModel
{
    public int ShiftId { get; set; }
    public int CompanyId { get; set; }

    [Required, StringLength(20)]
    public string ShiftCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ShiftName { get; set; } = string.Empty;

    [Required]
    public string StartTimeText { get; set; } = "09:00";

    [Required]
    public string EndTimeText { get; set; } = "18:00";

    [Range(0, 120)]
    public int GracePeriodMinutes { get; set; } = 15;

    [Range(0, 240)]
    public int BreakMinutes { get; set; } = 60;

    [Range(1, 24)]
    public decimal WorkingHours { get; set; } = 8.00m;

    public bool IsNightShift { get; set; }

    [Required, StringLength(60)]
    public string WeeklyOffDays { get; set; } = "Saturday,Sunday";

    public bool IsActive { get; set; } = true;
}

public class OrgChartDepartmentNodeViewModel
{
    public int DepartmentId { get; set; }
    public int? ParentDepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public int Headcount { get; set; }
    public List<OrgChartDepartmentNodeViewModel> Children { get; set; } = new();
}

public class OrgChartEmployeeNodeViewModel
{
    public int EmployeeId { get; set; }
    public int? ReportingManagerId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public List<OrgChartEmployeeNodeViewModel> DirectReports { get; set; } = new();
}

public class OrgChartViewModel
{
    public List<OrgChartDepartmentNodeViewModel> DepartmentTree { get; set; } = new();
    public List<OrgChartEmployeeNodeViewModel> ReportingTree { get; set; } = new();
}

public class PositionsAndGradesPageViewModel
{
    public List<JobGradeViewModel> JobGrades { get; set; } = new();
    public List<JobPositionViewModel> JobPositions { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<DesignationListItemViewModel> Designations { get; set; } = new();
}

public class LocationsAndCostCentersPageViewModel
{
    public List<CostCenterViewModel> CostCenters { get; set; } = new();
    public List<WorkLocationViewModel> WorkLocations { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
}
