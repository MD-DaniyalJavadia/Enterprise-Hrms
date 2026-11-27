USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 3: Extend tbl_Employees & Create Employee Profile Sub-Tables
-- ============================================================================

IF COL_LENGTH(N'dbo.tbl_Employees', N'MiddleName') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_Employees ADD
        MiddleName              NVARCHAR(80)  NULL,
        PersonalEmail           NVARCHAR(120) NULL,
        MobilePhone             NVARCHAR(30)  NULL,
        MaritalStatus           NVARCHAR(20)  NULL,
        Nationality             NVARCHAR(60)  NULL,
        NationalIdNumber        NVARCHAR(50)  NULL,
        BloodGroup              NVARCHAR(10)  NULL,
        CurrentAddress          NVARCHAR(300) NULL,
        PermanentAddress        NVARCHAR(300) NULL,
        City                    NVARCHAR(80)  NULL,
        Country                 NVARCHAR(80)  NULL,
        WorkLocationId          INT           NULL,
        JobPositionId           INT           NULL,
        ShiftId                 INT           NULL,
        BankName                NVARCHAR(120) NULL,
        BankAccountTitle        NVARCHAR(120) NULL,
        BankAccountNumber       NVARCHAR(50)  NULL,
        BankRoutingCode         NVARCHAR(40)  NULL,
        TaxIdentificationNumber NVARCHAR(50)  NULL,
        TaxFilingStatus         NVARCHAR(40)  NULL;
END
GO

IF OBJECT_ID(N'dbo.FK_tbl_Employees_WorkLocation', N'F') IS NULL
    ALTER TABLE dbo.tbl_Employees ADD CONSTRAINT FK_tbl_Employees_WorkLocation FOREIGN KEY (WorkLocationId) REFERENCES dbo.tbl_WorkLocations(WorkLocationId);
IF OBJECT_ID(N'dbo.FK_tbl_Employees_JobPosition', N'F') IS NULL
    ALTER TABLE dbo.tbl_Employees ADD CONSTRAINT FK_tbl_Employees_JobPosition FOREIGN KEY (JobPositionId) REFERENCES dbo.tbl_JobPositions(JobPositionId);
IF OBJECT_ID(N'dbo.FK_tbl_Employees_Shift', N'F') IS NULL
    ALTER TABLE dbo.tbl_Employees ADD CONSTRAINT FK_tbl_Employees_Shift FOREIGN KEY (ShiftId) REFERENCES dbo.tbl_Shifts(ShiftId);
GO

