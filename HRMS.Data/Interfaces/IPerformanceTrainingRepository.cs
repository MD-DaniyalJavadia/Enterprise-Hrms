using HRMS.Models.Entities;
using HRMS.Models.ViewModels.PerformanceTraining;

namespace HRMS.Data.Interfaces;

public interface IPerformanceTrainingRepository
{
    Task<IEnumerable<AppraisalCycle>> GetCyclesAsync(int companyId);
    Task<int> SaveCycleAsync(int companyId, AppraisalCycleFormViewModel model, int userId);
    Task DeleteCycleAsync(int companyId, int cycleId, int userId);

    Task<IEnumerable<PerformanceGoal>> GetGoalsAsync(int companyId, int? cycleId = null, int? employeeId = null, string? status = null);
    Task<int> SaveGoalAsync(int companyId, PerformanceGoalFormViewModel model, int userId);
    Task UpdateGoalProgressAsync(int companyId, GoalProgressUpdateViewModel model, int userId);
    Task DeleteGoalAsync(int companyId, int goalId, int userId);

    Task<(IEnumerable<PerformanceAppraisal> Appraisals, PerformanceKpiSummaryViewModel Kpi)> GetAppraisalsAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null);
    Task<int> InitializeCycleAppraisalsAsync(int companyId, int cycleId, int userId);
    Task SubmitSelfAppraisalAsync(int companyId, SelfAppraisalSubmitViewModel model, int userId);
    Task SubmitManagerReviewAsync(int companyId, ManagerAppraisalReviewViewModel model, int userId);

    Task<(IEnumerable<TrainingProgram> Programs, TrainingKpiSummaryViewModel Kpi)> GetTrainingProgramsAsync(
        int companyId, string? category = null, string? status = null, string? searchTerm = null);
    Task<int> SaveTrainingProgramAsync(int companyId, TrainingProgramFormViewModel model, int userId);
    Task DeleteTrainingProgramAsync(int companyId, int programId, int userId);

    Task<IEnumerable<TrainingEnrollment>> GetEnrollmentsAsync(int companyId, int? programId = null, int? employeeId = null, string? status = null);
    Task<int> EnrollEmployeeAsync(int companyId, TrainingEnrollFormViewModel model, int userId);
    Task UpdateEnrollmentOutcomeAsync(int companyId, TrainingOutcomeViewModel model, int userId);
    Task DeleteEnrollmentAsync(int companyId, int enrollmentId, int userId);
}
