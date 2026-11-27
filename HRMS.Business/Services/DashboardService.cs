using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.RequestModels;
using HRMS.Models.ViewModels.Dashboard;

namespace HRMS.Business.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardViewModel> GetDashboardSummaryAsync(
        DashboardFilterRequest filter,
        int userId,
        string companyName)
    {
        var effectiveDate = filter.AsOfDate?.Date ?? DateTime.Today;
        var summary = await _dashboardRepository.GetDashboardSummaryAsync(
            filter.CompanyId,
            filter.BranchId,
            effectiveDate,
            userId);

        summary.CompanyName = companyName;
        return summary;
    }

    public async Task<IEnumerable<GlobalSearchResultDto>> GlobalSearchAsync(int companyId, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
        {
            return Enumerable.Empty<GlobalSearchResultDto>();
        }

        return await _dashboardRepository.GlobalSearchAsync(companyId, searchTerm.Trim());
    }
}
