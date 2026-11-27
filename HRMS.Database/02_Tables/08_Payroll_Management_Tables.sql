USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 6: Payroll Management Tables & Extensions
-- ============================================================================

-- 1. Extend dbo.tbl_PayrollRuns
IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'PayDate') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD PayDate DATE NULL;
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'TotalEmployerContributions') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD TotalEmployerContributions DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_PayrollRuns_EmpContrib DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'TotalTaxDeducted') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD TotalTaxDeducted DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_PayrollRuns_Tax DEFAULT (0);
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'Notes') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD Notes NVARCHAR(500) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'ApprovedBy') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD ApprovedBy INT NULL, ApprovedDate DATETIME2(0) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'LockedBy') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD LockedBy INT NULL, LockedDate DATETIME2(0) NULL;
GO

IF COL_LENGTH(N'dbo.tbl_PayrollRuns', N'PaidDate') IS NULL
    ALTER TABLE dbo.tbl_PayrollRuns ADD PaidDate DATETIME2(0) NULL;
GO

-- 2. Salary Components (Earnings, Deductions, Employer Contributions)
IF OBJECT_ID(N'dbo.tbl_SalaryComponents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SalaryComponents (
        ComponentId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_SalaryComponents PRIMARY KEY,
        CompanyId         INT           NOT NULL,
        ComponentCode     NVARCHAR(20)  NOT NULL,
        ComponentName     NVARCHAR(100) NOT NULL,
        ComponentType     NVARCHAR(30)  NOT NULL, -- Earning, Deduction, EmployerContribution
        CalculationType   NVARCHAR(20)  NOT NULL, -- Fixed, PercentOfBasic, PercentOfGross
        DefaultValue      DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_SalComp_Default DEFAULT (0),
        IsTaxable         BIT           NOT NULL CONSTRAINT DF_tbl_SalComp_Taxable DEFAULT (1),
        IsStatutory       BIT           NOT NULL CONSTRAINT DF_tbl_SalComp_Statutory DEFAULT (0),
        IsSystemComponent BIT           NOT NULL CONSTRAINT DF_tbl_SalComp_System DEFAULT (0),
        DisplayOrder      INT           NOT NULL CONSTRAINT DF_tbl_SalComp_Order DEFAULT (10),
        IsActive          BIT           NOT NULL CONSTRAINT DF_tbl_SalComp_Active DEFAULT (1),
        IsDeleted         BIT           NOT NULL CONSTRAINT DF_tbl_SalComp_Deleted DEFAULT (0),
        CreatedBy         INT           NULL,
        CreatedDate       DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_SalComp_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy        INT           NULL,
        ModifiedDate      DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_SalComp_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT UQ_tbl_SalComp_Company_Code UNIQUE (CompanyId, ComponentCode)
    );
END
GO

