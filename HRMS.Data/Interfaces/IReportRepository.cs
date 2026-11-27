using HRMS.Models.ViewModels.Reports;

namespace HRMS.Data.Interfaces;

public interface IReportRepository
{
    Task<ReportsHubPageViewModel> GetAnalyticsHubAsync(
        int companyId, int? branchId, int? departmentId, DateTime fromDate, DateTime toDate, int userId);

    Task<List<WorkforceReportRowViewModel>> GetWorkforceHeadcountAsync(
        int companyId, int? branchId = null, int? departmentId = null, byte? employmentStatus = null, string? search = null);

    Task<List<AttendanceLeaveReportRowViewModel>> GetAttendanceLeaveSummaryAsync(
        int companyId, int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null);

    Task<List<PayrollCostReportRowViewModel>> GetPayrollCostReportAsync(
        int companyId, int? payrollYear = null, int? payrollMonth = null, int? branchId = null, int? departmentId = null);

    Task<(List<RecruitmentFunnelRowViewModel> Funnel, List<TalentPerformanceRowViewModel> Talent)> GetRecruitmentTalentReportAsync(
        int companyId, int? departmentId = null);

    Task<(List<LoanExpenseSummaryRowViewModel> Financial, List<AssetExitComplianceRowViewModel> Compliance)> GetFinancialComplianceReportAsync(
        int companyId, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null);

    Task<int> SaveReportTemplateAsync(int companyId, SaveReportTemplateFormViewModel model, int userId);
    Task DeleteReportTemplateAsync(int companyId, int templateId, int userId);
    Task LogReportExportAsync(
        int companyId, string reportKey, string reportTitle, string exportFormat,
        string? filterSummary, int recordCount, int exportedBy, string exportedByName, string? ipAddress);
}
