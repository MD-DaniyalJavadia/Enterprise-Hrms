using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Exit;

namespace HRMS.Data.Repositories;

public sealed class ExitRepository : BaseRepository, IExitRepository
{
    public ExitRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(List<SeparationRequest> Requests, ExitKpiSummaryViewModel Kpi)> GetSeparationRequestsAsync(
        int companyId, int? employeeId = null, int? managerEmployeeId = null, int? departmentId = null,
        string? status = null, string? separationType = null, string? search = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@EmployeeId", employeeId);
        p.Add("@ManagerEmployeeId", managerEmployeeId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@Status", status);
        p.Add("@Type", separationType);
        p.Add("@Search", search?.Trim());

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_SeparationRequest_GetAll", p, commandType: CommandType.StoredProcedure);
        var requests = (await multi.ReadAsync<SeparationRequest>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<ExitKpiSummaryViewModel>() ?? new ExitKpiSummaryViewModel();
        return (requests, kpi);
    }

    public async Task<ExitDetailsPageViewModel?> GetSeparationByIdAsync(int companyId, int separationId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SeparationId", separationId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_SeparationRequest_GetById", p, commandType: CommandType.StoredProcedure);
        var req = await multi.ReadFirstOrDefaultAsync<SeparationRequest>();
        if (req is null) return null;

        var readiness = await multi.ReadFirstOrDefaultAsync<ExitReadinessSnapshotViewModel>() ?? new ExitReadinessSnapshotViewModel();
        var items = (await multi.ReadAsync<ExitClearanceItem>()).ToList();
        var interview = await multi.ReadFirstOrDefaultAsync<ExitInterview>();
        var settlement = await multi.ReadFirstOrDefaultAsync<FinalSettlement>();

        return new ExitDetailsPageViewModel
        {
            Request = req,
            Readiness = readiness,
            ClearanceItems = items,
            ExitInterview = interview,
            Settlement = settlement
        };
    }

    public async Task<int> SubmitSeparationAsync(int companyId, SeparationSubmitFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@EmployeeId", model.EmployeeId);
        p.Add("@SeparationType", model.SeparationType);
        p.Add("@ReasonCategory", model.ReasonCategory);
        p.Add("@SubmissionDate", model.SubmissionDate.Date);
        p.Add("@NoticePeriodDays", model.NoticePeriodDays);
        p.Add("@RequestedLastWorkingDay", model.RequestedLastWorkingDay.Date);
        p.Add("@IsNoticeWaived", model.IsNoticeWaived);
        p.Add("@EmployeeComments", model.EmployeeComments?.Trim());
        p.Add("@HrComments", model.HrComments?.Trim());
        p.Add("@InitiateAsHrApproved", model.InitiateAsHrApproved);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_SeparationRequest_Submit", p);
        return p.Get<int>("@SavedId");
    }

    public Task ReviewSeparationAsync(int companyId, SeparationReviewFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SeparationId", model.SeparationId);
        p.Add("@Action", model.Action);
        p.Add("@ApprovedLastWorkingDay", model.ApprovedLastWorkingDay?.Date);
        p.Add("@IsNoticeWaived", model.IsNoticeWaived);
        p.Add("@IsRehireEligible", model.IsRehireEligible);
        p.Add("@Comments", model.Comments?.Trim());
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_SeparationRequest_Review", p);
    }

    public Task FinalizeExitAsync(int companyId, int separationId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SeparationId", separationId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_SeparationRequest_FinalizeExit", p);
    }

    public Task<IEnumerable<ExitClearanceTemplate>> GetClearanceTemplatesAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        return QuerySpAsync<ExitClearanceTemplate>("dbo.sp_ExitClearance_GetTemplates", p);
    }

    public async Task<int> SaveClearanceTemplateAsync(int companyId, ClearanceTemplateFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@TemplateId", model.TemplateId);
        p.Add("@CompanyId", companyId);
        p.Add("@DepartmentScope", model.DepartmentScope);
        p.Add("@ItemName", model.ItemName.Trim());
        p.Add("@Description", model.Description?.Trim());
        p.Add("@IsMandatory", model.IsMandatory);
        p.Add("@DisplayOrder", model.DisplayOrder);
        p.Add("@IsActive", model.IsActive);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_ExitClearance_SaveTemplate", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteClearanceTemplateAsync(int companyId, int templateId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@TemplateId", templateId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_ExitClearance_DeleteTemplate", p);
    }

    public Task UpdateClearanceItemAsync(int companyId, ClearanceItemUpdateViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@ClearanceItemId", model.ClearanceItemId);
        p.Add("@ClearanceStatus", model.ClearanceStatus);
        p.Add("@RecoveryAmount", model.RecoveryAmount);
        p.Add("@Remarks", model.Remarks?.Trim());
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_ExitClearance_UpdateItem", p);
    }

    public Task<IEnumerable<ExitInterview>> GetExitInterviewsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        return QuerySpAsync<ExitInterview>("dbo.sp_ExitInterview_GetAll", p);
    }

    public async Task<int> SaveExitInterviewAsync(int companyId, ExitInterviewFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SeparationId", model.SeparationId);
        p.Add("@InterviewDate", model.InterviewDate.Date);
        p.Add("@OverallSatisfactionRating", model.OverallSatisfactionRating);
        p.Add("@ManagerSupportRating", model.ManagerSupportRating);
        p.Add("@WorkLifeBalanceRating", model.WorkLifeBalanceRating);
        p.Add("@CompensationRating", model.CompensationRating);
        p.Add("@WouldRecommendCompany", model.WouldRecommendCompany);
        p.Add("@WouldConsiderRejoining", model.WouldConsiderRejoining);
        p.Add("@PrimaryLeavingReason", model.PrimaryLeavingReason.Trim());
        p.Add("@LikedMost", model.LikedMost?.Trim());
        p.Add("@ImprovementSuggestions", model.ImprovementSuggestions?.Trim());
        p.Add("@InterviewerNotes", model.InterviewerNotes?.Trim());
        p.Add("@IsConfidential", model.IsConfidential);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_ExitInterview_Save", p);
        return p.Get<int>("@SavedId");
    }

    public async Task<int> CalculateAndSaveSettlementAsync(int companyId, FinalSettlementFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SeparationId", model.SeparationId);
        p.Add("@WorkedDaysInFinalMonth", model.WorkedDaysInFinalMonth);
        p.Add("@EncashableLeaveDays", model.EncashableLeaveDays);
        p.Add("@GratuityOrSeverance", model.GratuityOrSeverance);
        p.Add("@BonusOrOtherEarnings", model.BonusOrOtherEarnings);
        p.Add("@OutstandingLoanRecovery", model.OutstandingLoanRecovery);
        p.Add("@AssetOrNoticeRecovery", model.AssetOrNoticeRecovery);
        p.Add("@TaxAndOtherDeductions", model.TaxAndOtherDeductions);
        p.Add("@Notes", model.Notes?.Trim());
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_FinalSettlement_CalculateAndSave", p);
        return p.Get<int>("@SavedId");
    }

    public Task UpdateSettlementStatusAsync(int companyId, FinalSettlementStatusFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@SettlementId", model.SettlementId);
        p.Add("@SettlementStatus", model.SettlementStatus);
        p.Add("@PaymentReference", model.PaymentReference?.Trim());
        p.Add("@PaidDate", model.PaidDate?.Date);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_FinalSettlement_UpdateStatus", p);
    }
}
