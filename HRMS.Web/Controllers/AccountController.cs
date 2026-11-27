using System.Security.Claims;
using HRMS.Business.Interfaces;
using HRMS.Models.ViewModels.Auth;
using HRMS.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HRMS.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLandingPage(User.HasPermission("DASHBOARD.VIEW"), User.HasPermission("MANAGER.VIEW"), User.HasPermission("ESS.VIEW"));
        }

        if (!await _authService.HasAnyAdminUserAsync())
        {
            return RedirectToAction(nameof(Setup));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [EnableRateLimiting("LoginRateLimit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.AuthenticateAsync(model, ipAddress, userAgent);
        if (!result.Succeeded || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(HrmsClaimTypes.FullName, user.FullName),
            new(HrmsClaimTypes.CompanyId, user.CompanyId.ToString()),
            new(HrmsClaimTypes.CompanyName, user.CompanyName),
            new(HrmsClaimTypes.MustChangePassword, user.MustChangePassword.ToString())
        };

        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim(HrmsClaimTypes.BranchId, user.BranchId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.BranchName))
        {
            claims.Add(new Claim(HrmsClaimTypes.BranchName, user.BranchName));
        }

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(HrmsClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));
        }

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim(HrmsClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        if (user.MustChangePassword)
        {
            TempData["WarningMessage"] = "For security reasons, please update your password before continuing.";
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        bool isSuperAdmin = user.Roles.Any(r => string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase));
        bool canDashboard = isSuperAdmin || user.Permissions.Contains("DASHBOARD.VIEW", StringComparer.OrdinalIgnoreCase);
        bool canManager = isSuperAdmin || user.Permissions.Contains("MANAGER.VIEW", StringComparer.OrdinalIgnoreCase);
        bool canEss = isSuperAdmin || user.Permissions.Contains("ESS.VIEW", StringComparer.OrdinalIgnoreCase);
        return RedirectToLandingPage(canDashboard, canManager, canEss);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        if (await _authService.HasAnyAdminUserAsync())
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new InitialSetupViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(InitialSetupViewModel model)
    {
        if (await _authService.HasAnyAdminUserAsync())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.CreateInitialAdminAsync(
            model.Username,
            model.Email,
            model.FullName,
            model.Password,
            ipAddress);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Super Administrator account created. Sign in with your new credentials.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        var username = User.Identity?.Name ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        if (userId > 0)
        {
            await _authService.RecordLogoutAsync(userId, companyId, username, ipAddress, userAgent);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.ChangePasswordAsync(
            User.GetUserId(),
            User.GetCompanyId(),
            model,
            ipAddress,
            userAgent);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToLandingPage(User.HasPermission("DASHBOARD.VIEW"), User.HasPermission("MANAGER.VIEW"), User.HasPermission("ESS.VIEW"));
    }

    private IActionResult RedirectToLandingPage(bool canDashboard, bool canManager, bool canEss)
    {
        if (canDashboard) return RedirectToAction("Index", "Dashboard");
        if (canManager) return RedirectToAction("Index", "ManagerPortal");
        if (canEss) return RedirectToAction("Index", "SelfService");
        return RedirectToAction("Index", "Dashboard");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        return View(new AccessDeniedViewModel { ReturnUrl = returnUrl });
    }
}
