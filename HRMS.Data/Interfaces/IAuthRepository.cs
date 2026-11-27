using HRMS.Models.DTOs;

namespace HRMS.Data.Interfaces;

public interface IAuthRepository
{
    Task<AuthenticatedUserDto?> GetUserForLoginAsync(string usernameOrEmail);
    Task<AuthenticatedUserDto?> GetUserByIdAsync(int userId);
    Task<(List<string> Roles, List<string> Permissions)> GetUserRolesAndPermissionsAsync(int userId);
    Task RecordLoginAttemptAsync(string usernameOrEmail, int? userId, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent);
    Task UpdateLoginSuccessAsync(int userId, string? ipAddress);
    Task UpdateFailedLoginAsync(int userId, int failedCount, DateTime? lockoutEndUtc);
    Task UpdatePasswordAsync(int userId, string newPasswordHash, int modifiedBy);
    Task<bool> HasAnyAdminUserAsync();
    Task<int> CreateInitialAdminAsync(string username, string email, string fullName, string passwordHash, string ipAddress);
}
