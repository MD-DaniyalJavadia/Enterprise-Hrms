USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 13: Reports, Analytics & Export Features Tables
-- ============================================================================

-- 1. Saved Report Presets / Templates
IF OBJECT_ID(N'dbo.tbl_SavedReportTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SavedReportTemplates (
        TemplateId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_SavedReportTemplates PRIMARY KEY,
        CompanyId     INT               NOT NULL,
        TemplateName  NVARCHAR(120)     NOT NULL,
        ReportKey     NVARCHAR(50)      NOT NULL, -- Workforce, AttendanceLeave, PayrollCost, TalentRecruitment, FinancialCompliance
        BranchId      INT               NULL,
        DepartmentId  INT               NULL,
        DateRangeDays INT               NOT NULL CONSTRAINT DF_tbl_SavedRep_Range DEFAULT (30),
        StatusFilter  NVARCHAR(40)      NULL,
        Notes         NVARCHAR(300)     NULL,
        IsShared      BIT               NOT NULL CONSTRAINT DF_tbl_SavedRep_Shared DEFAULT (1),
        IsDeleted     BIT               NOT NULL CONSTRAINT DF_tbl_SavedRep_Deleted DEFAULT (0),
        CreatedBy     INT               NOT NULL,
        CreatedDate   DATETIME2(0)      NOT NULL CONSTRAINT DF_tbl_SavedRep_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy    INT               NULL,
        ModifiedDate  DATETIME2(0)      NULL,
        CONSTRAINT FK_tbl_SavedRep_Company    FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_SavedRep_Branch     FOREIGN KEY (BranchId) REFERENCES dbo.tbl_Branches(BranchId),
        CONSTRAINT FK_tbl_SavedRep_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.tbl_Departments(DepartmentId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_SavedReportTemplates_Company' AND object_id = OBJECT_ID(N'dbo.tbl_SavedReportTemplates'))
BEGIN
    CREATE INDEX IX_tbl_SavedReportTemplates_Company
        ON dbo.tbl_SavedReportTemplates (CompanyId, ReportKey, IsShared)
        WHERE IsDeleted = 0;
END
GO

-- 2. Report Export Audit Log
IF OBJECT_ID(N'dbo.tbl_ReportExportLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ReportExportLogs (
        ExportLogId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ReportExportLogs PRIMARY KEY,
        CompanyId     INT                  NOT NULL,
        ReportKey     NVARCHAR(50)         NOT NULL,
        ReportTitle   NVARCHAR(150)        NOT NULL,
        ExportFormat  NVARCHAR(20)         NOT NULL, -- CSV, Print
        FilterSummary NVARCHAR(300)        NULL,
        RecordCount   INT                  NOT NULL CONSTRAINT DF_tbl_RepExp_Count DEFAULT (0),
        ExportedBy    INT                  NOT NULL,
        ExportedByName NVARCHAR(100)       NOT NULL,
        IpAddress     NVARCHAR(45)         NULL,
        ExportedDate  DATETIME2(0)         NOT NULL CONSTRAINT DF_tbl_RepExp_Date DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_tbl_RepExp_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbl_ReportExportLogs_Company_Date' AND object_id = OBJECT_ID(N'dbo.tbl_ReportExportLogs'))
BEGIN
    CREATE INDEX IX_tbl_ReportExportLogs_Company_Date
        ON dbo.tbl_ReportExportLogs (CompanyId, ExportedDate DESC);
END
GO
