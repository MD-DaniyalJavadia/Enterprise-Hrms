using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Exit;

namespace HRMS.Business.Interfaces;

public interface IExitService
{
    Task<ExitIndexPageViewModel> GetExitIndexPageAsync(
        int companyId, int? departmentId = null, string? status = null, string? separationType = null,
        string? search = null, int? currentEmployeeId = null, bool canManage = false, bool isManager = false);

    Task<ExitDetailsPageViewModel?> GetSeparationDetailsAsync(
        int companyId, int separationId, int? currentEmployeeId, bool canManage, bool isManager);

    Task<OperationResult<int>> SubmitSeparationAsync(int companyId, SeparationSubmitFormViewModel model, int userId, string username);
    Task<OperationResult> ReviewSeparationAsync(
        int companyId, SeparationReviewFormViewModel model, int? currentEmployeeId, bool canManage, bool isManager, int userId, string username);
    Task<OperationResult> FinalizeExitAsync(int companyId, int separationId, int userId, string username);

    Task<List<ExitClearanceTemplate>> GetClearanceTemplatesAsync(int companyId);
    Task<OperationResult<int>> SaveClearanceTemplateAsync(int companyId, ClearanceTemplateFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteClearanceTemplateAsync(int companyId, int templateId, int userId, string username);
    Task<OperationResult> UpdateClearanceItemAsync(int companyId, ClearanceItemUpdateViewModel model, int userId, string username);

    Task<ExitInterviewsPageViewModel> GetExitInterviewsPageAsync(int companyId);
    Task<OperationResult<int>> SaveExitInterviewAsync(
        int companyId, ExitInterviewFormViewModel model, int? currentEmployeeId, bool canManage, int userId, string username);

    Task<OperationResult<int>> CalculateAndSaveSettlementAsync(int companyId, FinalSettlementFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateSettlementStatusAsync(int companyId, FinalSettlementStatusFormViewModel model, int userId, string username);
}
