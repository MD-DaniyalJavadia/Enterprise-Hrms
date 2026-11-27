USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 14: Advanced Notifications, Workflow Engine, Audit Logs & System Settings
-- ============================================================================

-- 1. Extend dbo.tbl_Notifications with Module, Priority, Sender & Expiration
IF COL_LENGTH(N'dbo.tbl_Notifications', N'ModuleName') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Notifications ADD ModuleName NVARCHAR(60) NULL;
END
IF COL_LENGTH(N'dbo.tbl_Notifications', N'Priority') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Notifications ADD Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_tbl_Notifications_Priority DEFAULT (N'Normal');
END
IF COL_LENGTH(N'dbo.tbl_Notifications', N'CreatedBy') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Notifications ADD CreatedBy INT NULL;
END
IF COL_LENGTH(N'dbo.tbl_Notifications', N'ExpiresAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Notifications ADD ExpiresAtUtc DATETIME2(0) NULL;
END
GO

-- 2. Extend dbo.tbl_SystemSettings with Category, DataType & Sensitivity
IF COL_LENGTH(N'dbo.tbl_SystemSettings', N'Category') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_SystemSettings ADD Category NVARCHAR(50) NOT NULL CONSTRAINT DF_tbl_SystemSettings_Category DEFAULT (N'General');
END
IF COL_LENGTH(N'dbo.tbl_SystemSettings', N'DataType') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_SystemSettings ADD DataType NVARCHAR(20) NOT NULL CONSTRAINT DF_tbl_SystemSettings_DataType DEFAULT (N'Text');
END
IF COL_LENGTH(N'dbo.tbl_SystemSettings', N'IsSensitive') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_SystemSettings ADD IsSensitive BIT NOT NULL CONSTRAINT DF_tbl_SystemSettings_IsSensitive DEFAULT (0);
END
GO

-- 3. tbl_NotificationTemplates
IF OBJECT_ID(N'dbo.tbl_NotificationTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_NotificationTemplates (
        TemplateId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_NotificationTemplates PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        TemplateCode    NVARCHAR(60)  NOT NULL,
        TemplateName    NVARCHAR(120) NOT NULL,
        ModuleName      NVARCHAR(60)  NOT NULL,
        EventTrigger    NVARCHAR(80)  NOT NULL,
        SubjectTemplate NVARCHAR(180) NOT NULL,
        BodyTemplate    NVARCHAR(600) NOT NULL,
        DefaultType     NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Type DEFAULT (N'Info'),
        DefaultPriority NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Priority DEFAULT (N'Normal'),
        SendInApp       BIT           NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_InApp DEFAULT (1),
        SendEmailDigest BIT           NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Email DEFAULT (0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Active DEFAULT (1),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Deleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_NotificationTemplates_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT           NULL,
        ModifiedDate    DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_NotificationTemplates_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_NotificationTemplates_Code UNIQUE (CompanyId, TemplateCode)
    );
END
GO

-- 4. tbl_WorkflowDefinitions
IF OBJECT_ID(N'dbo.tbl_WorkflowDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_WorkflowDefinitions (
        WorkflowId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_WorkflowDefinitions PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        WorkflowCode       NVARCHAR(40)  NOT NULL,
        WorkflowName       NVARCHAR(120) NOT NULL,
        ModuleName         NVARCHAR(50)  NOT NULL,
        DepartmentId       INT           NULL,
        MinAmountThreshold DECIMAL(18,2) NULL,
        AutoEscalateHours  INT           NOT NULL CONSTRAINT DF_tbl_WorkflowDefinitions_Escalate DEFAULT (48),
        Description        NVARCHAR(300) NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_tbl_WorkflowDefinitions_Active DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_WorkflowDefinitions_Deleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_WorkflowDefinitions_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_WorkflowDefinitions_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_WorkflowDefinitions_Dept FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId),
        CONSTRAINT UQ_tbl_WorkflowDefinitions_Code UNIQUE (CompanyId, WorkflowCode)
    );
    CREATE NONCLUSTERED INDEX IX_tbl_WorkflowDefinitions_Company_Module ON dbo.tbl_WorkflowDefinitions(CompanyId, ModuleName) WHERE IsDeleted = 0;
END
GO

-- 5. tbl_WorkflowSteps
IF OBJECT_ID(N'dbo.tbl_WorkflowSteps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_WorkflowSteps (
        WorkflowStepId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_WorkflowSteps PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        WorkflowId         INT           NOT NULL,
        StepOrder          INT           NOT NULL,
        StepName           NVARCHAR(100) NOT NULL,
        ApproverType       NVARCHAR(30)  NOT NULL, -- ReportingManager, RoleBased, SpecificEmployee, DepartmentHead
        ApproverRoleId     INT           NULL,
        ApproverEmployeeId INT           NULL,
        EscalationRoleId   INT           NULL,
        SlaHours           INT           NOT NULL CONSTRAINT DF_tbl_WorkflowSteps_Sla DEFAULT (24),
        CanDelegate        BIT           NOT NULL CONSTRAINT DF_tbl_WorkflowSteps_Delegate DEFAULT (1),
        IsMandatory        BIT           NOT NULL CONSTRAINT DF_tbl_WorkflowSteps_Mandatory DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_WorkflowSteps_Deleted DEFAULT (0),
        CONSTRAINT FK_tbl_WorkflowSteps_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_WorkflowSteps_Workflow FOREIGN KEY (WorkflowId) REFERENCES dbo.tbl_WorkflowDefinitions(WorkflowId),
        CONSTRAINT FK_tbl_WorkflowSteps_Role FOREIGN KEY (ApproverRoleId) REFERENCES dbo.tbl_Roles(RoleId),
        CONSTRAINT FK_tbl_WorkflowSteps_Employee FOREIGN KEY (ApproverEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_WorkflowSteps_EscRole FOREIGN KEY (EscalationRoleId) REFERENCES dbo.tbl_Roles(RoleId)
    );
    CREATE NONCLUSTERED INDEX IX_tbl_WorkflowSteps_Workflow ON dbo.tbl_WorkflowSteps(WorkflowId, StepOrder) WHERE IsDeleted = 0;
END
GO

-- 6. tbl_WorkflowInstances
IF OBJECT_ID(N'dbo.tbl_WorkflowInstances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_WorkflowInstances (
        InstanceId            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_WorkflowInstances PRIMARY KEY,
        CompanyId             INT           NOT NULL,
        WorkflowId            INT           NOT NULL,
        ModuleName            NVARCHAR(50)  NOT NULL,
        EntityId              NVARCHAR(50)  NOT NULL,
        ReferenceNumber       NVARCHAR(60)  NOT NULL,
        RequestedByUserId     INT           NULL,
        RequestedByEmployeeId INT           NULL,
        CurrentStepOrder      INT           NOT NULL CONSTRAINT DF_tbl_WorkflowInstances_Step DEFAULT (1),
        TotalSteps            INT           NOT NULL CONSTRAINT DF_tbl_WorkflowInstances_Total DEFAULT (1),
        InstanceStatus        NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_WorkflowInstances_Status DEFAULT (N'InProgress'),
        DueAtUtc              DATETIME2(0)  NULL,
        CompletedDate         DATETIME2(0)  NULL,
        Summary               NVARCHAR(300) NULL,
        CreatedDate           DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_WorkflowInstances_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_WorkflowInstances_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_WorkflowInstances_Workflow FOREIGN KEY (WorkflowId) REFERENCES dbo.tbl_WorkflowDefinitions(WorkflowId),
        CONSTRAINT FK_tbl_WorkflowInstances_User FOREIGN KEY (RequestedByUserId) REFERENCES dbo.tbl_Users(UserId),
        CONSTRAINT FK_tbl_WorkflowInstances_Employee FOREIGN KEY (RequestedByEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
    CREATE NONCLUSTERED INDEX IX_tbl_WorkflowInstances_Company_Status ON dbo.tbl_WorkflowInstances(CompanyId, InstanceStatus, CreatedDate DESC);
END
GO

-- 7. tbl_WorkflowActionLogs
IF OBJECT_ID(N'dbo.tbl_WorkflowActionLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_WorkflowActionLogs (
        ActionLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_WorkflowActionLogs PRIMARY KEY,
        CompanyId   INT           NOT NULL,
        InstanceId  BIGINT        NOT NULL,
        StepOrder   INT           NOT NULL,
        StepName    NVARCHAR(100) NOT NULL,
        ActionTaken NVARCHAR(30)  NOT NULL,
        ActorUserId INT           NULL,
        ActorName   NVARCHAR(120) NOT NULL,
        Comments    NVARCHAR(400) NULL,
        ActionDate  DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_WorkflowActionLogs_Date DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_WorkflowActionLogs_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_WorkflowActionLogs_Instance FOREIGN KEY (InstanceId) REFERENCES dbo.tbl_WorkflowInstances(InstanceId)
    );
    CREATE NONCLUSTERED INDEX IX_tbl_WorkflowActionLogs_Instance ON dbo.tbl_WorkflowActionLogs(InstanceId, ActionDate DESC);
END
GO
