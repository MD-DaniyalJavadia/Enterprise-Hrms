using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Portal;

namespace HRMS.Data.Interfaces;

public interface IPortalRepository
{
    Task<EssDashboardViewModel> GetEssDashboardAsync(int companyId, int employeeId, DateTime asOfDate);
    Task<List<EssPayslipItemViewModel>> GetEssPayslipsAsync(int companyId, int employeeId, int? year = null);
    Task<EssAttendancePageViewModel> GetEssAttendanceAsync(int companyId, int employeeId, int year, int month);
    Task<int> SubmitProfileUpdateRequestAsync(int companyId, int employeeId, ProfileUpdateRequestFormViewModel model, int userId);
    Task CancelProfileUpdateRequestAsync(int companyId, int requestId, int employeeId, int userId);
    Task ReviewProfileUpdateRequestAsync(int companyId, ProfileUpdateReviewViewModel model, int userId);

    Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, DateTime asOfDate);
    Task<List<ManagerTeamMemberViewModel>> GetManagerTeamRosterAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, int? departmentId = null, string? searchTerm = null);
    Task<List<ManagerPendingApprovalItemViewModel>> GetManagerPendingApprovalsAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, string? category = null);
    Task<int?> GetManagerRequestOwnerAsync(int companyId, string category, int requestId);
    Task<bool> VerifyManagerTeamMemberAsync(int companyId, int managerEmployeeId, int targetEmployeeId);
}
