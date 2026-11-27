USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 11: Asset & Document Management Tables
-- ============================================================================

-- 1. Asset Categories
IF OBJECT_ID(N'dbo.tbl_AssetCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AssetCategories (
        CategoryId                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AssetCategories PRIMARY KEY,
        CompanyId                 INT               NOT NULL,
        CategoryCode              NVARCHAR(20)      NOT NULL,
        CategoryName              NVARCHAR(100)     NOT NULL,
        AssetClass                NVARCHAR(30)      NOT NULL, -- IT, Furniture, Vehicle, AccessCard, Mobile, Other
        RequiresSerialNumber      BIT               NOT NULL CONSTRAINT DF_tbl_AssetCat_Serial DEFAULT (1),
        DefaultDepreciationMonths INT               NULL,
        Description               NVARCHAR(300)     NULL,
        IsActive                  BIT               NOT NULL CONSTRAINT DF_tbl_AssetCat_Active DEFAULT (1),
        IsDeleted                 BIT               NOT NULL CONSTRAINT DF_tbl_AssetCat_Deleted DEFAULT (0),
        CreatedBy                 INT               NULL,
        CreatedDate               DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_AssetCat_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy                INT               NULL,
        ModifiedDate              DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_AssetCat_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_AssetCategories_Code' AND object_id = OBJECT_ID(N'dbo.tbl_AssetCategories'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_AssetCategories_Code
        ON dbo.tbl_AssetCategories (CompanyId, CategoryCode)
        WHERE IsDeleted = 0;
END
GO

-- 2. Enterprise Asset Register
IF OBJECT_ID(N'dbo.tbl_Assets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Assets (
        AssetId            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Assets PRIMARY KEY,
        CompanyId          INT               NOT NULL,
        CategoryId         INT               NOT NULL,
        BranchId           INT               NULL,
        AssetTag           NVARCHAR(40)      NOT NULL,
        AssetName          NVARCHAR(150)     NOT NULL,
        Brand              NVARCHAR(80)      NULL,
        ModelNumber        NVARCHAR(80)      NULL,
        SerialNumber       NVARCHAR(100)     NULL,
        PurchaseDate       DATE              NULL,
        PurchaseCost       DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_Assets_Cost DEFAULT (0),
        WarrantyExpiryDate DATE              NULL,
        VendorName         NVARCHAR(120)     NULL,
        ConditionStatus    NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_Assets_Condition DEFAULT (N'Good'), -- New, Good, Fair, Damaged
        AssetStatus        NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_Assets_Status DEFAULT (N'Available'), -- Available, Assigned, Maintenance, Retired, Lost
        CurrentEmployeeId  INT               NULL,
        Notes              NVARCHAR(500)     NULL,
        IsDeleted          BIT               NOT NULL CONSTRAINT DF_tbl_Assets_Deleted DEFAULT (0),
        CreatedBy          INT               NULL,
        CreatedDate        DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_Assets_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT               NULL,
        ModifiedDate       DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_Assets_Company  FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Assets_Category FOREIGN KEY (CategoryId) REFERENCES dbo.tbl_AssetCategories(CategoryId),
        CONSTRAINT FK_tbl_Assets_Branch   FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_Assets_Employee FOREIGN KEY (CurrentEmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_Assets_Tag' AND object_id = OBJECT_ID(N'dbo.tbl_Assets'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_Assets_Tag
        ON dbo.tbl_Assets (CompanyId, AssetTag)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_Assets_StatusEmp' AND object_id = OBJECT_ID(N'dbo.tbl_Assets'))
BEGIN
    CREATE INDEX IX_tbl_Assets_StatusEmp
        ON dbo.tbl_Assets (CompanyId, AssetStatus, CurrentEmployeeId)
        WHERE IsDeleted = 0;
END
GO

-- 3. Asset Allocations (Custody & Handover History)
IF OBJECT_ID(N'dbo.tbl_AssetAllocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AssetAllocations (
        AllocationId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AssetAllocations PRIMARY KEY,
        CompanyId            INT               NOT NULL,
        AssetId              INT               NOT NULL,
        EmployeeId           INT               NOT NULL,
        AssignedDate         DATE              NOT NULL,
        ExpectedReturnDate   DATE              NULL,
        ReturnedDate         DATE              NULL,
        ConditionAtIssue     NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_AssetAlloc_IssueCond DEFAULT (N'Good'),
        ConditionAtReturn    NVARCHAR(20)      NULL,
        AllocationStatus     NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_AssetAlloc_Status DEFAULT (N'Active'), -- Active, Returned, Lost, Damaged
        IssuedBy             INT               NULL,
        ReturnedTo           INT               NULL,
        IssueRemarks         NVARCHAR(400)     NULL,
        ReturnRemarks        NVARCHAR(400)     NULL,
        EmployeeAcknowledged BIT               NOT NULL CONSTRAINT DF_tbl_AssetAlloc_Ack DEFAULT (0),
        AcknowledgedDate     DATETIME2(0)      NULL,
        IsDeleted            BIT               NOT NULL CONSTRAINT DF_tbl_AssetAlloc_Deleted DEFAULT (0),
        CreatedBy            INT               NULL,
        CreatedDate          DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_AssetAlloc_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy           INT               NULL,
        ModifiedDate         DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_AssetAlloc_Company  FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_AssetAlloc_Asset    FOREIGN KEY (AssetId) REFERENCES dbo.tbl_Assets(AssetId),
        CONSTRAINT FK_tbl_AssetAlloc_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_AssetAllocations_ActiveAsset' AND object_id = OBJECT_ID(N'dbo.tbl_AssetAllocations'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_AssetAllocations_ActiveAsset
        ON dbo.tbl_AssetAllocations (CompanyId, AssetId)
        WHERE AllocationStatus = N'Active' AND IsDeleted = 0;
END
GO

-- 4. Asset Maintenance & Repair Logs
IF OBJECT_ID(N'dbo.tbl_AssetMaintenanceLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_AssetMaintenanceLogs (
        MaintenanceId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_AssetMaintenanceLogs PRIMARY KEY,
        CompanyId          INT               NOT NULL,
        AssetId            INT               NOT NULL,
        MaintenanceType    NVARCHAR(30)      NOT NULL, -- Repair, Preventive, Upgrade, Calibration, Inspection
        ServiceDate        DATE              NOT NULL,
        CompletionDate     DATE              NULL,
        VendorOrTechnician NVARCHAR(120)     NULL,
        Cost               DECIMAL(18,2)     NOT NULL CONSTRAINT DF_tbl_AssetMaint_Cost DEFAULT (0),
        MaintenanceStatus  NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_AssetMaint_Status DEFAULT (N'Scheduled'), -- Scheduled, InProgress, Completed, Cancelled
        Description        NVARCHAR(500)     NOT NULL,
        IsDeleted          BIT               NOT NULL CONSTRAINT DF_tbl_AssetMaint_Deleted DEFAULT (0),
        CreatedBy          INT               NULL,
        CreatedDate        DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_AssetMaint_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT               NULL,
        ModifiedDate       DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_AssetMaint_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_AssetMaint_Asset   FOREIGN KEY (AssetId) REFERENCES dbo.tbl_Assets(AssetId)
    );
END
GO

-- 5. Document Categories
IF OBJECT_ID(N'dbo.tbl_DocumentCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DocumentCategories (
        CategoryId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_DocumentCategories PRIMARY KEY,
        CompanyId              INT               NOT NULL,
        CategoryCode           NVARCHAR(20)      NOT NULL,
        CategoryName           NVARCHAR(100)     NOT NULL,
        ScopeType              NVARCHAR(20)      NOT NULL, -- CompanyPolicy, Template, Compliance, Guide
        RequiresAcknowledgment BIT               NOT NULL CONSTRAINT DF_tbl_DocCat_Ack DEFAULT (0),
        DefaultVisibility      NVARCHAR(30)      NOT NULL CONSTRAINT DF_tbl_DocCat_Vis DEFAULT (N'AllEmployees'),
        RetentionMonths        INT               NULL,
        Description            NVARCHAR(300)     NULL,
        IsActive               BIT               NOT NULL CONSTRAINT DF_tbl_DocCat_Active DEFAULT (1),
        IsDeleted              BIT               NOT NULL CONSTRAINT DF_tbl_DocCat_Deleted DEFAULT (0),
        CreatedBy              INT               NULL,
        CreatedDate            DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_DocCat_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy             INT               NULL,
        ModifiedDate           DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_DocCat_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_DocumentCategories_Code' AND object_id = OBJECT_ID(N'dbo.tbl_DocumentCategories'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_DocumentCategories_Code
        ON dbo.tbl_DocumentCategories (CompanyId, CategoryCode)
        WHERE IsDeleted = 0;
END
GO

-- 6. Central Company Document Repository & Versioned Policies
IF OBJECT_ID(N'dbo.tbl_CompanyDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_CompanyDocuments (
        DocumentId             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_CompanyDocuments PRIMARY KEY,
        CompanyId              INT               NOT NULL,
        CategoryId             INT               NOT NULL,
        DepartmentId           INT               NULL,
        DocumentCode           NVARCHAR(30)      NOT NULL,
        Title                  NVARCHAR(180)     NOT NULL,
        VersionNumber          NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_CompDoc_Ver DEFAULT (N'1.0'),
        Visibility             NVARCHAR(30)      NOT NULL CONSTRAINT DF_tbl_CompDoc_Vis DEFAULT (N'AllEmployees'), -- AllEmployees, ManagersOnly, HrOnly, DepartmentSpecific
        RequiresAcknowledgment BIT               NOT NULL CONSTRAINT DF_tbl_CompDoc_Ack DEFAULT (0),
        EffectiveDate          DATE              NOT NULL,
        ExpiryDate             DATE              NULL,
        OriginalFileName       NVARCHAR(255)     NOT NULL,
        StoredFileName         NVARCHAR(255)     NOT NULL,
        ContentType            NVARCHAR(120)     NOT NULL,
        FileSizeBytes          BIGINT            NOT NULL,
        Description            NVARCHAR(500)     NULL,
        DocumentStatus         NVARCHAR(20)      NOT NULL CONSTRAINT DF_tbl_CompDoc_Status DEFAULT (N'Published'), -- Draft, Published, Archived
        IsDeleted              BIT               NOT NULL CONSTRAINT DF_tbl_CompDoc_Deleted DEFAULT (0),
        CreatedBy              INT               NULL,
        CreatedDate            DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_CompDoc_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy             INT               NULL,
        ModifiedDate           DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_CompDoc_Company    FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_CompDoc_Category   FOREIGN KEY (CategoryId) REFERENCES dbo.tbl_DocumentCategories(CategoryId),
        CONSTRAINT FK_tbl_CompDoc_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_CompanyDocuments_Code' AND object_id = OBJECT_ID(N'dbo.tbl_CompanyDocuments'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_CompanyDocuments_Code
        ON dbo.tbl_CompanyDocuments (CompanyId, DocumentCode)
        WHERE IsDeleted = 0;
END
GO

-- 7. Policy Read & Sign-Off Acknowledgments
IF OBJECT_ID(N'dbo.tbl_DocumentAcknowledgments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_DocumentAcknowledgments (
        AcknowledgmentId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_DocumentAcknowledgments PRIMARY KEY,
        CompanyId           INT               NOT NULL,
        DocumentId          INT               NOT NULL,
        EmployeeId          INT               NOT NULL,
        AcknowledgedVersion NVARCHAR(20)      NOT NULL,
        AcknowledgedDate    DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_DocAck_Date DEFAULT (SYSUTCDATETIME()),
        IpAddress           NVARCHAR(45)      NULL,
        Remarks             NVARCHAR(250)     NULL,
        CONSTRAINT FK_tbl_DocAck_Company  FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_DocAck_Document FOREIGN KEY (DocumentId) REFERENCES dbo.tbl_CompanyDocuments(DocumentId),
        CONSTRAINT FK_tbl_DocAck_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_tbl_DocumentAcknowledgments_DocEmpVer' AND object_id = OBJECT_ID(N'dbo.tbl_DocumentAcknowledgments'))
BEGIN
    CREATE UNIQUE INDEX UQ_tbl_DocumentAcknowledgments_DocEmpVer
        ON dbo.tbl_DocumentAcknowledgments (CompanyId, DocumentId, EmployeeId, AcknowledgedVersion);
END
GO
