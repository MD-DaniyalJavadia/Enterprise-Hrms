using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HRMS.Web.Authorization;

public static class HrmsClaimTypes
{
    public const string CompanyId = "hrms:company_id";
    public const string CompanyName = "hrms:company_name";
    public const string BranchId = "hrms:branch_id";
    public const string BranchName = "hrms:branch_name";
    public const string EmployeeId = "hrms:employee_id";
    public const string FullName = "hrms:full_name";
    public const string Permission = "hrms:permission";
    public const string MustChangePassword = "hrms:must_change_password";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERMISSION:";

    public HasPermissionAttribute(string permissionCode) : base($"{PolicyPrefix}{permissionCode}")
    {
    }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole("SuperAdmin") ||
            context.User.HasClaim(c => c.Type == HrmsClaimTypes.Permission &&
                                       string.Equals(c.Value, requirement.PermissionCode, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionCode = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out int id) ? id : 0;
    }

    public static int GetCompanyId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(HrmsClaimTypes.CompanyId);
        return int.TryParse(raw, out int id) ? id : 0;
    }

    public static string GetCompanyName(this ClaimsPrincipal user) =>
        user.FindFirstValue(HrmsClaimTypes.CompanyName) ?? "Enterprise HRMS";

    public static int? GetBranchId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(HrmsClaimTypes.BranchId);
        return int.TryParse(raw, out int id) ? id : null;
    }

    public static int? GetEmployeeId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(HrmsClaimTypes.EmployeeId);
        return int.TryParse(raw, out int id) ? id : null;
    }

    public static string GetFullName(this ClaimsPrincipal user) =>
        user.FindFirstValue(HrmsClaimTypes.FullName) ?? user.Identity?.Name ?? "User";

    public static bool HasPermission(this ClaimsPrincipal user, string permissionCode)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("SuperAdmin")) return true;
        return user.HasClaim(c => c.Type == HrmsClaimTypes.Permission &&
                                  string.Equals(c.Value, permissionCode, StringComparison.OrdinalIgnoreCase));
    }
}
