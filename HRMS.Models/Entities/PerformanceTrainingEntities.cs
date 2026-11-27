namespace HRMS.Models.Entities;

public class AppraisalCycle
{
    public int CycleId { get; set; }
    public int CompanyId { get; set; }
    public string CycleCode { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public int ReviewYear { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SelfReviewDeadline { get; set; }
    public DateTime? ManagerReviewDeadline { get; set; }
    public string CycleStatus { get; set; } = "Draft";
    public string? Description { get; set; }
    public int AppraisalsCount { get; set; }
}

public class PerformanceGoal
{
    public int GoalId { get; set; }
    public int CompanyId { get; set; }
    public int CycleId { get; set; }
    public string CycleCode { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string GoalTitle { get; set; } = string.Empty;
    public string? KpiMetric { get; set; }
    public decimal WeightPercent { get; set; }
    public string? TargetValue { get; set; }
    public string? AchievedValue { get; set; }
    public int ProgressPercent { get; set; }
    public string GoalStatus { get; set; } = "NotStarted";
    public string? ManagerNotes { get; set; }
}

public class PerformanceAppraisal
{
    public int AppraisalId { get; set; }
    public int CompanyId { get; set; }
    public int CycleId { get; set; }
    public string CycleCode { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DesignationTitle { get; set; } = string.Empty;
    public int? ReviewerEmployeeId { get; set; }
    public string? ReviewerName { get; set; }
    public decimal? SelfRating { get; set; }
    public string? SelfComments { get; set; }
    public DateTime? SelfSubmittedDate { get; set; }
    public decimal? ManagerRating { get; set; }
    public decimal? FinalRating { get; set; }
    public string? RatingBand { get; set; }
    public string? Strengths { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? ManagerComments { get; set; }
    public decimal? RecommendedIncrementPercent { get; set; }
    public string AppraisalStatus { get; set; } = "NotStarted";
    public DateTime? CompletedDate { get; set; }
}

public class TrainingProgram
{
    public int ProgramId { get; set; }
    public int CompanyId { get; set; }
    public string ProgramCode { get; set; } = string.Empty;
    public string ProgramTitle { get; set; } = string.Empty;
    public string Category { get; set; } = "Technical";
    public string DeliveryMode { get; set; } = "Workshop";
    public decimal DurationHours { get; set; }
    public string? TrainerOrProvider { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxParticipants { get; set; }
    public string? LocationOrUrl { get; set; }
    public bool IsMandatory { get; set; }
    public string ProgramStatus { get; set; } = "Planned";
    public string? Description { get; set; }
    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }
}

public class TrainingEnrollment
{
    public int EnrollmentId { get; set; }
    public int CompanyId { get; set; }
    public int ProgramId { get; set; }
    public string ProgramCode { get; set; } = string.Empty;
    public string ProgramTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string AttendanceStatus { get; set; } = "Enrolled";
    public decimal? AssessmentScore { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? CompletionDate { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackNotes { get; set; }
}
