using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Exit;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("EXIT.VIEW")]
public sealed class ExitController : Controller
{
    private readonly IExitService _service;

    public ExitController(IExitService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId = null, string? status = null, string? separationType = null, string? search = null)
    {
        var canManage = User.HasPermission("EXIT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var model = await _service.GetExitIndexPageAsync(
            User.GetCompanyId(), departmentId, status, separationType, search, User.GetEmployeeId(), canManage, isManager);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var canManage = User.HasPermission("EXIT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var detail = await _service.GetSeparationDetailsAsync(
            User.GetCompanyId(), id, User.GetEmployeeId(), canManage, isManager);

        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromForm] SeparationSubmitFormViewModel model)
    {
        var canManage = User.HasPermission("EXIT.MANAGE");
        if (!canManage)
        {
            var currentEmpId = User.GetEmployeeId();
            if (!currentEmpId.HasValue)
            {
                return Json(ApiResponse<int>.Fail("No employee profile is linked to your account."));
            }

            model.EmployeeId = currentEmpId.Value;
            model.SeparationType = "Resignation";
            model.IsNoticeWaived = false;
            model.InitiateAsHrApproved = false;
            model.HrComments = null;
        }
        else if (model.EmployeeId <= 0)
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Please select an active employee.");
        }

        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SubmitSeparationAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review([FromForm] SeparationReviewFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var canManage = User.HasPermission("EXIT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var result = await _service.ReviewSeparationAsync(
            User.GetCompanyId(), model, User.GetEmployeeId(), canManage, isManager, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeExit(int id)
    {
        var result = await _service.FinalizeExitAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClearanceItem([FromForm] ClearanceItemUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.UpdateClearanceItemAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateSettlement([FromForm] FinalSettlementFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.CalculateAndSaveSettlementAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettlementStatus([FromForm] FinalSettlementStatusFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.UpdateSettlementStatusAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Interviews()
    {
        var model = await _service.GetExitInterviewsPageAsync(User.GetCompanyId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExitInterview([FromForm] ExitInterviewFormViewModel model)
    {
        var canManage = User.HasPermission("EXIT.MANAGE");
        if (!canManage)
        {
            model.InterviewerNotes = null;
        }

        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveExitInterviewAsync(
            User.GetCompanyId(), model, User.GetEmployeeId(), canManage, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Templates()
    {
        var templates = await _service.GetClearanceTemplatesAsync(User.GetCompanyId());
        return View(templates);
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate([FromForm] ClearanceTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveClearanceTemplateAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("EXIT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var result = await _service.DeleteClearanceTemplateAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Export(int? departmentId = null, string? status = null, string? separationType = null, string? search = null)
    {
        var canManage = User.HasPermission("EXIT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var model = await _service.GetExitIndexPageAsync(
            User.GetCompanyId(), departmentId, status, separationType, search, User.GetEmployeeId(), canManage, isManager);

        var bytes = CsvExportHelper.BuildCsv(model.Requests, new (string, Func<SeparationRequest, object?>)[]
        {
            ("Separation #", x => x.SeparationNumber),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Type", x => x.SeparationType),
            ("Reason", x => x.ReasonCategory),
            ("Submitted", x => x.SubmissionDate.ToString("yyyy-MM-dd")),
            ("Notice Days", x => x.NoticePeriodDays),
            ("Requested LWD", x => x.RequestedLastWorkingDay.ToString("yyyy-MM-dd")),
            ("Approved LWD", x => x.ApprovedLastWorkingDay?.ToString("yyyy-MM-dd")),
            ("Shortfall Days", x => x.NoticeShortfallDays),
            ("Clearance", x => $"{x.ClearedItemsCount}/{x.TotalClearanceItemsCount}"),
            ("Active Assets", x => x.ActiveAssetsCount),
            ("Active Loans", x => x.ActiveLoansCount),
            ("Exit Interview", x => x.HasExitInterview ? "Yes" : "No"),
            ("Settlement Status", x => x.SettlementStatus ?? "Not Calculated"),
            ("Settlement Net", x => x.SettlementNetAmount),
            ("Rehire Eligible", x => x.IsRehireEligible ? "Yes" : "No"),
            ("Status", x => x.SeparationStatus)
        });

        return File(bytes, "text/csv", $"SeparationRequests_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
