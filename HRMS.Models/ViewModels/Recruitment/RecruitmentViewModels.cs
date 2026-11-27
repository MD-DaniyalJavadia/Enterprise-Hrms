using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.Recruitment;

public class RecruitmentKpiSummaryViewModel
{
    public int OpenRequisitions { get; set; }
    public int TotalOpenPositions { get; set; }
    public int ActiveApplicants { get; set; }
    public int ScheduledInterviews { get; set; }
    public int ActiveOffers { get; set; }
}

public class RequisitionsPageViewModel
{
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }

    public RecruitmentKpiSummaryViewModel Kpi { get; set; } = new();
    public List<JobRequisition> Requisitions { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
    public List<DesignationListItemViewModel> Designations { get; set; } = new();
    public List<JobPositionViewModel> Positions { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class JobRequisitionFormViewModel
{
    public int RequisitionId { get; set; }

    [StringLength(30)]
    public string? RequisitionCode { get; set; }

    [Required, StringLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public int BranchId { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int DesignationId { get; set; }

    public int? JobPositionId { get; set; }
    public int? HiringManagerId { get; set; }

    [Required, StringLength(30)]
    public string EmploymentType { get; set; } = "Full-Time";

    [Required, StringLength(30)]
    public string WorkMode { get; set; } = "On-Site";

    [Required, Range(1, 500)]
    public int OpeningsCount { get; set; } = 1;

    [Range(0, 100000000)]
    public decimal? MinSalary { get; set; }

    [Range(0, 100000000)]
    public decimal? MaxSalary { get; set; }

    [Range(0, 60)]
    public int ExperienceMinYears { get; set; }

    [Range(0, 60)]
    public int? ExperienceMaxYears { get; set; }

    [DataType(DataType.Date)]
    public DateTime? TargetHireDate { get; set; }

    [Required, StringLength(30)]
    public string RequisitionStatus { get; set; } = "Open";

    [StringLength(1000)]
    public string? JobSummary { get; set; }

    [StringLength(500)]
    public string? RequiredSkills { get; set; }
}

public class CandidatesPageViewModel
{
    public string? Source { get; set; }
    public string? SearchTerm { get; set; }
    public List<Candidate> Candidates { get; set; } = new();
}

public class CandidateFormViewModel
{
    public int CandidateId { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? CurrentCompany { get; set; }

    [StringLength(120)]
    public string? CurrentJobTitle { get; set; }

    [Range(0, 60)]
    public decimal TotalExperienceYears { get; set; }

    [Range(0, 100000000)]
    public decimal? ExpectedSalary { get; set; }

    [StringLength(500)]
    public string? Skills { get; set; }

    [Required, StringLength(50)]
    public string Source { get; set; } = "CareerPortal";

    [StringLength(250)]
    public string? LinkedInUrl { get; set; }

    public string? ResumeFileName { get; set; }
    public string? ResumeStoredName { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class PipelineStageCountsViewModel
{
    public int AppliedCount { get; set; }
    public int ScreeningCount { get; set; }
    public int InterviewCount { get; set; }
    public int AssessmentCount { get; set; }
    public int OfferCount { get; set; }
    public int HiredCount { get; set; }
    public int ClosedCount { get; set; }
}

public class PipelinePageViewModel
{
    public int? RequisitionId { get; set; }
    public string? PipelineStage { get; set; }
    public PipelineStageCountsViewModel StageCounts { get; set; } = new();
    public List<JobApplication> Applications { get; set; } = new();
    public List<JobRequisition> OpenRequisitions { get; set; } = new();
    public List<Candidate> Candidates { get; set; } = new();
}

public class JobApplicationSubmitViewModel
{
    [Required]
    public int RequisitionId { get; set; }

    [Required]
    public int CandidateId { get; set; }

    [Range(0, 100)]
    public int? MatchScore { get; set; }
}

public class JobApplicationMoveStageViewModel
{
    [Required]
    public int ApplicationId { get; set; }

    [Required, StringLength(30)]
    public string PipelineStage { get; set; } = string.Empty;

    [StringLength(300)]
    public string? RejectionReason { get; set; }
}

public class InterviewsPageViewModel
{
    public string? InterviewStatus { get; set; }
    public List<InterviewSchedule> Interviews { get; set; } = new();
    public List<JobApplication> Applications { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class InterviewScheduleFormViewModel
{
    public int InterviewId { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required, StringLength(50)]
    public string InterviewRound { get; set; } = "Technical Round";

    [Required, StringLength(30)]
    public string InterviewMode { get; set; } = "Video";

    [Required]
    public DateTime ScheduledStart { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

    [Required, Range(15, 480)]
    public int DurationMinutes { get; set; } = 45;

    [StringLength(250)]
    public string? LocationOrLink { get; set; }

    public int? InterviewerEmployeeId { get; set; }
}

public class InterviewFeedbackViewModel
{
    [Required]
    public int InterviewId { get; set; }

    [Required, StringLength(20)]
    public string InterviewStatus { get; set; } = "Completed";

    [Range(1, 5)]
    public int? TechnicalScore { get; set; }

    [Range(1, 5)]
    public int? CommunicationScore { get; set; }

    [StringLength(30)]
    public string? OverallRecommendation { get; set; }

    [StringLength(1000)]
    public string? FeedbackNotes { get; set; }
}

public class OffersPageViewModel
{
    public string? OfferStatus { get; set; }
    public List<JobOffer> Offers { get; set; } = new();
    public List<JobApplication> EligibleApplications { get; set; } = new();
}

public class JobOfferFormViewModel
{
    public int OfferId { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required, Range(1, 100000000)]
    public decimal OfferedSalaryAnnual { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime JoiningDate { get; set; } = DateTime.Today.AddDays(21);

    [Required, DataType(DataType.Date)]
    public DateTime OfferExpiryDate { get; set; } = DateTime.Today.AddDays(7);

    [Required, StringLength(30)]
    public string EmploymentType { get; set; } = "Full-Time";

    [StringLength(500)]
    public string? BenefitsSummary { get; set; }
}
