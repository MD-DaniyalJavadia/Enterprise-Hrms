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
public sealed class BranchController : Controller
{
    private readonly IBranchService _branchService;

    public BranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, bool? isActive = null)
    {
        ViewData["SearchTerm"] = search;
        ViewData["IsActiveFilter"] = isActive;
        var branches = await _branchService.GetAllAsync(User.GetCompanyId(), search, isActive);
        return View(branches);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var branch = await _branchService.GetByIdAsync(User.GetCompanyId(), id);
        if (branch is null) return NotFound();
        return View(branch);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new BranchFormViewModel { CompanyId = User.GetCompanyId(), IsActive = true });
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchFormViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid) return View(model);

        var result = await _branchService.CreateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _branchService.GetByIdAsync(User.GetCompanyId(), id);
        if (model is null) return NotFound();
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BranchFormViewModel model)
    {
        model.CompanyId = User.GetCompanyId();
        if (!ModelState.IsValid) return View(model);

        var result = await _branchService.UpdateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _branchService.DeleteAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<object>.Ok(true, result.Message)
            : ApiResponse<object>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search = null, bool? isActive = null)
    {
        var rows = await _branchService.GetAllAsync(User.GetCompanyId(), search, isActive);
        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<BranchListItemViewModel, object?>)[]
        {
            ("Branch Code", x => x.BranchCode),
            ("Branch Name", x => x.BranchName),
            ("Head Office", x => x.IsHeadOffice ? "Yes" : "No"),
            ("City", x => x.City),
            ("Country", x => x.Country),
            ("Email", x => x.Email),
            ("Phone", x => x.Phone),
            ("Employees", x => x.EmployeeCount),
            ("Status", x => x.IsActive ? "Active" : "Inactive")
        });

        return File(bytes, "text/csv", $"Branches_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
