using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.PerformanceTraining;

namespace HRMS.Business.Interfaces;

public interface IPerformanceTrainingService
{
    Task<List<AppraisalCycle>> GetCyclesAsync(int companyId);
    Task<OperationResult<int>> SaveCycleAsync(int companyId, AppraisalCycleFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteCycleAsync(int companyId, int cycleId, int userId, string username);

    Task<PerformanceGoalsPageViewModel> GetGoalsPageAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false);
    Task<OperationResult<int>> SaveGoalAsync(int companyId, PerformanceGoalFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateGoalProgressAsync(int companyId, GoalProgressUpdateViewModel model, int userId, string username);
    Task<OperationResult> DeleteGoalAsync(int companyId, int goalId, int userId, string username);

    Task<PerformanceIndexPageViewModel> GetAppraisalsPageAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false);
    Task<OperationResult<int>> InitializeCycleAppraisalsAsync(int companyId, int cycleId, int userId, string username);
    Task<OperationResult> SubmitSelfAppraisalAsync(int companyId, SelfAppraisalSubmitViewModel model, int userId, string username);
    Task<OperationResult> SubmitManagerReviewAsync(int companyId, ManagerAppraisalReviewViewModel model, int userId, string username);

    Task<TrainingIndexPageViewModel> GetTrainingProgramsPageAsync(int companyId, string? category = null, string? status = null, string? searchTerm = null);
    Task<OperationResult<int>> SaveTrainingProgramAsync(int companyId, TrainingProgramFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteTrainingProgramAsync(int companyId, int programId, int userId, string username);

    Task<TrainingEnrollmentsPageViewModel> GetEnrollmentsPageAsync(
        int companyId, int? programId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false);
    Task<OperationResult<int>> EnrollEmployeeAsync(int companyId, TrainingEnrollFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateEnrollmentOutcomeAsync(int companyId, TrainingOutcomeViewModel model, int userId, string username);
    Task<OperationResult> DeleteEnrollmentAsync(int companyId, int enrollmentId, int userId, string username);
}
