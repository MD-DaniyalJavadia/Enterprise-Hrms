USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 8: Loan, Salary Advance & Expense Management Tables
-- ============================================================================

-- 1. Loan & Salary Advance Types
IF OBJECT_ID(N'dbo.tbl_LoanTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LoanTypes (
        LoanTypeId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LoanTypes PRIMARY KEY,
        CompanyId           INT            NOT NULL,
        TypeCode            NVARCHAR(20)   NOT NULL,
        TypeName            NVARCHAR(100)  NOT NULL,
        Category            NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_LoanTypes_Cat DEFAULT (N'Loan'), -- Loan, SalaryAdvance
        MaxAmount           DECIMAL(18,2)  NOT NULL CONSTRAINT DF_tbl_LoanTypes_MaxAmt DEFAULT (25000.00),
        DefaultInterestRate DECIMAL(5,2)   NOT NULL CONSTRAINT DF_tbl_LoanTypes_Int DEFAULT (0.00),
        MaxTenureMonths     INT            NOT NULL CONSTRAINT DF_tbl_LoanTypes_Tenure DEFAULT (24),
        RequiresGuarantor   BIT            NOT NULL CONSTRAINT DF_tbl_LoanTypes_Guar DEFAULT (0),
        Description         NVARCHAR(400)  NULL,
        IsActive            BIT            NOT NULL CONSTRAINT DF_tbl_LoanTypes_Active DEFAULT (1),
        IsDeleted           BIT            NOT NULL CONSTRAINT DF_tbl_LoanTypes_Deleted DEFAULT (0),
        CreatedBy           INT            NULL,
        CreatedDate         DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_LoanTypes_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy          INT            NULL,
        ModifiedDate        DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_LoanTypes_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_LoanTypes_Company_Code UNIQUE (CompanyId, TypeCode)
    );
END
GO

