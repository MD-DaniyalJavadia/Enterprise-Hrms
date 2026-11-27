using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Dashboard;

namespace HRMS.Data.Repositories;

public sealed class DashboardRepository : BaseRepository, IDashboardRepository
{
    public DashboardRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<DashboardViewModel> GetDashboardSummaryAsync(int companyId, int? branchId, DateTime asOfDate, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@BranchId", branchId, DbType.Int32);
        parameters.Add("@AsOfDate", asOfDate.Date, DbType.Date);
        parameters.Add("@UserId", userId, DbType.Int32);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "sp_Dashboard_GetSummary",
            parameters,
            commandType: CommandType.StoredProcedure);

        var kpis = await multi.ReadFirstOrDefaultAsync<DashboardKpiViewModel>() ?? new DashboardKpiViewModel();
        var payroll = await multi.ReadFirstOrDefaultAsync<PayrollSummaryViewModel>() ?? new PayrollSummaryViewModel();
        var branches = (await multi.ReadAsync<BranchOptionViewModel>()).ToList();
        var departmentStats = (await multi.ReadAsync<DepartmentStatViewModel>()).ToList();
        var genderStats = (await multi.ReadAsync<GenderStatViewModel>()).ToList();
        var monthlyHiring = (await multi.ReadAsync<MonthlyHiringStatViewModel>()).ToList();
        var attendanceAnalytics = (await multi.ReadAsync<AttendanceAnalyticsViewModel>()).ToList();
        var birthdays = (await multi.ReadAsync<UpcomingEventViewModel>()).ToList();
        var anniversaries = (await multi.ReadAsync<UpcomingEventViewModel>()).ToList();
        var recentActivities = (await multi.ReadAsync<RecentActivityViewModel>()).ToList();
        var notifications = (await multi.ReadAsync<NotificationItemViewModel>()).ToList();

        return new DashboardViewModel
        {
            CompanyId = companyId,
            SelectedBranchId = branchId,
            AsOfDate = asOfDate.Date,
            Kpis = kpis,
            PayrollSummary = payroll,
            Branches = branches,
            DepartmentStats = departmentStats,
            GenderStats = genderStats,
            MonthlyHiringStats = monthlyHiring,
            AttendanceAnalytics = attendanceAnalytics,
            UpcomingBirthdays = birthdays,
            UpcomingAnniversaries = anniversaries,
            RecentActivities = recentActivities,
            Notifications = notifications
        };
    }

    public async Task<IEnumerable<GlobalSearchResultDto>> GlobalSearchAsync(int companyId, string searchTerm)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 100);

        return await QuerySpAsync<GlobalSearchResultDto>("sp_Dashboard_GlobalSearch", parameters);
    }
}
