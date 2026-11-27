using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Models.ViewModels.Portal;

namespace HRMS.Business.Interfaces;

public interface IPortalService
{
    Task<EssDashboardViewModel> GetEssDashboardAsync(int companyId, int? employeeId, bool isSuperAdminPreview = false);
    Task<EssMyProfilePageViewModel> GetEssMyProfileAsync(int companyId, int? employeeId, bool isSuperAdminPreview = false);
    Task<EssAttendancePageViewModel> GetEssAttendanceAsync(int companyId, int? employeeId, int year, int month, bool isSuperAdminPreview = false);
    Task<EssPayslipsPageViewModel> GetEssPayslipsAsync(int companyId, int? employeeId, int? year = null, bool isSuperAdminPreview = false);

    Task<OperationResult<int>> SubmitProfileUpdateRequestAsync(int companyId, int employeeId, ProfileUpdateRequestFormViewModel model, int userId, string username);
    Task<OperationResult> CancelProfileUpdateRequestAsync(int companyId, int requestId, int employeeId, int userId, string username);

    Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int companyId, int? managerEmployeeId, bool isCompanyWide);
    Task<ManagerTeamPageViewModel> GetManagerTeamAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, int? departmentId = null, string? searchTerm = null);
    Task<ManagerApprovalsPageViewModel> GetManagerApprovalsAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, string? category = null);
    Task<bool> CanManagerAccessEmployeeAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, int targetEmployeeId);
    Task<int?> GetRequestOwnerEmployeeIdAsync(int companyId, string category, int requestId);
    Task<OperationResult> ProcessManagerApprovalAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, ManagerApprovalActionViewModel model, int userId, string username);
    Task<OperationResult> SubmitTeamAppraisalReviewAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, ManagerAppraisalReviewViewModel model, int userId, string username);
}
