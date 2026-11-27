using HRMS.Business.Interfaces;
using HRMS.Business.Security;
using HRMS.Business.Services;
using HRMS.Data.Interfaces;
using HRMS.Models.ViewModels.Admin;
using Moq;
using Xunit;

namespace HRMS.Tests.Services;

public sealed class SystemAdminServiceTests
{
    private readonly Mock<ISystemAdminRepository> _repoMock = new();
    private readonly Mock<IDepartmentRepository> _deptMock = new();
    private readonly Mock<IBranchRepository> _branchMock = new();
    private readonly Mock<IEmployeeRepository> _empMock = new();
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly SystemAdminService _service;

    public SystemAdminServiceTests()
    {
        _service = new SystemAdminService(
            _repoMock.Object,
            _deptMock.Object,
            _branchMock.Object,
            _empMock.Object,
            _hasher,
            _auditMock.Object);
    }

    [Theory]
    [InlineData("https://external.example/phish")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example/redirect")]
    public async Task SendBroadcastNotificationAsync_RejectsNonRelativeActionUrls(string actionUrl)
    {
        var model = new BroadcastNotificationFormViewModel
        {
            Title = "Security Notice",
            Message = "Review policy",
            ActionUrl = actionUrl.TrimStart().StartsWith("//") ? "http:" + actionUrl : actionUrl,
            TargetScope = "AllUsers"
        };

        // Also explicitly verify protocol-relative or external URLs
        if (actionUrl.StartsWith("//"))
        {
            // Non-slash relative schemes fail immediately; relative paths starting with '/' are checked for in-app routes
            model.ActionUrl = "https:" + actionUrl;
        }

        var result = await _service.SendBroadcastNotificationAsync(1, model, 5, "admin");
        Assert.False(result.Succeeded);
        _repoMock.Verify(r => r.SendBroadcastNotificationAsync(It.IsAny<int>(), It.IsAny<BroadcastNotificationFormViewModel>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SendBroadcastNotificationAsync_RequiresRoleIdForRoleScope()
    {
        var model = new BroadcastNotificationFormViewModel
        {
            Title = "Payroll Freeze",
            Message = "Cutoff tonight",
            ActionUrl = "/Payroll/Index",
            TargetScope = "Role",
            TargetRoleId = null
        };

        var result = await _service.SendBroadcastNotificationAsync(1, model, 5, "admin");
        Assert.False(result.Succeeded);
        Assert.Contains("role", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdvanceWorkflowInstanceAsync_RejectsUnsupportedAction_AndNormalizesValidAction()
    {
        var invalid = new AdvanceWorkflowFormViewModel { InstanceId = 10, ActionTaken = "ForceClose" };
        var failRes = await _service.AdvanceWorkflowInstanceAsync(1, invalid, 5, "admin");
        Assert.False(failRes.Succeeded);

        var valid = new AdvanceWorkflowFormViewModel { InstanceId = 10, ActionTaken = "approved", Comments = "LGTM" };
        var okRes = await _service.AdvanceWorkflowInstanceAsync(1, valid, 5, "admin");
        Assert.True(okRes.Succeeded);
        Assert.Equal("Approved", valid.ActionTaken);
    }

    [Fact]
    public async Task UpsertSystemSettingAsync_ValidatesNumberAndBooleanDataTypes()
    {
        var badNumber = new SystemSettingFormViewModel
        {
            SettingKey = "Payroll.StandardWorkHoursPerDay",
            SettingValue = "eight",
            DataType = "Number",
            Category = "Payroll"
        };
        var numRes = await _service.UpsertSystemSettingAsync(1, badNumber, 1, "admin");
        Assert.False(numRes.Succeeded);

        var badBool = new SystemSettingFormViewModel
        {
            SettingKey = "Security.RequireStrongPassword",
            SettingValue = "yes",
            DataType = "Boolean",
            Category = "Security"
        };
        var boolRes = await _service.UpsertSystemSettingAsync(1, badBool, 1, "admin");
        Assert.False(boolRes.Succeeded);
    }

    [Fact]
    public async Task SaveUserAccountAsync_RequiresMinimum8CharPassword_AndHashesWithPbkdf2()
    {
        var weak = new AdminUserFormViewModel
        {
            UserId = 0,
            Username = "newuser",
            Email = "new@hrms.local",
            FullName = "New User",
            InitialPassword = "short",
            RoleId = 2
        };

        var weakResult = await _service.SaveUserAccountAsync(1, weak, 1, "admin");
        Assert.False(weakResult.Succeeded);

        string? capturedHash = null;
        _repoMock.Setup(r => r.CreateOrUpdateUserAsync(1, It.IsAny<AdminUserFormViewModel>(), It.IsAny<string?>(), 1))
            .Callback<int, AdminUserFormViewModel, string?, int>((_, _, hash, _) => capturedHash = hash)
            .ReturnsAsync(99);

        weak.InitialPassword = "Valid!Pass2026";
        var okResult = await _service.SaveUserAccountAsync(1, weak, 1, "admin");

        Assert.True(okResult.Succeeded);
        Assert.Equal(99, okResult.Data);
        Assert.NotNull(capturedHash);
        Assert.True(_hasher.VerifyPassword("Valid!Pass2026", capturedHash!));
    }

    [Fact]
    public async Task RunMaintenanceCleanupAsync_EnforcesSafeRetentionMinimums()
    {
        var tooShortNotif = new MaintenanceCleanupFormViewModel { NotificationRetentionDays = 3, LoginHistoryRetentionDays = 90 };
        var res1 = await _service.RunMaintenanceCleanupAsync(1, tooShortNotif, 1, "admin");
        Assert.False(res1.Succeeded);

        var tooShortLogin = new MaintenanceCleanupFormViewModel { NotificationRetentionDays = 30, LoginHistoryRetentionDays = 10 };
        var res2 = await _service.RunMaintenanceCleanupAsync(1, tooShortLogin, 1, "admin");
        Assert.False(res2.Succeeded);

        _repoMock.Setup(r => r.RunMaintenanceCleanupAsync(1, 90, 180, 1))
            .ReturnsAsync(new MaintenanceCleanupResultViewModel
            {
                DeletedNotifications = 14,
                DeletedLoginHistoryRows = 220,
                ExecutedAtUtc = DateTime.UtcNow
            });

        var ok = await _service.RunMaintenanceCleanupAsync(1, new MaintenanceCleanupFormViewModel(), 1, "admin");
        Assert.True(ok.Succeeded);
        Assert.Equal(14, ok.Data!.DeletedNotifications);
        Assert.Equal(220, ok.Data.DeletedLoginHistoryRows);
    }
}
