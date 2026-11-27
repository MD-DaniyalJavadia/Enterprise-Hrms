using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Reports;

namespace HRMS.Data.Repositories;

public sealed class ReportRepository : BaseRepository, IReportRepository
{
    public ReportRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<ReportsHubPageViewModel> GetAnalyticsHubAsync(
        int companyId, int? branchId, int? departmentId, DateTime fromDate, DateTime toDate, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@BranchId", branchId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@FromDate", fromDate.Date);
        p.Add("@ToDate", toDate.Date);
        p.Add("@UserId", userId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Reports_GetAnalyticsHub", p, commandType: CommandType.StoredProcedure);

        var kpi = await multi.ReadFirstOrDefaultAsync<ExecutiveAnalyticsKpiViewModel>() ?? new ExecutiveAnalyticsKpiViewModel();
        var deptStats = (await multi.ReadAsync<DepartmentWorkforceAnalyticsViewModel>()).ToList();
        var finTrends = (await multi.ReadAsync<MonthlyFinancialTrendViewModel>()).ToList();
        var attrition = (await multi.ReadAsync<AttritionBreakdownViewModel>()).ToList();
        var templates = (await multi.ReadAsync<SavedReportTemplate>()).ToList();
        var exports = (await multi.ReadAsync<ReportExportLog>()).ToList();

        return new ReportsHubPageViewModel
        {
            BranchId = branchId,
            DepartmentId = departmentId,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            Kpi = kpi,
            DepartmentStats = deptStats,
            FinancialTrends = finTrends,
            AttritionBreakdown = attrition,
            SavedTemplates = templates,
            RecentExports = exports
        };
    }

    public async Task<List<WorkforceReportRowViewModel>> GetWorkforceHeadcountAsync(
        int companyId, int? branchId = null, int? departmentId = null, byte? employmentStatus = null, string? search = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@BranchId", branchId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@EmploymentStatus", employmentStatus);
        p.Add("@Search", search?.Trim());
        return (await QuerySpAsync<WorkforceReportRowViewModel>("dbo.sp_Reports_GetWorkforceHeadcount", p)).ToList();
    }

    public async Task<List<AttendanceLeaveReportRowViewModel>> GetAttendanceLeaveSummaryAsync(
        int companyId, int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@BranchId", branchId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);
        return (await QuerySpAsync<AttendanceLeaveReportRowViewModel>("dbo.sp_Reports_GetAttendanceLeaveSummary", p)).ToList();
    }

    public async Task<List<PayrollCostReportRowViewModel>> GetPayrollCostReportAsync(
        int companyId, int? payrollYear = null, int? payrollMonth = null, int? branchId = null, int? departmentId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@PayrollYear", payrollYear);
        p.Add("@PayrollMonth", payrollMonth);
        p.Add("@BranchId", branchId);
        p.Add("@DepartmentId", departmentId);
        return (await QuerySpAsync<PayrollCostReportRowViewModel>("dbo.sp_Reports_GetPayrollCostReport", p)).ToList();
    }

    public async Task<(List<RecruitmentFunnelRowViewModel> Funnel, List<TalentPerformanceRowViewModel> Talent)> GetRecruitmentTalentReportAsync(
        int companyId, int? departmentId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DepartmentId", departmentId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Reports_GetRecruitmentTalentReport", p, commandType: CommandType.StoredProcedure);
        var funnel = (await multi.ReadAsync<RecruitmentFunnelRowViewModel>()).ToList();
        var talent = (await multi.ReadAsync<TalentPerformanceRowViewModel>()).ToList();
        return (funnel, talent);
    }

    public async Task<(List<LoanExpenseSummaryRowViewModel> Financial, List<AssetExitComplianceRowViewModel> Compliance)> GetFinancialComplianceReportAsync(
        int companyId, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DepartmentId", departmentId);
        p.Add("@FromDate", fromDate?.Date);
        p.Add("@ToDate", toDate?.Date);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Reports_GetFinancialComplianceReport", p, commandType: CommandType.StoredProcedure);
        var financial = (await multi.ReadAsync<LoanExpenseSummaryRowViewModel>()).ToList();
        var compliance = (await multi.ReadAsync<AssetExitComplianceRowViewModel>()).ToList();
        return (financial, compliance);
    }

    public async Task<int> SaveReportTemplateAsync(int companyId, SaveReportTemplateFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@TemplateId", model.TemplateId);
        p.Add("@CompanyId", companyId);
        p.Add("@TemplateName", model.TemplateName.Trim());
        p.Add("@ReportKey", model.ReportKey);
        p.Add("@BranchId", model.BranchId);
        p.Add("@DepartmentId", model.DepartmentId);
        p.Add("@DateRangeDays", model.DateRangeDays);
        p.Add("@StatusFilter", model.StatusFilter?.Trim());
        p.Add("@Notes", model.Notes?.Trim());
        p.Add("@IsShared", model.IsShared);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_ReportTemplate_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteReportTemplateAsync(int companyId, int templateId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@TemplateId", templateId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_ReportTemplate_Delete", p);
    }

    public Task LogReportExportAsync(
        int companyId, string reportKey, string reportTitle, string exportFormat,
        string? filterSummary, int recordCount, int exportedBy, string exportedByName, string? ipAddress)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@ReportKey", reportKey);
        p.Add("@ReportTitle", reportTitle);
        p.Add("@ExportFormat", exportFormat);
        p.Add("@FilterSummary", filterSummary);
        p.Add("@RecordCount", recordCount);
        p.Add("@ExportedBy", exportedBy);
        p.Add("@ExportedByName", exportedByName);
        p.Add("@IpAddress", ipAddress);
        return ExecuteSpAsync("dbo.sp_ReportExportLog_Insert", p);
    }
}
