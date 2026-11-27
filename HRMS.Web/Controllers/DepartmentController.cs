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
public sealed class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IBranchService _branchService;

    public DepartmentController(IDepartmentService departmentService, IBranchService branchService)
    {
        _departmentService = departmentService;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId = null, string? search = null)
    {
        var companyId = User.GetCompanyId();
        ViewData["SelectedBranchId"] = branchId;
        ViewData["SearchTerm"] = search;
        ViewData["Branches"] = (await _branchService.GetAllAsync(companyId, isActive: true)).ToList();

        var departments = await _departmentService.GetAllAsync(companyId, branchId, search);
        return View(departments);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _departmentService.GetByIdAsync(User.GetCompanyId(), id);
        if (model is null) return NotFound();
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await _departmentService.PrepareFormAsync(User.GetCompanyId());
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;
        if (!ModelState.IsValid)
        {
            return View(await _departmentService.PrepareFormAsync(companyId, model));
        }

        var result = await _departmentService.CreateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _departmentService.PrepareFormAsync(companyId, model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var companyId = User.GetCompanyId();
        var existing = await _departmentService.GetByIdAsync(companyId, id);
        if (existing is null) return NotFound();

        var model = await _departmentService.PrepareFormAsync(companyId, existing);
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DepartmentFormViewModel model)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;
        if (!ModelState.IsValid)
        {
            return View(await _departmentService.PrepareFormAsync(companyId, model));
        }

        var result = await _departmentService.UpdateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _departmentService.PrepareFormAsync(companyId, model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _departmentService.DeleteAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<object>.Ok(true, result.Message)
            : ApiResponse<object>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Export(int? branchId = null, string? search = null)
    {
        var rows = await _departmentService.GetAllAsync(User.GetCompanyId(), branchId, search);
        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<DepartmentListItemViewModel, object?>)[]
        {
            ("Code", x => x.DepartmentCode),
            ("Department Name", x => x.DepartmentName),
            ("Parent Department", x => x.ParentDepartmentName),
            ("Branch", x => x.BranchName),
            ("Cost Center", x => x.CostCenterCode),
            ("Employees", x => x.EmployeeCount),
            ("Status", x => x.IsActive ? "Active" : "Inactive")
        });

        return File(bytes, "text/csv", $"Departments_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
