using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Exit;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class ExitService : IExitService
{
    private static readonly HashSet<string> ValidSepTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Resignation", "Termination", "Retirement", "ContractEnd", "MutualSeparation" };
    private static readonly HashSet<string> ValidReviewActions = new(StringComparer.OrdinalIgnoreCase)
        { "ManagerApprove", "HrApprove", "Reject", "Withdraw" };
    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase)
        { "IT", "Assets", "Finance", "HR", "Manager", "AdminFacilities" };
    private static readonly HashSet<string> ValidClearanceStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Pending", "Cleared", "Waived", "Hold" };
    private static readonly HashSet<string> ValidSettlementStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Approved", "Paid" };

    private readonly IExitRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPortalRepository _portalRepository;
    private readonly IAuditLogService _auditLogService;

    public ExitService(
        IExitRepository repository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPortalRepository portalRepository,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _portalRepository = portalRepository;
        _auditLogService = auditLogService;
    }

    public async Task<ExitIndexPageViewModel> GetExitIndexPageAsync(
        int companyId, int? departmentId = null, string? status = null, string? separationType = null,
        string? search = null, int? currentEmployeeId = null, bool canManage = false, bool isManager = false)
    {
        int? empFilter = (!canManage && !isManager) ? (currentEmployeeId ?? -1) : null;
        int? mgrFilter = (!canManage && isManager) ? (currentEmployeeId ?? -1) : null;

        var (requests, kpi) = await _repository.GetSeparationRequestsAsync(
            companyId, empFilter, mgrFilter, departmentId, status, separationType, search);

        var departments = canManage ? (await _departmentRepository.GetAllAsync(companyId)).ToList() : new();
        var employees = canManage ? (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList() : new();

        return new ExitIndexPageViewModel
        {
            DepartmentId = departmentId,
            Status = status,
            SeparationType = separationType,
            Search = search,
            CanManage = canManage,
            IsManager = isManager,
            CurrentEmployeeId = currentEmployeeId,
            Kpi = kpi,
            Requests = requests,
            Departments = departments,
            Employees = employees
        };
    }

    public async Task<ExitDetailsPageViewModel?> GetSeparationDetailsAsync(
        int companyId, int separationId, int? currentEmployeeId, bool canManage, bool isManager)
    {
        var detail = await _repository.GetSeparationByIdAsync(companyId, separationId);
        if (detail is null) return null;

        var isOwner = currentEmployeeId.HasValue && detail.Request.EmployeeId == currentEmployeeId.Value;
        var isTeamMember = !isOwner && isManager && currentEmployeeId.HasValue &&
                           await _portalRepository.VerifyManagerTeamMemberAsync(companyId, currentEmployeeId.Value, detail.Request.EmployeeId);

        if (!canManage && !isOwner && !isTeamMember)
        {
            return null;
        }

        detail.CanManage = canManage;
        detail.CanManagerApprove = canManage || isTeamMember;
        detail.IsOwner = isOwner;
        return detail;
    }

    public async Task<OperationResult<int>> SubmitSeparationAsync(int companyId, SeparationSubmitFormViewModel model, int userId, string username)
    {
        if (!ValidSepTypes.Contains(model.SeparationType))
            return OperationResult<int>.Failure("Invalid separation type selected.");

        if (model.RequestedLastWorkingDay.Date < model.SubmissionDate.Date)
            return OperationResult<int>.Failure("Requested last working day cannot precede submission date.");

        model.SeparationType = ValidSepTypes.First(x => x.Equals(model.SeparationType, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SubmitSeparationAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Separation", "SeparationRequest", id.ToString(),
                $"Submitted {model.SeparationType} request #{id} for employee #{model.EmployeeId}.");
            return OperationResult<int>.Success(id, "Separation request submitted and clearance checklist initialized.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewSeparationAsync(
        int companyId, SeparationReviewFormViewModel model, int? currentEmployeeId, bool canManage, bool isManager, int userId, string username)
    {
        if (!ValidReviewActions.Contains(model.Action))
            return OperationResult.Failure("Invalid review action.");

        model.Action = ValidReviewActions.First(x => x.Equals(model.Action, StringComparison.OrdinalIgnoreCase));

        var detail = await _repository.GetSeparationByIdAsync(companyId, model.SeparationId);
        if (detail is null)
            return OperationResult.Failure("Separation request not found.");

        var isOwner = currentEmployeeId.HasValue && detail.Request.EmployeeId == currentEmployeeId.Value;

        if (model.Action == "Withdraw")
        {
            if (!canManage && !isOwner)
                return OperationResult.Failure("You do not have permission to withdraw this request.");
        }
        else if (model.Action == "HrApprove")
        {
            if (!canManage)
                return OperationResult.Failure("HR separation management permission is required for HR approval.");
        }
        else // ManagerApprove or Reject
        {
            var isTeamMember = isManager && currentEmployeeId.HasValue &&
                               await _portalRepository.VerifyManagerTeamMemberAsync(companyId, currentEmployeeId.Value, detail.Request.EmployeeId);
            if (!canManage && !isTeamMember)
                return OperationResult.Failure("You do not have authority to review this separation request.");
        }

        try
        {
            await _repository.ReviewSeparationAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Separation", "SeparationRequest", model.SeparationId.ToString(),
                $"Processed separation action '{model.Action}' on request #{model.SeparationId}.");
            return OperationResult.Success($"Separation request updated ({model.Action}).");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> FinalizeExitAsync(int companyId, int separationId, int userId, string username)
    {
        try
        {
            await _repository.FinalizeExitAsync(companyId, separationId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Separation", "SeparationRequest", separationId.ToString(),
                $"Finalized offboarding and deactivated employee/portal access for separation #{separationId}.");
            return OperationResult.Success("Employee exit finalized. Employment record and portal access updated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<List<ExitClearanceTemplate>> GetClearanceTemplatesAsync(int companyId)
        => (await _repository.GetClearanceTemplatesAsync(companyId)).ToList();

    public async Task<OperationResult<int>> SaveClearanceTemplateAsync(int companyId, ClearanceTemplateFormViewModel model, int userId, string username)
    {
        if (!ValidScopes.Contains(model.DepartmentScope))
            return OperationResult<int>.Failure("Invalid department scope.");

        model.DepartmentScope = ValidScopes.First(x => x.Equals(model.DepartmentScope, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveClearanceTemplateAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.TemplateId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Separation", "ClearanceTemplate", id.ToString(), $"Saved clearance template '{model.ItemName}'.");
            return OperationResult<int>.Success(id, "Clearance template saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteClearanceTemplateAsync(int companyId, int templateId, int userId, string username)
    {
        await _repository.DeleteClearanceTemplateAsync(companyId, templateId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Separation", "ClearanceTemplate", templateId.ToString(), $"Deleted clearance template #{templateId}.");
        return OperationResult.Success("Clearance template deleted.");
    }

    public async Task<OperationResult> UpdateClearanceItemAsync(int companyId, ClearanceItemUpdateViewModel model, int userId, string username)
    {
        if (!ValidClearanceStatuses.Contains(model.ClearanceStatus))
            return OperationResult.Failure("Invalid clearance status.");

        model.ClearanceStatus = ValidClearanceStatuses.First(x => x.Equals(model.ClearanceStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _repository.UpdateClearanceItemAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Separation", "ClearanceItem", model.ClearanceItemId.ToString(),
                $"Updated clearance item #{model.ClearanceItemId} to {model.ClearanceStatus}.");
            return OperationResult.Success("Clearance item updated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<ExitInterviewsPageViewModel> GetExitInterviewsPageAsync(int companyId)
    {
        var interviews = (await _repository.GetExitInterviewsAsync(companyId)).ToList();
        var (allReqs, _) = await _repository.GetSeparationRequestsAsync(companyId);
        var pendingInterview = allReqs.Where(r => !r.HasExitInterview && r.SeparationStatus is not ("Rejected" or "Withdrawn")).ToList();

        return new ExitInterviewsPageViewModel
        {
            Interviews = interviews,
            SeparationsWithoutInterview = pendingInterview
        };
    }

    public async Task<OperationResult<int>> SaveExitInterviewAsync(
        int companyId, ExitInterviewFormViewModel model, int? currentEmployeeId, bool canManage, int userId, string username)
    {
        var detail = await _repository.GetSeparationByIdAsync(companyId, model.SeparationId);
        if (detail is null)
            return OperationResult<int>.Failure("Separation request not found.");

        var isOwner = currentEmployeeId.HasValue && detail.Request.EmployeeId == currentEmployeeId.Value;
        if (!canManage && !isOwner)
            return OperationResult<int>.Failure("You do not have permission to record this exit interview.");

        try
        {
            var id = await _repository.SaveExitInterviewAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Separation", "ExitInterview", id.ToString(), $"Saved exit interview for separation #{model.SeparationId}.");
            return OperationResult<int>.Success(id, "Exit interview saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> CalculateAndSaveSettlementAsync(int companyId, FinalSettlementFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _repository.CalculateAndSaveSettlementAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Separation", "FinalSettlement", id.ToString(), $"Calculated Full & Final settlement for separation #{model.SeparationId}.");
            return OperationResult<int>.Success(id, "Full & Final settlement calculated and saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateSettlementStatusAsync(int companyId, FinalSettlementStatusFormViewModel model, int userId, string username)
    {
        if (!ValidSettlementStatuses.Contains(model.SettlementStatus))
            return OperationResult.Failure("Invalid settlement status.");

        model.SettlementStatus = ValidSettlementStatuses.First(x => x.Equals(model.SettlementStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _repository.UpdateSettlementStatusAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Separation", "FinalSettlement", model.SettlementId.ToString(),
                $"Updated final settlement #{model.SettlementId} status to {model.SettlementStatus}.");
            return OperationResult.Success($"Settlement marked as {model.SettlementStatus}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
