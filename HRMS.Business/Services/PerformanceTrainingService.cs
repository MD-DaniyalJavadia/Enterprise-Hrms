using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.PerformanceTraining;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class PerformanceTrainingService : IPerformanceTrainingService
{
    private static readonly HashSet<string> ValidCycleStatuses = new(StringComparer.OrdinalIgnoreCase) { "Draft", "Active", "Closed" };
    private static readonly HashSet<string> ValidGoalStatuses = new(StringComparer.OrdinalIgnoreCase) { "NotStarted", "InProgress", "Completed", "Deferred" };
    private static readonly HashSet<string> ValidProgramStatuses = new(StringComparer.OrdinalIgnoreCase) { "Planned", "Ongoing", "Completed", "Cancelled" };
    private static readonly HashSet<string> ValidAttendanceStatuses = new(StringComparer.OrdinalIgnoreCase) { "Enrolled", "Attended", "Completed", "NoShow", "Dropped" };

    private readonly IPerformanceTrainingRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;

    public PerformanceTrainingService(
        IPerformanceTrainingRepository repository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<AppraisalCycle>> GetCyclesAsync(int companyId)
        => (await _repository.GetCyclesAsync(companyId)).ToList();

    public async Task<OperationResult<int>> SaveCycleAsync(int companyId, AppraisalCycleFormViewModel model, int userId, string username)
    {
        if (!ValidCycleStatuses.Contains(model.CycleStatus))
        {
            return OperationResult<int>.Failure("Invalid appraisal cycle status.");
        }

        if (model.EndDate.Date < model.StartDate.Date)
        {
            return OperationResult<int>.Failure("Cycle end date cannot be earlier than start date.");
        }

        model.CycleStatus = ValidCycleStatuses.First(s => string.Equals(s, model.CycleStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveCycleAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.CycleId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Performance", "AppraisalCycle", id.ToString(), $"Saved appraisal cycle '{model.CycleName}' ({model.CycleStatus}).");
            return OperationResult<int>.Success(id, "Appraisal cycle saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteCycleAsync(int companyId, int cycleId, int userId, string username)
    {
        try
        {
            await _repository.DeleteCycleAsync(companyId, cycleId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Performance", "AppraisalCycle", cycleId.ToString(), $"Deleted appraisal cycle #{cycleId}.");
            return OperationResult.Success("Appraisal cycle deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<PerformanceGoalsPageViewModel> GetGoalsPageAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : currentEmployeeId;
        var cycles = (await _repository.GetCyclesAsync(companyId)).ToList();
        var goals = (await _repository.GetGoalsAsync(companyId, cycleId, effectiveEmpFilter, status)).ToList();
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();

        return new PerformanceGoalsPageViewModel
        {
            CycleId = cycleId,
            EmployeeId = effectiveEmpFilter,
            Status = status,
            Goals = goals,
            Cycles = cycles,
            Employees = canManage ? employees : employees.Where(e => e.EmployeeId == currentEmployeeId).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveGoalAsync(int companyId, PerformanceGoalFormViewModel model, int userId, string username)
    {
        if (!ValidGoalStatuses.Contains(model.GoalStatus))
        {
            return OperationResult<int>.Failure("Invalid goal status.");
        }

        model.GoalStatus = ValidGoalStatuses.First(s => string.Equals(s, model.GoalStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveGoalAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.GoalId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Performance", "PerformanceGoal", id.ToString(), $"Saved KPI goal '{model.GoalTitle}' ({model.WeightPercent}%).");
            return OperationResult<int>.Success(id, "Performance goal saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateGoalProgressAsync(int companyId, GoalProgressUpdateViewModel model, int userId, string username)
    {
        if (!ValidGoalStatuses.Contains(model.GoalStatus))
        {
            return OperationResult.Failure("Invalid goal status.");
        }

        model.GoalStatus = ValidGoalStatuses.First(s => string.Equals(s, model.GoalStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _repository.UpdateGoalProgressAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Performance", "PerformanceGoal", model.GoalId.ToString(),
                $"Updated goal #{model.GoalId} progress to {model.ProgressPercent}% ({model.GoalStatus}).");
            return OperationResult.Success("Goal progress updated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteGoalAsync(int companyId, int goalId, int userId, string username)
    {
        await _repository.DeleteGoalAsync(companyId, goalId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Performance", "PerformanceGoal", goalId.ToString(), $"Deleted goal #{goalId}.");
        return OperationResult.Success("Goal deleted.");
    }

    public async Task<PerformanceIndexPageViewModel> GetAppraisalsPageAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : currentEmployeeId;
        var cycles = (await _repository.GetCyclesAsync(companyId)).ToList();
        var (appraisals, kpi) = await _repository.GetAppraisalsAsync(companyId, cycleId, effectiveEmpFilter, status);
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();

        return new PerformanceIndexPageViewModel
        {
            CycleId = cycleId,
            EmployeeId = effectiveEmpFilter,
            Status = status,
            Kpi = kpi,
            Appraisals = appraisals.ToList(),
            Cycles = cycles,
            Employees = canManage ? employees : employees.Where(e => e.EmployeeId == currentEmployeeId).ToList()
        };
    }

    public async Task<OperationResult<int>> InitializeCycleAppraisalsAsync(int companyId, int cycleId, int userId, string username)
    {
        try
        {
            var created = await _repository.InitializeCycleAppraisalsAsync(companyId, cycleId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Performance", "AppraisalBatch", cycleId.ToString(),
                $"Initialized {created} appraisal records for cycle #{cycleId}.");
            return OperationResult<int>.Success(created, created > 0
                ? $"Created {created} new employee appraisal record(s) for the selected cycle."
                : "All active employees already have appraisal records for this cycle.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> SubmitSelfAppraisalAsync(int companyId, SelfAppraisalSubmitViewModel model, int userId, string username)
    {
        try
        {
            await _repository.SubmitSelfAppraisalAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Performance", "PerformanceAppraisal", model.AppraisalId.ToString(),
                $"Submitted self-appraisal rating {model.SelfRating:0.00} for appraisal #{model.AppraisalId}.");
            return OperationResult.Success("Self-appraisal submitted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> SubmitManagerReviewAsync(int companyId, ManagerAppraisalReviewViewModel model, int userId, string username)
    {
        try
        {
            await _repository.SubmitManagerReviewAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Performance", "PerformanceAppraisal", model.AppraisalId.ToString(),
                $"Recorded manager evaluation (Final: {model.FinalRating:0.00}, Finalized: {model.Finalize}).");
            return OperationResult.Success(model.Finalize ? "Appraisal completed and finalized." : "Manager review saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<TrainingIndexPageViewModel> GetTrainingProgramsPageAsync(
        int companyId, string? category = null, string? status = null, string? searchTerm = null)
    {
        var (programs, kpi) = await _repository.GetTrainingProgramsAsync(companyId, category, status, searchTerm);
        return new TrainingIndexPageViewModel
        {
            Category = category,
            Status = status,
            SearchTerm = searchTerm,
            Kpi = kpi,
            Programs = programs.ToList()
        };
    }

    public async Task<OperationResult<int>> SaveTrainingProgramAsync(int companyId, TrainingProgramFormViewModel model, int userId, string username)
    {
        if (!ValidProgramStatuses.Contains(model.ProgramStatus))
        {
            return OperationResult<int>.Failure("Invalid training program status.");
        }

        if (model.EndDate.Date < model.StartDate.Date)
        {
            return OperationResult<int>.Failure("Program end date cannot be before start date.");
        }

        model.ProgramStatus = ValidProgramStatuses.First(s => string.Equals(s, model.ProgramStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveTrainingProgramAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.ProgramId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Training", "TrainingProgram", id.ToString(), $"Saved training program '{model.ProgramTitle}' ({model.ProgramCode}).");
            return OperationResult<int>.Success(id, "Training program saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteTrainingProgramAsync(int companyId, int programId, int userId, string username)
    {
        try
        {
            await _repository.DeleteTrainingProgramAsync(companyId, programId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Training", "TrainingProgram", programId.ToString(), $"Deleted training program #{programId}.");
            return OperationResult.Success("Training program deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<TrainingEnrollmentsPageViewModel> GetEnrollmentsPageAsync(
        int companyId, int? programId = null, int? employeeId = null, string? status = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : currentEmployeeId;
        var (programs, _) = await _repository.GetTrainingProgramsAsync(companyId);
        var enrollments = (await _repository.GetEnrollmentsAsync(companyId, programId, effectiveEmpFilter, status)).ToList();
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();

        return new TrainingEnrollmentsPageViewModel
        {
            ProgramId = programId,
            EmployeeId = effectiveEmpFilter,
            Status = status,
            Enrollments = enrollments,
            Programs = programs.ToList(),
            Employees = canManage ? employees : employees.Where(e => e.EmployeeId == currentEmployeeId).ToList()
        };
    }

    public async Task<OperationResult<int>> EnrollEmployeeAsync(int companyId, TrainingEnrollFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _repository.EnrollEmployeeAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Training", "TrainingEnrollment", id.ToString(),
                $"Enrolled employee #{model.EmployeeId} into training program #{model.ProgramId}.");
            return OperationResult<int>.Success(id, "Employee enrolled in training program.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateEnrollmentOutcomeAsync(int companyId, TrainingOutcomeViewModel model, int userId, string username)
    {
        if (!ValidAttendanceStatuses.Contains(model.AttendanceStatus))
        {
            return OperationResult.Failure("Invalid enrollment status.");
        }

        model.AttendanceStatus = ValidAttendanceStatuses.First(s => string.Equals(s, model.AttendanceStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _repository.UpdateEnrollmentOutcomeAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Training", "TrainingEnrollment", model.EnrollmentId.ToString(),
                $"Updated enrollment #{model.EnrollmentId} outcome to {model.AttendanceStatus}.");
            return OperationResult.Success("Training outcome & certificate record saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteEnrollmentAsync(int companyId, int enrollmentId, int userId, string username)
    {
        await _repository.DeleteEnrollmentAsync(companyId, enrollmentId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Training", "TrainingEnrollment", enrollmentId.ToString(), $"Removed training enrollment #{enrollmentId}.");
        return OperationResult.Success("Training enrollment removed.");
    }
}
