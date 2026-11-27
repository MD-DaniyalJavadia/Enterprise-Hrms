using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Recruitment;

namespace HRMS.Data.Repositories;

public sealed class RecruitmentRepository : BaseRepository, IRecruitmentRepository
{
    public RecruitmentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(IEnumerable<JobRequisition> Requisitions, RecruitmentKpiSummaryViewModel Kpi)> GetRequisitionsAsync(
        int companyId, int? departmentId = null, string? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 30);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_JobRequisition_GetAll", p, commandType: CommandType.StoredProcedure);
        var list = (await multi.ReadAsync<JobRequisition>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<RecruitmentKpiSummaryViewModel>() ?? new RecruitmentKpiSummaryViewModel();
        return (list, kpi);
    }

    public async Task<int> SaveRequisitionAsync(int companyId, JobRequisitionFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@RequisitionId", model.RequisitionId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequisitionCode", string.IsNullOrWhiteSpace(model.RequisitionCode) ? null : model.RequisitionCode.Trim().ToUpperInvariant(), DbType.String, size: 30);
        p.Add("@JobTitle", model.JobTitle.Trim(), DbType.String, size: 150);
        p.Add("@BranchId", model.BranchId, DbType.Int32);
        p.Add("@DepartmentId", model.DepartmentId, DbType.Int32);
        p.Add("@DesignationId", model.DesignationId, DbType.Int32);
        p.Add("@JobPositionId", model.JobPositionId, DbType.Int32);
        p.Add("@HiringManagerId", model.HiringManagerId, DbType.Int32);
        p.Add("@EmploymentType", model.EmploymentType.Trim(), DbType.String, size: 30);
        p.Add("@WorkMode", model.WorkMode.Trim(), DbType.String, size: 30);
        p.Add("@OpeningsCount", model.OpeningsCount, DbType.Int32);
        p.Add("@MinSalary", model.MinSalary, DbType.Decimal);
        p.Add("@MaxSalary", model.MaxSalary, DbType.Decimal);
        p.Add("@ExperienceMinYears", model.ExperienceMinYears, DbType.Int32);
        p.Add("@ExperienceMaxYears", model.ExperienceMaxYears, DbType.Int32);
        p.Add("@TargetHireDate", model.TargetHireDate?.Date, DbType.Date);
        p.Add("@RequisitionStatus", model.RequisitionStatus.Trim(), DbType.String, size: 30);
        p.Add("@JobSummary", model.JobSummary, DbType.String, size: 1000);
        p.Add("@RequiredSkills", model.RequiredSkills, DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobRequisition_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task UpdateRequisitionStatusAsync(int companyId, int requisitionId, string status, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequisitionId", requisitionId, DbType.Int32);
        p.Add("@RequisitionStatus", status.Trim(), DbType.String, size: 30);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobRequisition_UpdateStatus", p);
    }

    public Task DeleteRequisitionAsync(int companyId, int requisitionId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequisitionId", requisitionId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobRequisition_Delete", p);
    }

    public Task<IEnumerable<Candidate>> GetCandidatesAsync(int companyId, string? source = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Source", string.IsNullOrWhiteSpace(source) ? null : source.Trim(), DbType.String, size: 50);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);
        return QuerySpAsync<Candidate>("sp_Candidate_GetAll", p);
    }

    public async Task<int> SaveCandidateAsync(int companyId, CandidateFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CandidateId", model.CandidateId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@FirstName", model.FirstName.Trim(), DbType.String, size: 80);
        p.Add("@LastName", model.LastName.Trim(), DbType.String, size: 80);
        p.Add("@Email", model.Email.Trim(), DbType.String, size: 120);
        p.Add("@Phone", model.Phone, DbType.String, size: 30);
        p.Add("@CurrentCompany", model.CurrentCompany, DbType.String, size: 150);
        p.Add("@CurrentJobTitle", model.CurrentJobTitle, DbType.String, size: 120);
        p.Add("@TotalExperienceYears", model.TotalExperienceYears, DbType.Decimal);
        p.Add("@ExpectedSalary", model.ExpectedSalary, DbType.Decimal);
        p.Add("@Skills", model.Skills, DbType.String, size: 500);
        p.Add("@Source", model.Source.Trim(), DbType.String, size: 50);
        p.Add("@LinkedInUrl", model.LinkedInUrl, DbType.String, size: 250);
        p.Add("@ResumeFileName", model.ResumeFileName, DbType.String, size: 255);
        p.Add("@ResumeStoredName", model.ResumeStoredName, DbType.String, size: 255);
        p.Add("@Notes", model.Notes, DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Candidate_Save", p);
        return p.Get<int>("@SavedId");
    }

    public async Task<(string? ResumeFileName, string? ResumeStoredName)?> GetCandidateResumeAsync(int companyId, int candidateId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CandidateId", candidateId, DbType.Int32);

        var row = await QueryFirstOrDefaultSpAsync<Candidate>("sp_Candidate_GetResume", p);
        return row is null ? null : (row.ResumeFileName, row.ResumeStoredName);
    }

