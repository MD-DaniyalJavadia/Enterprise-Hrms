USE [HRMS_DB];
GO

-- ============================================================================
-- sp_AuditLog_Insert
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_AuditLog_Insert
    @CompanyId   INT,
    @UserId      INT = NULL,
    @Username    NVARCHAR(60) = NULL,
    @ActionType  NVARCHAR(50),
    @ModuleName  NVARCHAR(80),
    @EntityName  NVARCHAR(80),
    @EntityId    NVARCHAR(50) = NULL,
    @Description NVARCHAR(500) = NULL,
    @OldValues   NVARCHAR(MAX) = NULL,
    @NewValues   NVARCHAR(MAX) = NULL,
    @IpAddress   NVARCHAR(45) = NULL,
    @UserAgent   NVARCHAR(500) = NULL,
    @AuditLogId  BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tbl_AuditLogs (
        CompanyId, UserId, Username, ActionType, ModuleName, EntityName, EntityId,
        Description, OldValues, NewValues, IpAddress, UserAgent, CreatedDate
    )
    VALUES (
        @CompanyId, @UserId, @Username, @ActionType, @ModuleName, @EntityName, @EntityId,
        @Description, @OldValues, @NewValues, @IpAddress, @UserAgent, SYSUTCDATETIME()
    );

    SET @AuditLogId = SCOPE_IDENTITY();
END
GO

-- ============================================================================
-- sp_AuditLog_GetRecent
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_AuditLog_GetRecent
    @CompanyId INT,
    @TopCount  INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopCount)
        AuditLogId,
        CompanyId,
        UserId,
        Username,
        ActionType,
        ModuleName,
        EntityName,
        EntityId,
        Description,
        IpAddress,
        CreatedDate
    FROM dbo.tbl_AuditLogs
    WHERE CompanyId = @CompanyId
    ORDER BY CreatedDate DESC, AuditLogId DESC;
END
GO

-- ============================================================================
-- sp_Notification_GetByUserId
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Notification_GetByUserId
    @CompanyId INT,
    @UserId    INT,
    @TopCount  INT = 15
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopCount)
        NotificationId,
        CompanyId,
        UserId,
        Title,
        Message,
        NotificationType,
        ActionUrl,
        IsRead,
        CreatedDate,
        ReadDate
    FROM dbo.tbl_Notifications
    WHERE CompanyId = @CompanyId
      AND UserId = @UserId
    ORDER BY IsRead ASC, CreatedDate DESC;
END
GO

-- ============================================================================
-- sp_Notification_MarkAsRead
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Notification_MarkAsRead
    @NotificationId BIGINT,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Notifications
    SET IsRead = 1,
        ReadDate = SYSUTCDATETIME()
    WHERE NotificationId = @NotificationId
      AND UserId = @UserId
      AND IsRead = 0;
END
GO

-- ============================================================================
-- sp_Notification_MarkAllAsRead
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Notification_MarkAllAsRead
    @CompanyId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Notifications
    SET IsRead = 1,
        ReadDate = SYSUTCDATETIME()
    WHERE CompanyId = @CompanyId
      AND UserId = @UserId
      AND IsRead = 0;
END
GO
