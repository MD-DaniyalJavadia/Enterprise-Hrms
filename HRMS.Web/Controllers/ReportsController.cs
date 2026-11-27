using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Reports;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("REPORTS.VIEW")]
public sealed class ReportsController : Controller
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetAnalyticsHubAsync(
            User.GetCompanyId(), branchId, departmentId, fromDate, toDate,
            User.GetUserId(), User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalyticsData(int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetAnalyticsHubAsync(
            User.GetCompanyId(), branchId, departmentId, fromDate, toDate,
            User.GetUserId(), User.HasPermission("REPORTS.EXPORT"));
        return Json(ApiResponse<ReportsHubPageViewModel>.Ok(model));
    }

    [HttpGet]
    public async Task<IActionResult> Workforce(int? branchId = null, int? departmentId = null, byte? employmentStatus = null, string? search = null)
    {
        var model = await _service.GetWorkforceReportAsync(
            User.GetCompanyId(), branchId, departmentId, employmentStatus, search, User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AttendanceLeave(int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetAttendanceLeaveReportAsync(
            User.GetCompanyId(), branchId, departmentId, fromDate, toDate, User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PayrollCost(int? payrollYear = null, int? payrollMonth = null, int? branchId = null, int? departmentId = null)
    {
        var model = await _service.GetPayrollCostReportAsync(
            User.GetCompanyId(), payrollYear, payrollMonth, branchId, departmentId, User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TalentRecruitment(int? departmentId = null)
    {
        var model = await _service.GetTalentRecruitmentReportAsync(
            User.GetCompanyId(), departmentId, User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FinancialCompliance(int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetFinancialComplianceReportAsync(
            User.GetCompanyId(), departmentId, fromDate, toDate, User.HasPermission("REPORTS.EXPORT"));
        return View(model);
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate([FromForm] SaveReportTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveReportTemplateAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var result = await _service.DeleteReportTemplateAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogPrintExport(string reportKey, string reportTitle, int recordCount, string? filterSummary = null)
    {
        if (string.IsNullOrWhiteSpace(reportKey) || string.IsNullOrWhiteSpace(reportTitle))
            return Json(ApiResponse<bool>.Fail("Report metadata is required."));

        await _service.LogExportAsync(
            User.GetCompanyId(), reportKey.Trim(), reportTitle.Trim(), "Print",
            filterSummary?.Trim(), Math.Max(0, recordCount), User.GetUserId(),
            User.Identity?.Name ?? "system", HttpContext.Connection.RemoteIpAddress?.ToString());

        return Json(ApiResponse<bool>.Ok(true));
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportWorkforce(int? branchId = null, int? departmentId = null, byte? employmentStatus = null, string? search = null)
    {
        var model = await _service.GetWorkforceReportAsync(
            User.GetCompanyId(), branchId, departmentId, employmentStatus, search, true);

        var bytes = CsvExportHelper.BuildCsv(model.Rows, new (string, Func<WorkforceReportRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Full Name", x => x.FullName),
            ("Gender", x => x.Gender),
            ("Work Email", x => x.WorkEmail),
            ("Branch", x => x.BranchName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Grade", x => x.JobGrade),
            ("Reporting Manager", x => x.ManagerName),
            ("Hire Date", x => x.HireDate.ToString("yyyy-MM-dd")),
            ("Exit Date", x => x.ExitDate?.ToString("yyyy-MM-dd")),
            ("Tenure (Years)", x => x.TenureYears),
            ("Status", x => x.EmploymentStatusLabel),
            ("Annual Base Salary", x => x.BaseSalary)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "Workforce", "Workforce Headcount & Compensation Report", "CSV",
            $"Branch={branchId?.ToString() ?? "All"}, Dept={departmentId?.ToString() ?? "All"}",
            model.Rows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"WorkforceHeadcount_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportAttendanceLeave(int? branchId = null, int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetAttendanceLeaveReportAsync(
            User.GetCompanyId(), branchId, departmentId, fromDate, toDate, true);

        var bytes = CsvExportHelper.BuildCsv(model.Rows, new (string, Func<AttendanceLeaveReportRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Branch", x => x.BranchName),
            ("Department", x => x.DepartmentName),
            ("Present Days", x => x.PresentDays),
            ("Late Days", x => x.LateDays),
            ("Absent Days", x => x.AbsentDays),
            ("On Leave Days", x => x.OnLeaveDays),
            ("Work Hours", x => x.TotalWorkHours),
            ("Overtime Hours", x => x.TotalOvertimeHours),
            ("Late Minutes", x => x.TotalLateMinutes),
            ("Approved Leave Days", x => x.ApprovedLeaveDays),
            ("Remaining Leave Balance", x => x.RemainingLeaveBalance)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "AttendanceLeave", "Attendance & Leave Utilization Report", "CSV",
            $"{model.FromDate:yyyy-MM-dd} to {model.ToDate:yyyy-MM-dd}",
            model.Rows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"AttendanceLeaveSummary_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportPayrollCost(int? payrollYear = null, int? payrollMonth = null, int? branchId = null, int? departmentId = null)
    {
        var model = await _service.GetPayrollCostReportAsync(
            User.GetCompanyId(), payrollYear, payrollMonth, branchId, departmentId, true);

        var bytes = CsvExportHelper.BuildCsv(model.Rows, new (string, Func<PayrollCostReportRowViewModel, object?>)[]
        {
            ("Period", x => x.PeriodLabel),
            ("Payroll Status", x => x.PayrollStatus),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Branch", x => x.BranchName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Paid Days", x => x.PaidDays),
            ("Basic Pay", x => x.BasicPay),
            ("Allowances", x => x.TotalAllowances),
            ("Overtime Pay", x => x.OvertimePay),
            ("Leave Encashment", x => x.LeaveEncashmentPay),
            ("Bonus", x => x.BonusAmount),
            ("Gross Pay", x => x.GrossPay),
            ("Income Tax", x => x.IncomeTax),
            ("Statutory Deductions", x => x.StatutoryDeductions),
            ("Other/Absence Deductions", x => x.OtherAndAbsenceDeductions),
            ("Total Deductions", x => x.TotalDeductions),
            ("Net Pay", x => x.NetPay),
            ("Employer Contributions", x => x.EmployerContributions),
            ("Cost To Company", x => x.CostToCompany),
            ("Payment Status", x => x.PaymentStatus)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "PayrollCost", "Payroll Cost-to-Company Register", "CSV",
            $"{model.PayrollYear}-{model.PayrollMonth:D2}",
            model.Rows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"PayrollCostRegister_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportRecruitmentFunnel(int? departmentId = null)
    {
        var model = await _service.GetTalentRecruitmentReportAsync(User.GetCompanyId(), departmentId, true);

        var bytes = CsvExportHelper.BuildCsv(model.FunnelRows, new (string, Func<RecruitmentFunnelRowViewModel, object?>)[]
        {
            ("Requisition Code", x => x.RequisitionCode),
            ("Job Title", x => x.JobTitle),
            ("Department", x => x.DepartmentName),
            ("Branch", x => x.BranchName),
            ("Openings", x => x.OpeningsCount),
            ("Status", x => x.RequisitionStatus),
            ("Target Hire Date", x => x.TargetHireDate?.ToString("yyyy-MM-dd")),
            ("Total Applicants", x => x.TotalApplicants),
            ("Interview Stage", x => x.InterviewStageCount),
            ("Offer Stage", x => x.OfferStageCount),
            ("Hired", x => x.HiredCount)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "TalentRecruitment", "Recruitment Pipeline Funnel Report", "CSV",
            $"Dept={departmentId?.ToString() ?? "All"}",
            model.FunnelRows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"RecruitmentFunnel_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportTalentPerformance(int? departmentId = null)
    {
        var model = await _service.GetTalentRecruitmentReportAsync(User.GetCompanyId(), departmentId, true);

        var bytes = CsvExportHelper.BuildCsv(model.TalentRows, new (string, Func<TalentPerformanceRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Appraisal Cycle", x => x.LatestCycleName),
            ("Appraisal Status", x => x.AppraisalStatus),
            ("Self Rating", x => x.SelfRating),
            ("Manager Rating", x => x.ManagerRating),
            ("Final Rating", x => x.FinalRating),
            ("Rating Band", x => x.RatingBand),
            ("Completed Trainings", x => x.CompletedTrainings),
            ("Total Enrolled", x => x.TotalEnrolledTrainings),
            ("Avg Training Score", x => x.AvgTrainingScore)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "TalentRecruitment", "Performance & Training Readiness Report", "CSV",
            $"Dept={departmentId?.ToString() ?? "All"}",
            model.TalentRows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"TalentPerformanceReadiness_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportFinancialLoans(int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetFinancialComplianceReportAsync(User.GetCompanyId(), departmentId, fromDate, toDate, true);

        var bytes = CsvExportHelper.BuildCsv(model.FinancialRows, new (string, Func<LoanExpenseSummaryRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Active Loans", x => x.ActiveLoansCount),
            ("Total Loan Principal", x => x.TotalLoanPrincipal),
            ("Remaining Loan Balance", x => x.RemainingLoanBalance),
            ("Expense Claims", x => x.ExpenseClaimCount),
            ("Total Claimed", x => x.TotalClaimedAmount),
            ("Approved / Reimbursed", x => x.ApprovedExpenseAmount)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "FinancialCompliance", "Loans & Expenses Financial Summary", "CSV",
            $"{model.FromDate:yyyy-MM-dd} to {model.ToDate:yyyy-MM-dd}",
            model.FinancialRows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"LoansExpensesSummary_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("REPORTS.EXPORT")]
    [HttpGet]
    public async Task<IActionResult> ExportComplianceLedger(int? departmentId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var model = await _service.GetFinancialComplianceReportAsync(User.GetCompanyId(), departmentId, fromDate, toDate, true);

        var bytes = CsvExportHelper.BuildCsv(model.ComplianceRows, new (string, Func<AssetExitComplianceRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Assigned Assets", x => x.AssignedAssetCount),
            ("Asset Value", x => x.AssignedAssetValue),
            ("Acknowledged Policies", x => x.AcknowledgedPoliciesCount),
            ("Mandatory Policies", x => x.MandatoryPoliciesCount),
            ("Separation #", x => x.SeparationNumber),
            ("Separation Status", x => x.SeparationStatus),
            ("Settlement Status", x => x.SettlementStatus),
            ("Settlement Net", x => x.SettlementNetAmount)
        });

        await _service.LogExportAsync(
            User.GetCompanyId(), "FinancialCompliance", "Asset Custody, Policy & Exit Compliance Report", "CSV",
            $"{model.FromDate:yyyy-MM-dd} to {model.ToDate:yyyy-MM-dd}",
            model.ComplianceRows.Count, User.GetUserId(), User.Identity?.Name ?? "system",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return File(bytes, "text/csv", $"ComplianceAuditLedger_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
