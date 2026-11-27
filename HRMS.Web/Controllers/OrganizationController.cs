using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Organization;
using HRMS.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ORG.VIEW")]
public sealed class OrganizationController : Controller
{
    private readonly IOrgStructureService _orgStructureService;

    public OrganizationController(IOrgStructureService orgStructureService)
    {
        _orgStructureService = orgStructureService;
    }

    [HttpGet]
    public async Task<IActionResult> Positions()
    {
        var model = await _orgStructureService.GetPositionsAndGradesPageAsync(User.GetCompanyId());
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Locations()
    {
        var model = await _orgStructureService.GetLocationsAndCostCentersPageAsync(User.GetCompanyId());
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Shifts()
    {
        var shifts = await _orgStructureService.GetShiftsAsync(User.GetCompanyId());
        return View(shifts);
    }

    [HttpGet]
    public async Task<IActionResult> OrgChart()
    {
        var chart = await _orgStructureService.GetOrgChartAsync(User.GetCompanyId());
        return View(chart);
    }

    // --- AJAX Endpoints for Job Grades ---
    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveJobGrade([FromForm] JobGradeViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _orgStructureService.SaveJobGradeAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJobGrade(int id)
    {
        var result = await _orgStructureService.DeleteJobGradeAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    // --- AJAX Endpoints for Job Positions ---
    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveJobPosition([FromForm] JobPositionViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _orgStructureService.SaveJobPositionAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJobPosition(int id)
    {
        var result = await _orgStructureService.DeleteJobPositionAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    // --- AJAX Endpoints for Cost Centers ---
    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCostCenter([FromForm] CostCenterViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _orgStructureService.SaveCostCenterAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCostCenter(int id)
    {
        var result = await _orgStructureService.DeleteCostCenterAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    // --- AJAX Endpoints for Work Locations ---
    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWorkLocation([FromForm] WorkLocationViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _orgStructureService.SaveWorkLocationAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWorkLocation(int id)
    {
        var result = await _orgStructureService.DeleteWorkLocationAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    // --- AJAX Endpoints for Shifts ---
    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShift([FromForm] ShiftViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _orgStructureService.SaveShiftAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShift(int id)
    {
        var result = await _orgStructureService.DeleteShiftAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }
}
