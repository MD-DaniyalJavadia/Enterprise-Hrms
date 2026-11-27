using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.AssetDocument;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ASSET.VIEW")]
public sealed class AssetController : Controller
{
    private readonly IAssetDocumentService _service;

    public AssetController(IAssetDocumentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null, string? search = null)
    {
        var canManage = User.HasPermission("ASSET.MANAGE");
        var model = await _service.GetAssetIndexPageAsync(
            User.GetCompanyId(), categoryId, branchId, status, employeeId, search, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var canManage = User.HasPermission("ASSET.MANAGE");
        var detail = await _service.GetAssetDetailsAsync(User.GetCompanyId(), id, canManage);
        if (detail is null) return NotFound();

        if (!canManage && detail.Asset.CurrentEmployeeId != User.GetEmployeeId())
        {
            return Forbid();
        }

        return View(detail);
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAsset([FromForm] AssetFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveAssetAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsset(int id)
    {
        var result = await _service.DeleteAssetAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Allocations(int? assetId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("ASSET.MANAGE");
        var model = await _service.GetAllocationsPageAsync(
            User.GetCompanyId(), assetId, employeeId, status, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAsset([FromForm] AssetAssignFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.AssignAssetAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnAsset([FromForm] AssetReturnFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.ReturnAssetAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeAllocation(int allocationId)
    {
        var empId = User.GetEmployeeId();
        if (!empId.HasValue)
        {
            return Json(ApiResponse<bool>.Fail("No employee profile is linked to your account."));
        }

        var result = await _service.AcknowledgeAllocationAsync(
            User.GetCompanyId(), allocationId, empId.Value, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Maintenance(int? assetId = null, string? status = null)
    {
        var model = await _service.GetMaintenancePageAsync(User.GetCompanyId(), assetId, status);
        return View(model);
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMaintenance([FromForm] AssetMaintenanceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveMaintenanceLogAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMaintenance(int id)
    {
        var result = await _service.DeleteMaintenanceLogAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _service.GetAssetCategoriesAsync(User.GetCompanyId());
        return View(categories);
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory([FromForm] AssetCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveAssetCategoryAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ASSET.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _service.DeleteAssetCategoryAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportAssets(int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null, string? search = null)
    {
        var canManage = User.HasPermission("ASSET.MANAGE");
        var model = await _service.GetAssetIndexPageAsync(
            User.GetCompanyId(), categoryId, branchId, status, employeeId, search, User.GetEmployeeId(), canManage);

        var bytes = CsvExportHelper.BuildCsv(model.Assets, new (string, Func<AssetItem, object?>)[]
        {
            ("Asset Tag", x => x.AssetTag),
            ("Asset Name", x => x.AssetName),
            ("Category", x => x.CategoryName),
            ("Class", x => x.AssetClass),
            ("Brand", x => x.Brand),
            ("Model", x => x.ModelNumber),
            ("Serial Number", x => x.SerialNumber),
            ("Branch", x => x.BranchName),
            ("Condition", x => x.ConditionStatus),
            ("Status", x => x.AssetStatus),
            ("Assigned To", x => x.CurrentEmployeeName),
            ("Purchase Date", x => x.PurchaseDate?.ToString("yyyy-MM-dd")),
            ("Purchase Cost", x => x.PurchaseCost),
            ("Warranty Expiry", x => x.WarrantyExpiryDate?.ToString("yyyy-MM-dd"))
        });

        return File(bytes, "text/csv", $"AssetsInventory_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
