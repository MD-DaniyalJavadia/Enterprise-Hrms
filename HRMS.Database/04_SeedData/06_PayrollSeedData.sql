USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 6: Payroll Management Seed Data
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions WHERE PermissionCode = N'PAYROLL.MANAGE')
BEGIN
    INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
    VALUES (N'PAYROLL.MANAGE', N'Payroll', N'Manage', N'Manage', N'Configure salary components, tax slabs, structures, and run payroll');
END
GO

DECLARE @PayrollManageId INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'PAYROLL.MANAGE');
DECLARE @PayrollViewId   INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'PAYROLL.VIEW');

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @PayrollManageId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @PayrollManageId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @PayrollViewId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND @PayrollViewId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @PayrollViewId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');
IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @Year INT = YEAR(GETDATE());
    DECLARE @Month INT = MONTH(GETDATE());

    -- 1. Seed Salary Components
    INSERT INTO dbo.tbl_SalaryComponents (
        CompanyId, ComponentCode, ComponentName, ComponentType, CalculationType, DefaultValue,
        IsTaxable, IsStatutory, IsSystemComponent, DisplayOrder, IsActive
    )
    SELECT @CompanyId, v.Code, v.Name, v.CompType, v.CalcType, v.DefVal,
           v.IsTaxable, v.IsStatutory, v.IsSystem, v.DispOrder, 1
    FROM (VALUES
        (N'BASIC', N'Basic Salary',                 N'Earning',              N'Fixed',          0.00,  1, 0, 1, 1),
        (N'HRA',   N'Housing Allowance',            N'Earning',              N'PercentOfBasic', 25.00, 1, 0, 0, 10),
        (N'TRA',   N'Transport Allowance',          N'Earning',              N'Fixed',          300.00,1, 0, 0, 20),
        (N'MED',   N'Medical Allowance',            N'Earning',              N'Fixed',          200.00,0, 0, 0, 30),
        (N'PF',    N'Provident Fund Contribution',  N'Deduction',            N'PercentOfBasic', 5.00,  0, 1, 0, 40),
        (N'HLTH',  N'Health Insurance Premium',     N'Deduction',            N'Fixed',          120.00,0, 1, 0, 50),
        (N'EPF',   N'Employer Pension Match',       N'EmployerContribution', N'PercentOfBasic', 5.00,  0, 1, 0, 60)
    ) AS v(Code, Name, CompType, CalcType, DefVal, IsTaxable, IsStatutory, IsSystem, DispOrder)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.tbl_SalaryComponents sc WHERE sc.CompanyId = @CompanyId AND sc.ComponentCode = v.Code AND sc.IsDeleted = 0
    );

    -- 2. Seed Progressive Tax Slabs for Current Year
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_TaxSlabs WHERE CompanyId = @CompanyId AND TaxYear = @Year AND IsDeleted = 0)
    BEGIN
        INSERT INTO dbo.tbl_TaxSlabs (CompanyId, TaxYear, SlabName, MinAnnualIncome, MaxAnnualIncome, TaxRatePercent, FixedTaxAmount, IsActive)
        VALUES
            (@CompanyId, @Year, N'Tax-Free Base Bracket',     0.00,      30000.00,  0.00,  0.00, 1),
            (@CompanyId, @Year, N'Standard Bracket (10%)',    30000.00,  70000.00,  10.00, 0.00, 1),
            (@CompanyId, @Year, N'Mid-Income Bracket (18%)',  70000.00,  120000.00, 18.00, 0.00, 1),
            (@CompanyId, @Year, N'Executive Bracket (25%)',   120000.00, NULL,      25.00, 0.00, 1);
    END

    -- 3. Seed Salary Structures for Active Employees without one
    DECLARE @HraId  INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'HRA'  AND IsDeleted = 0);
    DECLARE @TraId  INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'TRA'  AND IsDeleted = 0);
    DECLARE @MedId  INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'MED'  AND IsDeleted = 0);
    DECLARE @PfId   INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'PF'   AND IsDeleted = 0);
    DECLARE @HlthId INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'HLTH' AND IsDeleted = 0);
    DECLARE @EpfId  INT = (SELECT ComponentId FROM dbo.tbl_SalaryComponents WHERE CompanyId = @CompanyId AND ComponentCode = N'EPF'  AND IsDeleted = 0);

    DECLARE @EmpId INT, @AnnualSal DECIMAL(18,2), @HireDt DATE;
    DECLARE seed_emp CURSOR LOCAL FAST_FORWARD FOR
        SELECT e.EmployeeId, e.BaseSalary, e.HireDate
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
          AND NOT EXISTS (
              SELECT 1 FROM dbo.tbl_EmployeeSalaryStructures ss
              WHERE ss.CompanyId = @CompanyId AND ss.EmployeeId = e.EmployeeId AND ss.IsActive = 1 AND ss.IsDeleted = 0
          );

    OPEN seed_emp;
    FETCH NEXT FROM seed_emp INTO @EmpId, @AnnualSal, @HireDt;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @MonthlyGross DECIMAL(18,2) = ROUND(@AnnualSal / 12.0, 2);
        DECLARE @Basic DECIMAL(18,2) = ROUND((@MonthlyGross - 500.00) / 1.25, 2);
        IF @Basic < 1000.00 SET @Basic = ROUND(@MonthlyGross * 0.65, 2);
        DECLARE @Ot DECIMAL(18,2) = ROUND((@Basic / 160.0) * 1.5, 2);

        INSERT INTO dbo.tbl_EmployeeSalaryStructures (
            CompanyId, EmployeeId, EffectiveFrom, BasicSalaryMonthly, HourlyOvertimeRate, TaxRegime, PaymentMethod, Remarks, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @EmpId, @HireDt, @Basic, @Ot, N'Standard', N'BankTransfer', N'Initial standard enterprise compensation package', 1, 1
        );

        DECLARE @NewStructId INT = SCOPE_IDENTITY();

        INSERT INTO dbo.tbl_EmployeeSalaryStructureItems (CompanyId, SalaryStructureId, ComponentId, CalculationType, AmountOrPercent, CalculatedMonthlyAmount)
        VALUES
            (@CompanyId, @NewStructId, @HraId,  N'PercentOfBasic', 25.00,  ROUND(@Basic * 0.25, 2)),
            (@CompanyId, @NewStructId, @TraId,  N'Fixed',          300.00, 300.00),
            (@CompanyId, @NewStructId, @MedId,  N'Fixed',          200.00, 200.00),
            (@CompanyId, @NewStructId, @PfId,   N'PercentOfBasic', 5.00,   ROUND(@Basic * 0.05, 2)),
            (@CompanyId, @NewStructId, @HlthId, N'Fixed',          120.00, 120.00),
            (@CompanyId, @NewStructId, @EpfId,  N'PercentOfBasic', 5.00,   ROUND(@Basic * 0.05, 2));

        FETCH NEXT FROM seed_emp INTO @EmpId, @AnnualSal, @HireDt;
    END

    CLOSE seed_emp;
    DEALLOCATE seed_emp;

    -- 4. Populate payslips for the current month sample payroll run if it has no payslips yet
    DECLARE @SampleRunId INT, @SampleStatus NVARCHAR(20);
    SELECT @SampleRunId = PayrollRunId, @SampleStatus = PayrollStatus
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId AND PayrollYear = @Year AND PayrollMonth = @Month AND IsDeleted = 0;

    IF @SampleRunId IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Payslips WHERE PayrollRunId = @SampleRunId AND IsDeleted = 0)
    BEGIN
        IF @SampleRunId IS NOT NULL AND @SampleStatus <> N'Draft'
        BEGIN
            UPDATE dbo.tbl_PayrollRuns SET PayrollStatus = N'Draft' WHERE PayrollRunId = @SampleRunId;
        END

        DECLARE @GenId INT;
        EXEC dbo.sp_PayrollRun_Generate
            @CompanyId = @CompanyId,
            @PayrollYear = @Year,
            @PayrollMonth = @Month,
            @PayDate = NULL,
            @Notes = N'Monthly automated payroll batch',
            @UserId = 1,
            @PayrollRunId = @GenId OUTPUT;

        IF @SampleStatus IN (N'Approved', N'Locked', N'Paid')
        BEGIN
            EXEC dbo.sp_PayrollRun_UpdateStatus @CompanyId = @CompanyId, @PayrollRunId = @GenId, @PayrollStatus = N'Approved', @UserId = 1;
        END
    END
END
GO
