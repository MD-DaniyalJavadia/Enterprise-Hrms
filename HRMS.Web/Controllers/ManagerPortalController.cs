using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Models.ViewModels.Portal;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("MANAGER.VIEW")]
public sealed class ManagerPortalController : Controller
{
    private readonly IPortalService _portalService;
    private readonly ILeaveService _leaveService;
    private readonly ILoanExpenseService _loanExpenseService;
    private readonly IWebHostEnvironment _env;

    public ManagerPortalController(
        IPortalService portalService,
        ILeaveService leaveService,
        ILoanExpenseService loanExpenseService,
        IWebHostEnvironment env)
    {
        _portalService = portalService;
        _leaveService = leaveService;
        _loanExpenseService = loanExpenseService;
        _env = env;
    }

    private (int? ManagerEmployeeId, bool IsCompanyWide) ResolveManagerScope()
    {
        bool isCompanyWide = User.IsInRole("SuperAdmin") || User.HasPermission("ADMIN.VIEW");
        return (User.GetEmployeeId(), isCompanyWide);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var model = await _portalService.GetManagerDashboardAsync(User.GetCompanyId(), mgrEmpId, isCompanyWide);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Team(int? departmentId = null, string? search = null)
    {
        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var model = await _portalService.GetManagerTeamAsync(User.GetCompanyId(), mgrEmpId, isCompanyWide, departmentId, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportTeamCsv(int? departmentId = null, string? search = null)
    {
        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var model = await _portalService.GetManagerTeamAsync(User.GetCompanyId(), mgrEmpId, isCompanyWide, departmentId, search);

        var bytes = CsvExportHelper.BuildCsv(model.Members, new (string, Func<ManagerTeamMemberViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Full Name", x => x.FullName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Branch", x => x.BranchName),
            ("Reporting Level", x => x.ReportingLevel == 1 ? "Direct Report" : $"Level {x.ReportingLevel}"),
            ("Reporting Manager", x => x.ManagerName),
            ("Shift", x => x.ShiftName),
            ("Today Status", x => x.TodayStatus),
            ("Check In", x => x.CheckInTime?.ToString("HH:mm")),
            ("Check Out", x => x.CheckOutTime?.ToString("HH:mm")),
            ("Open Goals", x => x.OpenGoalsCount),
            ("Pending Leaves", x => x.PendingLeaveCount),
            ("Work Email", x => x.WorkEmail)
        });

        return File(bytes, "text/csv", $"ManagerTeamRoster_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Approvals(string? category = null)
    {
        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var model = await _portalService.GetManagerApprovalsAsync(User.GetCompanyId(), mgrEmpId, isCompanyWide, category);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessApproval([FromForm] ManagerApprovalActionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var result = await _portalService.ProcessManagerApprovalAsync(
            User.GetCompanyId(), mgrEmpId, isCompanyWide, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTeamAppraisalReview([FromForm] ManagerAppraisalReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        var result = await _portalService.SubmitTeamAppraisalReviewAsync(
            User.GetCompanyId(), mgrEmpId, isCompanyWide, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadTeamLeaveAttachment(int id)
    {
        var companyId = User.GetCompanyId();
        var app = await _leaveService.GetApplicationByIdAsync(companyId, id);
        if (app is null || string.IsNullOrWhiteSpace(app.AttachmentStoredName))
        {
            return NotFound();
        }

        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        if (!await _portalService.CanManagerAccessEmployeeAsync(companyId, mgrEmpId, isCompanyWide, app.EmployeeId))
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveLeaveAttachmentPath(app.AttachmentStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", app.AttachmentFileName ?? "attachment");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadTeamExpenseReceipt(int id)
    {
        var companyId = User.GetCompanyId();
        var claim = await _loanExpenseService.GetExpenseClaimByIdAsync(companyId, id);
        if (claim is null || string.IsNullOrWhiteSpace(claim.ReceiptStoredName))
        {
            return NotFound();
        }

        var (mgrEmpId, isCompanyWide) = ResolveManagerScope();
        if (!await _portalService.CanManagerAccessEmployeeAsync(companyId, mgrEmpId, isCompanyWide, claim.EmployeeId))
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveExpenseReceiptPath(claim.ReceiptStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", claim.ReceiptFileName ?? "receipt");
    }
}
