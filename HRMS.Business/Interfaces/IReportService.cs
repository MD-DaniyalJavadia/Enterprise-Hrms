using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Reports;

namespace HRMS.Business.Interfaces;

public interface IReportService
{
    Task<ReportsHubPageViewModel> GetAnalyticsHubAsync(
        int companyId, int? branchId, int? departmentId, DateTime? fromDate, DateTime? toDate, int userId, bool canExport);

    Task<WorkforceReportPageViewModel> GetWorkforceReportAsync(
        int companyId, int? branchId, int? departmentId, byte? employmentStatus, string? search, bool canExport);

    Task<AttendanceLeaveReportPageViewModel> GetAttendanceLeaveReportAsync(
        int companyId, int? branchId, int? departmentId, DateTime? fromDate, DateTime? toDate, bool canExport);

    Task<PayrollCostReportPageViewModel> GetPayrollCostReportAsync(
        int companyId, int? payrollYear, int? payrollMonth, int? branchId, int? departmentId, bool canExport);

    Task<TalentRecruitmentReportPageViewModel> GetTalentRecruitmentReportAsync(
        int companyId, int? departmentId, bool canExport);

    Task<FinancialComplianceReportPageViewModel> GetFinancialComplianceReportAsync(
        int companyId, int? departmentId, DateTime? fromDate, DateTime? toDate, bool canExport);

    Task<OperationResult<int>> SaveReportTemplateAsync(int companyId, SaveReportTemplateFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteReportTemplateAsync(int companyId, int templateId, int userId, string username);

    Task LogExportAsync(
        int companyId, string reportKey, string reportTitle, string exportFormat,
        string? filterSummary, int recordCount, int userId, string username, string? ipAddress);
}
