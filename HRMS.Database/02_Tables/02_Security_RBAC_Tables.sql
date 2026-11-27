USE [HRMS_DB];
GO

-- ============================================================================
-- 1. tbl_Roles
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Roles (
        RoleId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Roles PRIMARY KEY,
        CompanyId    INT           NOT NULL,
        RoleCode     NVARCHAR(40)  NOT NULL,
        RoleName     NVARCHAR(80)  NOT NULL,
        Description  NVARCHAR(250) NULL,
        IsSystemRole BIT           NOT NULL CONSTRAINT DF_tbl_Roles_IsSystemRole DEFAULT (0),
        IsActive     BIT           NOT NULL CONSTRAINT DF_tbl_Roles_IsActive DEFAULT (1),
        IsDeleted    BIT           NOT NULL CONSTRAINT DF_tbl_Roles_IsDeleted DEFAULT (0),
        CreatedBy    INT           NULL,
        CreatedDate  DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Roles_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy   INT           NULL,
        ModifiedDate DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Roles_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_Roles_Company_Code UNIQUE (CompanyId, RoleCode)
    );
END
GO

-- ============================================================================
-- 2. tbl_Permissions
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Permissions (
        PermissionId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Permissions PRIMARY KEY,
        PermissionCode NVARCHAR(80)  NOT NULL,
        ModuleName     NVARCHAR(60)  NOT NULL,
        PageName       NVARCHAR(60)  NOT NULL,
        ActionName     NVARCHAR(40)  NOT NULL,
        Description    NVARCHAR(250) NULL,
        CONSTRAINT UQ_tbl_Permissions_Code UNIQUE (PermissionCode)
    );
END
GO

-- ============================================================================
-- 3. tbl_RolePermissions
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_RolePermissions (
        RolePermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_RolePermissions PRIMARY KEY,
        RoleId           INT NOT NULL,
        PermissionId     INT NOT NULL,
        CreatedBy        INT NULL,
        CreatedDate      DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_RolePermissions_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES dbo.tbl_Roles(RoleId),
        CONSTRAINT FK_tbl_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.tbl_Permissions(PermissionId),
        CONSTRAINT UQ_tbl_RolePermissions_Pair UNIQUE (RoleId, PermissionId)
    );
END
GO

-- ============================================================================
-- 4. tbl_Users
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Users (
        UserId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Users PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        BranchId           INT           NULL,
        EmployeeId         INT           NULL,
        Username           NVARCHAR(60)  NOT NULL,
        Email              NVARCHAR(120) NOT NULL,
        PasswordHash       NVARCHAR(500) NOT NULL,
        FullName           NVARCHAR(120) NOT NULL,
        MustChangePassword BIT           NOT NULL CONSTRAINT DF_tbl_Users_MustChangePassword DEFAULT (0),
        FailedLoginCount   INT           NOT NULL CONSTRAINT DF_tbl_Users_FailedCount DEFAULT (0),
        LockoutEndUtc      DATETIME2(0)  NULL,
        LastLoginUtc       DATETIME2(0)  NULL,
        LastLoginIp        NVARCHAR(45)  NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_tbl_Users_IsActive DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_Users_IsDeleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Users_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Users_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Users_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_Users_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_tbl_Users_Email UNIQUE (Email)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Users_CompanyId ON dbo.tbl_Users(CompanyId) WHERE IsDeleted = 0;
END
GO

-- ============================================================================
-- 5. tbl_UserRoles
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_UserRoles (
        UserRoleId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_UserRoles PRIMARY KEY,
        UserId      INT NOT NULL,
        RoleId      INT NOT NULL,
        CreatedBy   INT NULL,
        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_UserRoles_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users(UserId),
        CONSTRAINT FK_tbl_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.tbl_Roles(RoleId),
        CONSTRAINT UQ_tbl_UserRoles_Pair UNIQUE (UserId, RoleId)
    );
END
GO

-- ============================================================================
-- 6. tbl_LoginHistory
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_LoginHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LoginHistory (
        LoginHistoryId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LoginHistory PRIMARY KEY,
        UserId          INT           NULL,
        UsernameOrEmail NVARCHAR(120) NOT NULL,
        IsSuccess       BIT           NOT NULL,
        FailureReason   NVARCHAR(250) NULL,
        IpAddress       NVARCHAR(45)  NULL,
        UserAgent       NVARCHAR(500) NULL,
        AttemptedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_LoginHistory_AttemptedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE NONCLUSTERED INDEX IX_tbl_LoginHistory_User_Date ON dbo.tbl_LoginHistory(UserId, AttemptedAtUtc DESC);
END
GO

-- ============================================================================
-- 7. tbl_AuditLogs
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AuditLogs (
        AuditLogId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AuditLogs PRIMARY KEY,
        CompanyId   INT           NOT NULL,
        UserId      INT           NULL,
        Username    NVARCHAR(60)  NULL,
        ActionType  NVARCHAR(50)  NOT NULL,
        ModuleName  NVARCHAR(80)  NOT NULL,
        EntityName  NVARCHAR(80)  NOT NULL,
        EntityId    NVARCHAR(50)  NULL,
        Description NVARCHAR(500) NULL,
        OldValues   NVARCHAR(MAX) NULL,
        NewValues   NVARCHAR(MAX) NULL,
        IpAddress   NVARCHAR(45)  NULL,
        UserAgent   NVARCHAR(500) NULL,
        CreatedDate DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_AuditLogs_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_AuditLogs_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_AuditLogs_Company_Date ON dbo.tbl_AuditLogs(CompanyId, CreatedDate DESC);
END
GO

-- ============================================================================
-- 8. tbl_Notifications
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Notifications (
        NotificationId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Notifications PRIMARY KEY,
        CompanyId        INT           NOT NULL,
        UserId           INT           NOT NULL,
        Title            NVARCHAR(150) NOT NULL,
        Message          NVARCHAR(500) NOT NULL,
        NotificationType NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_Notifications_Type DEFAULT ('Info'),
        ActionUrl        NVARCHAR(260) NULL,
        IsRead           BIT           NOT NULL CONSTRAINT DF_tbl_Notifications_IsRead DEFAULT (0),
        CreatedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Notifications_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ReadDate         DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Notifications_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.tbl_Users(UserId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Notifications_User_Unread ON dbo.tbl_Notifications(CompanyId, UserId, IsRead, CreatedDate DESC);
END
GO

-- ============================================================================
-- 9. tbl_SystemSettings
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SystemSettings (
        SettingId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_SystemSettings PRIMARY KEY,
        CompanyId    INT           NOT NULL,
        SettingKey   NVARCHAR(80)  NOT NULL,
        SettingValue NVARCHAR(500) NOT NULL,
        Description  NVARCHAR(250) NULL,
        ModifiedBy   INT           NULL,
        ModifiedDate DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_SystemSettings_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_SystemSettings_Company_Key UNIQUE (CompanyId, SettingKey)
    );
END
GO
