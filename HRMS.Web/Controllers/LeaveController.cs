using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Leave;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("LEAVE.VIEW")]
public sealed class LeaveController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IWebHostEnvironment _env;

    public LeaveController(ILeaveService leaveService, IWebHostEnvironment env)
    {
        _leaveService = leaveService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId = null, int? departmentId = null, int? leaveTypeId = null, string? status = null)
    {
        var model = await _leaveService.GetApplicationsPageAsync(
            User.GetCompanyId(), employeeId, departmentId, leaveTypeId, status, User.GetEmployeeId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply([FromForm] LeaveApplyViewModel model, IFormFile? attachmentFile)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("LEAVE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
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

        var result = await _leaveService.SubmitApplicationAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review([FromForm] LeaveReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid review submission."));
        }

        var result = await _leaveService.ReviewApplicationAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var companyId = User.GetCompanyId();
        var existing = await _leaveService.GetApplicationByIdAsync(companyId, id);
        if (existing is null)
        {
            return Json(ApiResponse<bool>.Fail("Leave application not found."));
        }

        if (!User.HasPermission("LEAVE.MANAGE") && User.GetEmployeeId() != existing.EmployeeId)
        {
            return Forbid();
        }

        var result = await _leaveService.CancelApplicationAsync(companyId, id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var app = await _leaveService.GetApplicationByIdAsync(User.GetCompanyId(), id);
        if (app is null || string.IsNullOrWhiteSpace(app.AttachmentStoredName))
        {
            return NotFound();
        }

        var path = FileStorageHelper.ResolveLeaveAttachmentPath(app.AttachmentStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", app.AttachmentFileName ?? "attachment");
    }

    [HttpGet]
    public async Task<IActionResult> ExportApplications(int? employeeId = null, int? departmentId = null, int? leaveTypeId = null, string? status = null)
    {
        var model = await _leaveService.GetApplicationsPageAsync(
            User.GetCompanyId(), employeeId, departmentId, leaveTypeId, status);

        var bytes = CsvExportHelper.BuildCsv(model.Applications, new (string, Func<LeaveApplication, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Department", x => x.DepartmentName),
            ("Leave Type", x => x.LeaveName),
            ("Start Date", x => x.StartDate.ToString("yyyy-MM-dd")),
            ("End Date", x => x.EndDate.ToString("yyyy-MM-dd")),
            ("Days", x => x.TotalDays),
            ("Half Day", x => x.IsHalfDay ? (x.HalfDaySession ?? "Yes") : "No"),
            ("Status", x => x.ApprovalStatus),
            ("Approved By", x => x.ApprovedByName),
            ("Reason", x => x.Reason)
        });

        return File(bytes, "text/csv", $"LeaveApplications_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Balances(int? year = null, int? employeeId = null, int? departmentId = null)
    {
        var model = await _leaveService.GetBalancesPageAsync(User.GetCompanyId(), year ?? DateTime.Today.Year, employeeId, departmentId);
        return View(model);
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AllocateAnnual(int leaveYear, bool includeCarryForward = true)
    {
        var result = await _leaveService.AllocateAnnualBalancesAsync(
            User.GetCompanyId(), leaveYear, includeCarryForward, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustBalance([FromForm] LeaveBalanceAdjustViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.AdjustBalanceAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportBalances(int? year = null, int? employeeId = null, int? departmentId = null)
    {
        int targetYear = year ?? DateTime.Today.Year;
        var model = await _leaveService.GetBalancesPageAsync(User.GetCompanyId(), targetYear, employeeId, departmentId);

        var bytes = CsvExportHelper.BuildCsv(model.Balances, new (string, Func<LeaveBalance, object?>)[]
        {
            ("Year", x => x.LeaveYear),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Department", x => x.DepartmentName),
            ("Leave Type", x => x.LeaveName),
            ("Opening", x => x.OpeningBalance),
            ("Accrued", x => x.AccruedDays),
            ("Carry Forward", x => x.CarryForwardDays),
            ("Used", x => x.UsedDays),
            ("Pending", x => x.PendingDays),
            ("Encashed", x => x.EncashedDays),
            ("Available", x => x.AvailableDays)
        });

        return File(bytes, "text/csv", $"LeaveBalances_{targetYear}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Types()
    {
        var list = await _leaveService.GetLeaveTypesAsync(User.GetCompanyId());
        return View(list);
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLeaveType([FromForm] LeaveTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.SaveLeaveTypeAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLeaveType(int id)
    {
        var result = await _leaveService.DeleteLeaveTypeAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(int? year = null, int? month = null, int? departmentId = null)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        if (m < 1 || m > 12) m = DateTime.Today.Month;

        var model = await _leaveService.GetCalendarPageAsync(User.GetCompanyId(), y, m, departmentId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Encashment(string? status = null)
    {
        var model = await _leaveService.GetEncashmentPageAsync(User.GetCompanyId(), status, User.GetEmployeeId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEncashment([FromForm] LeaveEncashmentSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("LEAVE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _leaveService.SubmitEncashmentAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("LEAVE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewEncashment([FromForm] LeaveEncashmentReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid review submission."));
        }

        var result = await _leaveService.ReviewEncashmentAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }
}
