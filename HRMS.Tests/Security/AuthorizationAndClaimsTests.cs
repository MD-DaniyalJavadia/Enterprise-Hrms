using System.Security.Claims;
using HRMS.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HRMS.Tests.Security;

public sealed class AuthorizationAndClaimsTests
{
    [Fact]
    public async Task PermissionHandler_DeniesUnauthenticatedUser()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("PAYROLL.MANAGE");
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_AllowsSuperAdmin_ForAnyPermission()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("ADMIN.MANAGE");
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        }, "TestAuth");
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(identity), null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_MatchesExplicitPermissionClaimCaseInsensitively()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("LEAVE.MANAGE");
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(HrmsClaimTypes.Permission, "leave.manage")
        }, "TestAuth");
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(identity), null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionHandler_DeniesUserWithoutPermission()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("ADMIN.MANAGE");
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim(HrmsClaimTypes.Permission, "ESS.VIEW")
        }, "TestAuth");
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(identity), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void ClaimsPrincipalExtensions_ParseTenantAndIdentityClaimsSafely()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "105"),
            new Claim(ClaimTypes.Name, "jdoe"),
            new Claim(HrmsClaimTypes.CompanyId, "7"),
            new Claim(HrmsClaimTypes.CompanyName, "Acme Global"),
            new Claim(HrmsClaimTypes.BranchId, "12"),
            new Claim(HrmsClaimTypes.EmployeeId, "301"),
            new Claim(HrmsClaimTypes.FullName, "Jane Doe"),
            new Claim(HrmsClaimTypes.Permission, "REPORTS.VIEW")
        }, "TestAuth");

        var user = new ClaimsPrincipal(identity);

        Assert.Equal(105, user.GetUserId());
        Assert.Equal(7, user.GetCompanyId());
        Assert.Equal("Acme Global", user.GetCompanyName());
        Assert.Equal(12, user.GetBranchId());
        Assert.Equal(301, user.GetEmployeeId());
        Assert.Equal("Jane Doe", user.GetFullName());
        Assert.True(user.HasPermission("REPORTS.VIEW"));
        Assert.False(user.HasPermission("REPORTS.EXPORT"));
    }
}
