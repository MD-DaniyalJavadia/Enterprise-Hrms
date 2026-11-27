USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 15: Performance Covering Indexes, System Health & Maintenance Procedures
-- ============================================================================

-- 1. Covering Nonclustered Indexes for High-Volume Queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Employees_Manager_Active' AND object_id = OBJECT_ID(N'dbo.tbl_Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Employees_Manager_Active
        ON dbo.tbl_Employees (CompanyId, ReportingManagerId, IsActive)
        INCLUDE (FirstName, LastName, DepartmentId, DesignationId)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Attendance_Emp_Date' AND object_id = OBJECT_ID(N'dbo.tbl_Attendance'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Attendance_Emp_Date
        ON dbo.tbl_Attendance (CompanyId, EmployeeId, AttendanceDate DESC)
        INCLUDE (Status, LateMinutes, OvertimeMinutes)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_LeaveApplications_Emp_Status' AND object_id = OBJECT_ID(N'dbo.tbl_LeaveApplications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_LeaveApplications_Emp_Status
        ON dbo.tbl_LeaveApplications (CompanyId, EmployeeId, ApprovalStatus, StartDate DESC)
        INCLUDE (LeaveTypeId, TotalDays)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Payslips_Emp_Run' AND object_id = OBJECT_ID(N'dbo.tbl_Payslips'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_Payslips_Emp_Run
        ON dbo.tbl_Payslips (CompanyId, EmployeeId, PayrollRunId DESC)
        INCLUDE (GrossPay, TotalDeductions, NetPay, PaymentStatus)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_AuditLogs_Module_Action_Date' AND object_id = OBJECT_ID(N'dbo.tbl_AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_AuditLogs_Module_Action_Date
        ON dbo.tbl_AuditLogs (CompanyId, ModuleName, ActionType, CreatedDate DESC)
        INCLUDE (Username, EntityName, EntityId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_LoginHistory_AttemptedAt_Success' AND object_id = OBJECT_ID(N'dbo.tbl_LoginHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_LoginHistory_AttemptedAt_Success
        ON dbo.tbl_LoginHistory (AttemptedAtUtc DESC, IsSuccess)
        INCLUDE (UserId, UsernameOrEmail, IpAddress);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_ExpenseClaims_Company_Status' AND object_id = OBJECT_ID(N'dbo.tbl_ExpenseClaims'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_ExpenseClaims_Company_Status
        ON dbo.tbl_ExpenseClaims (CompanyId, ClaimStatus, ExpenseDate DESC)
        INCLUDE (EmployeeId, ClaimAmount, ApprovedAmount)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_AssetAllocations_Employee_Status' AND object_id = OBJECT_ID(N'dbo.tbl_AssetAllocations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_AssetAllocations_Employee_Status
        ON dbo.tbl_AssetAllocations (CompanyId, EmployeeId, AllocationStatus)
        INCLUDE (AssetId, AssignedDate)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_SeparationRequests_Company_Status' AND object_id = OBJECT_ID(N'dbo.tbl_SeparationRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_SeparationRequests_Company_Status
        ON dbo.tbl_SeparationRequests (CompanyId, SeparationStatus, RequestedLastWorkingDay)
        INCLUDE (EmployeeId, SeparationNumber, SeparationType)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_WorkflowInstances_Workflow_Status' AND object_id = OBJECT_ID(N'dbo.tbl_WorkflowInstances'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_WorkflowInstances_Workflow_Status
        ON dbo.tbl_WorkflowInstances (CompanyId, WorkflowId, InstanceStatus, DueAtUtc);
END
GO

-- ============================================================================
-- 2. sp_System_HealthCheck
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_System_HealthCheck
    @CompanyId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TableCount INT = (SELECT COUNT(1) FROM sys.tables WHERE is_ms_shipped = 0);
    DECLARE @SpCount    INT = (SELECT COUNT(1) FROM sys.procedures WHERE is_ms_shipped = 0 AND name LIKE N'sp[_]%');

    SELECT
        N'Healthy'                                           AS [Status],
        DB_NAME()                                            AS DatabaseName,
        CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(60)) AS ProductVersion,
        CAST(SERVERPROPERTY('Edition') AS NVARCHAR(120))     AS Edition,
        SYSUTCDATETIME()                                     AS UtcTimestamp,
        @TableCount                                          AS TableCount,
        @SpCount                                             AS StoredProcedureCount,
        (SELECT COUNT(1) FROM dbo.tbl_Companies WHERE IsActive = 1 AND IsDeleted = 0) AS ActiveCompanies,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND IsActive = 1 AND IsDeleted = 0)
             ELSE 0 END AS ActiveEmployees,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_Users WHERE CompanyId = @CompanyId AND IsActive = 1 AND IsDeleted = 0)
             ELSE 0 END AS ActiveUsers,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_WorkflowInstances WHERE CompanyId = @CompanyId AND InstanceStatus IN (N'InProgress', N'Escalated'))
             ELSE 0 END AS PendingWorkflows,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_Notifications WHERE CompanyId = @CompanyId AND IsRead = 0 AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > SYSUTCDATETIME()))
             ELSE 0 END AS UnreadNotifications,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_AuditLogs WHERE CompanyId = @CompanyId)
             ELSE 0 END AS AuditLogCount,
        CASE WHEN @CompanyId > 0
             THEN (SELECT COUNT(1) FROM dbo.tbl_LoginHistory lh INNER JOIN dbo.tbl_Users u ON lh.UserId = u.UserId WHERE u.CompanyId = @CompanyId)
             ELSE 0 END AS LoginHistoryCount;
END
GO

-- ============================================================================
-- 3. sp_System_MaintenanceCleanup
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_System_MaintenanceCleanup
    @CompanyId                 INT,
    @NotificationRetentionDays INT = 90,
    @LoginHistoryRetentionDays INT = 180,
    @ActorUserId               INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NotificationRetentionDays < 7
        THROW 50151, N'Notification retention period must be at least 7 days.', 1;

    IF @LoginHistoryRetentionDays < 30
        THROW 50152, N'Login history retention period must be at least 30 days.', 1;

    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @NotifCutoff DATETIME2(0) = DATEADD(DAY, -@NotificationRetentionDays, @NowUtc);
    DECLARE @LoginCutoff DATETIME2(0) = DATEADD(DAY, -@LoginHistoryRetentionDays, @NowUtc);

    DECLARE @DeletedNotifications INT = 0;
    DECLARE @DeletedLoginHistory INT = 0;

    BEGIN TRAN;

    DELETE FROM dbo.tbl_Notifications
    WHERE CompanyId = @CompanyId
      AND (
            (ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc < @NowUtc)
         OR (IsRead = 1 AND CreatedDate < @NotifCutoff)
      );
    SET @DeletedNotifications = @@ROWCOUNT;

    DELETE lh
    FROM dbo.tbl_LoginHistory lh
    INNER JOIN dbo.tbl_Users u ON lh.UserId = u.UserId
    WHERE u.CompanyId = @CompanyId
      AND lh.AttemptedAtUtc < @LoginCutoff;
    SET @DeletedLoginHistory = @@ROWCOUNT;

    COMMIT TRAN;

    SELECT
        @DeletedNotifications AS DeletedNotifications,
        @DeletedLoginHistory  AS DeletedLoginHistoryRows,
        @NowUtc               AS ExecutedAtUtc;
END
GO
