namespace HRMS.Models.Entities;

public class Company : BaseEntity
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string TimeZoneId { get; set; } = "UTC";
}

public class Branch : BaseEntity
{
    public int BranchId { get; set; }
    public int CompanyId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsHeadOffice { get; set; }
}

public class Department : BaseEntity
{
    public int DepartmentId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public int? ParentDepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? CostCenterCode { get; set; }
}

public class Designation : BaseEntity
{
    public int DesignationId { get; set; }
    public int CompanyId { get; set; }
    public string DesignationCode { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public string? JobGrade { get; set; }
}

public class JobGrade : BaseEntity
{
    public int JobGradeId { get; set; }
    public int CompanyId { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int LevelOrder { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public string? Description { get; set; }
}

public class CostCenter : BaseEntity
{
    public int CostCenterId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class WorkLocation : BaseEntity
{
    public int WorkLocationId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public bool IsRemote { get; set; }
}

public class JobPosition : BaseEntity
{
    public int JobPositionId { get; set; }
    public int CompanyId { get; set; }
    public int DepartmentId { get; set; }
    public int DesignationId { get; set; }
    public int? JobGradeId { get; set; }
    public int? ReportsToPositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public int HeadcountBudget { get; set; } = 1;
    public string? Description { get; set; }
}

public class Shift : BaseEntity
{
    public int ShiftId { get; set; }
    public int CompanyId { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GracePeriodMinutes { get; set; } = 15;
    public int BreakMinutes { get; set; } = 60;
    public decimal WorkingHours { get; set; } = 8.00m;
    public bool IsNightShift { get; set; }
    public string WeeklyOffDays { get; set; } = "Saturday,Sunday";
}

