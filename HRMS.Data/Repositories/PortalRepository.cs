using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Portal;

namespace HRMS.Data.Repositories;

public sealed class PortalRepository : BaseRepository, IPortalRepository
{
    public PortalRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<EssDashboardViewModel> GetEssDashboardAsync(int companyId, int employeeId, DateTime asOfDate)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@AsOfDate", asOfDate.Date, DbType.Date);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_ESS_GetDashboard", p, commandType: CommandType.StoredProcedure);

        var profile = await multi.ReadFirstOrDefaultAsync<EssEmployeeSummaryViewModel>();
        var todayAtt = await multi.ReadFirstOrDefaultAsync<EssTodayAttendanceViewModel>();
        var balances = (await multi.ReadAsync<LeaveBalance>()).ToList();
        var leaves = (await multi.ReadAsync<LeaveApplication>()).ToList();
        var payslips = (await multi.ReadAsync<EssPayslipItemViewModel>()).ToList();
        var loans = (await multi.ReadAsync<EmployeeLoan>()).ToList();
        var expenses = (await multi.ReadAsync<ExpenseClaim>()).ToList();
        var appraisals = (await multi.ReadAsync<PerformanceAppraisal>()).ToList();
        var goals = (await multi.ReadAsync<PerformanceGoal>()).ToList();
        var enrollments = (await multi.ReadAsync<TrainingEnrollment>()).ToList();
        var profileRequests = (await multi.ReadAsync<ProfileUpdateRequest>()).ToList();
        var holidays = (await multi.ReadAsync<Holiday>()).ToList();

        return new EssDashboardViewModel
        {
            HasEmployeeProfile = profile is not null,
            SelectedEmployeeId = profile?.EmployeeId,
            Profile = profile,
            TodayAttendance = todayAtt,
            LeaveBalances = balances,
            RecentLeaves = leaves,
            RecentPayslips = payslips,
            ActiveLoans = loans,
            RecentExpenses = expenses,
            Appraisals = appraisals,
            Goals = goals,
            Enrollments = enrollments,
            ProfileRequests = profileRequests,
            UpcomingHolidays = holidays
        };
    }

    public async Task<List<EssPayslipItemViewModel>> GetEssPayslipsAsync(int companyId, int employeeId, int? year = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);

        return (await QuerySpAsync<EssPayslipItemViewModel>("sp_ESS_GetMyPayslips", p)).ToList();
    }

    public async Task<EssAttendancePageViewModel> GetEssAttendanceAsync(int companyId, int employeeId, int year, int month)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);
        p.Add("@Month", month, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_ESS_GetMyAttendance", p, commandType: CommandType.StoredProcedure);

        return new EssAttendancePageViewModel
        {
            HasEmployeeProfile = true,
            EmployeeId = employeeId,
            Year = year,
            Month = month,
            Logs = (await multi.ReadAsync<AttendanceRecord>()).ToList(),
            Regularizations = (await multi.ReadAsync<AttendanceRegularization>()).ToList(),
            OvertimeRequests = (await multi.ReadAsync<OvertimeRequest>()).ToList()
        };
    }

    public async Task<int> SubmitProfileUpdateRequestAsync(int companyId, int employeeId, ProfileUpdateRequestFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@RequestType", model.RequestType.Trim(), DbType.String, size: 40);
        p.Add("@FieldSummary", model.FieldSummary.Trim(), DbType.String, size: 200);
        p.Add("@RequestedPhone", string.IsNullOrWhiteSpace(model.RequestedPhone) ? null : model.RequestedPhone.Trim(), DbType.String, size: 30);
        p.Add("@RequestedMobilePhone", string.IsNullOrWhiteSpace(model.RequestedMobilePhone) ? null : model.RequestedMobilePhone.Trim(), DbType.String, size: 30);
        p.Add("@RequestedPersonalEmail", string.IsNullOrWhiteSpace(model.RequestedPersonalEmail) ? null : model.RequestedPersonalEmail.Trim(), DbType.String, size: 120);
        p.Add("@RequestedAddress", string.IsNullOrWhiteSpace(model.RequestedAddress) ? null : model.RequestedAddress.Trim(), DbType.String, size: 300);
        p.Add("@RequestedCity", string.IsNullOrWhiteSpace(model.RequestedCity) ? null : model.RequestedCity.Trim(), DbType.String, size: 80);
        p.Add("@RequestedCountry", string.IsNullOrWhiteSpace(model.RequestedCountry) ? null : model.RequestedCountry.Trim(), DbType.String, size: 80);
        p.Add("@RequestedBankName", string.IsNullOrWhiteSpace(model.RequestedBankName) ? null : model.RequestedBankName.Trim(), DbType.String, size: 120);
        p.Add("@RequestedBankAccountTitle", string.IsNullOrWhiteSpace(model.RequestedBankAccountTitle) ? null : model.RequestedBankAccountTitle.Trim(), DbType.String, size: 120);
        p.Add("@RequestedBankAccountNumber", string.IsNullOrWhiteSpace(model.RequestedBankAccountNumber) ? null : model.RequestedBankAccountNumber.Trim(), DbType.String, size: 50);
        p.Add("@RequestedBankRoutingCode", string.IsNullOrWhiteSpace(model.RequestedBankRoutingCode) ? null : model.RequestedBankRoutingCode.Trim(), DbType.String, size: 40);
        p.Add("@RequestedEmergencyContactName", string.IsNullOrWhiteSpace(model.RequestedEmergencyContactName) ? null : model.RequestedEmergencyContactName.Trim(), DbType.String, size: 120);
        p.Add("@RequestedEmergencyRelationship", string.IsNullOrWhiteSpace(model.RequestedEmergencyRelationship) ? null : model.RequestedEmergencyRelationship.Trim(), DbType.String, size: 50);
        p.Add("@RequestedEmergencyPhone", string.IsNullOrWhiteSpace(model.RequestedEmergencyPhone) ? null : model.RequestedEmergencyPhone.Trim(), DbType.String, size: 30);
        p.Add("@EmployeeNotes", string.IsNullOrWhiteSpace(model.EmployeeNotes) ? null : model.EmployeeNotes.Trim(), DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_ProfileUpdateRequest_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task CancelProfileUpdateRequestAsync(int companyId, int requestId, int employeeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequestId", requestId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_ProfileUpdateRequest_Cancel", p);
    }

    public Task ReviewProfileUpdateRequestAsync(int companyId, ProfileUpdateReviewViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RequestId", model.RequestId, DbType.Int32);
        p.Add("@ApprovalStatus", model.ApprovalStatus.Trim(), DbType.String, size: 20);
        p.Add("@ReviewerComments", string.IsNullOrWhiteSpace(model.ReviewerComments) ? null : model.ReviewerComments.Trim(), DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_ProfileUpdateRequest_Review", p);
    }

    public async Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, DateTime asOfDate)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ManagerEmployeeId", managerEmployeeId, DbType.Int32);
        p.Add("@IsCompanyWide", isCompanyWide, DbType.Boolean);
        p.Add("@AsOfDate", asOfDate.Date, DbType.Date);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_Manager_GetDashboard", p, commandType: CommandType.StoredProcedure);

        return new ManagerDashboardViewModel
        {
            IsCompanyWideView = isCompanyWide,
            Kpis = await multi.ReadFirstOrDefaultAsync<ManagerKpiSummaryViewModel>() ?? new ManagerKpiSummaryViewModel(),
            TeamToday = (await multi.ReadAsync<ManagerTeamMemberViewModel>()).ToList(),
            PendingApprovals = (await multi.ReadAsync<ManagerPendingApprovalItemViewModel>()).ToList(),
            PendingAppraisals = (await multi.ReadAsync<PerformanceAppraisal>()).ToList(),
            DepartmentStats = (await multi.ReadAsync<ManagerDeptStatViewModel>()).ToList(),
            UpcomingLeaves = (await multi.ReadAsync<ManagerUpcomingLeaveViewModel>()).ToList()
        };
    }

    public async Task<List<ManagerTeamMemberViewModel>> GetManagerTeamRosterAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, int? departmentId = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ManagerEmployeeId", managerEmployeeId, DbType.Int32);
        p.Add("@IsCompanyWide", isCompanyWide, DbType.Boolean);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        return (await QuerySpAsync<ManagerTeamMemberViewModel>("sp_Manager_GetTeamRoster", p)).ToList();
    }

    public async Task<List<ManagerPendingApprovalItemViewModel>> GetManagerPendingApprovalsAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, string? category = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ManagerEmployeeId", managerEmployeeId, DbType.Int32);
        p.Add("@IsCompanyWide", isCompanyWide, DbType.Boolean);
        p.Add("@Category", string.IsNullOrWhiteSpace(category) ? null : category.Trim(), DbType.String, size: 30);

        return (await QuerySpAsync<ManagerPendingApprovalItemViewModel>("sp_Manager_GetPendingApprovals", p)).ToList();
    }

    public async Task<int?> GetManagerRequestOwnerAsync(int companyId, string category, int requestId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Category", category.Trim(), DbType.String, size: 30);
        p.Add("@RequestId", requestId, DbType.Int32);

        return await QueryFirstOrDefaultSpAsync<int?>("sp_Manager_GetRequestOwner", p);
    }

    public async Task<bool> VerifyManagerTeamMemberAsync(int companyId, int managerEmployeeId, int targetEmployeeId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ManagerEmployeeId", managerEmployeeId, DbType.Int32);
        p.Add("@TargetEmployeeId", targetEmployeeId, DbType.Int32);

        return await QueryFirstOrDefaultSpAsync<bool>("sp_Manager_VerifyTeamMember", p);
    }
}