    public Task DeleteCandidateAsync(int companyId, int candidateId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CandidateId", candidateId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Candidate_Delete", p);
    }

    public async Task<(IEnumerable<JobApplication> Applications, PipelineStageCountsViewModel StageCounts)> GetPipelineAsync(
        int companyId, int? requisitionId = null, string? pipelineStage = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequisitionId", requisitionId, DbType.Int32);
        p.Add("@PipelineStage", string.IsNullOrWhiteSpace(pipelineStage) ? null : pipelineStage.Trim(), DbType.String, size: 30);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_JobApplication_GetPipeline", p, commandType: CommandType.StoredProcedure);
        var apps = (await multi.ReadAsync<JobApplication>()).ToList();
        var counts = await multi.ReadFirstOrDefaultAsync<PipelineStageCountsViewModel>() ?? new PipelineStageCountsViewModel();
        return (apps, counts);
    }

    public async Task<int> SubmitApplicationAsync(int companyId, JobApplicationSubmitViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequisitionId", model.RequisitionId, DbType.Int32);
        p.Add("@CandidateId", model.CandidateId, DbType.Int32);
        p.Add("@MatchScore", model.MatchScore, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobApplication_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task MoveApplicationStageAsync(int companyId, JobApplicationMoveStageViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ApplicationId", model.ApplicationId, DbType.Int32);
        p.Add("@PipelineStage", model.PipelineStage.Trim(), DbType.String, size: 30);
        p.Add("@RejectionReason", model.RejectionReason, DbType.String, size: 300);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobApplication_MoveStage", p);
    }

    public Task<IEnumerable<InterviewSchedule>> GetInterviewsAsync(int companyId, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@InterviewStatus", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        return QuerySpAsync<InterviewSchedule>("sp_Interview_GetAll", p);
    }

    public async Task<int> ScheduleInterviewAsync(int companyId, InterviewScheduleFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@InterviewId", model.InterviewId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ApplicationId", model.ApplicationId, DbType.Int32);
        p.Add("@InterviewRound", model.InterviewRound.Trim(), DbType.String, size: 50);
        p.Add("@InterviewMode", model.InterviewMode.Trim(), DbType.String, size: 30);
        p.Add("@ScheduledStart", model.ScheduledStart, DbType.DateTime2);
        p.Add("@DurationMinutes", model.DurationMinutes, DbType.Int32);
        p.Add("@LocationOrLink", model.LocationOrLink, DbType.String, size: 250);
        p.Add("@InterviewerEmployeeId", model.InterviewerEmployeeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Interview_Schedule", p);
        return p.Get<int>("@SavedId");
    }

    public Task SubmitInterviewFeedbackAsync(int companyId, InterviewFeedbackViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@InterviewId", model.InterviewId, DbType.Int32);
        p.Add("@InterviewStatus", model.InterviewStatus.Trim(), DbType.String, size: 20);
        p.Add("@TechnicalScore", model.TechnicalScore, DbType.Int32);
        p.Add("@CommunicationScore", model.CommunicationScore, DbType.Int32);
        p.Add("@OverallRecommendation", model.OverallRecommendation, DbType.String, size: 30);
        p.Add("@FeedbackNotes", model.FeedbackNotes, DbType.String, size: 1000);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Interview_SubmitFeedback", p);
    }

    public Task<IEnumerable<JobOffer>> GetOffersAsync(int companyId, string? offerStatus = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@OfferStatus", string.IsNullOrWhiteSpace(offerStatus) ? null : offerStatus.Trim(), DbType.String, size: 20);
        return QuerySpAsync<JobOffer>("sp_JobOffer_GetAll", p);
    }

    public async Task<int> SaveOfferAsync(int companyId, JobOfferFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@OfferId", model.OfferId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ApplicationId", model.ApplicationId, DbType.Int32);
        p.Add("@OfferedSalaryAnnual", model.OfferedSalaryAnnual, DbType.Decimal);
        p.Add("@JoiningDate", model.JoiningDate.Date, DbType.Date);
        p.Add("@OfferExpiryDate", model.OfferExpiryDate.Date, DbType.Date);
        p.Add("@EmploymentType", model.EmploymentType.Trim(), DbType.String, size: 30);
        p.Add("@BenefitsSummary", model.BenefitsSummary, DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobOffer_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task UpdateOfferStatusAsync(int companyId, int offerId, string offerStatus, string? responseNotes, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@OfferId", offerId, DbType.Int32);
        p.Add("@OfferStatus", offerStatus.Trim(), DbType.String, size: 20);
        p.Add("@ResponseNotes", responseNotes, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobOffer_UpdateStatus", p);
    }

    public async Task<int> ConvertOfferToEmployeeAsync(int companyId, int offerId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@OfferId", offerId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewEmployeeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobOffer_ConvertToEmployee", p);
        return p.Get<int>("@NewEmployeeId");
    }
}
