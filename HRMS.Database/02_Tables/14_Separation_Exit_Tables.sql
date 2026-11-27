USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 12: Separation & Exit Management Tables
-- ============================================================================

-- 1. Separation & Resignation Requests
IF OBJECT_ID(N'dbo.tbl_SeparationRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SeparationRequests (
        SeparationId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_SeparationRequests PRIMARY KEY,
        CompanyId               INT               NOT NULL,
        EmployeeId              INT               NOT NULL,
        SeparationNumber        NVARCHAR(30)      NOT NULL,
        SeparationType          NVARCHAR(30)      NOT NULL, -- Resignation, Termination, Retirement, ContractEnd, MutualSeparation
        ReasonCategory          NVARCHAR(60)      NOT NULL, -- BetterOpportunity, Relocation, PersonalHealth, CareerChange, Performance, Misconduct, Redundancy, Retirement, ContractCompletion, Other
        SubmissionDate          DATE              NOT NULL,
        NoticePeriodDays        INT               NOT NULL CONSTRAINT DF_tbl_SepReq_NoticeDays DEFAULT (30),
        RequestedLastWorkingDay DATE              NOT NULL,
        ApprovedLastWorkingDay  DATE              NULL,
        IsNoticeWaived          BIT               NOT NULL CONSTRAINT DF_tbl_SepReq_Waived DEFAULT (0),
        NoticeShortfallDays     INT               NOT NULL CONSTRAINT DF_tbl_SepReq_Shortfall DEFAULT (0),
        EmployeeComments        NVARCHAR(600)     NULL,
        ManagerComments         NVARCHAR(600)     NULL,
        HrComments              NVARCHAR(600)     NULL,
        SeparationStatus        NVARCHAR(25)      NOT NULL CONSTRAINT DF_tbl_SepReq_Status DEFAULT (N'Submitted'), -- Submitted, ManagerApproved, HrApproved, ClearanceInProgress, Settled, Completed, Rejected, Withdrawn
        IsRehireEligible        BIT               NOT NULL CONSTRAINT DF_tbl_SepReq_Rehire DEFAULT (1),
        ManagerApprovedBy       INT               NULL,
        ManagerApprovedDate     DATETIME2(0)      NULL,
        HrApprovedBy            INT               NULL,
        HrApprovedDate          DATETIME2(0)      NULL,
        CompletedBy             INT               NULL,
        CompletedDate           DATETIME2(0)      NULL,
        IsDeleted               BIT               NOT NULL CONSTRAINT DF_tbl_SepReq_Deleted DEFAULT (0),
        CreatedBy               INT               NULL,
        CreatedDate             DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_SepReq_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy              INT               NULL,
        ModifiedDate            DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_SepReq_Company  FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_SepReq_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_SeparationRequests_Number' AND object_id = OBJECT_ID(N'dbo.tbl_SeparationRequests'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_SeparationRequests_Number
        ON dbo.tbl_SeparationRequests (CompanyId, SeparationNumber)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_SeparationRequests_ActiveEmp' AND object_id = OBJECT_ID(N'dbo.tbl_SeparationRequests'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_SeparationRequests_ActiveEmp
        ON dbo.tbl_SeparationRequests (CompanyId, EmployeeId)
        WHERE IsDeleted = 0 AND SeparationStatus NOT IN (N'Rejected', N'Withdrawn', N'Completed');
END
GO

-- 2. Exit Clearance Checklist Templates
IF OBJECT_ID(N'dbo.tbl_ExitClearanceTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ExitClearanceTemplates (
        TemplateId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ExitClearanceTemplates PRIMARY KEY,
        CompanyId       INT               NOT NULL,
        DepartmentScope NVARCHAR(40)      NOT NULL, -- IT, Assets, Finance, HR, Manager, AdminFacilities
        ItemName        NVARCHAR(150)     NOT NULL,
        Description     NVARCHAR(300)     NULL,
        IsMandatory     BIT               NOT NULL CONSTRAINT DF_tbl_ExitClrTpl_Mand DEFAULT (1),
        DisplayOrder    INT               NOT NULL CONSTRAINT DF_tbl_ExitClrTpl_Order DEFAULT (1),
        IsActive        BIT               NOT NULL CONSTRAINT DF_tbl_ExitClrTpl_Active DEFAULT (1),
        IsDeleted       BIT               NOT NULL CONSTRAINT DF_tbl_ExitClrTpl_Deleted DEFAULT (0),
        CreatedBy       INT               NULL,
        CreatedDate     DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_ExitClrTpl_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT               NULL,
        ModifiedDate    DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_ExitClrTpl_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );
END
GO

-- 3. Per-Separation Exit Clearance Items
IF OBJECT_ID(N'dbo.tbl_ExitClearanceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ExitClearanceItems (
        ClearanceItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ExitClearanceItems PRIMARY KEY,
        CompanyId       INT               NOT NULL,
        SeparationId    INT               NOT NULL,
        DepartmentScope NVARCHAR(40)      NOT NULL,
        ItemName        NVARCHAR(150)     NOT NULL,
        IsMandatory     BIT               NOT NULL CONSTRAINT DF_tbl_ExitClrItem_Mand DEFAULT (1),
        ClearanceStatus NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_ExitClrItem_Status DEFAULT (N'Pending'), -- Pending, Cleared, Waived, Hold
        ClearedBy       INT               NULL,
        ClearedDate     DATETIME2(0)      NULL,
        RecoveryAmount  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_ExitClrItem_Rec DEFAULT (0.00),
        Remarks         NVARCHAR(400)     NULL,
        IsDeleted       BIT               NOT NULL CONSTRAINT DF_tbl_ExitClrItem_Deleted DEFAULT (0),
        CreatedBy       INT               NULL,
        CreatedDate     DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_ExitClrItem_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT               NULL,
        ModifiedDate    DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_ExitClrItem_Company    FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_ExitClrItem_Separation FOREIGN KEY (SeparationId) REFERENCES dbo.tbl_SeparationRequests(SeparationId)
    );
END
GO

-- 4. Exit Interviews
IF OBJECT_ID(N'dbo.tbl_ExitInterviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ExitInterviews (
        ExitInterviewId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ExitInterviews PRIMARY KEY,
        CompanyId                 INT               NOT NULL,
        SeparationId              INT               NOT NULL,
        EmployeeId                INT               NOT NULL,
        InterviewDate             DATE              NOT NULL,
        ConductedByUserId         INT               NULL,
        OverallSatisfactionRating INT               NOT NULL, -- 1 to 5
        ManagerSupportRating      INT               NOT NULL, -- 1 to 5
        WorkLifeBalanceRating     INT               NOT NULL, -- 1 to 5
        CompensationRating        INT               NOT NULL, -- 1 to 5
        WouldRecommendCompany     BIT               NOT NULL CONSTRAINT DF_tbl_ExitInt_Rec DEFAULT (1),
        WouldConsiderRejoining    BIT               NOT NULL CONSTRAINT DF_tbl_ExitInt_Rejoin DEFAULT (1),
        PrimaryLeavingReason      NVARCHAR(200)     NOT NULL,
        LikedMost                 NVARCHAR(500)     NULL,
        ImprovementSuggestions    NVARCHAR(500)     NULL,
        InterviewerNotes          NVARCHAR(500)     NULL,
        IsConfidential            BIT               NOT NULL CONSTRAINT DF_tbl_ExitInt_Conf DEFAULT (1),
        CreatedBy                 INT               NULL,
        CreatedDate               DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_ExitInt_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy                INT               NULL,
        ModifiedDate              DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_ExitInt_Company    FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_ExitInt_Separation FOREIGN KEY (SeparationId) REFERENCES dbo.tbl_SeparationRequests(SeparationId),
        CONSTRAINT FK_tbl_ExitInt_Employee   FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_ExitInterviews_Separation UNIQUE (CompanyId, SeparationId)
    );
END
GO

-- 5. Full & Final Settlements
IF OBJECT_ID(N'dbo.tbl_FinalSettlements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_FinalSettlements (
        SettlementId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_FinalSettlements PRIMARY KEY,
        CompanyId               INT               NOT NULL,
        SeparationId            INT               NOT NULL,
        EmployeeId              INT               NOT NULL,
        SettlementNumber        NVARCHAR(30)      NOT NULL,
        MonthlyBaseSalary       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Base DEFAULT (0.00),
        WorkedDaysInFinalMonth  INT               NOT NULL CONSTRAINT DF_tbl_FinSet_Days DEFAULT (0),
        ProratedSalaryPay       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Prorated DEFAULT (0.00),
        EncashableLeaveDays     DECIMAL(5,2)      NOT NULL CONSTRAINT DF_tbl_FinSet_LeaveDays DEFAULT (0.00),
        LeaveEncashmentAmount   DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_LeaveAmt DEFAULT (0.00),
        GratuityOrSeverance     DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Gratuity DEFAULT (0.00),
        BonusOrOtherEarnings    DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Bonus DEFAULT (0.00),
        OutstandingLoanRecovery DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Loan DEFAULT (0.00),
        AssetOrNoticeRecovery   DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_AssetNotice DEFAULT (0.00),
        TaxAndOtherDeductions   DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Tax DEFAULT (0.00),
        GrossEarnings           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Gross DEFAULT (0.00),
        TotalDeductions         DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_TotDed DEFAULT (0.00),
        NetPayableAmount        DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_FinSet_Net DEFAULT (0.00),
        SettlementStatus        NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_FinSet_Status DEFAULT (N'Draft'), -- Draft, Approved, Paid
        PaymentReference        NVARCHAR(80)      NULL,
        PaidDate                DATE              NULL,
        Notes                   NVARCHAR(500)     NULL,
        ApprovedBy              INT               NULL,
        ApprovedDate            DATETIME2(0)      NULL,
        IsDeleted               BIT               NOT NULL CONSTRAINT DF_tbl_FinSet_Deleted DEFAULT (0),
        CreatedBy               INT               NULL,
        CreatedDate             DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_FinSet_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy              INT               NULL,
        ModifiedDate            DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_FinSet_Company    FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_FinSet_Separation FOREIGN KEY (SeparationId) REFERENCES dbo.tbl_SeparationRequests(SeparationId),
        CONSTRAINT FK_tbl_FinSet_Employee   FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_FinalSettlements_Separation UNIQUE (CompanyId, SeparationId),
        CONSTRAINT UQ_tbl_FinalSettlements_Number UNIQUE (CompanyId, SettlementNumber)
    );
END
GO
