using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Auth;

namespace HRMS.Business.Interfaces;

public interface IAuthService
{
    Task<OperationResult<AuthenticatedUserDto>> AuthenticateAsync(LoginViewModel model, string? ipAddress, string? userAgent);
    Task<OperationResult> ChangePasswordAsync(int userId, int companyId, ChangePasswordViewModel model, string? ipAddress, string? userAgent);
    Task RecordLogoutAsync(int userId, int companyId, string username, string? ipAddress, string? userAgent);
    Task<bool> HasAnyAdminUserAsync();
    Task<OperationResult<int>> CreateInitialAdminAsync(string username, string email, string fullName, string password, string? ipAddress);
}
