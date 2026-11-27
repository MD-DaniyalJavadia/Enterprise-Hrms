USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 9: Performance & Training Management Tables
-- ============================================================================

-- 1. Appraisal Cycles
IF OBJECT_ID(N'dbo.tbl_AppraisalCycles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AppraisalCycles (
        CycleId               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AppraisalCycles PRIMARY KEY,
        CompanyId             INT            NOT NULL,
        CycleCode             NVARCHAR(20)   NOT NULL,
        CycleName             NVARCHAR(120)  NOT NULL,
        ReviewYear            INT            NOT NULL,
        StartDate             DATE           NOT NULL,
        EndDate               DATE           NOT NULL,
        SelfReviewDeadline    DATE           NULL,
        ManagerReviewDeadline DATE           NULL,
        CycleStatus           NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_AppCycles_Status DEFAULT (N'Draft'), -- Draft, Active, Closed
        Description           NVARCHAR(400)  NULL,
        IsDeleted             BIT            NOT NULL CONSTRAINT DF_tbl_AppCycles_Deleted DEFAULT (0),
        CreatedBy             INT            NULL,
        CreatedDate           DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_AppCycles_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy            INT            NULL,
        ModifiedDate          DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_AppCycles_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_AppCycles_Company_Code UNIQUE (CompanyId, CycleCode)
    );
END
GO

