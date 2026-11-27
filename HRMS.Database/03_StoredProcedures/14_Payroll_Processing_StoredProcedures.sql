USE [HRMS_DB];
GO

-- ============================================================================
-- Payroll Runs, Generation, Payslips & Bank Transfer Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_PayrollRun_GetAll
    @CompanyId INT,
    @Year      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Payroll Runs list
    SELECT
        PayrollRunId, CompanyId, PayrollYear, PayrollMonth, PeriodLabel,
        PayDate, TotalGrossPay, TotalDeductions, TotalNetPay,
        TotalEmployerContributions, TotalTaxDeducted, ProcessedEmployees,
        PayrollStatus, Notes, ApprovedDate, LockedDate, PaidDate, CreatedDate
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId
      AND IsDeleted = 0
      AND (@Year IS NULL OR PayrollYear = @Year)
    ORDER BY PayrollYear DESC, PayrollMonth DESC, PayrollRunId DESC;

    -- 2. Summary KPIs
    SELECT
        COUNT(*) AS TotalRuns,
        SUM(CASE WHEN PayrollStatus = N'Draft' THEN 1 ELSE 0 END) AS DraftCount,
        SUM(CASE WHEN PayrollStatus IN (N'Approved', N'Locked', N'Paid') THEN 1 ELSE 0 END) AS FinalizedCount,
        ISNULL((
            SELECT TOP 1 TotalNetPay
            FROM dbo.tbl_PayrollRuns
            WHERE CompanyId = @CompanyId AND IsDeleted = 0
            ORDER BY PayrollYear DESC, PayrollMonth DESC
        ), 0) AS LatestNetPay,
        ISNULL(SUM(CASE WHEN PayrollYear = ISNULL(@Year, YEAR(GETDATE())) THEN TotalGrossPay ELSE 0 END), 0) AS YearToDateGross,
        ISNULL(SUM(CASE WHEN PayrollYear = ISNULL(@Year, YEAR(GETDATE())) THEN TotalTaxDeducted ELSE 0 END), 0) AS YearToDateTax
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PayrollRun_Generate
    @CompanyId    INT,
    @PayrollYear  INT,
    @PayrollMonth INT,
    @PayDate      DATE = NULL,
    @Notes        NVARCHAR(500) = NULL,
    @UserId       INT,
    @PayrollRunId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @PayrollMonth < 1 OR @PayrollMonth > 12
        THROW 50521, 'Payroll month must be between 1 and 12.', 1;

    DECLARE @PeriodStart DATE = DATEFROMPARTS(@PayrollYear, @PayrollMonth, 1);
    DECLARE @PeriodEnd   DATE = EOMONTH(@PeriodStart);
    DECLARE @DaysInMonth DECIMAL(5,2) = DAY(@PeriodEnd);
    DECLARE @PeriodLabel NVARCHAR(40) = FORMAT(@PeriodStart, 'MMMM yyyy');
    DECLARE @EffectivePayDate DATE = ISNULL(@PayDate, @PeriodEnd);

    DECLARE @ExistingId INT, @ExistingStatus NVARCHAR(20), @ExistingDeleted BIT;
    SELECT @ExistingId = PayrollRunId, @ExistingStatus = PayrollStatus, @ExistingDeleted = IsDeleted
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId AND PayrollYear = @PayrollYear AND PayrollMonth = @PayrollMonth;

    IF @ExistingId IS NOT NULL AND @ExistingDeleted = 0 AND @ExistingStatus <> N'Draft'
        THROW 50522, 'Payroll for this period has already been Approved, Locked, or Paid and cannot be regenerated.', 1;

    DECLARE @EffectiveTaxYear INT = ISNULL(
        (SELECT TOP 1 TaxYear FROM dbo.tbl_TaxSlabs WHERE CompanyId = @CompanyId AND TaxYear = @PayrollYear AND IsDeleted = 0 AND IsActive = 1),
        (SELECT TOP 1 TaxYear FROM dbo.tbl_TaxSlabs WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1 ORDER BY TaxYear DESC)
    );

    BEGIN TRAN;

    IF @ExistingId IS NULL
    BEGIN
        INSERT INTO dbo.tbl_PayrollRuns (
            CompanyId, PayrollYear, PayrollMonth, PeriodLabel, PayDate,
            PayrollStatus, Notes, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @PayrollYear, @PayrollMonth, @PeriodLabel, @EffectivePayDate,
            N'Draft', @Notes, 0, @UserId, SYSUTCDATETIME()
        );
        SET @PayrollRunId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SET @PayrollRunId = @ExistingId;

        DELETE pl
        FROM dbo.tbl_PayslipLines pl
        INNER JOIN dbo.tbl_Payslips p ON pl.PayslipId = p.PayslipId
        WHERE p.PayrollRunId = @PayrollRunId AND p.CompanyId = @CompanyId;

        DELETE FROM dbo.tbl_Payslips
        WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId;

        UPDATE dbo.tbl_PayrollRuns
        SET PeriodLabel = @PeriodLabel,
            PayDate = @EffectivePayDate,
            Notes = @Notes,
            PayrollStatus = N'Draft',
            IsDeleted = 0,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE PayrollRunId = @PayrollRunId;
    END

    -- Cursor over active eligible employees (hired on or before period end)
    DECLARE @EmpId INT, @BranchId INT, @DeptId INT, @DesigId INT, @HireDate DATE,
            @BaseSalary DECIMAL(18,2), @BankName NVARCHAR(120), @BankAcct NVARCHAR(50), @BankRouting NVARCHAR(40);

    DECLARE emp_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT EmployeeId, BranchId, DepartmentId, DesignationId, HireDate, BaseSalary,
               BankName, BankAccountNumber, BankRoutingCode
        FROM dbo.tbl_Employees
        WHERE CompanyId = @CompanyId
          AND IsDeleted = 0
          AND IsActive = 1
          AND HireDate <= @PeriodEnd;

    OPEN emp_cur;
    FETCH NEXT FROM emp_cur INTO @EmpId, @BranchId, @DeptId, @DesigId, @HireDate, @BaseSalary, @BankName, @BankAcct, @BankRouting;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @StructId INT = NULL, @BasicMonthly DECIMAL(18,2) = 0, @OtRate DECIMAL(18,2) = 0, @PayMethod NVARCHAR(30) = N'BankTransfer';

        SELECT TOP 1
            @StructId = SalaryStructureId,
            @BasicMonthly = BasicSalaryMonthly,
            @OtRate = HourlyOvertimeRate,
            @PayMethod = PaymentMethod
        FROM dbo.tbl_EmployeeSalaryStructures
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND IsActive = 1 AND IsDeleted = 0 AND EffectiveFrom <= @PeriodEnd
        ORDER BY EffectiveFrom DESC, SalaryStructureId DESC;

        IF @StructId IS NULL
        BEGIN
            SET @BasicMonthly = ROUND(@BaseSalary / 12.0, 2);
            SET @OtRate = ROUND((@BasicMonthly / 160.0) * 1.5, 2);
        END
        ELSE IF @OtRate <= 0
        BEGIN
            SET @OtRate = ROUND((@BasicMonthly / 160.0) * 1.5, 2);
        END

        -- Structure recurring sums
        DECLARE @Allowances DECIMAL(18,2) = 0, @TaxableAllowances DECIMAL(18,2) = 0,
                @StatutoryDed DECIMAL(18,2) = 0, @OtherDed DECIMAL(18,2) = 0, @EmpContrib DECIMAL(18,2) = 0;

        IF @StructId IS NOT NULL
        BEGIN
            SELECT
                @Allowances = ISNULL(SUM(CASE WHEN sc.ComponentType = N'Earning' THEN si.CalculatedMonthlyAmount ELSE 0 END), 0),
                @TaxableAllowances = ISNULL(SUM(CASE WHEN sc.ComponentType = N'Earning' AND sc.IsTaxable = 1 THEN si.CalculatedMonthlyAmount ELSE 0 END), 0),
                @StatutoryDed = ISNULL(SUM(CASE WHEN sc.ComponentType = N'Deduction' AND sc.IsStatutory = 1 THEN si.CalculatedMonthlyAmount ELSE 0 END), 0),
                @OtherDed = ISNULL(SUM(CASE WHEN sc.ComponentType = N'Deduction' AND sc.IsStatutory = 0 THEN si.CalculatedMonthlyAmount ELSE 0 END), 0),
                @EmpContrib = ISNULL(SUM(CASE WHEN sc.ComponentType = N'EmployerContribution' THEN si.CalculatedMonthlyAmount ELSE 0 END), 0)
            FROM dbo.tbl_EmployeeSalaryStructureItems si
            INNER JOIN dbo.tbl_SalaryComponents sc ON si.ComponentId = sc.ComponentId
            WHERE si.SalaryStructureId = @StructId AND si.IsDeleted = 0 AND sc.IsDeleted = 0;
        END

        -- Attendance absences & unpaid leaves in this period
        DECLARE @AbsentDays DECIMAL(5,2) = ISNULL((
            SELECT CAST(COUNT(1) AS DECIMAL(5,2))
            FROM dbo.tbl_Attendance
            WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND IsDeleted = 0
              AND AttendanceDate BETWEEN @PeriodStart AND @PeriodEnd AND Status = N'Absent'
        ), 0);

        DECLARE @UnpaidLeaveDays DECIMAL(5,2) = ISNULL((
            SELECT SUM(
                CASE
                    WHEN la.IsHalfDay = 1 THEN 0.50
                    ELSE CAST(DATEDIFF(DAY,
                        CASE WHEN la.StartDate < @PeriodStart THEN @PeriodStart ELSE la.StartDate END,
                        CASE WHEN la.EndDate > @PeriodEnd THEN @PeriodEnd ELSE la.EndDate END
                    ) + 1 AS DECIMAL(5,2))
                END
            )
            FROM dbo.tbl_LeaveApplications la
            INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
            WHERE la.CompanyId = @CompanyId AND la.EmployeeId = @EmpId AND la.IsDeleted = 0
              AND la.ApprovalStatus = N'Approved' AND lt.IsPaid = 0
              AND la.StartDate <= @PeriodEnd AND la.EndDate >= @PeriodStart
        ), 0);

        -- Prorate if hired mid-month
        DECLARE @PreHireDays DECIMAL(5,2) = CASE WHEN @HireDate > @PeriodStart THEN CAST(DATEDIFF(DAY, @PeriodStart, @HireDate) AS DECIMAL(5,2)) ELSE 0 END;
        DECLARE @NonPaidTotal DECIMAL(5,2) = @AbsentDays + @UnpaidLeaveDays + @PreHireDays;
        IF @NonPaidTotal > @DaysInMonth SET @NonPaidTotal = @DaysInMonth;
        DECLARE @PaidDays DECIMAL(5,2) = @DaysInMonth - @NonPaidTotal;

        DECLARE @ScheduledEarnings DECIMAL(18,2) = @BasicMonthly + @Allowances;
        DECLARE @UnpaidAbsenceDed DECIMAL(18,2) = CASE
            WHEN @NonPaidTotal > 0 AND @DaysInMonth > 0 THEN ROUND((@ScheduledEarnings / @DaysInMonth) * @NonPaidTotal, 2)
            ELSE 0
        END;

        -- Approved Overtime
        DECLARE @OtHours DECIMAL(6,2) = ISNULL((
            SELECT SUM(ApprovedHours)
            FROM dbo.tbl_OvertimeRequests
            WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND IsDeleted = 0
              AND ApprovalStatus = N'Approved' AND OvertimeDate BETWEEN @PeriodStart AND @PeriodEnd
        ), 0);
        DECLARE @OtPay DECIMAL(18,2) = ROUND(@OtHours * @OtRate, 2);

        -- Approved Leave Encashment in period
        DECLARE @EncashPay DECIMAL(18,2) = ISNULL((
            SELECT SUM(EncashmentAmount)
            FROM dbo.tbl_LeaveEncashments
            WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND IsDeleted = 0
              AND ApprovalStatus = N'Approved' AND LeaveYear = @PayrollYear
              AND MONTH(ISNULL(ReviewedDate, CreatedDate)) = @PayrollMonth
        ), 0);

        DECLARE @GrossPay DECIMAL(18,2) = @BasicMonthly + @Allowances + @OtPay + @EncashPay;

        -- Progressive Annual Income Tax
        DECLARE @TaxableMonthly DECIMAL(18,2) = (@BasicMonthly + @TaxableAllowances + @OtPay + @EncashPay) - @UnpaidAbsenceDed;
        IF @TaxableMonthly < 0 SET @TaxableMonthly = 0;
        DECLARE @AnnualTaxable DECIMAL(18,2) = @TaxableMonthly * 12.0;

        DECLARE @MonthlyTax DECIMAL(18,2) = 0;
        IF @EffectiveTaxYear IS NOT NULL AND @AnnualTaxable > 0
        BEGIN
            SELECT @MonthlyTax = ROUND(ISNULL(SUM(
                ts.FixedTaxAmount +
                CASE
                    WHEN @AnnualTaxable <= ts.MinAnnualIncome THEN 0
                    WHEN ts.MaxAnnualIncome IS NULL OR @AnnualTaxable <= ts.MaxAnnualIncome THEN (@AnnualTaxable - ts.MinAnnualIncome) * (ts.TaxRatePercent / 100.0)
                    ELSE (ts.MaxAnnualIncome - ts.MinAnnualIncome) * (ts.TaxRatePercent / 100.0)
                END
            ), 0) / 12.0, 2)
            FROM dbo.tbl_TaxSlabs ts
            WHERE ts.CompanyId = @CompanyId AND ts.TaxYear = @EffectiveTaxYear AND ts.IsDeleted = 0 AND ts.IsActive = 1;
        END

        DECLARE @TotalDed DECIMAL(18,2) = @MonthlyTax + @StatutoryDed + @OtherDed + @UnpaidAbsenceDed;
        DECLARE @NetPay DECIMAL(18,2) = @GrossPay - @TotalDed;
        IF @NetPay < 0 SET @NetPay = 0;

        INSERT INTO dbo.tbl_Payslips (
            CompanyId, PayrollRunId, EmployeeId, BranchId, DepartmentId, DesignationId,
            WorkingDaysInMonth, PaidDays, UnpaidLeaveDays, AbsentDays, OvertimeHours,
            BasicPay, TotalAllowances, OvertimePay, LeaveEncashmentPay, BonusAmount, GrossPay,
            IncomeTax, StatutoryDeductions, OtherDeductions, UnpaidAbsenceDeduction, TotalDeductions,
            NetPay, EmployerContributions, PaymentMethod, BankName, BankAccountNumber, BankRoutingCode,
            PaymentStatus, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @PayrollRunId, @EmpId, @BranchId, @DeptId, @DesigId,
            @DaysInMonth, @PaidDays, @UnpaidLeaveDays, @AbsentDays, @OtHours,
            @BasicMonthly, @Allowances, @OtPay, @EncashPay, 0, @GrossPay,
            @MonthlyTax, @StatutoryDed, @OtherDed, @UnpaidAbsenceDed, @TotalDed,
            @NetPay, @EmpContrib, @PayMethod, @BankName, @BankAcct, @BankRouting,
            N'Pending', @UserId, SYSUTCDATETIME()
        );

        DECLARE @PayslipId INT = SCOPE_IDENTITY();

        -- Itemized Lines
        INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
        VALUES (@CompanyId, @PayslipId, NULL, N'BASIC', N'Basic Salary', N'Earning', @BasicMonthly, 1);

        IF @StructId IS NOT NULL
        BEGIN
            INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
            SELECT @CompanyId, @PayslipId, sc.ComponentId, sc.ComponentCode, sc.ComponentName, sc.ComponentType, si.CalculatedMonthlyAmount, sc.DisplayOrder
            FROM dbo.tbl_EmployeeSalaryStructureItems si
            INNER JOIN dbo.tbl_SalaryComponents sc ON si.ComponentId = sc.ComponentId
            WHERE si.SalaryStructureId = @StructId AND si.IsDeleted = 0 AND sc.IsDeleted = 0 AND si.CalculatedMonthlyAmount > 0;
        END

        IF @OtPay > 0
            INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
            VALUES (@CompanyId, @PayslipId, NULL, N'OT', N'Overtime Pay', N'Earning', @OtPay, 80);

        IF @EncashPay > 0
            INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
            VALUES (@CompanyId, @PayslipId, NULL, N'ENCASH', N'Leave Encashment', N'Earning', @EncashPay, 85);

        IF @MonthlyTax > 0
            INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
            VALUES (@CompanyId, @PayslipId, NULL, N'TAX', N'Income Tax (PAYE)', N'Deduction', @MonthlyTax, 90);

        IF @UnpaidAbsenceDed > 0
            INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
            VALUES (@CompanyId, @PayslipId, NULL, N'LWP', N'Unpaid Leave / Absence Deduction', N'Deduction', @UnpaidAbsenceDed, 95);

        FETCH NEXT FROM emp_cur INTO @EmpId, @BranchId, @DeptId, @DesigId, @HireDate, @BaseSalary, @BankName, @BankAcct, @BankRouting;
    END

    CLOSE emp_cur;
    DEALLOCATE emp_cur;

    -- Refresh Payroll Run Totals
    UPDATE pr
    SET TotalGrossPay = ISNULL(agg.SumGross, 0),
        TotalDeductions = ISNULL(agg.SumDed, 0),
        TotalNetPay = ISNULL(agg.SumNet, 0),
        TotalEmployerContributions = ISNULL(agg.SumContrib, 0),
        TotalTaxDeducted = ISNULL(agg.SumTax, 0),
        ProcessedEmployees = ISNULL(agg.Cnt, 0),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    FROM dbo.tbl_PayrollRuns pr
    CROSS APPLY (
        SELECT
            COUNT(1) AS Cnt,
            SUM(GrossPay) AS SumGross,
            SUM(TotalDeductions) AS SumDed,
            SUM(NetPay) AS SumNet,
            SUM(EmployerContributions) AS SumContrib,
            SUM(IncomeTax) AS SumTax
        FROM dbo.tbl_Payslips p
        WHERE p.PayrollRunId = pr.PayrollRunId AND p.IsDeleted = 0
    ) agg
    WHERE pr.PayrollRunId = @PayrollRunId;

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PayrollRun_GetDetails
    @CompanyId    INT,
    @PayrollRunId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Header
    SELECT
        PayrollRunId, CompanyId, PayrollYear, PayrollMonth, PeriodLabel,
        PayDate, TotalGrossPay, TotalDeductions, TotalNetPay,
        TotalEmployerContributions, TotalTaxDeducted, ProcessedEmployees,
        PayrollStatus, Notes, ApprovedDate, LockedDate, PaidDate, CreatedDate
    FROM dbo.tbl_PayrollRuns
    WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId AND IsDeleted = 0;

    -- 2. Payslips
    SELECT
        p.*,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        desig.DesignationTitle,
        b.BranchName
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_Employees e ON p.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON p.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON p.DesignationId = desig.DesignationId
    INNER JOIN dbo.tbl_Branches b ON p.BranchId = b.BranchId
    WHERE p.PayrollRunId = @PayrollRunId AND p.CompanyId = @CompanyId AND p.IsDeleted = 0
    ORDER BY e.EmployeeCode;

    -- 3. Department Cost Breakdown
    SELECT
        d.DepartmentName,
        COUNT(p.PayslipId) AS Headcount,
        SUM(p.GrossPay) AS TotalGross,
        SUM(p.TotalDeductions) AS TotalDeductions,
        SUM(p.NetPay) AS TotalNet
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_Departments d ON p.DepartmentId = d.DepartmentId
    WHERE p.PayrollRunId = @PayrollRunId AND p.CompanyId = @CompanyId AND p.IsDeleted = 0
    GROUP BY d.DepartmentName
    ORDER BY TotalGross DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PayrollRun_UpdateStatus
    @CompanyId     INT,
    @PayrollRunId  INT,
    @PayrollStatus NVARCHAR(20),
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(20), @PeriodLabel NVARCHAR(40);
    SELECT @CurrentStatus = PayrollStatus, @PeriodLabel = PeriodLabel
    FROM dbo.tbl_PayrollRuns
    WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @CurrentStatus IS NULL
        THROW 50523, 'Payroll run not found.', 1;

    IF @PayrollStatus = N'Approved' AND @CurrentStatus <> N'Draft'
        THROW 50524, 'Only Draft payroll runs can be Approved.', 1;

    IF @PayrollStatus = N'Locked' AND @CurrentStatus NOT IN (N'Draft', N'Approved')
        THROW 50525, 'Payroll run cannot be Locked from its current state.', 1;

    IF @PayrollStatus = N'Paid' AND @CurrentStatus NOT IN (N'Approved', N'Locked')
        THROW 50526, 'Payroll run must be Approved or Locked before marking as Paid.', 1;

    BEGIN TRAN;

    UPDATE dbo.tbl_PayrollRuns
    SET PayrollStatus = @PayrollStatus,
        ApprovedBy = CASE WHEN @PayrollStatus = N'Approved' THEN @UserId ELSE ApprovedBy END,
        ApprovedDate = CASE WHEN @PayrollStatus = N'Approved' THEN SYSUTCDATETIME() ELSE ApprovedDate END,
        LockedBy = CASE WHEN @PayrollStatus = N'Locked' THEN @UserId ELSE LockedBy END,
        LockedDate = CASE WHEN @PayrollStatus = N'Locked' THEN SYSUTCDATETIME() ELSE LockedDate END,
        PaidDate = CASE WHEN @PayrollStatus = N'Paid' THEN SYSUTCDATETIME() ELSE PaidDate END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId;

    IF @PayrollStatus = N'Paid'
    BEGIN
        UPDATE dbo.tbl_Payslips
        SET PaymentStatus = CASE WHEN PaymentStatus = N'Hold' THEN N'Hold' ELSE N'Paid' END,
            PaidDate = CASE WHEN PaymentStatus = N'Hold' THEN PaidDate ELSE SYSUTCDATETIME() END,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId AND IsDeleted = 0;
    END

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PayrollRun_Delete
    @CompanyId    INT,
    @PayrollRunId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Status NVARCHAR(20);
    SELECT @Status = PayrollStatus FROM dbo.tbl_PayrollRuns WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 50527, 'Payroll run not found.', 1;
    IF @Status <> N'Draft'
        THROW 50528, 'Only Draft payroll runs can be deleted.', 1;

    BEGIN TRAN;

    UPDATE dbo.tbl_Payslips
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId;

    UPDATE dbo.tbl_PayrollRuns
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE PayrollRunId = @PayrollRunId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payslip_Adjust
    @CompanyId       INT,
    @PayslipId       INT,
    @BonusAmount     DECIMAL(18,2),
    @OtherDeductions DECIMAL(18,2),
    @PaymentStatus   NVARCHAR(20),
    @Notes           NVARCHAR(300) = NULL,
    @UserId          INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RunId INT, @RunStatus NVARCHAR(20);
    SELECT @RunId = p.PayrollRunId, @RunStatus = pr.PayrollStatus
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
    WHERE p.PayslipId = @PayslipId AND p.CompanyId = @CompanyId AND p.IsDeleted = 0;

    IF @RunId IS NULL
        THROW 50529, 'Payslip not found.', 1;

    IF @RunStatus <> N'Draft'
        THROW 50530, 'Payslip amounts can only be adjusted while the payroll run is in Draft status.', 1;

    BEGIN TRAN;

    DECLARE @Gross DECIMAL(18,2), @Ded DECIMAL(18,2), @Net DECIMAL(18,2);

    UPDATE dbo.tbl_Payslips
    SET BonusAmount = @BonusAmount,
        OtherDeductions = @OtherDeductions,
        PaymentStatus = @PaymentStatus,
        Notes = @Notes,
        GrossPay = BasicPay + TotalAllowances + OvertimePay + LeaveEncashmentPay + @BonusAmount,
        TotalDeductions = IncomeTax + StatutoryDeductions + @OtherDeductions + UnpaidAbsenceDeduction,
        NetPay = CASE
            WHEN (BasicPay + TotalAllowances + OvertimePay + LeaveEncashmentPay + @BonusAmount) - (IncomeTax + StatutoryDeductions + @OtherDeductions + UnpaidAbsenceDeduction) < 0 THEN 0
            ELSE (BasicPay + TotalAllowances + OvertimePay + LeaveEncashmentPay + @BonusAmount) - (IncomeTax + StatutoryDeductions + @OtherDeductions + UnpaidAbsenceDeduction)
        END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE PayslipId = @PayslipId AND CompanyId = @CompanyId;

    -- Refresh bonus / manual deduction line items
    DELETE FROM dbo.tbl_PayslipLines
    WHERE PayslipId = @PayslipId AND CompanyId = @CompanyId AND ComponentCode IN (N'BONUS', N'ADJ_DED');

    IF @BonusAmount > 0
        INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
        VALUES (@CompanyId, @PayslipId, NULL, N'BONUS', N'Bonus / Incentive', N'Earning', @BonusAmount, 88);

    DECLARE @StructOtherDed DECIMAL(18,2) = ISNULL((
        SELECT SUM(Amount) FROM dbo.tbl_PayslipLines pl
        INNER JOIN dbo.tbl_SalaryComponents sc ON pl.ComponentId = sc.ComponentId
        WHERE pl.PayslipId = @PayslipId AND pl.ComponentType = N'Deduction' AND sc.IsStatutory = 0
    ), 0);
    DECLARE @ExtraManualDed DECIMAL(18,2) = @OtherDeductions - @StructOtherDed;
    IF @ExtraManualDed > 0
        INSERT INTO dbo.tbl_PayslipLines (CompanyId, PayslipId, ComponentId, ComponentCode, ComponentName, ComponentType, Amount, DisplayOrder)
        VALUES (@CompanyId, @PayslipId, NULL, N'ADJ_DED', N'Manual Adjustment Deduction', N'Deduction', @ExtraManualDed, 98);

    -- Refresh parent run totals
    UPDATE pr
    SET TotalGrossPay = ISNULL(agg.SumGross, 0),
        TotalDeductions = ISNULL(agg.SumDed, 0),
        TotalNetPay = ISNULL(agg.SumNet, 0),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    FROM dbo.tbl_PayrollRuns pr
    CROSS APPLY (
        SELECT SUM(GrossPay) AS SumGross, SUM(TotalDeductions) AS SumDed, SUM(NetPay) AS SumNet
        FROM dbo.tbl_Payslips WHERE PayrollRunId = pr.PayrollRunId AND IsDeleted = 0
    ) agg
    WHERE pr.PayrollRunId = @RunId;

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payslip_GetById
    @CompanyId INT,
    @PayslipId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Payslip + Employee + Company Info
    SELECT
        p.*,
        e.EmployeeCode,
        e.FullName,
        e.WorkEmail,
        e.TaxIdentificationNumber,
        d.DepartmentName,
        desig.DesignationTitle,
        b.BranchName,
        pr.PeriodLabel,
        pr.PayrollYear,
        pr.PayrollMonth,
        pr.PayDate,
        pr.PayrollStatus,
        c.CompanyName,
        c.LegalName,
        c.AddressLine1 AS CompanyAddress,
        c.CurrencyCode
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
    INNER JOIN dbo.tbl_Employees e ON p.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON p.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON p.DesignationId = desig.DesignationId
    INNER JOIN dbo.tbl_Branches b ON p.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Companies c ON p.CompanyId = c.CompanyId
    WHERE p.PayslipId = @PayslipId AND p.CompanyId = @CompanyId AND p.IsDeleted = 0;

    -- 2. Itemized Lines
    SELECT
        PayslipLineId, CompanyId, PayslipId, ComponentId, ComponentCode,
        ComponentName, ComponentType, Amount, DisplayOrder
    FROM dbo.tbl_PayslipLines
    WHERE PayslipId = @PayslipId AND CompanyId = @CompanyId
    ORDER BY ComponentType, DisplayOrder, ComponentName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Payroll_GetBankTransferList
    @CompanyId    INT,
    @PayrollRunId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.EmployeeCode,
        e.FullName,
        ISNULL(e.BankAccountTitle, e.FullName) AS BankAccountTitle,
        ISNULL(p.BankName, N'Pending Bank Setup') AS BankName,
        ISNULL(p.BankAccountNumber, N'') AS BankAccountNumber,
        ISNULL(p.BankRoutingCode, N'') AS BankRoutingCode,
        p.PaymentMethod,
        p.PaymentStatus,
        p.NetPay
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_Employees e ON p.EmployeeId = e.EmployeeId
    WHERE p.PayrollRunId = @PayrollRunId
      AND p.CompanyId = @CompanyId
      AND p.IsDeleted = 0
      AND p.PaymentStatus <> N'Hold'
      AND p.NetPay > 0
    ORDER BY e.EmployeeCode;
END
GO
