using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Reports;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class ReportService : IReportService
{
    private static readonly HashSet<string> ValidReportKeys = new(StringComparer.OrdinalIgnoreCase)
        { "Workforce", "AttendanceLeave", "PayrollCost", "TalentRecruitment", "FinancialCompliance" };

    private readonly IReportRepository _repository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditLogService _auditLogService;

    public ReportService(
        IReportRepository repository,
        IBranchRepository branchRepository,
        IDepartmentRepository departmentRepository,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _branchRepository = branchRepository;
        _departmentRepository = departmentRepository;
        _auditLogService = auditLogService;
    }

    public async Task<ReportsHubPageViewModel> GetAnalyticsHubAsync(
        int companyId, int? branchId, int? departmentId, DateTime? fromDate, DateTime? toDate, int userId, bool canExport)
    {
        var effTo = (toDate ?? DateTime.Today).Date;
        var effFrom = (fromDate ?? effTo.AddDays(-29)).Date;
        if (effFrom > effTo) (effFrom, effTo) = (effTo, effFrom);

        var hub = await _repository.GetAnalyticsHubAsync(companyId, branchId, departmentId, effFrom, effTo, userId);
        hub.CanExport = canExport;
        hub.Branches = (await _branchRepository.GetAllAsync(companyId)).ToList();
        hub.Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList();
        return hub;
    }

    public async Task<WorkforceReportPageViewModel> GetWorkforceReportAsync(
        int companyId, int? branchId, int? departmentId, byte? employmentStatus, string? search, bool canExport)
    {
        var rows = await _repository.GetWorkforceHeadcountAsync(companyId, branchId, departmentId, employmentStatus, search);
        return new WorkforceReportPageViewModel
        {
            BranchId = branchId,
            DepartmentId = departmentId,
            EmploymentStatus = employmentStatus,
            Search = search,
            CanExport = canExport,
            Rows = rows,
            Branches = (await _branchRepository.GetAllAsync(companyId)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList()
        };
    }

    public async Task<AttendanceLeaveReportPageViewModel> GetAttendanceLeaveReportAsync(
        int companyId, int? branchId, int? departmentId, DateTime? fromDate, DateTime? toDate, bool canExport)
    {
        var effTo = (toDate ?? DateTime.Today).Date;
        var effFrom = (fromDate ?? new DateTime(effTo.Year, effTo.Month, 1)).Date;
        if (effFrom > effTo) (effFrom, effTo) = (effTo, effFrom);

        var rows = await _repository.GetAttendanceLeaveSummaryAsync(companyId, branchId, departmentId, effFrom, effTo);
        return new AttendanceLeaveReportPageViewModel
        {
            BranchId = branchId,
            DepartmentId = departmentId,
            FromDate = effFrom,
            ToDate = effTo,
            CanExport = canExport,
            Rows = rows,
            Branches = (await _branchRepository.GetAllAsync(companyId)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList()
        };
    }

    public async Task<PayrollCostReportPageViewModel> GetPayrollCostReportAsync(
        int companyId, int? payrollYear, int? payrollMonth, int? branchId, int? departmentId, bool canExport)
    {
        var rows = await _repository.GetPayrollCostReportAsync(companyId, payrollYear, payrollMonth, branchId, departmentId);
        var effYear = payrollYear ?? rows.FirstOrDefault()?.PayrollYear ?? DateTime.Today.Year;
        var effMonth = payrollMonth ?? rows.FirstOrDefault()?.PayrollMonth ?? DateTime.Today.Month;

        return new PayrollCostReportPageViewModel
        {
            PayrollYear = effYear,
            PayrollMonth = effMonth,
            BranchId = branchId,
            DepartmentId = departmentId,
            CanExport = canExport,
            Rows = rows,
            Branches = (await _branchRepository.GetAllAsync(companyId)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList()
        };
    }

    public async Task<TalentRecruitmentReportPageViewModel> GetTalentRecruitmentReportAsync(
        int companyId, int? departmentId, bool canExport)
    {
        var (funnel, talent) = await _repository.GetRecruitmentTalentReportAsync(companyId, departmentId);
        return new TalentRecruitmentReportPageViewModel
        {
            DepartmentId = departmentId,
            CanExport = canExport,
            FunnelRows = funnel,
            TalentRows = talent,
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList()
        };
    }

    public async Task<FinancialComplianceReportPageViewModel> GetFinancialComplianceReportAsync(
        int companyId, int? departmentId, DateTime? fromDate, DateTime? toDate, bool canExport)
    {
        var effTo = (toDate ?? DateTime.Today).Date;
        var effFrom = (fromDate ?? effTo.AddDays(-89)).Date;
        if (effFrom > effTo) (effFrom, effTo) = (effTo, effFrom);

        var (financial, compliance) = await _repository.GetFinancialComplianceReportAsync(companyId, departmentId, effFrom, effTo);
        return new FinancialComplianceReportPageViewModel
        {
            DepartmentId = departmentId,
            FromDate = effFrom,
            ToDate = effTo,
            CanExport = canExport,
            FinancialRows = financial,
            ComplianceRows = compliance,
            Departments = (await _departmentRepository.GetAllAsync(companyId)).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveReportTemplateAsync(
        int companyId, SaveReportTemplateFormViewModel model, int userId, string username)
    {
        if (!ValidReportKeys.Contains(model.ReportKey))
            return OperationResult<int>.Failure("Invalid report module key selected.");

        model.ReportKey = ValidReportKeys.First(x => x.Equals(model.ReportKey, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveReportTemplateAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.TemplateId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Reports", "SavedReportTemplate", id.ToString(),
                $"Saved report preset '{model.TemplateName}' ({model.ReportKey}).");
            return OperationResult<int>.Success(id, "Report preset saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteReportTemplateAsync(int companyId, int templateId, int userId, string username)
    {
        await _repository.DeleteReportTemplateAsync(companyId, templateId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Reports", "SavedReportTemplate", templateId.ToString(),
            $"Deleted saved report preset #{templateId}.");
        return OperationResult.Success("Saved report preset deleted.");
    }

    public async Task LogExportAsync(
        int companyId, string reportKey, string reportTitle, string exportFormat,
        string? filterSummary, int recordCount, int userId, string username, string? ipAddress)
    {
        await _repository.LogReportExportAsync(
            companyId, reportKey, reportTitle, exportFormat, filterSummary, recordCount, userId, username, ipAddress);

        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Export.ToString(),
            "Reports", reportKey, reportKey,
            $"Exported '{reportTitle}' ({recordCount} rows, format: {exportFormat}).");
    }
}