-- 2. Employee Performance Goals / KPIs
IF OBJECT_ID(N'dbo.tbl_PerformanceGoals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_PerformanceGoals (
        GoalId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_PerformanceGoals PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        CycleId         INT           NOT NULL,
        EmployeeId      INT           NOT NULL,
        GoalTitle       NVARCHAR(150) NOT NULL,
        KpiMetric       NVARCHAR(200) NULL,
        WeightPercent   DECIMAL(5,2)  NOT NULL CONSTRAINT DF_tbl_PerfGoals_Weight DEFAULT (25.00),
        TargetValue     NVARCHAR(100) NULL,
        AchievedValue   NVARCHAR(100) NULL,
        ProgressPercent INT           NOT NULL CONSTRAINT DF_tbl_PerfGoals_Progress DEFAULT (0),
        GoalStatus      NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_PerfGoals_Status DEFAULT (N'NotStarted'), -- NotStarted, InProgress, Completed, Deferred
        ManagerNotes    NVARCHAR(400) NULL,
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_PerfGoals_Deleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_PerfGoals_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT           NULL,
        ModifiedDate    DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_PerfGoals_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_PerfGoals_Cycle FOREIGN KEY (CycleId) REFERENCES dbo.tbl_AppraisalCycles(CycleId),
        CONSTRAINT FK_tbl_PerfGoals_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_PerfGoals_Cycle_Emp ON dbo.tbl_PerformanceGoals(CompanyId, CycleId, EmployeeId) WHERE IsDeleted = 0;
END
GO

-- 3. Performance Appraisals (Self + Manager Evaluation)
IF OBJECT_ID(N'dbo.tbl_PerformanceAppraisals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_PerformanceAppraisals (
        AppraisalId                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_PerformanceAppraisals PRIMARY KEY,
        CompanyId                   INT            NOT NULL,
        CycleId                     INT            NOT NULL,
        EmployeeId                  INT            NOT NULL,
        ReviewerEmployeeId          INT            NULL,
        SelfRating                  DECIMAL(3,2)   NULL,
        SelfComments                NVARCHAR(1000) NULL,
        SelfSubmittedDate           DATETIME2(0)   NULL,
        ManagerRating               DECIMAL(3,2)   NULL,
        FinalRating                 DECIMAL(3,2)   NULL,
        RatingBand                  NVARCHAR(40)   NULL,
        Strengths                   NVARCHAR(600)  NULL,
        ImprovementAreas            NVARCHAR(600)  NULL,
        ManagerComments             NVARCHAR(1000) NULL,
        RecommendedIncrementPercent DECIMAL(5,2)   NULL,
        AppraisalStatus             NVARCHAR(25)   NOT NULL CONSTRAINT DF_tbl_PerfApp_Status DEFAULT (N'NotStarted'), -- NotStarted, SelfSubmitted, ManagerReviewed, Completed
        CompletedDate               DATETIME2(0)   NULL,
        IsDeleted                   BIT            NOT NULL CONSTRAINT DF_tbl_PerfApp_Deleted DEFAULT (0),
        CreatedBy                   INT            NULL,
        CreatedDate                 DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_PerfApp_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy                  INT            NULL,
        ModifiedDate                DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_PerfApp_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_PerfApp_Cycle FOREIGN KEY (CycleId) REFERENCES dbo.tbl_AppraisalCycles(CycleId),
        CONSTRAINT FK_tbl_PerfApp_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_PerfApp_Rev FOREIGN KEY (ReviewerEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_PerfApp_Cycle_Emp UNIQUE (CycleId, EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_PerfApp_Company_Status ON dbo.tbl_PerformanceAppraisals(CompanyId, CycleId, AppraisalStatus) WHERE IsDeleted = 0;
END
GO

-- 4. Training Programs & Courses
IF OBJECT_ID(N'dbo.tbl_TrainingPrograms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TrainingPrograms (
        ProgramId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_TrainingPrograms PRIMARY KEY,
        CompanyId         INT           NOT NULL,
        ProgramCode       NVARCHAR(25)  NOT NULL,
        ProgramTitle      NVARCHAR(150) NOT NULL,
        Category          NVARCHAR(50)  NOT NULL CONSTRAINT DF_tbl_TrnProg_Cat DEFAULT (N'Technical'), -- Technical, Leadership, Compliance, SoftSkills, Onboarding
        DeliveryMode      NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_TrnProg_Mode DEFAULT (N'Workshop'), -- Workshop, Online, Classroom, Hybrid
        DurationHours     DECIMAL(5,1)  NOT NULL CONSTRAINT DF_tbl_TrnProg_Dur DEFAULT (4.0),
        TrainerOrProvider NVARCHAR(150) NULL,
        StartDate         DATE          NOT NULL,
        EndDate           DATE          NOT NULL,
        MaxParticipants   INT           NOT NULL CONSTRAINT DF_tbl_TrnProg_Max DEFAULT (30),
        LocationOrUrl     NVARCHAR(250) NULL,
        IsMandatory       BIT           NOT NULL CONSTRAINT DF_tbl_TrnProg_Mand DEFAULT (0),
        ProgramStatus     NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_TrnProg_Status DEFAULT (N'Planned'), -- Planned, Ongoing, Completed, Cancelled
        Description       NVARCHAR(600) NULL,
        IsDeleted         BIT           NOT NULL CONSTRAINT DF_tbl_TrnProg_Deleted DEFAULT (0),
        CreatedBy         INT           NULL,
        CreatedDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_TrnProg_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT           NULL,
        ModifiedDate      DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_TrnProg_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_TrnProg_Company_Code UNIQUE (CompanyId, ProgramCode)
    );
END
GO

-- 5. Training Enrollments & Certifications
IF OBJECT_ID(N'dbo.tbl_TrainingEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TrainingEnrollments (
        EnrollmentId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_TrainingEnrollments PRIMARY KEY,
        CompanyId         INT           NOT NULL,
        ProgramId         INT           NOT NULL,
        EmployeeId        INT           NOT NULL,
        EnrollmentDate    DATE          NOT NULL CONSTRAINT DF_tbl_TrnEnr_Date DEFAULT (CAST(SYSUTCDATETIME() AS DATE)),
        AttendanceStatus  NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_TrnEnr_Status DEFAULT (N'Enrolled'), -- Enrolled, Attended, Completed, NoShow, Dropped
        AssessmentScore   DECIMAL(5,2)  NULL,
        CertificateNumber NVARCHAR(50)  NULL,
        CompletionDate    DATE          NULL,
        FeedbackRating    INT           NULL,
        FeedbackNotes     NVARCHAR(400) NULL,
        IsDeleted         BIT           NOT NULL CONSTRAINT DF_tbl_TrnEnr_Deleted DEFAULT (0),
        CreatedBy         INT           NULL,
        CreatedDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_TrnEnr_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT           NULL,
        ModifiedDate      DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_TrnEnr_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_TrnEnr_Prog FOREIGN KEY (ProgramId) REFERENCES dbo.tbl_TrainingPrograms(ProgramId),
        CONSTRAINT FK_tbl_TrnEnr_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_TrnEnr_Prog_Emp UNIQUE (ProgramId, EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_TrnEnr_Company_Emp ON dbo.tbl_TrainingEnrollments(CompanyId, EmployeeId, AttendanceStatus) WHERE IsDeleted = 0;
END
GO
