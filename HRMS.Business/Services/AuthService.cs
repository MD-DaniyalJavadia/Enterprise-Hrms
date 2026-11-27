using HRMS.Business.Interfaces;
using HRMS.Business.Security;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Auth;
using Microsoft.Extensions.Logging;

namespace HRMS.Business.Services;

public sealed class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IAuthRepository _authRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        IAuditLogService auditLogService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _auditLogService = auditLogService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<OperationResult<AuthenticatedUserDto>> AuthenticateAsync(
        LoginViewModel model,
        string? ipAddress,
        string? userAgent)
    {
        var identifier = model.UsernameOrEmail.Trim();
        var user = await _authRepository.GetUserForLoginAsync(identifier);

        if (user is null)
        {
            await _authRepository.RecordLoginAttemptAsync(identifier, null, false, "User not found", ipAddress, userAgent);
            return OperationResult<AuthenticatedUserDto>.Failure("Invalid username/email or password.");
        }

        if (!user.IsActive || user.IsDeleted)
        {
            await _authRepository.RecordLoginAttemptAsync(identifier, user.UserId, false, "Account inactive or deleted", ipAddress, userAgent);
            return OperationResult<AuthenticatedUserDto>.Failure("Your account is deactivated. Please contact your HR administrator.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var remainingMinutes = Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);
            await _authRepository.RecordLoginAttemptAsync(identifier, user.UserId, false, "Account locked out", ipAddress, userAgent);
            return OperationResult<AuthenticatedUserDto>.Failure($"Account is temporarily locked due to multiple failed attempts. Try again in {remainingMinutes} minute(s).");
        }

        if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
        {
            int nextFailedCount = user.FailedLoginCount + 1;
            DateTime? lockoutEnd = nextFailedCount >= MaxFailedAttempts
                ? DateTime.UtcNow.Add(LockoutDuration)
                : null;

            await _authRepository.UpdateFailedLoginAsync(user.UserId, nextFailedCount, lockoutEnd);
            await _authRepository.RecordLoginAttemptAsync(identifier, user.UserId, false, "Invalid password", ipAddress, userAgent);
            await _auditLogService.LogAsync(
                user.CompanyId,
                user.UserId,
                user.Username,
                AuditActionType.FailedLogin.ToString(),
                "Authentication",
                "User",
                user.UserId.ToString(),
                $"Failed login attempt ({nextFailedCount}/{MaxFailedAttempts}).",
                ipAddress,
                userAgent);

            if (lockoutEnd.HasValue)
            {
                _logger.LogWarning("User {Username} locked out until {LockoutEndUtc}", user.Username, lockoutEnd.Value);
                return OperationResult<AuthenticatedUserDto>.Failure("Maximum failed login attempts exceeded. Account locked for 15 minutes.");
            }

            return OperationResult<AuthenticatedUserDto>.Failure("Invalid username/email or password.");
        }

        var (roles, permissions) = await _authRepository.GetUserRolesAndPermissionsAsync(user.UserId);
        user.Roles = roles;
        user.Permissions = permissions;

        await _authRepository.UpdateLoginSuccessAsync(user.UserId, ipAddress);
        await _authRepository.RecordLoginAttemptAsync(identifier, user.UserId, true, null, ipAddress, userAgent);
        await _auditLogService.LogAsync(
            user.CompanyId,
            user.UserId,
            user.Username,
            AuditActionType.Login.ToString(),
            "Authentication",
            "User",
            user.UserId.ToString(),
            "User signed in successfully.",
            ipAddress,
            userAgent);

        return OperationResult<AuthenticatedUserDto>.Success(user, "Login successful.");
    }

    public async Task<OperationResult> ChangePasswordAsync(
        int userId,
        int companyId,
        ChangePasswordViewModel model,
        string? ipAddress,
        string? userAgent)
    {
        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return OperationResult.Failure("User account could not be found.");
        }

        if (!_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            return OperationResult.Failure("Current password is incorrect.");
        }

        if (model.CurrentPassword == model.NewPassword)
        {
            return OperationResult.Failure("New password must be different from your current password.");
        }

        var newHash = _passwordHasher.HashPassword(model.NewPassword);
        await _authRepository.UpdatePasswordAsync(userId, newHash, userId);
        await _auditLogService.LogAsync(
            companyId,
            userId,
            user.Username,
            AuditActionType.PasswordChanged.ToString(),
            "Authentication",
            "User",
            userId.ToString(),
            "User changed their password.",
            ipAddress,
            userAgent);

        return OperationResult.Success("Password updated successfully.");
    }

    public async Task RecordLogoutAsync(int userId, int companyId, string username, string? ipAddress, string? userAgent)
    {
        await _auditLogService.LogAsync(
            companyId,
            userId,
            username,
            AuditActionType.Logout.ToString(),
            "Authentication",
            "User",
            userId.ToString(),
            "User signed out.",
            ipAddress,
            userAgent);
    }

    public Task<bool> HasAnyAdminUserAsync()
    {
        return _authRepository.HasAnyAdminUserAsync();
    }

    public async Task<OperationResult<int>> CreateInitialAdminAsync(
        string username,
        string email,
        string fullName,
        string password,
        string? ipAddress)
    {
        if (await _authRepository.HasAnyAdminUserAsync())
        {
            return OperationResult<int>.Failure("An administrator account already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var newUserId = await _authRepository.CreateInitialAdminAsync(
            username.Trim(),
            email.Trim(),
            fullName.Trim(),
            passwordHash,
            ipAddress ?? "127.0.0.1");

        return OperationResult<int>.Success(newUserId, "Initial administrator account created successfully.");
    }
}
