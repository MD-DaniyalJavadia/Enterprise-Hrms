using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Admin;
using HRMS.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly ISystemAdminService _adminService;
    private readonly INotificationService _notificationService;

    public NotificationsController(ISystemAdminService adminService, INotificationService notificationService)
    {
        _adminService = adminService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? filterRead = null, string? notificationType = null, string? moduleName = null, string? search = null)
    {
        var model = await _adminService.GetNotificationInboxAsync(
            User.GetCompanyId(), User.GetUserId(), filterRead, notificationType, moduleName, search,
            User.HasPermission("NOTIFICATION.MANAGE"));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id)
    {
        var ok = await _notificationService.MarkAsReadAsync(id, User.GetUserId());
        return Json(ApiResponse<bool>.Ok(ok, ok ? "Marked as read." : "Already marked as read."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await _notificationService.MarkAllAsReadAsync(User.GetCompanyId(), User.GetUserId());
        return Json(ApiResponse<int>.Ok(count, $"{count} notification(s) marked as read."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotification(long id)
    {
        var result = await _adminService.DeleteNotificationAsync(User.GetCompanyId(), id, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("NOTIFICATION.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendBroadcast([FromForm] BroadcastNotificationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _adminService.SendBroadcastNotificationAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("NOTIFICATION.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate([FromForm] NotificationTemplateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _adminService.SaveNotificationTemplateAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("NOTIFICATION.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var result = await _adminService.DeleteNotificationTemplateAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }
}
