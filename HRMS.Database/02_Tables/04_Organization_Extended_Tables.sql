USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 2: Extended Organization Management Tables
-- ============================================================================

-- 1. tbl_JobGrades
IF OBJECT_ID(N'dbo.tbl_JobGrades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_JobGrades (
        JobGradeId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_JobGrades PRIMARY KEY,
        CompanyId    INT           NOT NULL,
        GradeCode    NVARCHAR(20)  NOT NULL,
        GradeName    NVARCHAR(100) NOT NULL,
        LevelOrder   INT           NOT NULL CONSTRAINT DF_tbl_JobGrades_Order DEFAULT (1),
        MinSalary    DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_JobGrades_Min DEFAULT (0),
        MaxSalary    DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_JobGrades_Max DEFAULT (0),
        Description  NVARCHAR(300) NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_tbl_JobGrades_IsActive DEFAULT (1),
        IsDeleted    BIT           NOT NULL CONSTRAINT DF_tbl_JobGrades_IsDeleted DEFAULT (0),
        CreatedBy    INT           NULL,
        CreatedDate  DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_JobGrades_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy   INT           NULL,
        ModifiedDate DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_JobGrades_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_JobGrades_Company_Code UNIQUE (CompanyId, GradeCode)
    );
END
GO

-- 2. tbl_CostCenters
IF OBJECT_ID(N'dbo.tbl_CostCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_CostCenters (
        CostCenterId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_CostCenters PRIMARY KEY,
        CompanyId      INT           NOT NULL,
        BranchId       INT           NULL,
        CostCenterCode NVARCHAR(30)  NOT NULL,
        CostCenterName NVARCHAR(120) NOT NULL,
        Description    NVARCHAR(300) NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_tbl_CostCenters_IsActive DEFAULT (1),
        IsDeleted      BIT           NOT NULL CONSTRAINT DF_tbl_CostCenters_IsDeleted DEFAULT (0),
        CreatedBy      INT           NULL,
        CreatedDate    DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_CostCenters_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy     INT           NULL,
        ModifiedDate   DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_CostCenters_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_CostCenters_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT UQ_tbl_CostCenters_Company_Code UNIQUE (CompanyId, CostCenterCode)
    );
END
GO

-- 3. tbl_WorkLocations
IF OBJECT_ID(N'dbo.tbl_WorkLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_WorkLocations (
        WorkLocationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_WorkLocations PRIMARY KEY,
        CompanyId      INT           NOT NULL,
        BranchId       INT           NULL,
        LocationCode   NVARCHAR(20)  NOT NULL,
        LocationName   NVARCHAR(120) NOT NULL,
        AddressLine1   NVARCHAR(250) NULL,
        City           NVARCHAR(80)  NULL,
        Country        NVARCHAR(80)  NULL,
        TimeZoneId     NVARCHAR(60)  NOT NULL CONSTRAINT DF_tbl_WorkLocations_TZ DEFAULT ('UTC'),
        IsRemote       BIT           NOT NULL CONSTRAINT DF_tbl_WorkLocations_Remote DEFAULT (0),
        IsActive       BIT           NOT NULL CONSTRAINT DF_tbl_WorkLocations_IsActive DEFAULT (1),
        IsDeleted      BIT           NOT NULL CONSTRAINT DF_tbl_WorkLocations_IsDeleted DEFAULT (0),
        CreatedBy      INT           NULL,
        CreatedDate    DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_WorkLocations_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy     INT           NULL,
        ModifiedDate   DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_WorkLocations_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_WorkLocations_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT UQ_tbl_WorkLocations_Company_Code UNIQUE (CompanyId, LocationCode)
    );
END
GO

-- 4. tbl_JobPositions
IF OBJECT_ID(N'dbo.tbl_JobPositions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_JobPositions (
        JobPositionId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_JobPositions PRIMARY KEY,
        CompanyId           INT           NOT NULL,
        DepartmentId        INT           NOT NULL,
        DesignationId       INT           NOT NULL,
        JobGradeId          INT           NULL,
        ReportsToPositionId INT           NULL,
        PositionCode        NVARCHAR(25)  NOT NULL,
        PositionTitle       NVARCHAR(120) NOT NULL,
        HeadcountBudget     INT           NOT NULL CONSTRAINT DF_tbl_JobPositions_Budget DEFAULT (1),
        Description         NVARCHAR(400) NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_tbl_JobPositions_IsActive DEFAULT (1),
        IsDeleted           BIT           NOT NULL CONSTRAINT DF_tbl_JobPositions_IsDeleted DEFAULT (0),
        CreatedBy           INT           NULL,
        CreatedDate         DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_JobPositions_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy          INT           NULL,
        ModifiedDate        DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_JobPositions_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_JobPositions_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId),
        CONSTRAINT FK_tbl_JobPositions_Designation FOREIGN KEY (DesignationId) REFERENCES dbo.tbl_Designations(DesignationId),
        CONSTRAINT FK_tbl_JobPositions_JobGrade FOREIGN KEY (JobGradeId) REFERENCES dbo.tbl_JobGrades(JobGradeId),
        CONSTRAINT FK_tbl_JobPositions_ReportsTo FOREIGN KEY (ReportsToPositionId) REFERENCES dbo.tbl_JobPositions(JobPositionId),
        CONSTRAINT UQ_tbl_JobPositions_Company_Code UNIQUE (CompanyId, PositionCode)
    );
END
GO

-- 5. tbl_Shifts
IF OBJECT_ID(N'dbo.tbl_Shifts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Shifts (
        ShiftId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Shifts PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        ShiftCode          NVARCHAR(20)  NOT NULL,
        ShiftName          NVARCHAR(100) NOT NULL,
        StartTime          TIME(0)       NOT NULL,
        EndTime            TIME(0)       NOT NULL,
        GracePeriodMinutes INT           NOT NULL CONSTRAINT DF_tbl_Shifts_Grace DEFAULT (15),
        BreakMinutes       INT           NOT NULL CONSTRAINT DF_tbl_Shifts_Break DEFAULT (60),
        WorkingHours       DECIMAL(4,2)  NOT NULL CONSTRAINT DF_tbl_Shifts_Hours DEFAULT (8.00),
        IsNightShift       BIT           NOT NULL CONSTRAINT DF_tbl_Shifts_Night DEFAULT (0),
        WeeklyOffDays      NVARCHAR(60)  NOT NULL CONSTRAINT DF_tbl_Shifts_WeeklyOff DEFAULT ('Saturday,Sunday'),
        IsActive           BIT           NOT NULL CONSTRAINT DF_tbl_Shifts_IsActive DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_Shifts_IsDeleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Shifts_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Shifts_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_Shifts_Company_Code UNIQUE (CompanyId, ShiftCode)
    );
END
GO
