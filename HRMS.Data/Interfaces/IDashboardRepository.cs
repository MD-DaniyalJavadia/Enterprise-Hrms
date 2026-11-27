using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Dashboard;

namespace HRMS.Data.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardViewModel> GetDashboardSummaryAsync(int companyId, int? branchId, DateTime asOfDate, int userId);
    Task<IEnumerable<GlobalSearchResultDto>> GlobalSearchAsync(int companyId, string searchTerm);
}
