USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 7: Recruitment Management Tables
-- ============================================================================

-- 1. Job Requisitions
IF OBJECT_ID(N'dbo.tbl_JobRequisitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_JobRequisitions (
        RequisitionId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_JobRequisitions PRIMARY KEY,
        CompanyId          INT            NOT NULL,
        RequisitionCode    NVARCHAR(30)   NOT NULL,
        JobTitle           NVARCHAR(150)  NOT NULL,
        BranchId           INT            NOT NULL,
        DepartmentId       INT            NOT NULL,
        DesignationId      INT            NOT NULL,
        JobPositionId      INT            NULL,
        HiringManagerId    INT            NULL,
        EmploymentType     NVARCHAR(30)   NOT NULL CONSTRAINT DF_tbl_JobReq_EmpType DEFAULT (N'Full-Time'),
        WorkMode           NVARCHAR(30)   NOT NULL CONSTRAINT DF_tbl_JobReq_WorkMode DEFAULT (N'On-Site'),
        OpeningsCount      INT            NOT NULL CONSTRAINT DF_tbl_JobReq_Openings DEFAULT (1),
        MinSalary          DECIMAL(18,2)  NULL,
        MaxSalary          DECIMAL(18,2)  NULL,
        ExperienceMinYears INT            NOT NULL CONSTRAINT DF_tbl_JobReq_ExpMin DEFAULT (0),
        ExperienceMaxYears INT            NULL,
        TargetHireDate     DATE           NULL,
        RequisitionStatus  NVARCHAR(30)   NOT NULL CONSTRAINT DF_tbl_JobReq_Status DEFAULT (N'Draft'), -- Draft, PendingApproval, Open, OnHold, Closed, Filled
        JobSummary         NVARCHAR(1000) NULL,
        RequiredSkills     NVARCHAR(500)  NULL,
        ApprovedBy         INT            NULL,
        ApprovedDate       DATETIME2(0)   NULL,
        IsDeleted          BIT            NOT NULL CONSTRAINT DF_tbl_JobReq_IsDeleted DEFAULT (0),
        CreatedBy          INT            NULL,
        CreatedDate        DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_JobReq_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT            NULL,
        ModifiedDate       DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_JobReq_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_JobReq_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_JobReq_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId),
        CONSTRAINT FK_tbl_JobReq_Designation FOREIGN KEY (DesignationId) REFERENCES dbo.tbl_Designations(DesignationId),
        CONSTRAINT FK_tbl_JobReq_Position FOREIGN KEY (JobPositionId) REFERENCES dbo.tbl_JobPositions(JobPositionId),
        CONSTRAINT FK_tbl_JobReq_Manager FOREIGN KEY (HiringManagerId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_JobReq_Company_Code UNIQUE (CompanyId, RequisitionCode)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_JobReq_Company_Status ON dbo.tbl_JobRequisitions(CompanyId, RequisitionStatus) WHERE IsDeleted = 0;
END
GO

-- 2. Candidate Pool
IF OBJECT_ID(N'dbo.tbl_Candidates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Candidates (
        CandidateId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Candidates PRIMARY KEY,
        CompanyId            INT           NOT NULL,
        FirstName            NVARCHAR(80)  NOT NULL,
        LastName             NVARCHAR(80)  NOT NULL,
        FullName             AS (CONCAT(FirstName, N' ', LastName)) PERSISTED,
        Email                NVARCHAR(120) NOT NULL,
        Phone                NVARCHAR(30)  NULL,
        CurrentCompany       NVARCHAR(150) NULL,
        CurrentJobTitle      NVARCHAR(120) NULL,
        TotalExperienceYears DECIMAL(4,1)  NOT NULL CONSTRAINT DF_tbl_Cand_Exp DEFAULT (0),
        ExpectedSalary       DECIMAL(18,2) NULL,
        Skills               NVARCHAR(500) NULL,
        Source               NVARCHAR(50)  NOT NULL CONSTRAINT DF_tbl_Cand_Source DEFAULT (N'CareerPortal'),
        LinkedInUrl          NVARCHAR(250) NULL,
        ResumeFileName       NVARCHAR(255) NULL,
        ResumeStoredName     NVARCHAR(255) NULL,
        Notes                NVARCHAR(500) NULL,
        IsDeleted            BIT           NOT NULL CONSTRAINT DF_tbl_Cand_IsDeleted DEFAULT (0),
        CreatedBy            INT           NULL,
        CreatedDate          DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Cand_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy           INT           NULL,
        ModifiedDate         DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Cand_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Cand_Company_Email ON dbo.tbl_Candidates(CompanyId, Email) WHERE IsDeleted = 0;
END
GO

-- 3. Job Applications (Pipeline Tracking)
IF OBJECT_ID(N'dbo.tbl_JobApplications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_JobApplications (
        ApplicationId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_JobApplications PRIMARY KEY,
        CompanyId        INT           NOT NULL,
        RequisitionId    INT           NOT NULL,
        CandidateId      INT           NOT NULL,
        PipelineStage    NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_JobApps_Stage DEFAULT (N'Applied'), -- Applied, Screening, Interview, Assessment, Offer, Hired, Rejected, Withdrawn
        MatchScore       INT           NULL,
        AppliedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_JobApps_Applied DEFAULT (SYSUTCDATETIME()),
        StageUpdatedDate DATETIME2(0)  NULL,
        RejectionReason  NVARCHAR(300) NULL,
        HiredEmployeeId  INT           NULL,
        IsDeleted        BIT           NOT NULL CONSTRAINT DF_tbl_JobApps_IsDeleted DEFAULT (0),
        CreatedBy        INT           NULL,
        CreatedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_JobApps_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       INT           NULL,
        ModifiedDate     DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_JobApps_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_JobApps_Req FOREIGN KEY (RequisitionId) REFERENCES dbo.tbl_JobRequisitions(RequisitionId),
        CONSTRAINT FK_tbl_JobApps_Cand FOREIGN KEY (CandidateId) REFERENCES dbo.tbl_Candidates(CandidateId),
        CONSTRAINT FK_tbl_JobApps_HiredEmp FOREIGN KEY (HiredEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_JobApps_Req_Cand UNIQUE (RequisitionId, CandidateId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_JobApps_Company_Stage ON dbo.tbl_JobApplications(CompanyId, RequisitionId, PipelineStage) WHERE IsDeleted = 0;
END
GO

-- 4. Interviews & Scorecards
IF OBJECT_ID(N'dbo.tbl_Interviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Interviews (
        InterviewId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Interviews PRIMARY KEY,
        CompanyId             INT            NOT NULL,
        ApplicationId         INT            NOT NULL,
        InterviewRound        NVARCHAR(50)   NOT NULL,
        InterviewMode         NVARCHAR(30)   NOT NULL CONSTRAINT DF_tbl_Int_Mode DEFAULT (N'Video'),
        ScheduledStart        DATETIME2(0)   NOT NULL,
        DurationMinutes       INT            NOT NULL CONSTRAINT DF_tbl_Int_Dur DEFAULT (45),
        LocationOrLink        NVARCHAR(250)  NULL,
        InterviewerEmployeeId INT            NULL,
        InterviewStatus       NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_Int_Status DEFAULT (N'Scheduled'), -- Scheduled, Completed, Cancelled, NoShow
        TechnicalScore        INT            NULL,
        CommunicationScore    INT            NULL,
        OverallRecommendation NVARCHAR(30)   NULL, -- StrongHire, Hire, Hold, NoHire
        FeedbackNotes         NVARCHAR(1000) NULL,
        IsDeleted             BIT            NOT NULL CONSTRAINT DF_tbl_Int_IsDeleted DEFAULT (0),
        CreatedBy             INT            NULL,
        CreatedDate           DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_Int_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy            INT            NULL,
        ModifiedDate          DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_Int_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Int_App FOREIGN KEY (ApplicationId) REFERENCES dbo.tbl_JobApplications(ApplicationId),
        CONSTRAINT FK_tbl_Int_Interviewer FOREIGN KEY (InterviewerEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 5. Job Offers
IF OBJECT_ID(N'dbo.tbl_JobOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_JobOffers (
        OfferId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_JobOffers PRIMARY KEY,
        CompanyId           INT           NOT NULL,
        ApplicationId       INT           NOT NULL,
        OfferCode           NVARCHAR(30)  NOT NULL,
        OfferedSalaryAnnual DECIMAL(18,2) NOT NULL,
        JoiningDate         DATE          NOT NULL,
        OfferExpiryDate     DATE          NOT NULL,
        EmploymentType      NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_Offers_EmpType DEFAULT (N'Full-Time'),
        OfferStatus         NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_Offers_Status DEFAULT (N'Draft'), -- Draft, Sent, Accepted, Declined, Expired, Converted
        BenefitsSummary     NVARCHAR(500) NULL,
        ResponseNotes       NVARCHAR(400) NULL,
        SentDate            DATETIME2(0)  NULL,
        RespondedDate       DATETIME2(0)  NULL,
        IsDeleted           BIT           NOT NULL CONSTRAINT DF_tbl_Offers_IsDeleted DEFAULT (0),
        CreatedBy           INT           NULL,
        CreatedDate         DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Offers_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy          INT           NULL,
        ModifiedDate        DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Offers_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Offers_App FOREIGN KEY (ApplicationId) REFERENCES dbo.tbl_JobApplications(ApplicationId),
        CONSTRAINT UQ_tbl_Offers_Company_Code UNIQUE (CompanyId, OfferCode)
    );
END
GO