-- 1. Emergency Contacts
IF OBJECT_ID(N'dbo.tbl_EmployeeEmergencyContacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeEmergencyContacts (
        EmergencyContactId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeEmergencyContacts PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        EmployeeId         INT           NOT NULL,
        ContactName        NVARCHAR(120) NOT NULL,
        Relationship       NVARCHAR(50)  NOT NULL,
        PrimaryPhone       NVARCHAR(30)  NOT NULL,
        AlternatePhone     NVARCHAR(30)  NULL,
        Address            NVARCHAR(250) NULL,
        IsPrimary          BIT           NOT NULL CONSTRAINT DF_tbl_EmpEmerg_Primary DEFAULT (0),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_EmpEmerg_Deleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpEmerg_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpEmerg_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpEmerg_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 2. Family Members
IF OBJECT_ID(N'dbo.tbl_EmployeeFamilyMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeFamilyMembers (
        FamilyMemberId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeFamilyMembers PRIMARY KEY,
        CompanyId      INT           NOT NULL,
        EmployeeId     INT           NOT NULL,
        FullName       NVARCHAR(120) NOT NULL,
        Relationship   NVARCHAR(50)  NOT NULL,
        DateOfBirth    DATE          NULL,
        ContactPhone   NVARCHAR(30)  NULL,
        IsDependent    BIT           NOT NULL CONSTRAINT DF_tbl_EmpFamily_Dep DEFAULT (0),
        IsDeleted      BIT           NOT NULL CONSTRAINT DF_tbl_EmpFamily_Deleted DEFAULT (0),
        CreatedBy      INT           NULL,
        CreatedDate    DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpFamily_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpFamily_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpFamily_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 3. Education Details
IF OBJECT_ID(N'dbo.tbl_EmployeeEducation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeEducation (
        EducationId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeEducation PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        EmployeeId      INT           NOT NULL,
        DegreeTitle     NVARCHAR(120) NOT NULL,
        InstitutionName NVARCHAR(150) NOT NULL,
        FieldOfStudy    NVARCHAR(120) NULL,
        StartYear       INT           NULL,
        CompletionYear  INT           NULL,
        GradeOrGpa      NVARCHAR(30)  NULL,
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_EmpEdu_Deleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpEdu_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpEdu_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpEdu_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 4. Professional Experience
IF OBJECT_ID(N'dbo.tbl_EmployeeExperience', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeExperience (
        ExperienceId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeExperience PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        EmployeeId      INT           NOT NULL,
        EmployerName    NVARCHAR(150) NOT NULL,
        JobTitle        NVARCHAR(120) NOT NULL,
        StartDate       DATE          NOT NULL,
        EndDate         DATE          NULL,
        Responsibilities NVARCHAR(500) NULL,
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_EmpExp_Deleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpExp_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpExp_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpExp_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 5. Skills and Certifications
IF OBJECT_ID(N'dbo.tbl_EmployeeSkills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeSkills (
        SkillId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeSkills PRIMARY KEY,
        CompanyId        INT           NOT NULL,
        EmployeeId       INT           NOT NULL,
        SkillName        NVARCHAR(100) NOT NULL,
        ProficiencyLevel NVARCHAR(30)  NOT NULL, -- Beginner, Intermediate, Advanced, Expert
        CertificationName NVARCHAR(150) NULL,
        IssuingAuthority NVARCHAR(120) NULL,
        ExpiryDate       DATE          NULL,
        IsDeleted        BIT           NOT NULL CONSTRAINT DF_tbl_EmpSkill_Deleted DEFAULT (0),
        CreatedBy        INT           NULL,
        CreatedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpSkill_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpSkill_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpSkill_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 6. Employee Documents
IF OBJECT_ID(N'dbo.tbl_EmployeeDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeDocuments (
        DocumentId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeDocuments PRIMARY KEY,
        CompanyId     INT           NOT NULL,
        EmployeeId    INT           NOT NULL,
        DocumentType  NVARCHAR(60)  NOT NULL, -- Contract, IDProof, Resume, Certificate, OfferLetter, Other
        DocumentTitle NVARCHAR(150) NOT NULL,
        OriginalFileName NVARCHAR(255) NOT NULL,
        StoredFileName   NVARCHAR(255) NOT NULL,
        ContentType   NVARCHAR(120) NOT NULL,
        FileSizeBytes BIGINT        NOT NULL,
        ExpiryDate    DATE          NULL,
        IsDeleted     BIT           NOT NULL CONSTRAINT DF_tbl_EmpDoc_Deleted DEFAULT (0),
        CreatedBy     INT           NULL,
        CreatedDate   DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpDoc_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpDoc_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpDoc_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 7. Employment History (Timeline: Hire, Promotion, Transfer, Status Change)
IF OBJECT_ID(N'dbo.tbl_EmployeeEmploymentHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeEmploymentHistory (
        HistoryId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmpHistory PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        EmployeeId         INT           NOT NULL,
        EventType          NVARCHAR(40)  NOT NULL, -- Hired, Promotion, Transfer, StatusChange
        EffectiveDate      DATE          NOT NULL,
        FromBranchName     NVARCHAR(120) NULL,
        ToBranchName       NVARCHAR(120) NULL,
        FromDepartmentName NVARCHAR(120) NULL,
        ToDepartmentName   NVARCHAR(120) NULL,
        FromDesignation    NVARCHAR(120) NULL,
        ToDesignation      NVARCHAR(120) NULL,
        Remarks            NVARCHAR(500) NULL,
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpHistory_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpHistory_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpHistory_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

-- 8. Salary History
IF OBJECT_ID(N'dbo.tbl_EmployeeSalaryHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeSalaryHistory (
        SalaryHistoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmpSalaryHistory PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        EmployeeId      INT           NOT NULL,
        PreviousSalary  DECIMAL(18,2) NOT NULL,
        NewSalary       DECIMAL(18,2) NOT NULL,
        EffectiveDate   DATE          NOT NULL,
        ChangeReason    NVARCHAR(200) NOT NULL,
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpSalary_Created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_EmpSalary_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpSalary_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO
