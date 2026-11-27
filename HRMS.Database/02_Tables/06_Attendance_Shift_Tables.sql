USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 4: Attendance and Shift Management Tables
-- ============================================================================

-- 1. Extend dbo.tbl_Attendance with Shift, WorkMinutes, EarlyExit, Source & Remarks
IF COL_LENGTH(N'dbo.tbl_Attendance', N'ShiftId') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Attendance ADD ShiftId INT NULL;
    ALTER TABLE dbo.tbl_Attendance ADD CONSTRAINT FK_tbl_Attendance_Shift FOREIGN KEY (ShiftId) REFERENCES dbo.tbl_Shifts(ShiftId);
END
GO

IF COL_LENGTH(N'dbo.tbl_Attendance', N'WorkMinutes') IS NULL
    ALTER TABLE dbo.tbl_Attendance ADD WorkMinutes INT NOT NULL CONSTRAINT DF_tbl_Attendance_WorkMin DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_Attendance', N'EarlyExitMinutes') IS NULL
    ALTER TABLE dbo.tbl_Attendance ADD EarlyExitMinutes INT NOT NULL CONSTRAINT DF_tbl_Attendance_EarlyExit DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_Attendance', N'Source') IS NULL
    ALTER TABLE dbo.tbl_Attendance ADD Source NVARCHAR(30) NOT NULL CONSTRAINT DF_tbl_Attendance_Source DEFAULT (N'Web');
GO

IF COL_LENGTH(N'dbo.tbl_Attendance', N'Remarks') IS NULL
    ALTER TABLE dbo.tbl_Attendance ADD Remarks NVARCHAR(400) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_Attendance', N'CheckInIp') IS NULL
    ALTER TABLE dbo.tbl_Attendance ADD CheckInIp NVARCHAR(45) NULL;
GO

-- 2. Employee Shift Assignments / Roster Schedule
IF OBJECT_ID(N'dbo.tbl_ShiftAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ShiftAssignments (
        AssignmentId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ShiftAssignments PRIMARY KEY,
        CompanyId     INT           NOT NULL,
        EmployeeId    INT           NOT NULL,
        ShiftId       INT           NOT NULL,
        EffectiveFrom DATE          NOT NULL,
        EffectiveTo   DATE          NULL,
        Notes         NVARCHAR(300) NULL,
        IsActive      BIT           NOT NULL CONSTRAINT DF_tbl_ShiftAssignments_IsActive DEFAULT (1),
        IsDeleted     BIT           NOT NULL CONSTRAINT DF_tbl_ShiftAssignments_IsDeleted DEFAULT (0),
        CreatedBy     INT           NULL,
        CreatedDate   DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_ShiftAssignments_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy    INT           NULL,
        ModifiedDate  DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_ShiftAssignments_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_ShiftAssignments_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_ShiftAssignments_Shift FOREIGN KEY (ShiftId) REFERENCES dbo.tbl_Shifts(ShiftId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_ShiftAssignments_Lookup
        ON dbo.tbl_ShiftAssignments(CompanyId, EmployeeId, EffectiveFrom, EffectiveTo)
        WHERE IsDeleted = 0 AND IsActive = 1;
END
GO

-- 3. Holiday Calendar
IF OBJECT_ID(N'dbo.tbl_Holidays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Holidays (
        HolidayId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Holidays PRIMARY KEY,
        CompanyId    INT           NOT NULL,
        BranchId     INT           NULL,
        HolidayName  NVARCHAR(120) NOT NULL,
        HolidayDate  DATE          NOT NULL,
        HolidayType  NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_Holidays_Type DEFAULT (N'Public'), -- Public, Company, Optional
        Description  NVARCHAR(300) NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_tbl_Holidays_IsActive DEFAULT (1),
        IsDeleted    BIT           NOT NULL CONSTRAINT DF_tbl_Holidays_IsDeleted DEFAULT (0),
        CreatedBy    INT           NULL,
        CreatedDate  DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Holidays_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy   INT           NULL,
        ModifiedDate DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Holidays_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Holidays_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Holidays_Company_Date
        ON dbo.tbl_Holidays(CompanyId, HolidayDate)
        WHERE IsDeleted = 0 AND IsActive = 1;
END
GO

-- 4. Attendance Regularization Requests
IF OBJECT_ID(N'dbo.tbl_AttendanceRegularizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AttendanceRegularizations (
        RegularizationId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AttendanceRegularizations PRIMARY KEY,
        CompanyId         INT           NOT NULL,
        EmployeeId        INT           NOT NULL,
        AttendanceDate    DATE          NOT NULL,
        RequestedCheckIn  DATETIME2(0)  NULL,
        RequestedCheckOut DATETIME2(0)  NULL,
        RequestedStatus   NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_AttReg_Status DEFAULT (N'Present'),
        Reason            NVARCHAR(500) NOT NULL,
        ApprovalStatus    NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_AttReg_Approval DEFAULT (N'Pending'), -- Pending, Approved, Rejected
        ReviewedBy        INT           NULL,
        ReviewedDate      DATETIME2(0)  NULL,
        ReviewerComments  NVARCHAR(400) NULL,
        IsDeleted         BIT           NOT NULL CONSTRAINT DF_tbl_AttReg_IsDeleted DEFAULT (0),
        CreatedBy         INT           NULL,
        CreatedDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_AttReg_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT           NULL,
        ModifiedDate      DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_AttReg_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_AttReg_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_AttReg_Company_Status
        ON dbo.tbl_AttendanceRegularizations(CompanyId, ApprovalStatus, AttendanceDate)
        WHERE IsDeleted = 0;
END
GO

-- 5. Overtime Pre-Approval / Claim Requests
IF OBJECT_ID(N'dbo.tbl_OvertimeRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_OvertimeRequests (
        OvertimeRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_OvertimeRequests PRIMARY KEY,
        CompanyId         INT           NOT NULL,
        EmployeeId        INT           NOT NULL,
        OvertimeDate      DATE          NOT NULL,
        RequestedHours    DECIMAL(5,2)  NOT NULL,
        ApprovedHours     DECIMAL(5,2)  NOT NULL CONSTRAINT DF_tbl_OTReq_ApprovedHours DEFAULT (0),
        Reason            NVARCHAR(500) NOT NULL,
        ApprovalStatus    NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_OTReq_Approval DEFAULT (N'Pending'), -- Pending, Approved, Rejected
        ReviewedBy        INT           NULL,
        ReviewedDate      DATETIME2(0)  NULL,
        ReviewerComments  NVARCHAR(400) NULL,
        IsDeleted         BIT           NOT NULL CONSTRAINT DF_tbl_OTReq_IsDeleted DEFAULT (0),
        CreatedBy         INT           NULL,
        CreatedDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_OTReq_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT           NULL,
        ModifiedDate      DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_OTReq_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_OTReq_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_OTReq_Company_Status
        ON dbo.tbl_OvertimeRequests(CompanyId, ApprovalStatus, OvertimeDate)
        WHERE IsDeleted = 0;
END
GO
