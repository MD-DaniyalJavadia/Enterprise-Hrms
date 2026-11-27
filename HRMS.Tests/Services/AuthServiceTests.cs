using HRMS.Business.Interfaces;
using HRMS.Business.Security;
using HRMS.Business.Services;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HRMS.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepoMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _authRepoMock.Object,
            _auditMock.Object,
            _hasher,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsFailure_WhenUserNotFound()
    {
        _authRepoMock.Setup(r => r.GetUserForLoginAsync("unknown"))
            .ReturnsAsync((AuthenticatedUserDto?)null);

        var result = await _service.AuthenticateAsync(
            new LoginViewModel { UsernameOrEmail = "unknown", Password = "Password123!" },
            "127.0.0.1",
            "xUnit");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username/email or password.", result.Message);
        _authRepoMock.Verify(r => r.RecordLoginAttemptAsync("unknown", null, false, "User not found", "127.0.0.1", "xUnit"), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_BlocksInactiveAccount()
    {
        _authRepoMock.Setup(r => r.GetUserForLoginAsync("inactive"))
            .ReturnsAsync(new AuthenticatedUserDto
            {
                UserId = 10,
                Username = "inactive",
                IsActive = false,
                IsDeleted = false,
                PasswordHash = _hasher.HashPassword("Pass1234!")
            });

        var result = await _service.AuthenticateAsync(
            new LoginViewModel { UsernameOrEmail = "inactive", Password = "Pass1234!" },
            "10.0.0.5",
            "xUnit");

        Assert.False(result.Succeeded);
        Assert.Contains("deactivated", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticateAsync_BlocksAccountUnderActiveLockout()
    {
        _authRepoMock.Setup(r => r.GetUserForLoginAsync("locked"))
            .ReturnsAsync(new AuthenticatedUserDto
            {
                UserId = 11,
                Username = "locked",
                IsActive = true,
                LockoutEndUtc = DateTime.UtcNow.AddMinutes(10),
                PasswordHash = _hasher.HashPassword("Pass1234!")
            });

        var result = await _service.AuthenticateAsync(
            new LoginViewModel { UsernameOrEmail = "locked", Password = "Pass1234!" },
            "10.0.0.6",
            "xUnit");

        Assert.False(result.Succeeded);
        Assert.Contains("temporarily locked", result.Message, StringComparison.OrdinalIgnoreCase);
        _authRepoMock.Verify(r => r.UpdateFailedLoginAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_LocksAccountOnFifthFailedPasswordAttempt()
    {
        _authRepoMock.Setup(r => r.GetUserForLoginAsync("jdoe"))
            .ReturnsAsync(new AuthenticatedUserDto
            {
                UserId = 12,
                CompanyId = 1,
                Username = "jdoe",
                IsActive = true,
                FailedLoginCount = 4,
                PasswordHash = _hasher.HashPassword("RealPass#2026")
            });

        var result = await _service.AuthenticateAsync(
            new LoginViewModel { UsernameOrEmail = "jdoe", Password = "WrongPassword" },
            "10.0.0.7",
            "xUnit");

        Assert.False(result.Succeeded);
        Assert.Contains("locked for 15 minutes", result.Message, StringComparison.OrdinalIgnoreCase);
        _authRepoMock.Verify(r => r.UpdateFailedLoginAsync(12, 5, It.Is<DateTime?>(d => d.HasValue && d.Value > DateTime.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_SucceedsAndLoadsRolesAndPermissions()
    {
        _authRepoMock.Setup(r => r.GetUserForLoginAsync("admin"))
            .ReturnsAsync(new AuthenticatedUserDto
            {
                UserId = 1,
                CompanyId = 1,
                Username = "admin",
                Email = "admin@hrms.local",
                FullName = "System Admin",
                IsActive = true,
                PasswordHash = _hasher.HashPassword("Admin#2026!")
            });

        _authRepoMock.Setup(r => r.GetUserRolesAndPermissionsAsync(1))
            .ReturnsAsync((new List<string> { "SuperAdmin" }, new List<string> { "DASHBOARD.VIEW", "ADMIN.MANAGE" }));

        var result = await _service.AuthenticateAsync(
            new LoginViewModel { UsernameOrEmail = "admin", Password = "Admin#2026!" },
            "127.0.0.1",
            "xUnit");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Contains("SuperAdmin", result.Data!.Roles);
        Assert.Contains("ADMIN.MANAGE", result.Data.Permissions);
        _authRepoMock.Verify(r => r.UpdateLoginSuccessAsync(1, "127.0.0.1"), Times.Once);
    }

    [Fact]
    public async Task CreateInitialAdminAsync_FailsWhenAdminAlreadyExists()
    {
        _authRepoMock.Setup(r => r.HasAnyAdminUserAsync()).ReturnsAsync(true);

        var result = await _service.CreateInitialAdminAsync("admin2", "a2@test.local", "Admin Two", "Strong!123", "127.0.0.1");

        Assert.False(result.Succeeded);
        _authRepoMock.Verify(r => r.CreateInitialAdminAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
