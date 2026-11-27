namespace HRMS.Models.Entities;

public class EmployeeEmergencyContact
{
    public int EmergencyContactId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? Address { get; set; }
    public bool IsPrimary { get; set; }
}

public class EmployeeFamilyMember
{
    public int FamilyMemberId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsDependent { get; set; }
}

public class EmployeeEducation
{
    public int EducationId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string DegreeTitle { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public int? StartYear { get; set; }
    public int? CompletionYear { get; set; }
    public string? GradeOrGpa { get; set; }
}

public class EmployeeExperience
{
    public int ExperienceId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Responsibilities { get; set; }
}

public class EmployeeSkill
{
    public int SkillId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = "Intermediate";
    public string? CertificationName { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class EmployeeDocument
{
    public int DocumentId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class EmployeeEmploymentHistory
{
    public int HistoryId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string? FromBranchName { get; set; }
    public string? ToBranchName { get; set; }
    public string? FromDepartmentName { get; set; }
    public string? ToDepartmentName { get; set; }
    public string? FromDesignation { get; set; }
    public string? ToDesignation { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class EmployeeSalaryHistory
{
    public int SalaryHistoryId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public decimal PreviousSalary { get; set; }
    public decimal NewSalary { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
