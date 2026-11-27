USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 14: Notifications, Workflow Engine, Audit Logs & System Settings SPs
-- ============================================================================

-- 1. Notification Inbox & Preferences
CREATE OR ALTER PROCEDURE dbo.sp_Notification_GetInbox
    @CompanyId        INT,
    @UserId           INT,
    @FilterRead       BIT = NULL,
    @NotificationType NVARCHAR(20) = NULL,
    @ModuleName       NVARCHAR(60) = NULL,
    @Search           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        NotificationId,
        CompanyId,
        UserId,
        Title,
        Message,
        NotificationType,
        ISNULL(ModuleName, N'General') AS ModuleName,
        Priority,
        ActionUrl,
        IsRead,
        CreatedDate,
        ReadDate,
        ExpiresAtUtc
    FROM dbo.tbl_Notifications
    WHERE CompanyId = @CompanyId
      AND UserId = @UserId
      AND (@FilterRead IS NULL OR IsRead = @FilterRead)
      AND (@NotificationType IS NULL OR @NotificationType = N'' OR NotificationType = @NotificationType)
      AND (@ModuleName IS NULL OR @ModuleName = N'' OR ISNULL(ModuleName, N'General') = @ModuleName)
      AND (@Search IS NULL OR @Search = N'' OR Title LIKE N'%' + @Search + N'%' OR Message LIKE N'%' + @Search + N'%')
      AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > SYSUTCDATETIME())
    ORDER BY IsRead ASC, CreatedDate DESC, NotificationId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Notification_SendBroadcast
    @CompanyId          INT,
    @Title              NVARCHAR(150),
    @Message            NVARCHAR(500),
    @NotificationType   NVARCHAR(20) = N'Info',
    @Priority           NVARCHAR(20) = N'Normal',
    @ModuleName         NVARCHAR(60) = N'System',
    @ActionUrl          NVARCHAR(260) = NULL,
    @TargetScope        NVARCHAR(30) = N'AllUsers', -- AllUsers, Role, Department, SpecificUser
    @TargetRoleId       INT = NULL,
    @TargetDepartmentId INT = NULL,
    @TargetUserId       INT = NULL,
    @SenderUserId       INT,
    @RecipientsInserted INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH TargetUsers AS (
        SELECT DISTINCT u.UserId
        FROM dbo.tbl_Users u
        LEFT JOIN dbo.tbl_UserRoles ur ON u.UserId = ur.UserId
        LEFT JOIN dbo.tbl_Employees e ON u.EmployeeId = e.EmployeeId
        WHERE u.CompanyId = @CompanyId
          AND u.IsActive = 1
          AND u.IsDeleted = 0
          AND (
              @TargetScope = N'AllUsers'
              OR (@TargetScope = N'Role' AND @TargetRoleId IS NOT NULL AND ur.RoleId = @TargetRoleId)
              OR (@TargetScope = N'Department' AND @TargetDepartmentId IS NOT NULL AND e.DepartmentId = @TargetDepartmentId AND e.IsDeleted = 0)
              OR (@TargetScope = N'SpecificUser' AND @TargetUserId IS NOT NULL AND u.UserId = @TargetUserId)
          )
    )
    INSERT INTO dbo.tbl_Notifications (
        CompanyId, UserId, Title, Message, NotificationType, ModuleName, Priority, ActionUrl, IsRead, CreatedBy, CreatedDate
    )
    SELECT
        @CompanyId, t.UserId, @Title, @Message, @NotificationType, @ModuleName, @Priority, @ActionUrl, 0, @SenderUserId, SYSUTCDATETIME()
    FROM TargetUsers t;

    SET @RecipientsInserted = @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Notification_Delete
    @CompanyId      INT,
    @NotificationId BIGINT,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.tbl_Notifications
    WHERE NotificationId = @NotificationId
      AND CompanyId = @CompanyId
      AND UserId = @UserId;
END
GO

