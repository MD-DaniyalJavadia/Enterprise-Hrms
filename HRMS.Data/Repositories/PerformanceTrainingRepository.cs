using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.PerformanceTraining;

namespace HRMS.Data.Repositories;

public sealed class PerformanceTrainingRepository : BaseRepository, IPerformanceTrainingRepository
{
    public PerformanceTrainingRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<AppraisalCycle>> GetCyclesAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<AppraisalCycle>("sp_AppraisalCycle_GetAll", p);
    }

    public async Task<int> SaveCycleAsync(int companyId, AppraisalCycleFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CycleId", model.CycleId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleCode", model.CycleCode.Trim().ToUpperInvariant(), DbType.String, size: 20);
        p.Add("@CycleName", model.CycleName.Trim(), DbType.String, size: 120);
        p.Add("@ReviewYear", model.ReviewYear, DbType.Int32);
        p.Add("@StartDate", model.StartDate.Date, DbType.Date);
        p.Add("@EndDate", model.EndDate.Date, DbType.Date);
        p.Add("@SelfReviewDeadline", model.SelfReviewDeadline?.Date, DbType.Date);
        p.Add("@ManagerReviewDeadline", model.ManagerReviewDeadline?.Date, DbType.Date);
        p.Add("@CycleStatus", model.CycleStatus.Trim(), DbType.String, size: 20);
        p.Add("@Description", model.Description, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_AppraisalCycle_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteCycleAsync(int companyId, int cycleId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleId", cycleId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_AppraisalCycle_Delete", p);
    }

    public Task<IEnumerable<PerformanceGoal>> GetGoalsAsync(int companyId, int? cycleId = null, int? employeeId = null, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleId", cycleId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        return QuerySpAsync<PerformanceGoal>("sp_PerformanceGoal_GetAll", p);
    }

    public async Task<int> SaveGoalAsync(int companyId, PerformanceGoalFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@GoalId", model.GoalId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleId", model.CycleId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@GoalTitle", model.GoalTitle.Trim(), DbType.String, size: 150);
        p.Add("@KpiMetric", model.KpiMetric, DbType.String, size: 200);
        p.Add("@WeightPercent", model.WeightPercent, DbType.Decimal);
        p.Add("@TargetValue", model.TargetValue, DbType.String, size: 100);
        p.Add("@AchievedValue", model.AchievedValue, DbType.String, size: 100);
        p.Add("@ProgressPercent", model.ProgressPercent, DbType.Int32);
        p.Add("@GoalStatus", model.GoalStatus.Trim(), DbType.String, size: 20);
        p.Add("@ManagerNotes", model.ManagerNotes, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_PerformanceGoal_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task UpdateGoalProgressAsync(int companyId, GoalProgressUpdateViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@GoalId", model.GoalId, DbType.Int32);
        p.Add("@AchievedValue", model.AchievedValue, DbType.String, size: 100);
        p.Add("@ProgressPercent", model.ProgressPercent, DbType.Int32);
        p.Add("@GoalStatus", model.GoalStatus.Trim(), DbType.String, size: 20);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PerformanceGoal_UpdateProgress", p);
    }

    public Task DeleteGoalAsync(int companyId, int goalId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@GoalId", goalId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PerformanceGoal_Delete", p);
    }

    public async Task<(IEnumerable<PerformanceAppraisal> Appraisals, PerformanceKpiSummaryViewModel Kpi)> GetAppraisalsAsync(
        int companyId, int? cycleId = null, int? employeeId = null, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleId", cycleId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 25);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_PerformanceAppraisal_GetAll", p, commandType: CommandType.StoredProcedure);
        var list = (await multi.ReadAsync<PerformanceAppraisal>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<PerformanceKpiSummaryViewModel>() ?? new PerformanceKpiSummaryViewModel();
        return (list, kpi);
    }

    public async Task<int> InitializeCycleAppraisalsAsync(int companyId, int cycleId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CycleId", cycleId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@CreatedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_PerformanceAppraisal_InitCycle", p);
        return p.Get<int>("@CreatedCount");
    }

    public Task SubmitSelfAppraisalAsync(int companyId, SelfAppraisalSubmitViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@AppraisalId", model.AppraisalId, DbType.Int32);
        p.Add("@SelfRating", model.SelfRating, DbType.Decimal);
        p.Add("@SelfComments", model.SelfComments, DbType.String, size: 1000);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PerformanceAppraisal_SubmitSelf", p);
    }

    public Task SubmitManagerReviewAsync(int companyId, ManagerAppraisalReviewViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@AppraisalId", model.AppraisalId, DbType.Int32);
        p.Add("@ManagerRating", model.ManagerRating, DbType.Decimal);
        p.Add("@FinalRating", model.FinalRating, DbType.Decimal);
        p.Add("@Strengths", model.Strengths, DbType.String, size: 600);
        p.Add("@ImprovementAreas", model.ImprovementAreas, DbType.String, size: 600);
        p.Add("@ManagerComments", model.ManagerComments, DbType.String, size: 1000);
        p.Add("@RecommendedIncrementPercent", model.RecommendedIncrementPercent, DbType.Decimal);
        p.Add("@Finalize", model.Finalize, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PerformanceAppraisal_SubmitManagerReview", p);
    }

    public async Task<(IEnumerable<TrainingProgram> Programs, TrainingKpiSummaryViewModel Kpi)> GetTrainingProgramsAsync(
        int companyId, string? category = null, string? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Category", string.IsNullOrWhiteSpace(category) ? null : category.Trim(), DbType.String, size: 50);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_TrainingProgram_GetAll", p, commandType: CommandType.StoredProcedure);
        var list = (await multi.ReadAsync<TrainingProgram>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<TrainingKpiSummaryViewModel>() ?? new TrainingKpiSummaryViewModel();
        return (list, kpi);
    }

    public async Task<int> SaveTrainingProgramAsync(int companyId, TrainingProgramFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@ProgramId", model.ProgramId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ProgramCode", model.ProgramCode.Trim().ToUpperInvariant(), DbType.String, size: 25);
        p.Add("@ProgramTitle", model.ProgramTitle.Trim(), DbType.String, size: 150);
        p.Add("@Category", model.Category.Trim(), DbType.String, size: 50);
        p.Add("@DeliveryMode", model.DeliveryMode.Trim(), DbType.String, size: 30);
        p.Add("@DurationHours", model.DurationHours, DbType.Decimal);
        p.Add("@TrainerOrProvider", model.TrainerOrProvider, DbType.String, size: 150);
        p.Add("@StartDate", model.StartDate.Date, DbType.Date);
        p.Add("@EndDate", model.EndDate.Date, DbType.Date);
        p.Add("@MaxParticipants", model.MaxParticipants, DbType.Int32);
        p.Add("@LocationOrUrl", model.LocationOrUrl, DbType.String, size: 250);
        p.Add("@IsMandatory", model.IsMandatory, DbType.Boolean);
        p.Add("@ProgramStatus", model.ProgramStatus.Trim(), DbType.String, size: 20);
        p.Add("@Description", model.Description, DbType.String, size: 600);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_TrainingProgram_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteTrainingProgramAsync(int companyId, int programId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ProgramId", programId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_TrainingProgram_Delete", p);
    }

    public Task<IEnumerable<TrainingEnrollment>> GetEnrollmentsAsync(int companyId, int? programId = null, int? employeeId = null, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ProgramId", programId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        return QuerySpAsync<TrainingEnrollment>("sp_TrainingEnrollment_GetAll", p);
    }

    public async Task<int> EnrollEmployeeAsync(int companyId, TrainingEnrollFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ProgramId", model.ProgramId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_TrainingEnrollment_Enroll", p);
        return p.Get<int>("@NewId");
    }

    public Task UpdateEnrollmentOutcomeAsync(int companyId, TrainingOutcomeViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EnrollmentId", model.EnrollmentId, DbType.Int32);
        p.Add("@AttendanceStatus", model.AttendanceStatus.Trim(), DbType.String, size: 20);
        p.Add("@AssessmentScore", model.AssessmentScore, DbType.Decimal);
        p.Add("@CertificateNumber", model.CertificateNumber, DbType.String, size: 50);
        p.Add("@CompletionDate", model.CompletionDate?.Date, DbType.Date);
        p.Add("@FeedbackRating", model.FeedbackRating, DbType.Int32);
        p.Add("@FeedbackNotes", model.FeedbackNotes, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_TrainingEnrollment_UpdateOutcome", p);
    }

    public Task DeleteEnrollmentAsync(int companyId, int enrollmentId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EnrollmentId", enrollmentId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_TrainingEnrollment_Delete", p);
    }
}
