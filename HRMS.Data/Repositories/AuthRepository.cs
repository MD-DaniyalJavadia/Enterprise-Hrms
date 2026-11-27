using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;

namespace HRMS.Data.Repositories;

public sealed class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<AuthenticatedUserDto?> GetUserForLoginAsync(string usernameOrEmail)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UsernameOrEmail", usernameOrEmail, DbType.String, size: 120);

        return await QueryFirstOrDefaultSpAsync<AuthenticatedUserDto>("sp_Auth_GetUserForLogin", parameters);
    }

    public async Task<AuthenticatedUserDto?> GetUserByIdAsync(int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);

        return await QueryFirstOrDefaultSpAsync<AuthenticatedUserDto>("sp_Auth_GetUserById", parameters);
    }

    public async Task<(List<string> Roles, List<string> Permissions)> GetUserRolesAndPermissionsAsync(int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            "sp_Auth_GetUserRolesAndPermissions",
            parameters,
            commandType: CommandType.StoredProcedure);

        var roles = (await multi.ReadAsync<string>()).ToList();
        var permissions = (await multi.ReadAsync<string>()).ToList();

        return (roles, permissions);
    }

    public async Task RecordLoginAttemptAsync(
        string usernameOrEmail,
        int? userId,
        bool isSuccess,
        string? failureReason,
        string? ipAddress,
        string? userAgent)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UsernameOrEmail", usernameOrEmail, DbType.String, size: 120);
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@IsSuccess", isSuccess, DbType.Boolean);
        parameters.Add("@FailureReason", failureReason, DbType.String, size: 250);
        parameters.Add("@IpAddress", ipAddress, DbType.String, size: 45);
        parameters.Add("@UserAgent", userAgent, DbType.String, size: 500);

        await ExecuteSpAsync("sp_Auth_RecordLoginAttempt", parameters);
    }

    public async Task UpdateLoginSuccessAsync(int userId, string? ipAddress)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@IpAddress", ipAddress, DbType.String, size: 45);

        await ExecuteSpAsync("sp_Auth_UpdateUserLoginSuccess", parameters);
    }

    public async Task UpdateFailedLoginAsync(int userId, int failedCount, DateTime? lockoutEndUtc)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@FailedLoginCount", failedCount, DbType.Int32);
        parameters.Add("@LockoutEndUtc", lockoutEndUtc, DbType.DateTime2);

        await ExecuteSpAsync("sp_Auth_UpdateUserLockout", parameters);
    }

    public async Task UpdatePasswordAsync(int userId, string newPasswordHash, int modifiedBy)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@PasswordHash", newPasswordHash, DbType.String, size: 500);
        parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

        await ExecuteSpAsync("sp_Auth_UpdatePassword", parameters);
    }

    public async Task<bool> HasAnyAdminUserAsync()
    {
        var count = await ExecuteScalarSpAsync<int>("sp_Auth_HasAnyAdminUser");
        return count > 0;
    }

    public async Task<int> CreateInitialAdminAsync(string username, string email, string fullName, string passwordHash, string ipAddress)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Username", username, DbType.String, size: 60);
        parameters.Add("@Email", email, DbType.String, size: 120);
        parameters.Add("@FullName", fullName, DbType.String, size: 120);
        parameters.Add("@PasswordHash", passwordHash, DbType.String, size: 500);
        parameters.Add("@IpAddress", ipAddress, DbType.String, size: 45);
        parameters.Add("@NewUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync(
                "sp_Auth_CreateInitialAdmin",
                parameters,
                transaction: tx,
                commandType: CommandType.StoredProcedure);
        });

        return parameters.Get<int>("@NewUserId");
    }
}