-- 2. Notification Templates
CREATE OR ALTER PROCEDURE dbo.sp_NotificationTemplate_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        TemplateId, CompanyId, TemplateCode, TemplateName, ModuleName, EventTrigger,
        SubjectTemplate, BodyTemplate, DefaultType, DefaultPriority, SendInApp, SendEmailDigest,
        IsActive, CreatedDate
    FROM dbo.tbl_NotificationTemplates
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY ModuleName, TemplateName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_NotificationTemplate_Save
    @TemplateId      INT,
    @CompanyId       INT,
    @TemplateCode    NVARCHAR(60),
    @TemplateName    NVARCHAR(120),
    @ModuleName      NVARCHAR(60),
    @EventTrigger    NVARCHAR(80),
    @SubjectTemplate NVARCHAR(180),
    @BodyTemplate    NVARCHAR(600),
    @DefaultType     NVARCHAR(20),
    @DefaultPriority NVARCHAR(20),
    @SendInApp       BIT,
    @SendEmailDigest BIT,
    @IsActive        BIT,
    @UserId          INT,
    @SavedId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_NotificationTemplates
        WHERE CompanyId = @CompanyId AND TemplateCode = @TemplateCode AND TemplateId <> @TemplateId AND IsDeleted = 0
    )
    BEGIN
        THROW 50201, N'A notification template with this code already exists.', 1;
    END

    IF @TemplateId = 0
    BEGIN
        INSERT INTO dbo.tbl_NotificationTemplates (
            CompanyId, TemplateCode, TemplateName, ModuleName, EventTrigger,
            SubjectTemplate, BodyTemplate, DefaultType, DefaultPriority,
            SendInApp, SendEmailDigest, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @TemplateCode, @TemplateName, @ModuleName, @EventTrigger,
            @SubjectTemplate, @BodyTemplate, @DefaultType, @DefaultPriority,
            @SendInApp, @SendEmailDigest, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_NotificationTemplates
        SET TemplateCode = @TemplateCode,
            TemplateName = @TemplateName,
            ModuleName = @ModuleName,
            EventTrigger = @EventTrigger,
            SubjectTemplate = @SubjectTemplate,
            BodyTemplate = @BodyTemplate,
            DefaultType = @DefaultType,
            DefaultPriority = @DefaultPriority,
            SendInApp = @SendInApp,
            SendEmailDigest = @SendEmailDigest,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50202, N'Notification template not found.', 1;
        SET @SavedId = @TemplateId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_NotificationTemplate_Delete
    @CompanyId  INT,
    @TemplateId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_NotificationTemplates
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- 3. Workflow Definitions & Steps
CREATE OR ALTER PROCEDURE dbo.sp_WorkflowDefinition_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Definitions
    SELECT
        w.WorkflowId,
        w.CompanyId,
        w.WorkflowCode,
        w.WorkflowName,
        w.ModuleName,
        w.DepartmentId,
        d.DepartmentName,
        w.MinAmountThreshold,
        w.AutoEscalateHours,
        w.Description,
        w.IsActive,
        w.CreatedDate,
        (SELECT COUNT(1) FROM dbo.tbl_WorkflowSteps s WHERE s.WorkflowId = w.WorkflowId AND s.IsDeleted = 0) AS StepCount
    FROM dbo.tbl_WorkflowDefinitions w
    LEFT JOIN dbo.tbl_Departments d ON w.DepartmentId = d.DepartmentId
    WHERE w.CompanyId = @CompanyId AND w.IsDeleted = 0
    ORDER BY w.ModuleName, w.WorkflowName;

    -- Result Set 2: Steps
    SELECT
        s.WorkflowStepId,
        s.CompanyId,
        s.WorkflowId,
        s.StepOrder,
        s.StepName,
        s.ApproverType,
        s.ApproverRoleId,
        r.RoleName AS ApproverRoleName,
        s.ApproverEmployeeId,
        e.FullName AS ApproverEmployeeName,
        s.EscalationRoleId,
        er.RoleName AS EscalationRoleName,
        s.SlaHours,
        s.CanDelegate,
        s.IsMandatory
    FROM dbo.tbl_WorkflowSteps s
    INNER JOIN dbo.tbl_WorkflowDefinitions w ON s.WorkflowId = w.WorkflowId
    LEFT JOIN dbo.tbl_Roles r ON s.ApproverRoleId = r.RoleId
    LEFT JOIN dbo.tbl_Employees e ON s.ApproverEmployeeId = e.EmployeeId
    LEFT JOIN dbo.tbl_Roles er ON s.EscalationRoleId = er.RoleId
    WHERE s.CompanyId = @CompanyId AND s.IsDeleted = 0 AND w.IsDeleted = 0
    ORDER BY s.WorkflowId, s.StepOrder;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowDefinition_Save
    @WorkflowId         INT,
    @CompanyId          INT,
    @WorkflowCode       NVARCHAR(40),
    @WorkflowName       NVARCHAR(120),
    @ModuleName         NVARCHAR(50),
    @DepartmentId       INT = NULL,
    @MinAmountThreshold DECIMAL(18,2) = NULL,
    @AutoEscalateHours  INT = 48,
    @Description        NVARCHAR(300) = NULL,
    @IsActive           BIT = 1,
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_WorkflowDefinitions
        WHERE CompanyId = @CompanyId AND WorkflowCode = @WorkflowCode AND WorkflowId <> @WorkflowId AND IsDeleted = 0
    )
    BEGIN
        THROW 50211, N'A workflow definition with this code already exists.', 1;
    END

    IF @WorkflowId = 0
    BEGIN
        INSERT INTO dbo.tbl_WorkflowDefinitions (
            CompanyId, WorkflowCode, WorkflowName, ModuleName, DepartmentId,
            MinAmountThreshold, AutoEscalateHours, Description, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @WorkflowCode, @WorkflowName, @ModuleName, @DepartmentId,
            @MinAmountThreshold, @AutoEscalateHours, @Description, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_WorkflowDefinitions
        SET WorkflowCode = @WorkflowCode,
            WorkflowName = @WorkflowName,
            ModuleName = @ModuleName,
            DepartmentId = @DepartmentId,
            MinAmountThreshold = @MinAmountThreshold,
            AutoEscalateHours = @AutoEscalateHours,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE WorkflowId = @WorkflowId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50212, N'Workflow definition not found.', 1;
        SET @SavedId = @WorkflowId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowDefinition_Delete
    @CompanyId  INT,
    @WorkflowId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM dbo.tbl_WorkflowInstances
        WHERE CompanyId = @CompanyId AND WorkflowId = @WorkflowId AND InstanceStatus IN (N'InProgress', N'Escalated')
    )
    BEGIN
        THROW 50213, N'Cannot delete a workflow definition with active in-progress instances.', 1;
    END

    UPDATE dbo.tbl_WorkflowDefinitions
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE WorkflowId = @WorkflowId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowStep_Save
    @WorkflowStepId     INT,
    @CompanyId          INT,
    @WorkflowId         INT,
    @StepOrder          INT,
    @StepName           NVARCHAR(100),
    @ApproverType       NVARCHAR(30),
    @ApproverRoleId     INT = NULL,
    @ApproverEmployeeId INT = NULL,
    @EscalationRoleId   INT = NULL,
    @SlaHours           INT = 24,
    @CanDelegate        BIT = 1,
    @IsMandatory        BIT = 1,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_WorkflowDefinitions WHERE WorkflowId = @WorkflowId AND CompanyId = @CompanyId AND IsDeleted = 0)
    BEGIN
        THROW 50214, N'Parent workflow definition not found.', 1;
    END

    IF @WorkflowStepId = 0
    BEGIN
        INSERT INTO dbo.tbl_WorkflowSteps (
            CompanyId, WorkflowId, StepOrder, StepName, ApproverType,
            ApproverRoleId, ApproverEmployeeId, EscalationRoleId, SlaHours, CanDelegate, IsMandatory
        )
        VALUES (
            @CompanyId, @WorkflowId, @StepOrder, @StepName, @ApproverType,
            @ApproverRoleId, @ApproverEmployeeId, @EscalationRoleId, @SlaHours, @CanDelegate, @IsMandatory
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_WorkflowSteps
        SET StepOrder = @StepOrder,
            StepName = @StepName,
            ApproverType = @ApproverType,
            ApproverRoleId = @ApproverRoleId,
            ApproverEmployeeId = @ApproverEmployeeId,
            EscalationRoleId = @EscalationRoleId,
            SlaHours = @SlaHours,
            CanDelegate = @CanDelegate,
            IsMandatory = @IsMandatory
        WHERE WorkflowStepId = @WorkflowStepId AND WorkflowId = @WorkflowId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50215, N'Workflow step not found.', 1;
        SET @SavedId = @WorkflowStepId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowStep_Delete
    @CompanyId      INT,
    @WorkflowStepId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_WorkflowSteps
    SET IsDeleted = 1
    WHERE WorkflowStepId = @WorkflowStepId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- 4. Workflow Instances & Action Transitions
CREATE OR ALTER PROCEDURE dbo.sp_WorkflowInstance_GetAll
    @CompanyId      INT,
    @ModuleName     NVARCHAR(50) = NULL,
    @InstanceStatus NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Workflow Instances
    SELECT TOP 150
        wi.InstanceId,
        wi.CompanyId,
        wi.WorkflowId,
        wd.WorkflowName,
        wd.WorkflowCode,
        wi.ModuleName,
        wi.EntityId,
        wi.ReferenceNumber,
        wi.RequestedByUserId,
        ISNULL(u.FullName, ISNULL(e.FullName, N'System')) AS RequesterName,
        wi.CurrentStepOrder,
        wi.TotalSteps,
        ISNULL(cs.StepName, N'Completed') AS CurrentStepName,
        wi.InstanceStatus,
        wi.DueAtUtc,
        wi.CompletedDate,
        wi.Summary,
        wi.CreatedDate
    FROM dbo.tbl_WorkflowInstances wi
    INNER JOIN dbo.tbl_WorkflowDefinitions wd ON wi.WorkflowId = wd.WorkflowId
    LEFT JOIN dbo.tbl_Users u ON wi.RequestedByUserId = u.UserId
    LEFT JOIN dbo.tbl_Employees e ON wi.RequestedByEmployeeId = e.EmployeeId
    OUTER APPLY (
        SELECT TOP 1 s.StepName
        FROM dbo.tbl_WorkflowSteps s
        WHERE s.WorkflowId = wi.WorkflowId AND s.StepOrder = wi.CurrentStepOrder AND s.IsDeleted = 0
    ) cs
    WHERE wi.CompanyId = @CompanyId
      AND (@ModuleName IS NULL OR @ModuleName = N'' OR wi.ModuleName = @ModuleName)
      AND (@InstanceStatus IS NULL OR @InstanceStatus = N'' OR wi.InstanceStatus = @InstanceStatus)
    ORDER BY
        CASE WHEN wi.InstanceStatus IN (N'InProgress', N'Escalated') THEN 0 ELSE 1 END,
        wi.CreatedDate DESC;

    -- Result Set 2: Recent Action Logs
    SELECT TOP 200
        al.ActionLogId,
        al.CompanyId,
        al.InstanceId,
        wi.ReferenceNumber,
        al.StepOrder,
        al.StepName,
        al.ActionTaken,
        al.ActorUserId,
        al.ActorName,
        al.Comments,
        al.ActionDate
    FROM dbo.tbl_WorkflowActionLogs al
    INNER JOIN dbo.tbl_WorkflowInstances wi ON al.InstanceId = wi.InstanceId
    WHERE al.CompanyId = @CompanyId
    ORDER BY al.ActionDate DESC, al.ActionLogId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowInstance_Initiate
    @CompanyId             INT,
    @WorkflowId            INT,
    @EntityId              NVARCHAR(50),
    @ReferenceNumber       NVARCHAR(60),
    @Summary               NVARCHAR(300),
    @RequestedByUserId     INT,
    @RequestedByEmployeeId INT = NULL,
    @ActorName             NVARCHAR(120),
    @NewInstanceId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ModuleName NVARCHAR(50);
    DECLARE @EscHours INT = 48;
    DECLARE @TotalSteps INT = 0;
    DECLARE @FirstStepOrder INT = 1;
    DECLARE @FirstStepName NVARCHAR(100) = N'Step 1';
    DECLARE @FirstSla INT = 24;

    SELECT
        @ModuleName = ModuleName,
        @EscHours = AutoEscalateHours
    FROM dbo.tbl_WorkflowDefinitions
    WHERE WorkflowId = @WorkflowId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1;

    IF @ModuleName IS NULL THROW 50216, N'Active workflow definition not found.', 1;

    SELECT @TotalSteps = COUNT(1)
    FROM dbo.tbl_WorkflowSteps
    WHERE WorkflowId = @WorkflowId AND IsDeleted = 0;

    IF @TotalSteps = 0 THROW 50217, N'Workflow has no configured approval steps.', 1;

    SELECT TOP 1
        @FirstStepOrder = StepOrder,
        @FirstStepName = StepName,
        @FirstSla = SlaHours
    FROM dbo.tbl_WorkflowSteps
    WHERE WorkflowId = @WorkflowId AND IsDeleted = 0
    ORDER BY StepOrder ASC;

    BEGIN TRANSACTION;

    INSERT INTO dbo.tbl_WorkflowInstances (
        CompanyId, WorkflowId, ModuleName, EntityId, ReferenceNumber,
        RequestedByUserId, RequestedByEmployeeId, CurrentStepOrder, TotalSteps,
        InstanceStatus, DueAtUtc, Summary
    )
    VALUES (
        @CompanyId, @WorkflowId, @ModuleName, @EntityId, @ReferenceNumber,
        @RequestedByUserId, @RequestedByEmployeeId, @FirstStepOrder, @TotalSteps,
        N'InProgress', DATEADD(HOUR, ISNULL(@FirstSla, @EscHours), SYSUTCDATETIME()), @Summary
    );

    SET @NewInstanceId = SCOPE_IDENTITY();

    INSERT INTO dbo.tbl_WorkflowActionLogs (
        CompanyId, InstanceId, StepOrder, StepName, ActionTaken, ActorUserId, ActorName, Comments
    )
    VALUES (
        @CompanyId, @NewInstanceId, @FirstStepOrder, @FirstStepName, N'Submitted',
        @RequestedByUserId, @ActorName, @Summary
    );

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkflowInstance_Advance
    @CompanyId   INT,
    @InstanceId  BIGINT,
    @ActionTaken NVARCHAR(30), -- Approved, Rejected, Escalated, Delegated, Cancelled
    @Comments    NVARCHAR(400) = NULL,
    @ActorUserId INT,
    @ActorName   NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @WorkflowId INT, @CurrentStepOrder INT, @CurrentStatus NVARCHAR(30),
            @RefNum NVARCHAR(60), @ReqUserId INT, @ModuleName NVARCHAR(50);

    SELECT
        @WorkflowId = WorkflowId,
        @CurrentStepOrder = CurrentStepOrder,
        @CurrentStatus = InstanceStatus,
        @RefNum = ReferenceNumber,
        @ReqUserId = RequestedByUserId,
        @ModuleName = ModuleName
    FROM dbo.tbl_WorkflowInstances
    WHERE InstanceId = @InstanceId AND CompanyId = @CompanyId;

    IF @WorkflowId IS NULL THROW 50218, N'Workflow instance not found.', 1;
    IF @CurrentStatus IN (N'Approved', N'Rejected', N'Cancelled')
        THROW 50219, N'This workflow instance is already finalized.', 1;

    DECLARE @CurrentStepName NVARCHAR(100) = N'Review Step';
    SELECT TOP 1 @CurrentStepName = StepName
    FROM dbo.tbl_WorkflowSteps
    WHERE WorkflowId = @WorkflowId AND StepOrder = @CurrentStepOrder AND IsDeleted = 0;

    BEGIN TRANSACTION;

    INSERT INTO dbo.tbl_WorkflowActionLogs (
        CompanyId, InstanceId, StepOrder, StepName, ActionTaken, ActorUserId, ActorName, Comments
    )
    VALUES (
        @CompanyId, @InstanceId, @CurrentStepOrder, @CurrentStepName, @ActionTaken, @ActorUserId, @ActorName, @Comments
    );

    IF @ActionTaken = N'Approved'
    BEGIN
        DECLARE @NextStepOrder INT = NULL, @NextSla INT = 24;
        SELECT TOP 1
            @NextStepOrder = StepOrder,
            @NextSla = SlaHours
        FROM dbo.tbl_WorkflowSteps
        WHERE WorkflowId = @WorkflowId AND StepOrder > @CurrentStepOrder AND IsDeleted = 0
        ORDER BY StepOrder ASC;

        IF @NextStepOrder IS NULL
        BEGIN
            UPDATE dbo.tbl_WorkflowInstances
            SET InstanceStatus = N'Approved',
                CompletedDate = SYSUTCDATETIME()
            WHERE InstanceId = @InstanceId;
        END
        ELSE
        BEGIN
            UPDATE dbo.tbl_WorkflowInstances
            SET CurrentStepOrder = @NextStepOrder,
                InstanceStatus = N'InProgress',
                DueAtUtc = DATEADD(HOUR, @NextSla, SYSUTCDATETIME())
            WHERE InstanceId = @InstanceId;
        END
    END
    ELSE IF @ActionTaken IN (N'Rejected', N'Cancelled')
    BEGIN
        UPDATE dbo.tbl_WorkflowInstances
        SET InstanceStatus = @ActionTaken,
            CompletedDate = SYSUTCDATETIME()
        WHERE InstanceId = @InstanceId;
    END
    ELSE IF @ActionTaken = N'Escalated'
    BEGIN
        UPDATE dbo.tbl_WorkflowInstances
        SET InstanceStatus = N'Escalated',
            DueAtUtc = DATEADD(HOUR, 24, SYSUTCDATETIME())
        WHERE InstanceId = @InstanceId;
    END
    ELSE IF @ActionTaken = N'Delegated'
    BEGIN
        UPDATE dbo.tbl_WorkflowInstances
        SET DueAtUtc = DATEADD(HOUR, 24, SYSUTCDATETIME())
        WHERE InstanceId = @InstanceId;
    END

    -- Automatic requester notification
    IF @ReqUserId IS NOT NULL
    BEGIN
        DECLARE @NotifTitle NVARCHAR(150) = CONCAT(N'Workflow ', @ActionTaken, N': ', @RefNum);
        DECLARE @NotifType NVARCHAR(20) = CASE
            WHEN @ActionTaken = N'Approved' THEN N'Success'
            WHEN @ActionTaken IN (N'Rejected', N'Cancelled') THEN N'Danger'
            WHEN @ActionTaken = N'Escalated' THEN N'Warning'
            ELSE N'Info' END;

        INSERT INTO dbo.tbl_Notifications (
            CompanyId, UserId, Title, Message, NotificationType, ModuleName, Priority, ActionUrl, IsRead, CreatedBy
        )
        VALUES (
            @CompanyId, @ReqUserId, @NotifTitle,
            CONCAT(@ModuleName, N' workflow step "', @CurrentStepName, N'" was ', LOWER(@ActionTaken), N' by ', @ActorName, N'.'),
            @NotifType, @ModuleName,
            CASE WHEN @ActionTaken = N'Escalated' THEN N'High' ELSE N'Normal' END,
            N'/Workflow', 0, @ActorUserId
        );
    END

    COMMIT TRANSACTION;
END
GO

-- 5. Audit Logs & Login Security History Search
CREATE OR ALTER PROCEDURE dbo.sp_AuditLog_Search
    @CompanyId  INT,
    @ModuleName NVARCHAR(80) = NULL,
    @ActionType NVARCHAR(50) = NULL,
    @Username   NVARCHAR(60) = NULL,
    @FromDate   DATE = NULL,
    @ToDate     DATE = NULL,
    @Search     NVARCHAR(100) = NULL,
    @TopCount   INT = 250
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Audit Trail
    SELECT TOP (@TopCount)
        AuditLogId, CompanyId, UserId, Username, ActionType, ModuleName,
        EntityName, EntityId, Description, OldValues, NewValues, IpAddress, UserAgent, CreatedDate
    FROM dbo.tbl_AuditLogs
    WHERE CompanyId = @CompanyId
      AND (@ModuleName IS NULL OR @ModuleName = N'' OR ModuleName = @ModuleName)
      AND (@ActionType IS NULL OR @ActionType = N'' OR ActionType = @ActionType)
      AND (@Username IS NULL OR @Username = N'' OR Username LIKE N'%' + @Username + N'%')
      AND (@FromDate IS NULL OR CAST(CreatedDate AS DATE) >= @FromDate)
      AND (@ToDate IS NULL OR CAST(CreatedDate AS DATE) <= @ToDate)
      AND (@Search IS NULL OR @Search = N'' OR
           Description LIKE N'%' + @Search + N'%' OR
           EntityName LIKE N'%' + @Search + N'%' OR
           ISNULL(EntityId, N'') LIKE N'%' + @Search + N'%')
    ORDER BY CreatedDate DESC, AuditLogId DESC;

    -- Result Set 2: Login Security History
    SELECT TOP 150
        lh.LoginHistoryId,
        lh.UserId,
        lh.UsernameOrEmail,
        lh.IsSuccess,
        lh.FailureReason,
        lh.IpAddress,
        lh.UserAgent,
        lh.AttemptedAtUtc
    FROM dbo.tbl_LoginHistory lh
    LEFT JOIN dbo.tbl_Users u ON lh.UserId = u.UserId
    WHERE (u.CompanyId = @CompanyId OR lh.UserId IS NULL)
      AND (@FromDate IS NULL OR CAST(lh.AttemptedAtUtc AS DATE) >= @FromDate)
      AND (@ToDate IS NULL OR CAST(lh.AttemptedAtUtc AS DATE) <= @ToDate)
      AND (@Username IS NULL OR @Username = N'' OR lh.UsernameOrEmail LIKE N'%' + @Username + N'%')
    ORDER BY lh.AttemptedAtUtc DESC, lh.LoginHistoryId DESC;
END
GO

-- 6. System Settings Management
CREATE OR ALTER PROCEDURE dbo.sp_SystemSettings_GetAll
    @CompanyId INT,
    @Category  NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SettingId,
        CompanyId,
        SettingKey,
        SettingValue,
        Category,
        DataType,
        IsSensitive,
        Description,
        ModifiedBy,
        ModifiedDate
    FROM dbo.tbl_SystemSettings
    WHERE CompanyId = @CompanyId
      AND (@Category IS NULL OR @Category = N'' OR Category = @Category)
    ORDER BY Category, SettingKey;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SystemSettings_Upsert
    @CompanyId    INT,
    @SettingKey   NVARCHAR(80),
    @SettingValue NVARCHAR(500),
    @Category     NVARCHAR(50) = N'General',
    @DataType     NVARCHAR(20) = N'Text',
    @Description  NVARCHAR(250) = NULL,
    @ModifiedBy   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_SystemSettings WHERE CompanyId = @CompanyId AND SettingKey = @SettingKey)
    BEGIN
        UPDATE dbo.tbl_SystemSettings
        SET SettingValue = @SettingValue,
            Category = COALESCE(NULLIF(@Category, N''), Category),
            DataType = COALESCE(NULLIF(@DataType, N''), DataType),
            Description = COALESCE(@Description, Description),
            ModifiedBy = @ModifiedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND SettingKey = @SettingKey;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tbl_SystemSettings (
            CompanyId, SettingKey, SettingValue, Category, DataType, IsSensitive, Description, ModifiedBy, ModifiedDate
        )
        VALUES (
            @CompanyId, @SettingKey, @SettingValue, @Category, @DataType, 0, @Description, @ModifiedBy, SYSUTCDATETIME()
        );
    END
