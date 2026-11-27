USE [HRMS_DB];
GO

-- ============================================================================
-- sp_Auth_GetUserForLogin
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserForLogin
    @UsernameOrEmail NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.UserId,
        u.CompanyId,
        c.CompanyName,
        u.BranchId,
        b.BranchName,
        u.EmployeeId,
        u.Username,
        u.Email,
        u.FullName,
        u.PasswordHash,
        u.IsActive,
        u.IsDeleted,
        u.MustChangePassword,
        u.FailedLoginCount,
        u.LockoutEndUtc
    FROM dbo.tbl_Users u
    INNER JOIN dbo.tbl_Companies c ON u.CompanyId = c.CompanyId
    LEFT JOIN dbo.tbl_Branches b ON u.BranchId = b.BranchId
    WHERE (u.Username = @UsernameOrEmail OR u.Email = @UsernameOrEmail)
      AND u.IsDeleted = 0
      AND c.IsDeleted = 0;
END
GO

-- ============================================================================
-- sp_Auth_GetUserById
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.UserId,
        u.CompanyId,
        c.CompanyName,
        u.BranchId,
        b.BranchName,
        u.EmployeeId,
        u.Username,
        u.Email,
        u.FullName,
        u.PasswordHash,
        u.IsActive,
        u.IsDeleted,
        u.MustChangePassword,
        u.FailedLoginCount,
        u.LockoutEndUtc
    FROM dbo.tbl_Users u
    INNER JOIN dbo.tbl_Companies c ON u.CompanyId = c.CompanyId
    LEFT JOIN dbo.tbl_Branches b ON u.BranchId = b.BranchId
    WHERE u.UserId = @UserId
      AND u.IsDeleted = 0;
END
GO

-- ============================================================================
-- sp_Auth_GetUserRolesAndPermissions
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserRolesAndPermissions
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT r.RoleCode
    FROM dbo.tbl_UserRoles ur
    INNER JOIN dbo.tbl_Roles r ON ur.RoleId = r.RoleId
    WHERE ur.UserId = @UserId
      AND r.IsActive = 1
      AND r.IsDeleted = 0;

    SELECT DISTINCT p.PermissionCode
    FROM dbo.tbl_UserRoles ur
    INNER JOIN dbo.tbl_Roles r ON ur.RoleId = r.RoleId
    INNER JOIN dbo.tbl_RolePermissions rp ON r.RoleId = rp.RoleId
    INNER JOIN dbo.tbl_Permissions p ON rp.PermissionId = p.PermissionId
    WHERE ur.UserId = @UserId
      AND r.IsActive = 1
      AND r.IsDeleted = 0;
END
GO

-- ============================================================================
-- sp_Auth_RecordLoginAttempt
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_RecordLoginAttempt
    @UsernameOrEmail NVARCHAR(120),
    @UserId          INT = NULL,
    @IsSuccess       BIT,
    @FailureReason   NVARCHAR(250) = NULL,
    @IpAddress       NVARCHAR(45) = NULL,
    @UserAgent       NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tbl_LoginHistory (
        UserId, UsernameOrEmail, IsSuccess, FailureReason, IpAddress, UserAgent, AttemptedAtUtc
    )
    VALUES (
        @UserId, @UsernameOrEmail, @IsSuccess, @FailureReason, @IpAddress, @UserAgent, SYSUTCDATETIME()
    );
END
GO

-- ============================================================================
-- sp_Auth_UpdateUserLoginSuccess
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_UpdateUserLoginSuccess
    @UserId    INT,
    @IpAddress NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Users
    SET FailedLoginCount = 0,
        LockoutEndUtc = NULL,
        LastLoginUtc = SYSUTCDATETIME(),
        LastLoginIp = @IpAddress,
        ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId;
END
GO

-- ============================================================================
-- sp_Auth_UpdateUserLockout
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_UpdateUserLockout
    @UserId           INT,
    @FailedLoginCount INT,
    @LockoutEndUtc    DATETIME2(0) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Users
    SET FailedLoginCount = @FailedLoginCount,
        LockoutEndUtc = @LockoutEndUtc,
        ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId;
END
GO

-- ============================================================================
-- sp_Auth_UpdatePassword
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_UpdatePassword
    @UserId       INT,
    @PasswordHash NVARCHAR(500),
    @ModifiedBy   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Users
    SET PasswordHash = @PasswordHash,
        MustChangePassword = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId
      AND IsDeleted = 0;
END
GO

-- ============================================================================
-- sp_Auth_HasAnyAdminUser
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_HasAnyAdminUser
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1)
    FROM dbo.tbl_Users u
    INNER JOIN dbo.tbl_UserRoles ur ON u.UserId = ur.UserId
    INNER JOIN dbo.tbl_Roles r ON ur.RoleId = r.RoleId
    WHERE r.RoleCode = N'SuperAdmin'
      AND u.IsDeleted = 0
      AND u.IsActive = 1;
END
GO

-- ============================================================================
-- sp_Auth_CreateInitialAdmin
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Auth_CreateInitialAdmin
    @Username     NVARCHAR(60),
    @Email        NVARCHAR(120),
    @FullName     NVARCHAR(120),
    @PasswordHash NVARCHAR(500),
    @IpAddress    NVARCHAR(45) = NULL,
    @NewUserId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CompanyId INT;
    DECLARE @BranchId INT;
    DECLARE @SuperAdminRoleId INT;

    SELECT TOP 1 @CompanyId = CompanyId
    FROM dbo.tbl_Companies
    WHERE IsDeleted = 0 AND IsActive = 1
    ORDER BY CompanyId;

    IF @CompanyId IS NULL
    BEGIN
        THROW 50001, 'Default company must exist before creating an initial administrator.', 1;
    END

    SELECT TOP 1 @BranchId = BranchId
    FROM dbo.tbl_Branches
    WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ORDER BY IsHeadOffice DESC, BranchId;

    SELECT TOP 1 @SuperAdminRoleId = RoleId
    FROM dbo.tbl_Roles
    WHERE CompanyId = @CompanyId AND RoleCode = N'SuperAdmin' AND IsDeleted = 0;

    IF @SuperAdminRoleId IS NULL
    BEGIN
        THROW 50002, 'SuperAdmin role does not exist.', 1;
    END

    INSERT INTO dbo.tbl_Users (
        CompanyId, BranchId, Username, Email, PasswordHash, FullName,
        MustChangePassword, IsActive, IsDeleted, LastLoginIp, CreatedDate
    )
    VALUES (
        @CompanyId, @BranchId, @Username, @Email, @PasswordHash, @FullName,
        0, 1, 0, @IpAddress, SYSUTCDATETIME()
    );

    SET @NewUserId = SCOPE_IDENTITY();

    INSERT INTO dbo.tbl_UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
    VALUES (@NewUserId, @SuperAdminRoleId, @NewUserId, SYSUTCDATETIME());

    INSERT INTO dbo.tbl_Notifications (
        CompanyId, UserId, Title, Message, NotificationType, ActionUrl, IsRead, CreatedDate
    )
    VALUES (
        @CompanyId, @NewUserId,
        N'Welcome to Enterprise HRMS',
        N'Your Super Administrator account is active. Review System Settings and Organization details.',
        N'Success', N'/Dashboard', 0, SYSUTCDATETIME()
    );
END
GO
