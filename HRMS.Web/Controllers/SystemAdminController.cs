using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Admin;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ADMIN.VIEW")]
public sealed class SystemAdminController : Controller
{
    private readonly ISystemAdminService _adminService;

    public SystemAdminController(ISystemAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> AuditLogs(
        string? moduleName = null, string? actionType = null, string? username = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? search = null)
    {
        var model = await _adminService.GetAuditLogsPageAsync(
            User.GetCompanyId(), moduleName, actionType, username, fromDate, toDate, search,
            User.HasPermission("ADMIN.MANAGE"));
        return View(model);
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> ExportAuditLogs(
        string? moduleName = null, string? actionType = null, string? username = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? search = null)
    {
        var model = await _adminService.GetAuditLogsPageAsync(
            User.GetCompanyId(), moduleName, actionType, username, fromDate, toDate, search, true);

        var bytes = CsvExportHelper.BuildCsv(model.AuditLogs, new (string, Func<AuditLog, object?>)[]
        {
            ("Timestamp (UTC)", x => x.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")),
            ("Username", x => x.Username),
            ("Action", x => x.ActionType),
            ("Module", x => x.ModuleName),
            ("Entity", x => x.EntityName),
            ("Entity Id", x => x.EntityId),
            ("Description", x => x.Description),
            ("IP Address", x => x.IpAddress)
        });

        return File(bytes, "text/csv", $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Settings(string? category = null)
    {
        var model = await _adminService.GetSystemSettingsPageAsync(
            User.GetCompanyId(), category, User.HasPermission("ADMIN.MANAGE"));
        return View(model);
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSetting([FromForm] SystemSettingFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _adminService.UpsertSystemSettingAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> AccessControl()
    {
        var model = await _adminService.GetAccessControlPageAsync(
            User.GetCompanyId(), User.HasPermission("ADMIN.MANAGE"));
        return View(model);
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRolePermissions([FromBody] SaveRolePermissionsFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Invalid permission payload."));
        }

        var result = await _adminService.SaveRolePermissionsAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUser([FromForm] AdminUserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _adminService.SaveUserAccountAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id, string statusAction, string? newPassword = null)
    {
        var result = await _adminService.ToggleUserStatusAsync(
            User.GetCompanyId(), id, statusAction, newPassword, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> SystemHealth()
    {
        var model = await _adminService.GetSystemHealthPageAsync(
            User.GetCompanyId(), User.HasPermission("ADMIN.MANAGE"));
        return View(model);
    }

    [HasPermission("ADMIN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMaintenance([FromForm] MaintenanceCleanupFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _adminService.RunMaintenanceCleanupAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<MaintenanceCleanupResultViewModel?>.Ok(result.Data, result.Message)
            : ApiResponse<MaintenanceCleanupResultViewModel?>.Fail(result.Message));
    }
}
