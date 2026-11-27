namespace HRMS.Models.Entities;

public class JobRequisition
{
    public int RequisitionId { get; set; }
    public int CompanyId { get; set; }
    public string RequisitionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int DesignationId { get; set; }
    public string DesignationTitle { get; set; } = string.Empty;
    public int? JobPositionId { get; set; }
    public int? HiringManagerId { get; set; }
    public string? HiringManagerName { get; set; }
    public string EmploymentType { get; set; } = "Full-Time";
    public string WorkMode { get; set; } = "On-Site";
    public int OpeningsCount { get; set; } = 1;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public int ExperienceMinYears { get; set; }
    public int? ExperienceMaxYears { get; set; }
    public DateTime? TargetHireDate { get; set; }
    public string RequisitionStatus { get; set; } = "Draft";
    public string? JobSummary { get; set; }
    public string? RequiredSkills { get; set; }
    public int ApplicantsCount { get; set; }
    public int HiredCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Candidate
{
    public int CandidateId { get; set; }
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentJobTitle { get; set; }
    public decimal TotalExperienceYears { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? Skills { get; set; }
    public string Source { get; set; } = "CareerPortal";
    public string? LinkedInUrl { get; set; }
    public string? ResumeFileName { get; set; }
    public string? ResumeStoredName { get; set; }
    public string? Notes { get; set; }
    public int ApplicationsCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class JobApplication
{
    public int ApplicationId { get; set; }
    public int CompanyId { get; set; }
    public int RequisitionId { get; set; }
    public string RequisitionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public decimal TotalExperienceYears { get; set; }
    public string? CurrentJobTitle { get; set; }
    public string? ResumeStoredName { get; set; }
    public string PipelineStage { get; set; } = "Applied";
    public int? MatchScore { get; set; }
    public DateTime AppliedDate { get; set; }
    public DateTime? StageUpdatedDate { get; set; }
    public string? RejectionReason { get; set; }
    public int? HiredEmployeeId { get; set; }
}

public class InterviewSchedule
{
    public int InterviewId { get; set; }
    public int CompanyId { get; set; }
    public int ApplicationId { get; set; }
    public int RequisitionId { get; set; }
    public string RequisitionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string InterviewRound { get; set; } = string.Empty;
    public string InterviewMode { get; set; } = "Video";
    public DateTime ScheduledStart { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public string? LocationOrLink { get; set; }
    public int? InterviewerEmployeeId { get; set; }
    public string? InterviewerName { get; set; }
    public string InterviewStatus { get; set; } = "Scheduled";
    public int? TechnicalScore { get; set; }
    public int? CommunicationScore { get; set; }
    public string? OverallRecommendation { get; set; }
    public string? FeedbackNotes { get; set; }
}

public class JobOffer
{
    public int OfferId { get; set; }
    public int CompanyId { get; set; }
    public int ApplicationId { get; set; }
    public int RequisitionId { get; set; }
    public int CandidateId { get; set; }
    public int? HiredEmployeeId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public string RequisitionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public decimal OfferedSalaryAnnual { get; set; }
    public DateTime JoiningDate { get; set; }
    public DateTime OfferExpiryDate { get; set; }
    public string EmploymentType { get; set; } = "Full-Time";
    public string OfferStatus { get; set; } = "Draft";
    public string? BenefitsSummary { get; set; }
    public string? ResponseNotes { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? RespondedDate { get; set; }
}
