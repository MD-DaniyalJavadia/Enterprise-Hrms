USE [HRMS_DB];
GO

-- ============================================================================
-- Operational Summary Tables Supporting Dashboard KPIs & Analytics
-- (Extended in Phases 4, 5, and 6 with full workflow procedures)
-- ============================================================================

IF OBJECT_ID(N'dbo.tbl_Attendance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Attendance (
        AttendanceId    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Attendance PRIMARY KEY,
        CompanyId       INT          NOT NULL,
        BranchId        INT          NOT NULL,
        EmployeeId      INT          NOT NULL,
        AttendanceDate  DATE         NOT NULL,
        CheckInTime     DATETIME2(0) NULL,
        CheckOutTime    DATETIME2(0) NULL,
        Status          NVARCHAR(20) NOT NULL, -- Present, Late, Absent, OnLeave, HalfDay
        LateMinutes     INT          NOT NULL CONSTRAINT DF_tbl_Attendance_LateMin DEFAULT (0),
        OvertimeMinutes INT          NOT NULL CONSTRAINT DF_tbl_Attendance_OTMin DEFAULT (0),
        IsDeleted       BIT          NOT NULL CONSTRAINT DF_tbl_Attendance_IsDeleted DEFAULT (0),
        CreatedBy       INT          NULL,
        CreatedDate     DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_Attendance_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT          NULL,
        ModifiedDate    DATETIME2(0) NULL,
        CONSTRAINT FK_tbl_Attendance_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Attendance_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_Attendance_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_Attendance_Employee_Date UNIQUE (EmployeeId, AttendanceDate)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Attendance_Company_Date ON dbo.tbl_Attendance(CompanyId, AttendanceDate, Status) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.tbl_LeaveTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LeaveTypes (
        LeaveTypeId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LeaveTypes PRIMARY KEY,
        CompanyId     INT          NOT NULL,
        LeaveCode     NVARCHAR(20) NOT NULL,
        LeaveName     NVARCHAR(80) NOT NULL,
        AnnualQuota   DECIMAL(5,2) NOT NULL CONSTRAINT DF_tbl_LeaveTypes_Quota DEFAULT (0),
        IsPaid        BIT          NOT NULL CONSTRAINT DF_tbl_LeaveTypes_IsPaid DEFAULT (1),
        IsActive      BIT          NOT NULL CONSTRAINT DF_tbl_LeaveTypes_IsActive DEFAULT (1),
        IsDeleted     BIT          NOT NULL CONSTRAINT DF_tbl_LeaveTypes_IsDeleted DEFAULT (0),
        CreatedBy     INT          NULL,
        CreatedDate   DATETIME2(0) NOT NULL CONSTRAINT DF_tbl_LeaveTypes_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_LeaveTypes_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_LeaveTypes_Company_Code UNIQUE (CompanyId, LeaveCode)
    );
END
GO

IF OBJECT_ID(N'dbo.tbl_LeaveApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LeaveApplications (
        LeaveApplicationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LeaveApplications PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        EmployeeId         INT           NOT NULL,
        LeaveTypeId        INT           NOT NULL,
        StartDate          DATE          NOT NULL,
        EndDate            DATE          NOT NULL,
        TotalDays          DECIMAL(5,2)  NOT NULL,
        Reason             NVARCHAR(500) NOT NULL,
        ApprovalStatus     NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_LeaveApplications_Status DEFAULT ('Pending'), -- Pending, Approved, Rejected, Cancelled
        ApprovedBy         INT           NULL,
        ApprovedDate       DATETIME2(0)  NULL,
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_LeaveApplications_IsDeleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_LeaveApplications_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_LeaveApplications_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_LeaveApplications_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_LeaveApplications_LeaveType FOREIGN KEY (LeaveTypeId) REFERENCES dbo.tbl_LeaveTypes(LeaveTypeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_LeaveApplications_Company_Status ON dbo.tbl_LeaveApplications(CompanyId, ApprovalStatus, StartDate, EndDate) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.tbl_PayrollRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_PayrollRuns (
        PayrollRunId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_PayrollRuns PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        PayrollYear        INT           NOT NULL,
        PayrollMonth       INT           NOT NULL,
        PeriodLabel        NVARCHAR(40)  NOT NULL,
        TotalGrossPay      DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_PayrollRuns_Gross DEFAULT (0),
        TotalDeductions    DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_PayrollRuns_Ded DEFAULT (0),
        TotalNetPay        DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_PayrollRuns_Net DEFAULT (0),
        ProcessedEmployees INT           NOT NULL CONSTRAINT DF_tbl_PayrollRuns_EmpCount DEFAULT (0),
        PayrollStatus      NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_PayrollRuns_Status DEFAULT ('Draft'), -- Draft, Approved, Locked
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_PayrollRuns_IsDeleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_PayrollRuns_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_PayrollRuns_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_PayrollRuns_Company_Period UNIQUE (CompanyId, PayrollYear, PayrollMonth)
    );
END
GO