-- 2. Employee Loans & Salary Advances
IF OBJECT_ID(N'dbo.tbl_EmployeeLoans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeLoans (
        LoanId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmployeeLoans PRIMARY KEY,
        CompanyId           INT            NOT NULL,
        EmployeeId          INT            NOT NULL,
        LoanTypeId          INT            NOT NULL,
        LoanNumber          NVARCHAR(30)   NOT NULL,
        PrincipalAmount     DECIMAL(18,2)  NOT NULL,
        InterestRatePercent DECIMAL(5,2)   NOT NULL CONSTRAINT DF_tbl_EmpLoans_IntRate DEFAULT (0.00),
        TenureMonths        INT            NOT NULL,
        MonthlyInstallment  DECIMAL(18,2)  NOT NULL,
        TotalPayableAmount  DECIMAL(18,2)  NOT NULL,
        PaidAmount          DECIMAL(18,2)  NOT NULL CONSTRAINT DF_tbl_EmpLoans_Paid DEFAULT (0.00),
        RemainingBalance    DECIMAL(18,2)  NOT NULL,
        Purpose             NVARCHAR(400)  NULL,
        RepaymentStartDate  DATE           NOT NULL,
        LoanStatus          NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_EmpLoans_Status DEFAULT (N'Pending'), -- Pending, Active, Closed, Rejected, Cancelled
        ApprovedBy          INT            NULL,
        ApprovedDate        DATETIME2(0)   NULL,
        DisbursedDate       DATE           NULL,
        ReviewerComments    NVARCHAR(400)  NULL,
        IsDeleted           BIT            NOT NULL CONSTRAINT DF_tbl_EmpLoans_Deleted DEFAULT (0),
        CreatedBy           INT            NULL,
        CreatedDate         DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_EmpLoans_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy          INT            NULL,
        ModifiedDate        DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_EmpLoans_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpLoans_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_EmpLoans_Type FOREIGN KEY (LoanTypeId) REFERENCES dbo.tbl_LoanTypes(LoanTypeId),
        CONSTRAINT UQ_tbl_EmpLoans_Company_Number UNIQUE (CompanyId, LoanNumber)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_EmpLoans_Company_Status ON dbo.tbl_EmployeeLoans(CompanyId, LoanStatus) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_tbl_EmpLoans_Employee ON dbo.tbl_EmployeeLoans(EmployeeId, LoanStatus) WHERE IsDeleted = 0;
END
GO

-- 3. Loan Repayment Schedule (EMI Ledger)
IF OBJECT_ID(N'dbo.tbl_LoanRepaymentSchedule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_LoanRepaymentSchedule (
        ScheduleId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_LoanRepaySched PRIMARY KEY,
        CompanyId          INT            NOT NULL,
        LoanId             INT            NOT NULL,
        InstallmentNumber  INT            NOT NULL,
        DueDate            DATE           NOT NULL,
        PrincipalComponent DECIMAL(18,2)  NOT NULL,
        InterestComponent  DECIMAL(18,2)  NOT NULL,
        InstallmentAmount  DECIMAL(18,2)  NOT NULL,
        PaidAmount         DECIMAL(18,2)  NOT NULL CONSTRAINT DF_tbl_LoanSched_Paid DEFAULT (0.00),
        PaymentStatus      NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_LoanSched_Status DEFAULT (N'Pending'), -- Pending, Paid, Skipped
        PaidDate           DATE           NULL,
        PaymentReference   NVARCHAR(80)   NULL,
        PayrollRunId       INT            NULL,
        IsDeleted          BIT            NOT NULL CONSTRAINT DF_tbl_LoanSched_Deleted DEFAULT (0),
        CreatedBy          INT            NULL,
        CreatedDate        DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_LoanSched_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT            NULL,
        ModifiedDate       DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_LoanSched_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_LoanSched_Loan FOREIGN KEY (LoanId) REFERENCES dbo.tbl_EmployeeLoans(LoanId),
        CONSTRAINT UQ_tbl_LoanSched_Loan_Inst UNIQUE (LoanId, InstallmentNumber)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_LoanSched_Due ON dbo.tbl_LoanRepaymentSchedule(CompanyId, DueDate, PaymentStatus) WHERE IsDeleted = 0;
END
GO

-- 4. Expense Categories
IF OBJECT_ID(N'dbo.tbl_ExpenseCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ExpenseCategories (
        CategoryId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ExpenseCategories PRIMARY KEY,
        CompanyId        INT            NOT NULL,
        CategoryCode     NVARCHAR(20)   NOT NULL,
        CategoryName     NVARCHAR(100)  NOT NULL,
        MaxLimitPerClaim DECIMAL(18,2)  NULL,
        RequiresReceipt  BIT            NOT NULL CONSTRAINT DF_tbl_ExpCat_Receipt DEFAULT (1),
        Description      NVARCHAR(400)  NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_tbl_ExpCat_Active DEFAULT (1),
        IsDeleted        BIT            NOT NULL CONSTRAINT DF_tbl_ExpCat_Deleted DEFAULT (0),
        CreatedBy        INT            NULL,
        CreatedDate      DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_ExpCat_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy       INT            NULL,
        ModifiedDate     DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_ExpCat_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_ExpCat_Company_Code UNIQUE (CompanyId, CategoryCode)
    );
END
GO

-- 5. Employee Expense Claims
IF OBJECT_ID(N'dbo.tbl_ExpenseClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_ExpenseClaims (
        ClaimId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_ExpenseClaims PRIMARY KEY,
        CompanyId         INT            NOT NULL,
        EmployeeId        INT            NOT NULL,
        CategoryId        INT            NOT NULL,
        ClaimNumber       NVARCHAR(30)   NOT NULL,
        ExpenseDate       DATE           NOT NULL,
        MerchantOrVendor  NVARCHAR(150)  NOT NULL,
        ClaimAmount       DECIMAL(18,2)  NOT NULL,
        ApprovedAmount    DECIMAL(18,2)  NULL,
        CurrencyCode      CHAR(3)        NOT NULL CONSTRAINT DF_tbl_ExpClaims_Curr DEFAULT ('USD'),
        Description       NVARCHAR(500)  NULL,
        ReceiptFileName   NVARCHAR(255)  NULL,
        ReceiptStoredName NVARCHAR(255)  NULL,
        ClaimStatus       NVARCHAR(20)   NOT NULL CONSTRAINT DF_tbl_ExpClaims_Status DEFAULT (N'Submitted'), -- Submitted, Approved, Rejected, Reimbursed, Cancelled
        ReviewedBy        INT            NULL,
        ReviewedDate      DATETIME2(0)   NULL,
        ReviewerComments  NVARCHAR(400)  NULL,
        ReimbursedDate    DATE           NULL,
        PaymentReference  NVARCHAR(80)   NULL,
        IsDeleted         BIT            NOT NULL CONSTRAINT DF_tbl_ExpClaims_Deleted DEFAULT (0),
        CreatedBy         INT            NULL,
        CreatedDate       DATETIME2(0)   NOT NULL CONSTRAINT DF_tbl_ExpClaims_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT            NULL,
        ModifiedDate      DATETIME2(0)   NULL,
        CONSTRAINT FK_tbl_ExpClaims_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_ExpClaims_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT FK_tbl_ExpClaims_Cat FOREIGN KEY (CategoryId) REFERENCES dbo.tbl_ExpenseCategories(CategoryId),
        CONSTRAINT UQ_tbl_ExpClaims_Company_Number UNIQUE (CompanyId, ClaimNumber)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_ExpClaims_Company_Status ON dbo.tbl_ExpenseClaims(CompanyId, ClaimStatus) WHERE IsDeleted = 0;
    CREATE NONCLUSTERED INDEX IX_tbl_ExpClaims_Employee ON dbo.tbl_ExpenseClaims(EmployeeId, ClaimStatus) WHERE IsDeleted = 0;
END
GO
