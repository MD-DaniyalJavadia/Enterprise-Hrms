using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Models.ViewModels.PerformanceTraining;

public class PerformanceKpiSummaryViewModel
{
    public int TotalAppraisals { get; set; }
    public int PendingSelfReview { get; set; }
    public int PendingManagerReview { get; set; }
    public int CompletedAppraisals { get; set; }
    public decimal AverageFinalRating { get; set; }
}

public class PerformanceIndexPageViewModel
{
    public int? CycleId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }

    public PerformanceKpiSummaryViewModel Kpi { get; set; } = new();
    public List<PerformanceAppraisal> Appraisals { get; set; } = new();
    public List<AppraisalCycle> Cycles { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class PerformanceGoalsPageViewModel
{
    public int? CycleId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }

    public List<PerformanceGoal> Goals { get; set; } = new();
    public List<AppraisalCycle> Cycles { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class AppraisalCycleFormViewModel
{
    public int CycleId { get; set; }

    [Required, StringLength(20)]
    public string CycleCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string CycleName { get; set; } = string.Empty;

    [Required, Range(2020, 2050)]
    public int ReviewYear { get; set; } = DateTime.Today.Year;

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = new DateTime(DateTime.Today.Year, 12, 31);

    [DataType(DataType.Date)]
    public DateTime? SelfReviewDeadline { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ManagerReviewDeadline { get; set; }

    [Required, StringLength(20)]
    public string CycleStatus { get; set; } = "Active";

    [StringLength(400)]
    public string? Description { get; set; }
}

public class PerformanceGoalFormViewModel
{
    public int GoalId { get; set; }

    [Required]
    public int CycleId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required, StringLength(150)]
    public string GoalTitle { get; set; } = string.Empty;

    [StringLength(200)]
    public string? KpiMetric { get; set; }

    [Required, Range(1, 100)]
    public decimal WeightPercent { get; set; } = 25m;

    [StringLength(100)]
    public string? TargetValue { get; set; }

    [StringLength(100)]
    public string? AchievedValue { get; set; }

    [Range(0, 100)]
    public int ProgressPercent { get; set; }

    [Required, StringLength(20)]
    public string GoalStatus { get; set; } = "InProgress";

    [StringLength(400)]
    public string? ManagerNotes { get; set; }
}

public class GoalProgressUpdateViewModel
{
    [Required]
    public int GoalId { get; set; }

    [StringLength(100)]
    public string? AchievedValue { get; set; }

    [Required, Range(0, 100)]
    public int ProgressPercent { get; set; }

    [Required, StringLength(20)]
    public string GoalStatus { get; set; } = "InProgress";
}

public class SelfAppraisalSubmitViewModel
{
    [Required]
    public int AppraisalId { get; set; }

    [Required, Range(1.0, 5.0)]
    public decimal SelfRating { get; set; } = 4.0m;

    [StringLength(1000)]
    public string? SelfComments { get; set; }
}

public class ManagerAppraisalReviewViewModel
{
    [Required]
    public int AppraisalId { get; set; }

    [Required, Range(1.0, 5.0)]
    public decimal ManagerRating { get; set; } = 4.0m;

    [Required, Range(1.0, 5.0)]
    public decimal FinalRating { get; set; } = 4.0m;

    [StringLength(600)]
    public string? Strengths { get; set; }

    [StringLength(600)]
    public string? ImprovementAreas { get; set; }

    [StringLength(1000)]
    public string? ManagerComments { get; set; }

    [Range(0, 100)]
    public decimal? RecommendedIncrementPercent { get; set; }

    public bool Finalize { get; set; } = true;
}

public class TrainingKpiSummaryViewModel
{
    public int TotalPrograms { get; set; }
    public int ActiveOrPlannedPrograms { get; set; }
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public decimal CompletionRatePercent { get; set; }
}

public class TrainingIndexPageViewModel
{
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }

    public TrainingKpiSummaryViewModel Kpi { get; set; } = new();
    public List<TrainingProgram> Programs { get; set; } = new();
}

public class TrainingProgramFormViewModel
{
    public int ProgramId { get; set; }

    [Required, StringLength(25)]
    public string ProgramCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string ProgramTitle { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = "Technical";

    [Required, StringLength(30)]
    public string DeliveryMode { get; set; } = "Workshop";

    [Required, Range(0.5, 500)]
    public decimal DurationHours { get; set; } = 8m;

    [StringLength(150)]
    public string? TrainerOrProvider { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(8);

    [Required, Range(1, 5000)]
    public int MaxParticipants { get; set; } = 30;

    [StringLength(250)]
    public string? LocationOrUrl { get; set; }

    public bool IsMandatory { get; set; }

    [Required, StringLength(20)]
    public string ProgramStatus { get; set; } = "Planned";

    [StringLength(600)]
    public string? Description { get; set; }
}

public class TrainingEnrollmentsPageViewModel
{
    public int? ProgramId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }

    public List<TrainingEnrollment> Enrollments { get; set; } = new();
    public List<TrainingProgram> Programs { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class TrainingEnrollFormViewModel
{
    [Required]
    public int ProgramId { get; set; }

    [Required]
    public int EmployeeId { get; set; }
}

public class TrainingOutcomeViewModel
{
    [Required]
    public int EnrollmentId { get; set; }

    [Required, StringLength(20)]
    public string AttendanceStatus { get; set; } = "Completed";

    [Range(0, 100)]
    public decimal? AssessmentScore { get; set; }

    [StringLength(50)]
    public string? CertificateNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? CompletionDate { get; set; }

    [Range(1, 5)]
    public int? FeedbackRating { get; set; }

    [StringLength(400)]
    public string? FeedbackNotes { get; set; }
}
