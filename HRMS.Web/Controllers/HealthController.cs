using HRMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[AllowAnonymous]
public sealed class HealthController : Controller
{
    private readonly ISystemAdminService _adminService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ISystemAdminService adminService, ILogger<HealthController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("/health")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var health = await _adminService.CheckDatabaseHealthAsync();
            if (health is null || !string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    database = "Unavailable",
                    utcTimestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "Healthy",
                database = "Connected",
                utcTimestamp = health.UtcTimestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed while probing database connectivity.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                database = "Unavailable",
                utcTimestamp = DateTime.UtcNow
            });
        }
    }
}
