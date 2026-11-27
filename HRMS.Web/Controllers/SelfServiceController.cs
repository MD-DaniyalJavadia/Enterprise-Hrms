using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Attendance;
using HRMS.Models.ViewModels.Leave;
using HRMS.Models.ViewModels.LoanExpense;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Models.ViewModels.Portal;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ESS.VIEW")]
public sealed class SelfServiceController : Controller
{
    private readonly IPortalService _portalService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILeaveService _leaveService;
    private readonly IPayrollService _payrollService;
    private readonly ILoanExpenseService _loanExpenseService;
    private readonly IPerformanceTrainingService _performanceService;
    private readonly IWebHostEnvironment _env;

    public SelfServiceController(
        IPortalService portalService,
        IAttendanceService attendanceService,
        ILeaveService leaveService,
        IPayrollService payrollService,
        ILoanExpenseService loanExpenseService,
        IPerformanceTrainingService performanceService,
        IWebHostEnvironment env)
    {
        _portalService = portalService;
        _attendanceService = attendanceService;
        _leaveService = leaveService;
        _payrollService = payrollService;
        _loanExpenseService = loanExpenseService;
        _performanceService = performanceService;
        _env = env;
    }

    private (int? EmployeeId, bool IsSuperAdminPreview) ResolveEssScope(int? previewEmployeeId)
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var claimEmpId = User.GetEmployeeId();
        if (isSuperAdmin)
        {
            return (previewEmployeeId ?? claimEmpId, !claimEmpId.HasValue || previewEmployeeId.HasValue);
        }
        return (claimEmpId, false);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? previewEmployeeId = null)
    {
        var (empId, isPreview) = ResolveEssScope(previewEmployeeId);
        var model = await _portalService.GetEssDashboardAsync(User.GetCompanyId(), empId, isPreview);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyProfile(int? previewEmployeeId = null)
    {
        var (empId, isPreview) = ResolveEssScope(previewEmployeeId);
        var model = await _portalService.GetEssMyProfileAsync(User.GetCompanyId(), empId, isPreview);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyAttendance(int? year = null, int? month = null, int? previewEmployeeId = null)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        if (m < 1 || m > 12) m = DateTime.Today.Month;

        var (empId, isPreview) = ResolveEssScope(previewEmployeeId);
        var model = await _portalService.GetEssAttendanceAsync(User.GetCompanyId(), empId, y, m, isPreview);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyPayslips(int? year = null, int? previewEmployeeId = null)
    {
        var (empId, isPreview) = ResolveEssScope(previewEmployeeId);
        var model = await _portalService.GetEssPayslipsAsync(User.GetCompanyId(), empId, year, isPreview);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ViewPayslip(int id)
    {
        var detail = await _payrollService.GetPayslipDetailAsync(User.GetCompanyId(), id);
        if (detail is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("SuperAdmin") && User.GetEmployeeId() != detail.Payslip.EmployeeId)
        {
            return Forbid();
        }

        return View("~/Views/Payroll/Payslip.cshtml", detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCheckIn([FromForm] QuickPunchViewModel model)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<bool>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _attendanceService.CheckInAsync(
            User.GetCompanyId(), model, ip, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCheckOut([FromForm] QuickPunchViewModel model)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<bool>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        var result = await _attendanceService.CheckOutAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyLeave([FromForm] LeaveApplyViewModel model, IFormFile? attachmentFile)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (attachmentFile is not null && attachmentFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveLeaveAttachmentAsync(attachmentFile, _env.ContentRootPath);
            if (!saved.Ok)
            {
                return Json(ApiResponse<bool>.Fail(saved.Error!));
            }
            model.AttachmentFileName = Path.GetFileName(attachmentFile.FileName);
            model.AttachmentStoredName = saved.StoredFileName;
        }

        var result = await _leaveService.SubmitApplicationAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelLeave(int id)
    {
        var companyId = User.GetCompanyId();
        var existing = await _leaveService.GetApplicationByIdAsync(companyId, id);
        if (existing is null)
        {
            return Json(ApiResponse<bool>.Fail("Leave application not found."));
        }

        if (!User.IsInRole("SuperAdmin") && User.GetEmployeeId() != existing.EmployeeId)
        {
            return Forbid();
        }

        var result = await _leaveService.CancelApplicationAsync(companyId, id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRegularization([FromForm] RegularizationSubmitViewModel model)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.SubmitRegularizationAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOvertime([FromForm] OvertimeSubmitViewModel model)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.SubmitOvertimeAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLoan([FromForm] EmployeeLoanFormViewModel model)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.SubmitLoanAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExpense([FromForm] ExpenseClaimFormViewModel model, IFormFile? receiptFile)
    {
        var (empId, _) = ResolveEssScope(model.EmployeeId > 0 ? model.EmployeeId : null);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        model.EmployeeId = empId.Value;
        ModelState.Remove(nameof(model.EmployeeId));
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (receiptFile is not null && receiptFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveExpenseReceiptAsync(receiptFile, _env.ContentRootPath);
            if (!saved.Ok)
            {
                return Json(ApiResponse<bool>.Fail(saved.Error!));
            }
            model.ReceiptFileName = Path.GetFileName(receiptFile.FileName);
            model.ReceiptStoredName = saved.StoredFileName;
        }

        var result = await _loanExpenseService.SubmitExpenseClaimAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGoalProgress([FromForm] GoalProgressUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var companyId = User.GetCompanyId();
        if (!User.IsInRole("SuperAdmin"))
        {
            var myEmpId = User.GetEmployeeId();
            var myGoals = await _performanceService.GetGoalsPageAsync(companyId, currentEmployeeId: myEmpId, canManage: false);
            if (!myGoals.Goals.Any(g => g.GoalId == model.GoalId))
            {
                return Forbid();
            }
        }

        var result = await _performanceService.UpdateGoalProgressAsync(
            companyId, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSelfAppraisal([FromForm] SelfAppraisalSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var companyId = User.GetCompanyId();
        if (!User.IsInRole("SuperAdmin"))
        {
            var myEmpId = User.GetEmployeeId();
            var myApps = await _performanceService.GetAppraisalsPageAsync(companyId, currentEmployeeId: myEmpId, canManage: false);
            if (!myApps.Appraisals.Any(a => a.AppraisalId == model.AppraisalId))
            {
                return Forbid();
            }
        }

        var result = await _performanceService.SubmitSelfAppraisalAsync(
            companyId, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProfileRequest([FromForm] ProfileUpdateRequestFormViewModel model, int? previewEmployeeId = null)
    {
        var (empId, _) = ResolveEssScope(previewEmployeeId);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
        }

        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _portalService.SubmitProfileUpdateRequestAsync(
            User.GetCompanyId(), empId.Value, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelProfileRequest(int id, int? previewEmployeeId = null)
    {
        var (empId, _) = ResolveEssScope(previewEmployeeId);
        if (!empId.HasValue)
        {
            return Json(ApiResponse<bool>.Fail("No employee profile is linked to your account."));
        }

        var result = await _portalService.CancelProfileUpdateRequestAsync(
            User.GetCompanyId(), id, empId.Value, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadLeaveAttachment(int id)
    {
        var app = await _leaveService.GetApplicationByIdAsync(User.GetCompanyId(), id);
        if (app is null || string.IsNullOrWhiteSpace(app.AttachmentStoredName))
        {
            return NotFound();
        }

        if (!User.IsInRole("SuperAdmin") && User.GetEmployeeId() != app.EmployeeId)
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveLeaveAttachmentPath(app.AttachmentStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", app.AttachmentFileName ?? "attachment");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExpenseReceipt(int id)
    {
        var claim = await _loanExpenseService.GetExpenseClaimByIdAsync(User.GetCompanyId(), id);
        if (claim is null || string.IsNullOrWhiteSpace(claim.ReceiptStoredName))
        {
            return NotFound();
        }

        if (!User.IsInRole("SuperAdmin") && User.GetEmployeeId() != claim.EmployeeId)
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveExpenseReceiptPath(claim.ReceiptStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", claim.ReceiptFileName ?? "receipt");
    }
}
