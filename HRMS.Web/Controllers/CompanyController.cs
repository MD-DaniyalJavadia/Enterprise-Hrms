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
public sealed class CompanyController : Controller
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, bool? isActive = null)
    {
        ViewData["SearchTerm"] = search;
        ViewData["IsActiveFilter"] = isActive;
        var companies = await _companyService.GetAllAsync(search, isActive);
        return View(companies);
    }

    [HttpGet]
    public async Task<IActionResult> GetList(string? search = null, bool? isActive = null)
    {
        var data = await _companyService.GetAllAsync(search, isActive);
        return Json(ApiResponse<object>.Ok(data));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company is null) return NotFound();
        return View(company);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CompanyFormViewModel());
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _companyService.CreateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
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
        var model = await _companyService.GetByIdAsync(id);
        if (model is null) return NotFound();
        return View(model);
    }

    [HasPermission("ORG.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _companyService.UpdateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
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
        var result = await _companyService.DeleteAsync(id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<object>.Ok(true, result.Message)
            : ApiResponse<object>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search = null, bool? isActive = null)
    {
        var rows = await _companyService.GetAllAsync(search, isActive);
        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<CompanyListItemViewModel, object?>)[]
        {
            ("Code", x => x.CompanyCode),
            ("Company Name", x => x.CompanyName),
            ("Legal Name", x => x.LegalName),
            ("Email", x => x.Email),
            ("Phone", x => x.Phone),
            ("City", x => x.City),
            ("Country", x => x.Country),
            ("Currency", x => x.CurrencyCode),
            ("Branches", x => x.BranchCount),
            ("Employees", x => x.EmployeeCount),
            ("Status", x => x.IsActive ? "Active" : "Inactive")
        });

        return File(bytes, "text/csv", $"Companies_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
