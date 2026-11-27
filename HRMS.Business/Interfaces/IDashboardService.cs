using HRMS.Models.DTOs;
using HRMS.Models.RequestModels;
using HRMS.Models.ViewModels.Dashboard;

namespace HRMS.Business.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardSummaryAsync(DashboardFilterRequest filter, int userId, string companyName);
    Task<IEnumerable<GlobalSearchResultDto>> GlobalSearchAsync(int companyId, string searchTerm);
}