END
GO

-- 7. RBAC Roles, Permissions & User Accounts Administration
CREATE OR ALTER PROCEDURE dbo.sp_Admin_GetRolesAndPermissions
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Roles
    SELECT
        r.RoleId, r.CompanyId, r.RoleCode, r.RoleName, r.Description, r.IsSystemRole, r.IsActive,
        (SELECT COUNT(1) FROM dbo.tbl_UserRoles ur INNER JOIN dbo.tbl_Users u ON ur.UserId = u.UserId
         WHERE ur.RoleId = r.RoleId AND u.CompanyId = @CompanyId AND u.IsDeleted = 0) AS AssignedUserCount,
        (SELECT COUNT(1) FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId) AS PermissionCount
    FROM dbo.tbl_Roles r
    WHERE r.CompanyId = @CompanyId AND r.IsDeleted = 0
    ORDER BY r.IsSystemRole DESC, r.RoleName;

    -- Result Set 2: All Permissions
    SELECT PermissionId, PermissionCode, ModuleName, PageName, ActionName, Description
    FROM dbo.tbl_Permissions
    ORDER BY ModuleName, PermissionCode;

    -- Result Set 3: Role-Permission Links
    SELECT rp.RoleId, rp.PermissionId
    FROM dbo.tbl_RolePermissions rp
    INNER JOIN dbo.tbl_Roles r ON rp.RoleId = r.RoleId
    WHERE r.CompanyId = @CompanyId AND r.IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Admin_SaveRolePermissions
    @CompanyId        INT,
    @RoleId           INT,
    @PermissionIdsCsv NVARCHAR(MAX),
    @ModifiedBy       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RoleCode NVARCHAR(40);
    SELECT @RoleCode = RoleCode FROM dbo.tbl_Roles WHERE RoleId = @RoleId AND CompanyId = @CompanyId AND IsDeleted = 0;
    IF @RoleCode IS NULL THROW 50221, N'Role not found.', 1;

    BEGIN TRANSACTION;

    DELETE FROM dbo.tbl_RolePermissions WHERE RoleId = @RoleId;

    INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId, CreatedBy)
    SELECT DISTINCT @RoleId, p.PermissionId, @ModifiedBy
    FROM STRING_SPLIT(ISNULL(@PermissionIdsCsv, N''), N',') s
    INNER JOIN dbo.tbl_Permissions p ON TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) = p.PermissionId;

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Admin_GetUsers
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.CompanyId,
        u.BranchId,
        b.BranchName,
        u.EmployeeId,
        e.EmployeeCode,
        u.Username,
        u.Email,
        u.FullName,
        u.MustChangePassword,
        u.FailedLoginCount,
        u.LockoutEndUtc,
        u.LastLoginUtc,
        u.LastLoginIp,
        u.IsActive,
        u.CreatedDate,
        ISNULL((
            SELECT TOP 1 r.RoleId
            FROM dbo.tbl_UserRoles ur
            INNER JOIN dbo.tbl_Roles r ON ur.RoleId = r.RoleId
            WHERE ur.UserId = u.UserId AND r.IsDeleted = 0
            ORDER BY ur.UserRoleId ASC
        ), 0) AS PrimaryRoleId,
        ISNULL((
            SELECT STRING_AGG(r.RoleName, N', ')
            FROM dbo.tbl_UserRoles ur
            INNER JOIN dbo.tbl_Roles r ON ur.RoleId = r.RoleId
            WHERE ur.UserId = u.UserId AND r.IsDeleted = 0
        ), N'Unassigned') AS RoleNames
    FROM dbo.tbl_Users u
    LEFT JOIN dbo.tbl_Branches b ON u.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Employees e ON u.EmployeeId = e.EmployeeId
    WHERE u.CompanyId = @CompanyId AND u.IsDeleted = 0
    ORDER BY u.FullName, u.Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Admin_CreateOrUpdateUser
    @CompanyId          INT,
    @UserId             INT,
    @EmployeeId         INT = NULL,
    @BranchId           INT = NULL,
    @Username           NVARCHAR(60),
    @Email              NVARCHAR(120),
    @FullName           NVARCHAR(120),
    @PasswordHash       NVARCHAR(500) = NULL,
    @RoleId             INT,
    @IsActive           BIT = 1,
    @MustChangePassword BIT = 1,
    @ModifiedBy         INT,
    @SavedUserId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE RoleId = @RoleId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50222, N'Selected role does not belong to this company.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Username = @Username AND UserId <> @UserId AND IsDeleted = 0)
        THROW 50223, N'Username is already taken.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Users WHERE Email = @Email AND UserId <> @UserId AND IsDeleted = 0)
        THROW 50224, N'Email address is already associated with another user.', 1;

    BEGIN TRANSACTION;

    IF @UserId = 0
    BEGIN
        IF @PasswordHash IS NULL OR @PasswordHash = N''
            THROW 50225, N'Initial password is required when creating a user.', 1;

        INSERT INTO dbo.tbl_Users (
            CompanyId, BranchId, EmployeeId, Username, Email, PasswordHash, FullName,
            MustChangePassword, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @BranchId, @EmployeeId, @Username, @Email, @PasswordHash, @FullName,
            @MustChangePassword, @IsActive, @ModifiedBy
        );

        SET @SavedUserId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_Users
        SET BranchId = @BranchId,
            EmployeeId = @EmployeeId,
            Username = @Username,
            Email = @Email,
            FullName = @FullName,
            MustChangePassword = @MustChangePassword,
            IsActive = @IsActive,
            ModifiedBy = @ModifiedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50226, N'User account not found.', 1;
        SET @SavedUserId = @UserId;
    END

    DELETE FROM dbo.tbl_UserRoles WHERE UserId = @SavedUserId;
    INSERT INTO dbo.tbl_UserRoles (UserId, RoleId, CreatedBy)
    VALUES (@SavedUserId, @RoleId, @ModifiedBy);

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Admin_ToggleUserStatus
    @CompanyId       INT,
    @UserId          INT,
    @Action          NVARCHAR(30), -- Unlock, Activate, Deactivate, ResetPassword
    @NewPasswordHash NVARCHAR(500) = NULL,
    @ModifiedBy      INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId = @ModifiedBy AND @Action = N'Deactivate'
        THROW 50227, N'You cannot deactivate your own active session account.', 1;

    IF @Action = N'Unlock'
    BEGIN
        UPDATE dbo.tbl_Users
        SET FailedLoginCount = 0, LockoutEndUtc = NULL, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND CompanyId = @CompanyId AND IsDeleted = 0;
    END
    ELSE IF @Action = N'Activate'
    BEGIN
        UPDATE dbo.tbl_Users
        SET IsActive = 1, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND CompanyId = @CompanyId AND IsDeleted = 0;
    END
    ELSE IF @Action = N'Deactivate'
    BEGIN
        UPDATE dbo.tbl_Users
        SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND CompanyId = @CompanyId AND IsDeleted = 0;
    END
    ELSE IF @Action = N'ResetPassword'
    BEGIN
        IF @NewPasswordHash IS NULL OR @NewPasswordHash = N''
            THROW 50228, N'A new password hash is required for reset.', 1;

        UPDATE dbo.tbl_Users
        SET PasswordHash = @NewPasswordHash,
            MustChangePassword = 1,
            FailedLoginCount = 0,
            LockoutEndUtc = NULL,
            ModifiedBy = @ModifiedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND CompanyId = @CompanyId AND IsDeleted = 0;
    END
    ELSE
    BEGIN
        THROW 50229, N'Unsupported account action.', 1;
    END

    IF @@ROWCOUNT = 0 THROW 50230, N'User account not found.', 1;
END
GO
