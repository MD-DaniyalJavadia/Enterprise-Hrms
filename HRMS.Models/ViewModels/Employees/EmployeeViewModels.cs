using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Employees;

public class EmployeeListItemViewModel
{
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int DesignationId { get; set; }
    public string DesignationTitle { get; set; } = string.Empty;
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public DateTime HireDate { get; set; }
    public byte EmploymentStatus { get; set; }
    public decimal BaseSalary { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; }

    public string StatusLabel => ((EmploymentStatus)EmploymentStatus).ToString();
}

public class EmployeeDirectoryFilterViewModel
{
    public int? BranchId { get; set; }
    public int? DepartmentId { get; set; }
    public byte? EmploymentStatus { get; set; }
    public string? SearchTerm { get; set; }

    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class EmployeeFormViewModel
{
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }

    [Required, Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required, Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required, Display(Name = "Designation")]
    public int DesignationId { get; set; }

    [Display(Name = "Reporting Manager")]
    public int? ReportingManagerId { get; set; }

    [Display(Name = "Work Location")]
    public int? WorkLocationId { get; set; }

    [Display(Name = "Job Position")]
    public int? JobPositionId { get; set; }

    [Display(Name = "Assigned Shift")]
    public int? ShiftId { get; set; }

    [Required, StringLength(30), Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, StringLength(80), Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(80), Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    [Required, StringLength(80), Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Gender { get; set; } = "Male";

    [Required, DataType(DataType.Date), Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);

    [StringLength(20), Display(Name = "Marital Status")]
    public string? MaritalStatus { get; set; }

    [StringLength(60)]
    public string? Nationality { get; set; }

    [StringLength(50), Display(Name = "National ID / Passport")]
    public string? NationalIdNumber { get; set; }

    [StringLength(10), Display(Name = "Blood Group")]
    public string? BloodGroup { get; set; }

    [Required, EmailAddress, StringLength(120), Display(Name = "Work Email")]
    public string WorkEmail { get; set; } = string.Empty;

    [EmailAddress, StringLength(120), Display(Name = "Personal Email")]
    public string? PersonalEmail { get; set; }

    [StringLength(30), Display(Name = "Work Phone")]
    public string? Phone { get; set; }

    [StringLength(30), Display(Name = "Mobile Phone")]
    public string? MobilePhone { get; set; }

    [StringLength(300), Display(Name = "Current Address")]
    public string? CurrentAddress { get; set; }

    [StringLength(300), Display(Name = "Permanent Address")]
    public string? PermanentAddress { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? Country { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date), Display(Name = "Confirmation Date")]
    public DateTime? ConfirmationDate { get; set; }

    [Required, Display(Name = "Employment Status")]
    public byte EmploymentStatus { get; set; } = (byte)Enums.EmploymentStatus.Active;

    [Required, Range(0, 100000000), Display(Name = "Base Salary")]
    public decimal BaseSalary { get; set; }

    [StringLength(120), Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [StringLength(120), Display(Name = "Account Title")]
    public string? BankAccountTitle { get; set; }

    [StringLength(50), Display(Name = "Account Number / IBAN")]
    public string? BankAccountNumber { get; set; }

    [StringLength(40), Display(Name = "Routing / SWIFT Code")]
    public string? BankRoutingCode { get; set; }

    [StringLength(50), Display(Name = "Tax ID / SSN")]
    public string? TaxIdentificationNumber { get; set; }

    [StringLength(40), Display(Name = "Tax Filing Status")]
    public string? TaxFilingStatus { get; set; }

    public string? PhotoPath { get; set; }

    // Dropdown Options
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<DesignationListItemViewModel> Designations { get; set; } = new();
    public List<EmployeeListItemViewModel> Managers { get; set; } = new();
    public List<WorkLocationViewModel> WorkLocations { get; set; } = new();
    public List<JobPositionViewModel> JobPositions { get; set; } = new();
    public List<ShiftViewModel> Shifts { get; set; } = new();
}

public class EmployeeDetailRecord
{
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int DesignationId { get; set; }
    public string DesignationTitle { get; set; } = string.Empty;
    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public int? WorkLocationId { get; set; }
    public string? WorkLocationName { get; set; }
    public int? JobPositionId { get; set; }
    public string? JobPositionTitle { get; set; }
    public int? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public string? NationalIdNumber { get; set; }
    public string? BloodGroup { get; set; }
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
    public DateTime? ExitDate { get; set; }
    public byte EmploymentStatus { get; set; }
    public decimal BaseSalary { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingCode { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? TaxFilingStatus { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; }
    public string StatusLabel => ((EmploymentStatus)EmploymentStatus).ToString();
}

public class EmployeeProfileViewModel
{
    public EmployeeDetailRecord Employee { get; set; } = new();
    public List<EmployeeEmergencyContact> EmergencyContacts { get; set; } = new();
    public List<EmployeeFamilyMember> FamilyMembers { get; set; } = new();
    public List<EmployeeEducation> EducationHistory { get; set; } = new();
    public List<EmployeeExperience> ExperienceHistory { get; set; } = new();
    public List<EmployeeSkill> Skills { get; set; } = new();
    public List<EmployeeDocument> Documents { get; set; } = new();
    public List<EmployeeEmploymentHistory> EmploymentTimeline { get; set; } = new();
    public List<EmployeeSalaryHistory> SalaryHistory { get; set; } = new();

    // Dropdowns for Career Action Modal
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<DesignationListItemViewModel> Designations { get; set; } = new();
}

public class EmployeeCareerActionViewModel
{
    [Required]
    public int EmployeeId { get; set; }

    [Required, StringLength(40)]
    public string ActionType { get; set; } = "Promotion"; // Promotion, Transfer, SalaryRevision, StatusChange

    [Required]
    public DateTime EffectiveDate { get; set; } = DateTime.Today;

    [Required]
    public int NewBranchId { get; set; }

    [Required]
    public int NewDepartmentId { get; set; }

    [Required]
    public int NewDesignationId { get; set; }

    [Required]
    public byte NewStatus { get; set; } = 1;

    [Required, Range(0, 100000000)]
    public decimal NewBaseSalary { get; set; }

    [Required, StringLength(500)]
    public string Remarks { get; set; } = string.Empty;
}

public class EmployeeImportResultViewModel
{
    public int TotalRowsProcessed { get; set; }
    public int ImportedCount { get; set; }
    public List<string> RowErrors { get; set; } = new();
}