-- 3. Progressive Income Tax Slabs
IF OBJECT_ID(N'dbo.tbl_TaxSlabs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_TaxSlabs (
        TaxSlabId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_TaxSlabs PRIMARY KEY,
        CompanyId       INT           NOT NULL,
        TaxYear         INT           NOT NULL,
        SlabName        NVARCHAR(80)  NOT NULL,
        MinAnnualIncome DECIMAL(18,2) NOT NULL,
        MaxAnnualIncome DECIMAL(18,2) NULL,
        TaxRatePercent  DECIMAL(5,2)  NOT NULL,
        FixedTaxAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_TaxSlabs_Fixed DEFAULT (0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_tbl_TaxSlabs_Active DEFAULT (1),
        IsDeleted       BIT           NOT NULL CONSTRAINT DF_tbl_TaxSlabs_Deleted DEFAULT (0),
        CreatedBy       INT           NULL,
        CreatedDate     DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_TaxSlabs_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy      INT           NULL,
        ModifiedDate    DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_TaxSlabs_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_TaxSlabs_Company_Year
        ON dbo.tbl_TaxSlabs(CompanyId, TaxYear, MinAnnualIncome)
        WHERE IsDeleted = 0 AND IsActive = 1;
END
GO

-- 4. Employee Salary Structures (Header)
IF OBJECT_ID(N'dbo.tbl_EmployeeSalaryStructures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeSalaryStructures (
        SalaryStructureId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmpSalStruct PRIMARY KEY,
        CompanyId          INT           NOT NULL,
        EmployeeId         INT           NOT NULL,
        EffectiveFrom      DATE          NOT NULL,
        BasicSalaryMonthly DECIMAL(18,2) NOT NULL,
        HourlyOvertimeRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_OTRate DEFAULT (0),
        TaxRegime          NVARCHAR(40)  NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_TaxRegime DEFAULT (N'Standard'),
        PaymentMethod      NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_PayMethod DEFAULT (N'BankTransfer'),
        Remarks            NVARCHAR(300) NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_Active DEFAULT (1),
        IsDeleted          BIT           NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_Deleted DEFAULT (0),
        CreatedBy          INT           NULL,
        CreatedDate        DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_EmpSalStruct_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy         INT           NULL,
        ModifiedDate       DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_EmpSalStruct_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpSalStruct_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_EmpSalStruct_Active
        ON dbo.tbl_EmployeeSalaryStructures(CompanyId, EmployeeId, IsActive)
        WHERE IsDeleted = 0;
END
GO

-- 5. Employee Salary Structure Items
IF OBJECT_ID(N'dbo.tbl_EmployeeSalaryStructureItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_EmployeeSalaryStructureItems (
        StructureItemId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_EmpSalStructItems PRIMARY KEY,
        CompanyId               INT           NOT NULL,
        SalaryStructureId       INT           NOT NULL,
        ComponentId             INT           NOT NULL,
        CalculationType         NVARCHAR(20)  NOT NULL,
        AmountOrPercent         DECIMAL(18,2) NOT NULL,
        CalculatedMonthlyAmount DECIMAL(18,2) NOT NULL,
        IsDeleted               BIT           NOT NULL CONSTRAINT DF_tbl_EmpSalStructItems_Del DEFAULT (0),
        CONSTRAINT FK_tbl_EmpSalItems_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_EmpSalItems_Header FOREIGN KEY (SalaryStructureId) REFERENCES dbo.tbl_EmployeeSalaryStructures(SalaryStructureId),
        CONSTRAINT FK_tbl_EmpSalItems_Comp FOREIGN KEY (ComponentId) REFERENCES dbo.tbl_SalaryComponents(ComponentId)
    );
END
GO

-- 6. Generated Payslips
IF OBJECT_ID(N'dbo.tbl_Payslips', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Payslips (
        PayslipId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_Payslips PRIMARY KEY,
        CompanyId              INT           NOT NULL,
        PayrollRunId           INT           NOT NULL,
        EmployeeId             INT           NOT NULL,
        BranchId               INT           NOT NULL,
        DepartmentId           INT           NOT NULL,
        DesignationId          INT           NOT NULL,
        WorkingDaysInMonth     DECIMAL(5,2)  NOT NULL,
        PaidDays               DECIMAL(5,2)  NOT NULL,
        UnpaidLeaveDays        DECIMAL(5,2)  NOT NULL CONSTRAINT DF_tbl_Payslips_Unpaid DEFAULT (0),
        AbsentDays             DECIMAL(5,2)  NOT NULL CONSTRAINT DF_tbl_Payslips_Absent DEFAULT (0),
        OvertimeHours          DECIMAL(6,2)  NOT NULL CONSTRAINT DF_tbl_Payslips_OTHours DEFAULT (0),
        BasicPay               DECIMAL(18,2) NOT NULL,
        TotalAllowances        DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_Allow DEFAULT (0),
        OvertimePay            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_OTPay DEFAULT (0),
        LeaveEncashmentPay     DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_Encash DEFAULT (0),
        BonusAmount            DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_Bonus DEFAULT (0),
        GrossPay               DECIMAL(18,2) NOT NULL,
        IncomeTax              DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_Tax DEFAULT (0),
        StatutoryDeductions    DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_StatDed DEFAULT (0),
        OtherDeductions        DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_OtherDed DEFAULT (0),
        UnpaidAbsenceDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_UnpaidDed DEFAULT (0),
        TotalDeductions        DECIMAL(18,2) NOT NULL,
        NetPay                 DECIMAL(18,2) NOT NULL,
        EmployerContributions  DECIMAL(18,2) NOT NULL CONSTRAINT DF_tbl_Payslips_EmpContrib DEFAULT (0),
        PaymentMethod          NVARCHAR(30)  NOT NULL CONSTRAINT DF_tbl_Payslips_PayMethod DEFAULT (N'BankTransfer'),
        BankName               NVARCHAR(120) NULL,
        BankAccountNumber      NVARCHAR(50)  NULL,
        BankRoutingCode        NVARCHAR(40)  NULL,
        PaymentStatus          NVARCHAR(20)  NOT NULL CONSTRAINT DF_tbl_Payslips_Status DEFAULT (N'Pending'), -- Pending, Paid, Hold
        PaidDate               DATETIME2(0)  NULL,
        Notes                  NVARCHAR(300) NULL,
        IsDeleted              BIT           NOT NULL CONSTRAINT DF_tbl_Payslips_Del DEFAULT (0),
        CreatedBy              INT           NULL,
        CreatedDate            DATETIME2(0)  NOT NULL CONSTRAINT DF_tbl_Payslips_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedBy             INT           NULL,
        ModifiedDate           DATETIME2(0)  NULL,
        CONSTRAINT FK_tbl_Payslips_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_Payslips_Run FOREIGN KEY (PayrollRunId) REFERENCES dbo.tbl_PayrollRuns(PayrollRunId),
        CONSTRAINT FK_tbl_Payslips_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.tbl_Employees(EmployeeId),
        CONSTRAINT UQ_tbl_Payslips_Run_Employee UNIQUE (PayrollRunId, EmployeeId)
    );

    CREATE NONCLUSTERED INDEX IX_tbl_Payslips_Company_Run ON dbo.tbl_Payslips(CompanyId, PayrollRunId) WHERE IsDeleted = 0;
END
GO

-- 7. Payslip Itemized Lines
IF OBJECT_ID(N'dbo.tbl_PayslipLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_PayslipLines (
        PayslipLineId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tbl_PayslipLines PRIMARY KEY,
        CompanyId     INT           NOT NULL,
        PayslipId     INT           NOT NULL,
        ComponentId   INT           NULL,
        ComponentCode NVARCHAR(20)  NOT NULL,
        ComponentName NVARCHAR(100) NOT NULL,
        ComponentType NVARCHAR(30)  NOT NULL, -- Earning, Deduction, EmployerContribution
        Amount        DECIMAL(18,2) NOT NULL,
        DisplayOrder  INT           NOT NULL CONSTRAINT DF_tbl_PayslipLines_Order DEFAULT (10),
        CONSTRAINT FK_tbl_PayslipLines_Company FOREIGN KEY (CompanyId) REFERENCES dbo.tbl_Companies(CompanyId),
        CONSTRAINT FK_tbl_PayslipLines_Payslip FOREIGN KEY (PayslipId) REFERENCES dbo.tbl_Payslips(PayslipId)
    );
END
GO
