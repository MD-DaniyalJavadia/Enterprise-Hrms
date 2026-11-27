using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Exit;

namespace HRMS.Data.Interfaces;

public interface IExitRepository
{
    Task<(List<SeparationRequest> Requests, ExitKpiSummaryViewModel Kpi)> GetSeparationRequestsAsync(
        int companyId, int? employeeId = null, int? managerEmployeeId = null, int? departmentId = null,
        string? status = null, string? separationType = null, string? search = null);
    Task<ExitDetailsPageViewModel?> GetSeparationByIdAsync(int companyId, int separationId);
    Task<int> SubmitSeparationAsync(int companyId, SeparationSubmitFormViewModel model, int userId);
    Task ReviewSeparationAsync(int companyId, SeparationReviewFormViewModel model, int userId);
    Task FinalizeExitAsync(int companyId, int separationId, int userId);

    Task<IEnumerable<ExitClearanceTemplate>> GetClearanceTemplatesAsync(int companyId);
    Task<int> SaveClearanceTemplateAsync(int companyId, ClearanceTemplateFormViewModel model, int userId);
    Task DeleteClearanceTemplateAsync(int companyId, int templateId, int userId);
    Task UpdateClearanceItemAsync(int companyId, ClearanceItemUpdateViewModel model, int userId);

    Task<IEnumerable<ExitInterview>> GetExitInterviewsAsync(int companyId);
    Task<int> SaveExitInterviewAsync(int companyId, ExitInterviewFormViewModel model, int userId);

    Task<int> CalculateAndSaveSettlementAsync(int companyId, FinalSettlementFormViewModel model, int userId);
    Task UpdateSettlementStatusAsync(int companyId, FinalSettlementStatusFormViewModel model, int userId);
}
