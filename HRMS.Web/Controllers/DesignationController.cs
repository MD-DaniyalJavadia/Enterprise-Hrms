using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Organization;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ORG.VIEW")]
public sealed class DesignationController : Controller
{
    private readonly IDesignationService _designationService;

    public DesignationController(IDesignationService designationService)
    {
        _designationService = designationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null)
    {
        ViewData["SearchTerm"] = search;
        var items = await _designationService.GetAllAsync(User.GetCompanyId(), search);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _designationService.GetByIdAsync(User.GetCompanyId(), id);
        if (model is null) return NotFound();
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await _designationService.PrepareFormAsync(User.GetCompanyId());
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DesignationFormViewModel model)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;
        if (!ModelState.IsValid)
        {
            return View(await _designationService.PrepareFormAsync(companyId, model));
        }

        var result = await _designationService.CreateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _designationService.PrepareFormAsync(companyId, model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var companyId = User.GetCompanyId();
        var existing = await _designationService.GetByIdAsync(companyId, id);
        if (existing is null) return NotFound();

        return View(await _designationService.PrepareFormAsync(companyId, existing));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DesignationFormViewModel model)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;
        if (!ModelState.IsValid)
        {
            return View(await _designationService.PrepareFormAsync(companyId, model));
        }

        var result = await _designationService.UpdateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _designationService.PrepareFormAsync(companyId, model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _designationService.DeleteAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<object>.Ok(true, result.Message)
            : ApiResponse<object>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search = null)
    {
        var rows = await _designationService.GetAllAsync(User.GetCompanyId(), search);
        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<DesignationListItemViewModel, object?>)[]
        {
            ("Code", x => x.DesignationCode),
            ("Title", x => x.DesignationTitle),
            ("Job Grade", x => x.JobGrade),
            ("Employees", x => x.EmployeeCount),
            ("Status", x => x.IsActive ? "Active" : "Inactive")
        });

        return File(bytes, "text/csv", $"Designations_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
