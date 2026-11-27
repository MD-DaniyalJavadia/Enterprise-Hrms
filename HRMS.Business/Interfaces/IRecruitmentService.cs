using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Recruitment;

namespace HRMS.Business.Interfaces;

public interface IRecruitmentService
{
    Task<RequisitionsPageViewModel> GetRequisitionsPageAsync(int companyId, int? departmentId = null, string? status = null, string? searchTerm = null);
    Task<OperationResult<int>> SaveRequisitionAsync(int companyId, JobRequisitionFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateRequisitionStatusAsync(int companyId, int requisitionId, string status, int userId, string username);
    Task<OperationResult> DeleteRequisitionAsync(int companyId, int requisitionId, int userId, string username);

    Task<CandidatesPageViewModel> GetCandidatesPageAsync(int companyId, string? source = null, string? searchTerm = null);
    Task<OperationResult<int>> SaveCandidateAsync(int companyId, CandidateFormViewModel model, int userId, string username);
    Task<(string? ResumeFileName, string? ResumeStoredName)?> GetCandidateResumeAsync(int companyId, int candidateId);
    Task<OperationResult> DeleteCandidateAsync(int companyId, int candidateId, int userId, string username);

    Task<PipelinePageViewModel> GetPipelinePageAsync(int companyId, int? requisitionId = null, string? pipelineStage = null);
    Task<OperationResult<int>> SubmitApplicationAsync(int companyId, JobApplicationSubmitViewModel model, int userId, string username);
    Task<OperationResult> MoveApplicationStageAsync(int companyId, JobApplicationMoveStageViewModel model, int userId, string username);

    Task<InterviewsPageViewModel> GetInterviewsPageAsync(int companyId, string? status = null);
    Task<OperationResult<int>> ScheduleInterviewAsync(int companyId, InterviewScheduleFormViewModel model, int userId, string username);
    Task<OperationResult> SubmitInterviewFeedbackAsync(int companyId, InterviewFeedbackViewModel model, int userId, string username);

    Task<OffersPageViewModel> GetOffersPageAsync(int companyId, string? offerStatus = null);
    Task<OperationResult<int>> SaveOfferAsync(int companyId, JobOfferFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateOfferStatusAsync(int companyId, int offerId, string offerStatus, string? responseNotes, int userId, string username);
    Task<OperationResult<int>> ConvertOfferToEmployeeAsync(int companyId, int offerId, int userId, string username);
}
