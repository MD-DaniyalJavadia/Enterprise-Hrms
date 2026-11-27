using HRMS.Business.Interfaces;
using HRMS.Models.RequestModels;
using HRMS.Models.ResponseModels;
using HRMS.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("DASHBOARD.VIEW")]
public sealed class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly INotificationService _notificationService;

    public DashboardController(
        IDashboardService dashboardService,
        INotificationService notificationService)
    {
        _dashboardService = dashboardService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId = null, DateTime? asOfDate = null)
    {
        var filter = new DashboardFilterRequest
        {
            CompanyId = User.GetCompanyId(),
            BranchId = branchId,
            AsOfDate = asOfDate ?? DateTime.Today
        };

        var model = await _dashboardService.GetDashboardSummaryAsync(
            filter,
            User.GetUserId(),
            User.GetCompanyName());

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetSummaryData(int? branchId = null, DateTime? asOfDate = null)
    {
        var filter = new DashboardFilterRequest
        {
            CompanyId = User.GetCompanyId(),
            BranchId = branchId,
            AsOfDate = asOfDate ?? DateTime.Today
        };

        var model = await _dashboardService.GetDashboardSummaryAsync(
            filter,
            User.GetUserId(),
            User.GetCompanyName());

        return Json(ApiResponse<object>.Ok(model));
    }

    [HttpGet]
    public async Task<IActionResult> GlobalSearch(string term)
    {
        var results = await _dashboardService.GlobalSearchAsync(User.GetCompanyId(), term);
        return Json(ApiResponse<object>.Ok(results));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(long notificationId)
    {
        var updated = await _notificationService.MarkAsReadAsync(notificationId, User.GetUserId());
        return Json(ApiResponse<bool>.Ok(updated, updated ? "Notification marked as read." : "Notification already read."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var count = await _notificationService.MarkAllAsReadAsync(User.GetCompanyId(), User.GetUserId());
        return Json(ApiResponse<int>.Ok(count, $"{count} notification(s) marked as read."));
    }
}
