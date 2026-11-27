USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 5: Leave Management Tables & Extensions
-- ============================================================================

-- 1. Extend dbo.tbl_LeaveTypes
IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'AllowCarryForward') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD AllowCarryForward BIT NOT NULL CONSTRAINT DF_tbl_LeaveTypes_CarryFwd DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'MaxCarryForwardDays') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD MaxCarryForwardDays DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveTypes_MaxCarry DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'AllowEncashment') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD AllowEncashment BIT NOT NULL CONSTRAINT DF_tbl_LeaveTypes_Encash DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'RequiresDocument') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD RequiresDocument BIT NOT NULL CONSTRAINT DF_tbl_LeaveTypes_ReqDoc DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'MaxConsecutiveDays') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD MaxConsecutiveDays INT NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'IsHalfDayAllowed') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD IsHalfDayAllowed BIT NOT NULL CONSTRAINT DF_tbl_LeaveTypes_HalfDay DEFAULT (1);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'GenderRestriction') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD GenderRestriction NVARCHAR(20) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'ColorHex') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD ColorHex NVARCHAR(15) NOT NULL CONSTRAINT DF_tbl_LeaveTypes_Color DEFAULT (N'#2563eb');
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'Description') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD Description NVARCHAR(300) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveTypes', N'ModifiedBy') IS NULL
    ALTER TABLE dbo.tbl_LeaveTypes ADD ModifiedBy INT NULL, ModifiedDate DATETIME2(0) NULL;
GO

-- 2. Extend dbo.tbl_LeaveApplications
IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'IsHalfDay') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD IsHalfDay BIT NOT NULL CONSTRAINT DF_tbl_LeaveApp_HalfDay DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'HalfDaySession') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD HalfDaySession NVARCHAR(20) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'ContactDuringLeave') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD ContactDuringLeave NVARCHAR(60) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'AttachmentFileName') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD AttachmentFileName NVARCHAR(255) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'AttachmentStoredName') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD AttachmentStoredName NVARCHAR(255) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'ReviewerComments') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD ReviewerComments NVARCHAR(400) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_LeaveApplications', N'CancelledDate') IS NULL
    ALTER TABLE dbo.tbl_LeaveApplications ADD CancelledDate DATETIME2(0) NULL;
GO

-- 3. Employee Leave Balances
IF OBJECT_ID(N'dbo.tbl_LeaveBalances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LeaveBalances (
        LeaveBalanceId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LeaveBalances PRIMARY KEY,
        CompanyId        INT          NOT NULL,
        EmployeeId       INT          NOT NULL,
        LeaveTypeId      INT          NOT NULL,
        LeaveYear        INT          NOT NULL,
        OpeningBalance   DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Opening DEFAULT (0),
        AccruedDays      DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Accrued DEFAULT (0),
        CarryForwardDays DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Carry DEFAULT (0),
        UsedDays         DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Used DEFAULT (0),
        PendingDays      DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Pending DEFAULT (0),
        EncashedDays     DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveBal_Encashed DEFAULT (0),
        CreatedBy        INT          NULL,
        CreatedDate      DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_LeaveBal_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       INT          NULL,
        ModifiedDate     DATETIME2(0) NULL,
        CONSTRAINT FK_tbl_LeaveBalances_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_LeaveBalances_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_LeaveBalances_LeaveType FOREIGN KEY (LeaveTypeId) REFERENCES dbo.tbl_LeaveTypes(LeaveTypeId),
        CONSTRAINT UQ_tbl_LeaveBalances_Emp_Type_Year UNIQUE (EmployeeId, LeaveTypeId, LeaveYear)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_LeaveBalances_Company_Year
        ON dbo.tbl_LeaveBalances(CompanyId, LeaveYear, EmployeeId);
END
GO

-- 4. Leave Encashment Requests
IF OBJECT_ID(N'dbo.tbl_LeaveEncashments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LeaveEncashments (
        EncashmentId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LeaveEncashments PRIMARY KEY,
        CompanyId        INT           NOT NULL,
        EmployeeId       INT           NOT NULL,
        LeaveTypeId      INT           NOT NULL,
        LeaveYear        INT           NOT NULL,
        DaysEncashed     DECIMAL(5,2)  NOT NULL,
        EncashmentAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_LeaveEnc_Amount DEFAULT (0),
        ApprovalStatus   NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_LeaveEnc_Status DEFAULT (N'Pending'),
        Remarks          NVARCHAR(300) NULL,
        ReviewedBy       INT           NULL,
        ReviewedDate     DATETIME2(0)  NULL,
        ReviewerComments NVARCHAR(400) NULL,
        IsDeleted        BIT           NOT NULL CONSTRAINT DF_tbl_LeaveEnc_IsDeleted DEFAULT (0),
        CreatedBy        INT           NULL,
        CreatedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_LeaveEnc_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       INT           NULL,
        ModifiedDate     DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_LeaveEnc_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_LeaveEnc_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_LeaveEnc_LeaveType FOREIGN KEY (LeaveTypeId) REFERENCES dbo.tbl_LeaveTypes(LeaveTypeId)
    );
END
GO
