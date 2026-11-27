USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 8: Loan & Salary Advance Management Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_LoanType_GetAll
    @CompanyId INT,
    @IncludeInactive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        lt.LoanTypeId,
        lt.CompanyId,
        lt.TypeCode,
        lt.TypeName,
        lt.Category,
        lt.MaxAmount,
        lt.DefaultInterestRate,
        lt.MaxTenureMonths,
        lt.RequiresGuarantor,
        lt.Description,
        lt.IsActive,
        (SELECT COUNT(1) FROM dbo.tbl_EmployeeLoans el WHERE el.LoanTypeId = lt.LoanTypeId AND el.IsDeleted = 0) AS LoansCount
    FROM dbo.tbl_LoanTypes lt
    WHERE lt.CompanyId = @CompanyId
      AND lt.IsDeleted = 0
      AND (@IncludeInactive = 1 OR lt.IsActive = 1)
    ORDER BY lt.Category, lt.TypeName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LoanType_Save
    @LoanTypeId          INT,
    @CompanyId           INT,
    @TypeCode            NVARCHAR(20),
    @TypeName            NVARCHAR(100),
    @Category            NVARCHAR(20),
    @MaxAmount           DECIMAL(18,2),
    @DefaultInterestRate DECIMAL(5,2),
    @MaxTenureMonths     INT,
    @RequiresGuarantor   BIT,
    @Description         NVARCHAR(400) = NULL,
    @IsActive            BIT,
    @UserId              INT,
    @SavedId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_LoanTypes
        WHERE CompanyId = @CompanyId AND TypeCode = @TypeCode AND LoanTypeId <> @LoanTypeId AND IsDeleted = 0
    )
    BEGIN
        THROW 50081, N'A loan or advance type with this code already exists.', 1;
    END

    IF @LoanTypeId = 0
    BEGIN
        INSERT INTO dbo.tbl_LoanTypes (
            CompanyId, TypeCode, TypeName, Category, MaxAmount, DefaultInterestRate,
            MaxTenureMonths, RequiresGuarantor, Description, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @TypeCode, @TypeName, @Category, @MaxAmount, @DefaultInterestRate,
            @MaxTenureMonths, @RequiresGuarantor, @Description, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_LoanTypes
        SET TypeCode = @TypeCode,
            TypeName = @TypeName,
            Category = @Category,
            MaxAmount = @MaxAmount,
            DefaultInterestRate = @DefaultInterestRate,
            MaxTenureMonths = @MaxTenureMonths,
            RequiresGuarantor = @RequiresGuarantor,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE LoanTypeId = @LoanTypeId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50082, N'Loan type not found.', 1;
        SET @SavedId = @LoanTypeId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LoanType_Delete
    @CompanyId  INT,
    @LoanTypeId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_EmployeeLoans WHERE CompanyId = @CompanyId AND LoanTypeId = @LoanTypeId AND IsDeleted = 0)
    BEGIN
        THROW 50083, N'Cannot delete loan type with existing employee loan records. Deactivate it instead.', 1;
    END

    UPDATE dbo.tbl_LoanTypes
    SET IsDeleted = 1,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE LoanTypeId = @LoanTypeId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeLoan_GetAll
    @CompanyId  INT,
    @EmployeeId INT = NULL,
    @Status     NVARCHAR(20) = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        el.LoanId,
        el.CompanyId,
        el.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        el.LoanTypeId,
        lt.TypeCode AS LoanTypeCode,
        lt.TypeName AS LoanTypeName,
        lt.Category AS LoanCategory,
        el.LoanNumber,
        el.PrincipalAmount,
        el.InterestRatePercent,
        el.TenureMonths,
        el.MonthlyInstallment,
        el.TotalPayableAmount,
        el.PaidAmount,
        el.RemainingBalance,
        el.Purpose,
        el.RepaymentStartDate,
        el.LoanStatus,
        el.ApprovedDate,
        el.DisbursedDate,
        el.ReviewerComments,
        el.CreatedDate
    FROM dbo.tbl_EmployeeLoans el
    INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LoanTypes lt ON el.LoanTypeId = lt.LoanTypeId
    WHERE el.CompanyId = @CompanyId
      AND el.IsDeleted = 0
      AND (@EmployeeId IS NULL OR el.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR el.LoanStatus = @Status)
      AND (@SearchTerm IS NULL OR el.LoanNumber LIKE N'%' + @SearchTerm + N'%' OR e.FullName LIKE N'%' + @SearchTerm + N'%' OR e.EmployeeCode LIKE N'%' + @SearchTerm + N'%')
    ORDER BY el.CreatedDate DESC, el.LoanId DESC;

    -- Result set 2: KPI Summary
    SELECT
        COUNT(CASE WHEN el.LoanStatus = N'Active' THEN 1 END) AS ActiveLoansCount,
        COUNT(CASE WHEN el.LoanStatus = N'Pending' THEN 1 END) AS PendingApprovalCount,
        ISNULL(SUM(CASE WHEN el.LoanStatus IN (N'Active', N'Closed') THEN el.PrincipalAmount ELSE 0 END), 0) AS TotalDisbursedAmount,
        ISNULL(SUM(CASE WHEN el.LoanStatus = N'Active' THEN el.RemainingBalance ELSE 0 END), 0) AS TotalOutstandingBalance,
        ISNULL(SUM(CASE WHEN el.LoanStatus = N'Active' THEN el.MonthlyInstallment ELSE 0 END), 0) AS MonthlyRecoveryTotal
    FROM dbo.tbl_EmployeeLoans el
    WHERE el.CompanyId = @CompanyId
      AND el.IsDeleted = 0
      AND (@EmployeeId IS NULL OR el.EmployeeId = @EmployeeId);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeLoan_GetById
    @CompanyId INT,
    @LoanId    INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        el.LoanId,
        el.CompanyId,
        el.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        el.LoanTypeId,
        lt.TypeCode AS LoanTypeCode,
        lt.TypeName AS LoanTypeName,
        lt.Category AS LoanCategory,
        el.LoanNumber,
        el.PrincipalAmount,
        el.InterestRatePercent,
        el.TenureMonths,
        el.MonthlyInstallment,
        el.TotalPayableAmount,
        el.PaidAmount,
        el.RemainingBalance,
        el.Purpose,
        el.RepaymentStartDate,
        el.LoanStatus,
        el.ApprovedDate,
        el.DisbursedDate,
        el.ReviewerComments,
        el.CreatedDate
    FROM dbo.tbl_EmployeeLoans el
    INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LoanTypes lt ON el.LoanTypeId = lt.LoanTypeId
    WHERE el.CompanyId = @CompanyId AND el.LoanId = @LoanId AND el.IsDeleted = 0;

    SELECT
        s.ScheduleId,
        s.CompanyId,
        s.LoanId,
        s.InstallmentNumber,
        s.DueDate,
        s.PrincipalComponent,
        s.InterestComponent,
        s.InstallmentAmount,
        s.PaidAmount,
        s.PaymentStatus,
        s.PaidDate,
        s.PaymentReference,
        s.PayrollRunId
    FROM dbo.tbl_LoanRepaymentSchedule s
    WHERE s.CompanyId = @CompanyId AND s.LoanId = @LoanId AND s.IsDeleted = 0
    ORDER BY s.InstallmentNumber;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeLoan_Submit
    @CompanyId          INT,
    @EmployeeId         INT,
    @LoanTypeId         INT,
    @PrincipalAmount    DECIMAL(18,2),
    @TenureMonths       INT,
    @RepaymentStartDate DATE,
    @Purpose            NVARCHAR(400) = NULL,
    @UserId             INT,
    @NewId              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaxAmt DECIMAL(18,2), @InterestRate DECIMAL(5,2), @MaxTenure INT, @IsActive BIT;
    SELECT @MaxAmt = MaxAmount, @InterestRate = DefaultInterestRate, @MaxTenure = MaxTenureMonths, @IsActive = IsActive
    FROM dbo.tbl_LoanTypes
    WHERE LoanTypeId = @LoanTypeId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @IsActive IS NULL OR @IsActive = 0
    BEGIN
        THROW 50084, N'Selected loan or advance policy is not active.', 1;
    END

    IF @PrincipalAmount <= 0 OR @PrincipalAmount > @MaxAmt
    BEGIN
        THROW 50085, N'Requested amount exceeds the allowed maximum limit for this loan type.', 1;
    END

    IF @TenureMonths < 1 OR @TenureMonths > @MaxTenure
    BEGIN
        THROW 50086, N'Requested tenure exceeds the maximum months allowed for this loan type.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        THROW 50087, N'Employee not found or inactive.', 1;
    END

    -- Flat annual interest calculation pro-rated by tenure months
    DECLARE @TotalInterest DECIMAL(18,2) = ROUND(@PrincipalAmount * (@InterestRate / 100.0) * (@TenureMonths / 12.0), 2);
    DECLARE @TotalPayable DECIMAL(18,2) = @PrincipalAmount + @TotalInterest;
    DECLARE @MonthlyEmi DECIMAL(18,2) = ROUND(@TotalPayable / @TenureMonths, 2);

    DECLARE @YearPrefix NVARCHAR(10) = CONCAT(N'LN-', YEAR(SYSUTCDATETIME()), N'-');
    DECLARE @NextSeq INT = ISNULL((
        SELECT MAX(TRY_CAST(RIGHT(LoanNumber, 4) AS INT))
        FROM dbo.tbl_EmployeeLoans
        WHERE CompanyId = @CompanyId AND LoanNumber LIKE @YearPrefix + N'%'
    ), 1000) + 1;
    DECLARE @LoanNumber NVARCHAR(30) = CONCAT(@YearPrefix, RIGHT(CONCAT(N'0000', @NextSeq), 4));

    INSERT INTO dbo.tbl_EmployeeLoans (
        CompanyId, EmployeeId, LoanTypeId, LoanNumber, PrincipalAmount, InterestRatePercent,
        TenureMonths, MonthlyInstallment, TotalPayableAmount, PaidAmount, RemainingBalance,
        Purpose, RepaymentStartDate, LoanStatus, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, @LoanTypeId, @LoanNumber, @PrincipalAmount, @InterestRate,
        @TenureMonths, @MonthlyEmi, @TotalPayable, 0.00, @TotalPayable,
        @Purpose, @RepaymentStartDate, N'Pending', @UserId
    );

    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeLoan_Review
    @CompanyId          INT,
    @LoanId             INT,
    @ActionStatus       NVARCHAR(20), -- Active (Approve & Disburse), Rejected, Cancelled
    @RepaymentStartDate DATE = NULL,
    @DisbursedDate      DATE = NULL,
    @ReviewerComments   NVARCHAR(400) = NULL,
    @UserId             INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(20), @Principal DECIMAL(18,2), @TotalPayable DECIMAL(18,2),
            @Tenure INT, @DefaultStart DATE;

    SELECT
        @CurrentStatus = LoanStatus,
        @Principal = PrincipalAmount,
        @TotalPayable = TotalPayableAmount,
        @Tenure = TenureMonths,
        @DefaultStart = RepaymentStartDate
    FROM dbo.tbl_EmployeeLoans
    WHERE LoanId = @LoanId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @CurrentStatus IS NULL THROW 50088, N'Loan application not found.', 1;

    IF @ActionStatus = N'Active'
    BEGIN
        IF @CurrentStatus <> N'Pending' THROW 50089, N'Only pending loans can be approved and disbursed.', 1;

        DECLARE @EffectiveStart DATE = ISNULL(@RepaymentStartDate, @DefaultStart);
        DECLARE @EffectiveDisburse DATE = ISNULL(@DisbursedDate, CAST(SYSUTCDATETIME() AS DATE));

        BEGIN TRAN;

        UPDATE dbo.tbl_EmployeeLoans
        SET LoanStatus = N'Active',
            RepaymentStartDate = @EffectiveStart,
            DisbursedDate = @EffectiveDisburse,
            ApprovedBy = @UserId,
            ApprovedDate = SYSUTCDATETIME(),
            ReviewerComments = @ReviewerComments,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE LoanId = @LoanId AND CompanyId = @CompanyId;

        -- Build EMI schedule
        DELETE FROM dbo.tbl_LoanRepaymentSchedule WHERE LoanId = @LoanId AND CompanyId = @CompanyId;

        DECLARE @BasePrincipal DECIMAL(18,2) = ROUND(@Principal / @Tenure, 2);
        DECLARE @TotalInterest DECIMAL(18,2) = @TotalPayable - @Principal;
        DECLARE @BaseInterest DECIMAL(18,2) = ROUND(@TotalInterest / @Tenure, 2);
        DECLARE @RemPrincipal DECIMAL(18,2) = @Principal;
        DECLARE @RemInterest DECIMAL(18,2) = @TotalInterest;
        DECLARE @i INT = 1;

        WHILE @i <= @Tenure
        BEGIN
            DECLARE @InstPrincipal DECIMAL(18,2) = CASE WHEN @i = @Tenure THEN @RemPrincipal ELSE @BasePrincipal END;
            DECLARE @InstInterest  DECIMAL(18,2) = CASE WHEN @i = @Tenure THEN @RemInterest ELSE @BaseInterest END;
            DECLARE @InstAmount    DECIMAL(18,2) = @InstPrincipal + @InstInterest;
            DECLARE @DueDate       DATE = DATEADD(MONTH, @i - 1, @EffectiveStart);

            INSERT INTO dbo.tbl_LoanRepaymentSchedule (
                CompanyId, LoanId, InstallmentNumber, DueDate,
                PrincipalComponent, InterestComponent, InstallmentAmount, PaymentStatus, CreatedBy
            )
            VALUES (
                @CompanyId, @LoanId, @i, @DueDate,
                @InstPrincipal, @InstInterest, @InstAmount, N'Pending', @UserId
            );

            SET @RemPrincipal -= @InstPrincipal;
            SET @RemInterest  -= @InstInterest;
            SET @i += 1;
        END

        COMMIT TRAN;
    END
    ELSE IF @ActionStatus IN (N'Rejected', N'Cancelled')
    BEGIN
        IF @CurrentStatus <> N'Pending' THROW 50090, N'Only pending loan applications can be rejected or cancelled.', 1;

        UPDATE dbo.tbl_EmployeeLoans
        SET LoanStatus = @ActionStatus,
            ReviewerComments = @ReviewerComments,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE LoanId = @LoanId AND CompanyId = @CompanyId;
    END
    ELSE
    BEGIN
        THROW 50091, N'Invalid loan review action.', 1;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeLoan_RecordRepayment
    @CompanyId        INT,
    @ScheduleId       INT,
    @PaidDate         DATE,
    @PaymentReference NVARCHAR(80) = NULL,
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @LoanId INT, @InstAmount DECIMAL(18,2), @PayStatus NVARCHAR(20), @LoanStatus NVARCHAR(20);
    SELECT
        @LoanId = s.LoanId,
        @InstAmount = s.InstallmentAmount,
        @PayStatus = s.PaymentStatus,
        @LoanStatus = l.LoanStatus
    FROM dbo.tbl_LoanRepaymentSchedule s
    INNER JOIN dbo.tbl_EmployeeLoans l ON s.LoanId = l.LoanId
    WHERE s.ScheduleId = @ScheduleId AND s.CompanyId = @CompanyId AND s.IsDeleted = 0 AND l.IsDeleted = 0;

    IF @LoanId IS NULL THROW 50092, N'Installment not found.', 1;
    IF @LoanStatus <> N'Active' THROW 50093, N'Repayments can only be recorded on active loans.', 1;
    IF @PayStatus = N'Paid' THROW 50094, N'This installment is already paid.', 1;

    BEGIN TRAN;

    UPDATE dbo.tbl_LoanRepaymentSchedule
    SET PaidAmount = @InstAmount,
        PaymentStatus = N'Paid',
        PaidDate = @PaidDate,
        PaymentReference = @PaymentReference,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ScheduleId = @ScheduleId AND CompanyId = @CompanyId;

    DECLARE @NewPaid DECIMAL(18,2) = ISNULL((
        SELECT SUM(PaidAmount) FROM dbo.tbl_LoanRepaymentSchedule
        WHERE LoanId = @LoanId AND CompanyId = @CompanyId AND IsDeleted = 0 AND PaymentStatus = N'Paid'
    ), 0);

    UPDATE dbo.tbl_EmployeeLoans
    SET PaidAmount = @NewPaid,
        RemainingBalance = CASE WHEN TotalPayableAmount - @NewPaid < 0 THEN 0 ELSE TotalPayableAmount - @NewPaid END,
        LoanStatus = CASE WHEN TotalPayableAmount - @NewPaid <= 0 THEN N'Closed' ELSE LoanStatus END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE LoanId = @LoanId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO
