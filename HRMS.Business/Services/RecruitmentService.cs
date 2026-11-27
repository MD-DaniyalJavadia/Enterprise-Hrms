using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Recruitment;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class RecruitmentService : IRecruitmentService
{
    private static readonly HashSet<string> ValidReqStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "PendingApproval", "Open", "OnHold", "Closed", "Filled"
    };

    private static readonly HashSet<string> ValidStages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Applied", "Screening", "Interview", "Assessment", "Offer", "Rejected", "Withdrawn"
    };

    private static readonly HashSet<string> ValidOfferStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Sent", "Accepted", "Declined", "Expired"
    };

    private readonly IRecruitmentRepository _recruitmentRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;

    public RecruitmentService(
        IRecruitmentRepository recruitmentRepository,
        IBranchRepository branchRepository,
        IDepartmentRepository departmentRepository,
        IDesignationRepository designationRepository,
        IOrgStructureRepository orgStructureRepository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService)
    {
        _recruitmentRepository = recruitmentRepository;
        _branchRepository = branchRepository;
        _departmentRepository = departmentRepository;
        _designationRepository = designationRepository;
        _orgStructureRepository = orgStructureRepository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<RequisitionsPageViewModel> GetRequisitionsPageAsync(int companyId, int? departmentId = null, string? status = null, string? searchTerm = null)
    {
        var (reqs, kpi) = await _recruitmentRepository.GetRequisitionsAsync(companyId, departmentId, status, searchTerm);
        return new RequisitionsPageViewModel
        {
            DepartmentId = departmentId,
            Status = status,
            SearchTerm = searchTerm,
            Kpi = kpi,
            Requisitions = reqs.ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList(),
            Designations = (await _designationRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList(),
            Positions = (await _orgStructureRepository.GetJobPositionsAsync(companyId)).Where(p => p.IsActive).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveRequisitionAsync(int companyId, JobRequisitionFormViewModel model, int userId, string username)
    {
        if (!ValidReqStatuses.Contains(model.RequisitionStatus))
        {
            return OperationResult<int>.Failure("Invalid requisition status.");
        }

        if (model.MinSalary.HasValue && model.MaxSalary.HasValue && model.MaxSalary < model.MinSalary)
        {
            return OperationResult<int>.Failure("Maximum salary cannot be lower than minimum salary.");
        }

        try
        {
            var id = await _recruitmentRepository.SaveRequisitionAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.RequisitionId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Recruitment", "JobRequisition", id.ToString(),
                $"Saved job requisition '{model.JobTitle}' ({model.RequisitionStatus}).");
            return OperationResult<int>.Success(id, "Job requisition saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateRequisitionStatusAsync(int companyId, int requisitionId, string status, int userId, string username)
    {
        if (!ValidReqStatuses.Contains(status))
        {
            return OperationResult.Failure("Invalid requisition status.");
        }

        var normalized = ValidReqStatuses.First(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
        await _recruitmentRepository.UpdateRequisitionStatusAsync(companyId, requisitionId, normalized, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
            "Recruitment", "JobRequisition", requisitionId.ToString(),
            $"Updated requisition #{requisitionId} status to {normalized}.");
        return OperationResult.Success($"Requisition status updated to {normalized}.");
    }

    public async Task<OperationResult> DeleteRequisitionAsync(int companyId, int requisitionId, int userId, string username)
    {
        try
        {
            await _recruitmentRepository.DeleteRequisitionAsync(companyId, requisitionId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Recruitment", "JobRequisition", requisitionId.ToString(), $"Deleted requisition #{requisitionId}.");
            return OperationResult.Success("Job requisition deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<CandidatesPageViewModel> GetCandidatesPageAsync(int companyId, string? source = null, string? searchTerm = null)
    {
        return new CandidatesPageViewModel
        {
            Source = source,
            SearchTerm = searchTerm,
            Candidates = (await _recruitmentRepository.GetCandidatesAsync(companyId, source, searchTerm)).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveCandidateAsync(int companyId, CandidateFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _recruitmentRepository.SaveCandidateAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.CandidateId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Recruitment", "Candidate", id.ToString(),
                $"Saved candidate '{model.FirstName} {model.LastName}' ({model.Email}).");
            return OperationResult<int>.Success(id, "Candidate profile saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public Task<(string? ResumeFileName, string? ResumeStoredName)?> GetCandidateResumeAsync(int companyId, int candidateId)
        => _recruitmentRepository.GetCandidateResumeAsync(companyId, candidateId);

    public async Task<OperationResult> DeleteCandidateAsync(int companyId, int candidateId, int userId, string username)
    {
        try
        {
            await _recruitmentRepository.DeleteCandidateAsync(companyId, candidateId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Recruitment", "Candidate", candidateId.ToString(), $"Deleted candidate #{candidateId}.");
            return OperationResult.Success("Candidate removed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<PipelinePageViewModel> GetPipelinePageAsync(int companyId, int? requisitionId = null, string? pipelineStage = null)
    {
        var (apps, counts) = await _recruitmentRepository.GetPipelineAsync(companyId, requisitionId, pipelineStage);
        var (reqs, _) = await _recruitmentRepository.GetRequisitionsAsync(companyId, status: "Open");
        return new PipelinePageViewModel
        {
            RequisitionId = requisitionId,
            PipelineStage = pipelineStage,
            StageCounts = counts,
            Applications = apps.ToList(),
            OpenRequisitions = reqs.ToList(),
            Candidates = (await _recruitmentRepository.GetCandidatesAsync(companyId)).ToList()
        };
    }

    public async Task<OperationResult<int>> SubmitApplicationAsync(int companyId, JobApplicationSubmitViewModel model, int userId, string username)
    {
        try
        {
            var id = await _recruitmentRepository.SubmitApplicationAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Recruitment", "JobApplication", id.ToString(),
                $"Added candidate #{model.CandidateId} to requisition #{model.RequisitionId}.");
            return OperationResult<int>.Success(id, "Candidate added to hiring pipeline.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> MoveApplicationStageAsync(int companyId, JobApplicationMoveStageViewModel model, int userId, string username)
    {
        if (!ValidStages.Contains(model.PipelineStage))
        {
            return OperationResult.Failure("Invalid pipeline stage.");
        }

        model.PipelineStage = ValidStages.First(s => string.Equals(s, model.PipelineStage, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _recruitmentRepository.MoveApplicationStageAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Recruitment", "JobApplication", model.ApplicationId.ToString(),
                $"Moved application #{model.ApplicationId} to stage {model.PipelineStage}.");
            return OperationResult.Success($"Candidate moved to {model.PipelineStage}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<InterviewsPageViewModel> GetInterviewsPageAsync(int companyId, string? status = null)
    {
        var (apps, _) = await _recruitmentRepository.GetPipelineAsync(companyId);
        return new InterviewsPageViewModel
        {
            InterviewStatus = status,
            Interviews = (await _recruitmentRepository.GetInterviewsAsync(companyId, status)).ToList(),
            Applications = apps.Where(a => a.PipelineStage is not ("Hired" or "Rejected" or "Withdrawn")).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult<int>> ScheduleInterviewAsync(int companyId, InterviewScheduleFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _recruitmentRepository.ScheduleInterviewAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.InterviewId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Recruitment", "Interview", id.ToString(),
                $"Scheduled {model.InterviewRound} ({model.InterviewMode}) for application #{model.ApplicationId}.");
            return OperationResult<int>.Success(id, "Interview scheduled successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> SubmitInterviewFeedbackAsync(int companyId, InterviewFeedbackViewModel model, int userId, string username)
    {
        await _recruitmentRepository.SubmitInterviewFeedbackAsync(companyId, model, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
            "Recruitment", "Interview", model.InterviewId.ToString(),
            $"Recorded interview feedback ({model.InterviewStatus} / {model.OverallRecommendation}).");
        return OperationResult.Success("Interview evaluation saved.");
    }

    public async Task<OffersPageViewModel> GetOffersPageAsync(int companyId, string? offerStatus = null)
    {
        var (apps, _) = await _recruitmentRepository.GetPipelineAsync(companyId);
        return new OffersPageViewModel
        {
            OfferStatus = offerStatus,
            Offers = (await _recruitmentRepository.GetOffersAsync(companyId, offerStatus)).ToList(),
            EligibleApplications = apps.Where(a => a.PipelineStage is "Interview" or "Assessment" or "Offer").ToList()
        };
    }

    public async Task<OperationResult<int>> SaveOfferAsync(int companyId, JobOfferFormViewModel model, int userId, string username)
    {
        if (model.OfferExpiryDate.Date > model.JoiningDate.Date)
        {
            return OperationResult<int>.Failure("Offer expiry date cannot be after the proposed joining date.");
        }

        try
        {
            var id = await _recruitmentRepository.SaveOfferAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.OfferId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Recruitment", "JobOffer", id.ToString(),
                $"Saved job offer #{id} for application #{model.ApplicationId}.");
            return OperationResult<int>.Success(id, "Job offer saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateOfferStatusAsync(int companyId, int offerId, string offerStatus, string? responseNotes, int userId, string username)
    {
        if (!ValidOfferStatuses.Contains(offerStatus))
        {
            return OperationResult.Failure("Invalid offer status.");
        }

        var normalized = ValidOfferStatuses.First(s => string.Equals(s, offerStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _recruitmentRepository.UpdateOfferStatusAsync(companyId, offerId, normalized, responseNotes, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Recruitment", "JobOffer", offerId.ToString(),
                $"Updated job offer #{offerId} status to {normalized}.");
            return OperationResult.Success($"Job offer marked as {normalized}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> ConvertOfferToEmployeeAsync(int companyId, int offerId, int userId, string username)
    {
        try
        {
            var empId = await _recruitmentRepository.ConvertOfferToEmployeeAsync(companyId, offerId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Recruitment", "Onboarding", empId.ToString(),
                $"Onboarded accepted offer #{offerId} as Employee ID {empId}.");
            return OperationResult<int>.Success(empId, "Candidate onboarded into Employee Master successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }
}
