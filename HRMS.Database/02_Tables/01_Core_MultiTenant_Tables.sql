USE [HRMS_DB];
GO

-- ============================================================================
-- 1. tbl_Companies (Multi-Tenant Root)
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Companies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Companies (
        CompanyId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Companies PRIMARY KEY,
        CompanyCode     NVARCHAR(20)  NOT NULL,
        CompanyName     NVARCHAR(150) NOT NULL,
        LegalName       NVARCHAR(200) NULL,
        TaxId           NVARCHAR(50)  NULL,
        Email           NVARCHAR(120) NULL,
        Phone           NVARCHAR(30)  NULL,
        Website         NVARCHAR(150) NULL,
        AddressLine1    NVARCHAR(250) NULL,
        City            NVARCHAR(80)  NULL,
        Country         NVARCHAR(80)  NULL,
        CurrencyCode    CHAR(3)       NOT NULL CONSTRAINT DF_tbl_Companies_Currency DEFAULT ('USD'),
        TimeZoneId      NVARCHAR(60)  NOT NULL CONSTRAINT DF_tbl_Companies_TimeZone DEFAULT ('UTC'),
        IsActive        BIT           NOT NULL CONSTRAINT DF_tbl_Companies_IsActive DEFAULT (1),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_Companies_IsDeleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Companies_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT           NULL,
        ModifiedDate    DATETIME2(0)  NULL,
        CONSTRAINT UQ_tbl_Companies_Code UNIQUE (CompanyCode)
    );
END
GO

-- ============================================================================
-- 2. tbl_Branches
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Branches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Branches (
        BranchId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Branches PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        BranchCode      NVARCHAR(20)  NOT NULL,
        BranchName      NVARCHAR(120) NOT NULL,
        Email           NVARCHAR(120) NULL,
        Phone           NVARCHAR(30)  NULL,
        AddressLine1    NVARCHAR(250) NULL,
        City            NVARCHAR(80)  NULL,
        Country         NVARCHAR(80)  NULL,
        IsHeadOffice    BIT           NOT NULL CONSTRAINT DF_tbl_Branches_IsHeadOffice DEFAULT (0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_tbl_Branches_IsActive DEFAULT (1),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_Branches_IsDeleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Branches_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT           NULL,
        ModifiedDate    DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Branches_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_Branches_Company_Code UNIQUE (CompanyId, BranchCode)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Branches_CompanyId ON dbo.tbl_Branches(CompanyId) WHERE IsDeleted = 0;
END
GO

-- ============================================================================
-- 3. tbl_Departments
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Departments (
        DepartmentId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Departments PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        BranchId           INT           NULL,
        ParentDepartmentId INT           NULL,
        DepartmentCode     NVARCHAR(20)  NOT NULL,
        DepartmentName     NVARCHAR(120) NOT NULL,
        CostCenterCode     NVARCHAR(30)  NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_tbl_Departments_IsActive DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_Departments_IsDeleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Departments_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Departments_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Departments_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_Departments_Parent FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId),
        CONSTRAINT UQ_tbl_Departments_Company_Code UNIQUE (CompanyId, DepartmentCode)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Departments_CompanyId ON dbo.tbl_Departments(CompanyId) WHERE IsDeleted = 0;
END
GO

-- ============================================================================
-- 4. tbl_Designations
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Designations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Designations (
        DesignationId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Designations PRIMARY KEY,
        CompanyId        INT           NOT NULL,
        DesignationCode  NVARCHAR(20)  NOT NULL,
        DesignationTitle NVARCHAR(120) NOT NULL,
        JobGrade         NVARCHAR(20)  NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_tbl_Designations_IsActive DEFAULT (1),
        IsDeleted        BIT           NOT NULL CONSTRAINT DF_tbl_Designations_IsDeleted DEFAULT (0),
        CreatedBy        INT           NULL,
        CreatedDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Designations_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       INT           NULL,
        ModifiedDate     DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Designations_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_Designations_Company_Code UNIQUE (CompanyId, DesignationCode)
    );
END
GO

-- ============================================================================
-- 5. tbl_Employees
-- ============================================================================
IF OBJECT_ID(N'dbo.tbl_Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Employees (
        EmployeeId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Employees PRIMARY KEY,
        CompanyId           INT           NOT NULL,
        BranchId            INT           NOT NULL,
        DepartmentId        INT           NOT NULL,
        DesignationId       INT           NOT NULL,
        ReportingManagerId  INT           NULL,
        EmployeeCode        NVARCHAR(30)  NOT NULL,
        FirstName           NVARCHAR(80)  NOT NULL,
        LastName            NVARCHAR(80)  NOT NULL,
        FullName            AS (CONCAT(FirstName, N' ', LastName)) PERSISTED,
        Gender              NVARCHAR(20)  NOT NULL,
        DateOfBirth         DATE          NOT NULL,
        WorkEmail           NVARCHAR(120) NOT NULL,
        Phone               NVARCHAR(30)  NULL,
        HireDate            DATE          NOT NULL,
        ConfirmationDate    DATE          NULL,
        ExitDate            DATE          NULL,
        EmploymentStatus    TINYINT       NOT NULL CONSTRAINT DF_tbl_Employees_Status DEFAULT (1), -- 1=Active, 2=Probation, 3=OnLeave, 4=Resigned, 5=Terminated, 6=Inactive
        BaseSalary          DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Employees_BaseSalary DEFAULT (0),
        PhotoPath           NVARCHAR(260) NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_tbl_Employees_IsActive DEFAULT (1),
        IsDeleted           BIT           NOT NULL CONSTRAINT DF_tbl_Employees_IsDeleted DEFAULT (0),
        CreatedBy           INT           NULL,
        CreatedDate         DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Employees_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedBy          INT           NULL,
        ModifiedDate        DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Employees_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Employees_Branch FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_Employees_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId),
        CONSTRAINT FK_tbl_Employees_Designation FOREIGN KEY (DesignationId) REFERENCES dbo.tbl_Designations(DesignationId),
        CONSTRAINT FK_tbl_Employees_Manager FOREIGN KEY (ReportingManagerId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_Employees_Company_Code UNIQUE (CompanyId, EmployeeCode),
        CONSTRAINT UQ_tbl_Employees_Company_Email UNIQUE (CompanyId, WorkEmail)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Employees_Company_Status ON dbo.tbl_Employees(CompanyId, BranchId, EmploymentStatus) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_tbl_Employees_Department ON dbo.tbl_Employees(DepartmentId) WHERE IsDeleted = 0;
END
GO
