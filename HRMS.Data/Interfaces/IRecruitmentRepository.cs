using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Recruitment;

namespace HRMS.Data.Interfaces;

public interface IRecruitmentRepository
{
    Task<(IEnumerable<JobRequisition> Requisitions, RecruitmentKpiSummaryViewModel Kpi)> GetRequisitionsAsync(
        int companyId, int? departmentId = null, string? status = null, string? searchTerm = null);
    Task<int> SaveRequisitionAsync(int companyId, JobRequisitionFormViewModel model, int userId);
    Task UpdateRequisitionStatusAsync(int companyId, int requisitionId, string status, int userId);
    Task DeleteRequisitionAsync(int companyId, int requisitionId, int userId);

    Task<IEnumerable<Candidate>> GetCandidatesAsync(int companyId, string? source = null, string? searchTerm = null);
    Task<int> SaveCandidateAsync(int companyId, CandidateFormViewModel model, int userId);
    Task<(string? ResumeFileName, string? ResumeStoredName)?> GetCandidateResumeAsync(int companyId, int candidateId);
    Task DeleteCandidateAsync(int companyId, int candidateId, int userId);

    Task<(IEnumerable<JobApplication> Applications, PipelineStageCountsViewModel StageCounts)> GetPipelineAsync(
        int companyId, int? requisitionId = null, string? pipelineStage = null);
    Task<int> SubmitApplicationAsync(int companyId, JobApplicationSubmitViewModel model, int userId);
    Task MoveApplicationStageAsync(int companyId, JobApplicationMoveStageViewModel model, int userId);

    Task<IEnumerable<InterviewSchedule>> GetInterviewsAsync(int companyId, string? status = null);
    Task<int> ScheduleInterviewAsync(int companyId, InterviewScheduleFormViewModel model, int userId);
    Task SubmitInterviewFeedbackAsync(int companyId, InterviewFeedbackViewModel model, int userId);

    Task<IEnumerable<JobOffer>> GetOffersAsync(int companyId, string? offerStatus = null);
    Task<int> SaveOfferAsync(int companyId, JobOfferFormViewModel model, int userId);
    Task UpdateOfferStatusAsync(int companyId, int offerId, string offerStatus, string? responseNotes, int userId);
    Task<int> ConvertOfferToEmployeeAsync(int companyId, int offerId, int userId);
}
