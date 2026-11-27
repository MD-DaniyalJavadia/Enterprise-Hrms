using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("PERFORMANCE.VIEW")]
public sealed class PerformanceController : Controller
{
    private readonly IPerformanceTrainingService _service;

    public PerformanceController(IPerformanceTrainingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? cycleId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("PERFORMANCE.MANAGE");
        var model = await _service.GetAppraisalsPageAsync(
            User.GetCompanyId(), cycleId, employeeId, status, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitializeCycle(int cycleId)
    {
        var result = await _service.InitializeCycleAppraisalsAsync(
            User.GetCompanyId(), cycleId, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
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
        if (!User.HasPermission("PERFORMANCE.MANAGE"))
        {
            var myPage = await _service.GetAppraisalsPageAsync(companyId, currentEmployeeId: User.GetEmployeeId(), canManage: false);
            if (!myPage.Appraisals.Any(a => a.AppraisalId == model.AppraisalId))
            {
                return Forbid();
            }
        }

        var result = await _service.SubmitSelfAppraisalAsync(
            companyId, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitManagerReview([FromForm] ManagerAppraisalReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SubmitManagerReviewAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportAppraisals(int? cycleId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("PERFORMANCE.MANAGE");
        var model = await _service.GetAppraisalsPageAsync(
            User.GetCompanyId(), cycleId, employeeId, status, User.GetEmployeeId(), canManage);

        var bytes = CsvExportHelper.BuildCsv(model.Appraisals, new (string, Func<PerformanceAppraisal, object?>)[]
        {
            ("Cycle", x => x.CycleCode),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Reviewer", x => x.ReviewerName),
            ("Self Rating", x => x.SelfRating),
            ("Manager Rating", x => x.ManagerRating),
            ("Final Rating", x => x.FinalRating),
            ("Rating Band", x => x.RatingBand),
            ("Increment %", x => x.RecommendedIncrementPercent),
            ("Status", x => x.AppraisalStatus)
        });

        return File(bytes, "text/csv", $"PerformanceAppraisals_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Goals(int? cycleId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("PERFORMANCE.MANAGE");
        var model = await _service.GetGoalsPageAsync(
            User.GetCompanyId(), cycleId, employeeId, status, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGoal([FromForm] PerformanceGoalFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveGoalAsync(
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
        if (!User.HasPermission("PERFORMANCE.MANAGE"))
        {
            var myGoals = await _service.GetGoalsPageAsync(companyId, currentEmployeeId: User.GetEmployeeId(), canManage: false);
            if (!myGoals.Goals.Any(g => g.GoalId == model.GoalId))
            {
                return Forbid();
            }
        }

        var result = await _service.UpdateGoalProgressAsync(
            companyId, model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGoal(int id)
    {
        var result = await _service.DeleteGoalAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Cycles()
    {
        var cycles = await _service.GetCyclesAsync(User.GetCompanyId());
        return View(cycles);
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCycle([FromForm] AppraisalCycleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveCycleAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("PERFORMANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCycle(int id)
    {
        var result = await _service.DeleteCycleAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }
}
