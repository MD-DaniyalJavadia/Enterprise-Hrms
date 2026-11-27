USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 10: Employee Self-Service & Manager Portal Tables
-- ============================================================================

IF OBJECT_ID(N'dbo.tbl_ProfileUpdateRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ProfileUpdateRequests (
        RequestId                          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ProfileUpdateRequests PRIMARY KEY,
        CompanyId                          INT           NOT NULL,
        EmployeeId                         INT           NOT NULL,
        RequestType                        NVARCHAR(40)  NOT NULL, -- ContactInfo, Address, BankDetails, EmergencyContact
        FieldSummary                       NVARCHAR(200) NOT NULL,
        RequestedPhone                     NVARCHAR(30)  NULL,
        RequestedMobilePhone               NVARCHAR(30)  NULL,
        RequestedPersonalEmail             NVARCHAR(120) NULL,
        RequestedAddress                   NVARCHAR(300) NULL,
        RequestedCity                      NVARCHAR(80)  NULL,
        RequestedCountry                   NVARCHAR(80)  NULL,
        RequestedBankName                  NVARCHAR(120) NULL,
        RequestedBankAccountTitle          NVARCHAR(120) NULL,
        RequestedBankAccountNumber         NVARCHAR(50)  NULL,
        RequestedBankRoutingCode           NVARCHAR(40)  NULL,
        RequestedEmergencyContactName      NVARCHAR(120) NULL,
        RequestedEmergencyRelationship     NVARCHAR(50)  NULL,
        RequestedEmergencyPhone            NVARCHAR(30)  NULL,
        EmployeeNotes                      NVARCHAR(500) NULL,
        ApprovalStatus                     NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_ProfReq_Status DEFAULT (N'Pending'), -- Pending, Approved, Rejected, Cancelled
        ReviewedBy                         INT           NULL,
        ReviewedDate                       DATETIME2(0)  NULL,
        ReviewerComments                   NVARCHAR(400) NULL,
        IsDeleted                          BIT           NOT NULL CONSTRAINT DF_tbl_ProfReq_Deleted DEFAULT (0),
        CreatedBy                          INT           NULL,
        CreatedDate                        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_ProfReq_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy                         INT           NULL,
        ModifiedDate                       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_ProfReq_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_ProfReq_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_ProfReq_Company_Status ON dbo.tbl_ProfileUpdateRequests(CompanyId, ApprovalStatus) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_tbl_ProfReq_Employee ON dbo.tbl_ProfileUpdateRequests(EmployeeId, CreatedDate DESC) WHERE IsDeleted = 0;
END
GO
