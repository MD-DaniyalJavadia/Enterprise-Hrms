USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 8: Expense Management Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseCategory_GetAll
    @CompanyId INT,
    @IncludeInactive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ec.CategoryId,
        ec.CompanyId,
        ec.CategoryCode,
        ec.CategoryName,
        ec.MaxLimitPerClaim,
        ec.RequiresReceipt,
        ec.Description,
        ec.IsActive,
        (SELECT COUNT(1) FROM dbo.tbl_ExpenseClaims c WHERE c.CategoryId = ec.CategoryId AND c.IsDeleted = 0) AS ClaimsCount
    FROM dbo.tbl_ExpenseCategories ec
    WHERE ec.CompanyId = @CompanyId
      AND ec.IsDeleted = 0
      AND (@IncludeInactive = 1 OR ec.IsActive = 1)
    ORDER BY ec.CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseCategory_Save
    @CategoryId       INT,
    @CompanyId        INT,
    @CategoryCode     NVARCHAR(20),
    @CategoryName     NVARCHAR(100),
    @MaxLimitPerClaim DECIMAL(18,2) = NULL,
    @RequiresReceipt  BIT,
    @Description      NVARCHAR(400) = NULL,
    @IsActive         BIT,
    @UserId           INT,
    @SavedId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_ExpenseCategories
        WHERE CompanyId = @CompanyId AND CategoryCode = @CategoryCode AND CategoryId <> @CategoryId AND IsDeleted = 0
    )
    BEGIN
        THROW 50095, N'An expense category with this code already exists.', 1;
    END

    IF @CategoryId = 0
    BEGIN
        INSERT INTO dbo.tbl_ExpenseCategories (
            CompanyId, CategoryCode, CategoryName, MaxLimitPerClaim, RequiresReceipt, Description, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @CategoryCode, @CategoryName, @MaxLimitPerClaim, @RequiresReceipt, @Description, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_ExpenseCategories
        SET CategoryCode = @CategoryCode,
            CategoryName = @CategoryName,
            MaxLimitPerClaim = @MaxLimitPerClaim,
            RequiresReceipt = @RequiresReceipt,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50096, N'Expense category not found.', 1;
        SET @SavedId = @CategoryId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseCategory_Delete
    @CompanyId  INT,
    @CategoryId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_ExpenseClaims WHERE CompanyId = @CompanyId AND CategoryId = @CategoryId AND IsDeleted = 0)
    BEGIN
        THROW 50097, N'Cannot delete category referenced by expense claims. Deactivate it instead.', 1;
    END

    UPDATE dbo.tbl_ExpenseCategories
    SET IsDeleted = 1,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseClaim_GetAll
    @CompanyId  INT,
    @EmployeeId INT = NULL,
    @CategoryId INT = NULL,
    @Status     NVARCHAR(20) = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ClaimId,
        c.CompanyId,
        c.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        c.CategoryId,
        ec.CategoryCode,
        ec.CategoryName,
        c.ClaimNumber,
        c.ExpenseDate,
        c.MerchantOrVendor,
        c.ClaimAmount,
        c.ApprovedAmount,
        c.CurrencyCode,
        c.Description,
        c.ReceiptFileName,
        c.ReceiptStoredName,
        c.ClaimStatus,
        c.ReviewedDate,
        c.ReviewerComments,
        c.ReimbursedDate,
        c.PaymentReference,
        c.CreatedDate
    FROM dbo.tbl_ExpenseClaims c
    INNER JOIN dbo.tbl_Employees e ON c.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_ExpenseCategories ec ON c.CategoryId = ec.CategoryId
    WHERE c.CompanyId = @CompanyId
      AND c.IsDeleted = 0
      AND (@EmployeeId IS NULL OR c.EmployeeId = @EmployeeId)
      AND (@CategoryId IS NULL OR c.CategoryId = @CategoryId)
      AND (@Status IS NULL OR c.ClaimStatus = @Status)
      AND (@SearchTerm IS NULL OR c.ClaimNumber LIKE N'%' + @SearchTerm + N'%' OR c.MerchantOrVendor LIKE N'%' + @SearchTerm + N'%' OR e.FullName LIKE N'%' + @SearchTerm + N'%')
    ORDER BY c.ExpenseDate DESC, c.ClaimId DESC;

    -- Result set 2: KPI Summary
    SELECT
        COUNT(1) AS TotalClaimsCount,
        COUNT(CASE WHEN c.ClaimStatus = N'Submitted' THEN 1 END) AS SubmittedCount,
        COUNT(CASE WHEN c.ClaimStatus = N'Approved' THEN 1 END) AS ApprovedAwaitingPayoutCount,
        ISNULL(SUM(CASE WHEN c.ClaimStatus = N'Submitted' THEN c.ClaimAmount ELSE 0 END), 0) AS TotalPendingAmount,
        ISNULL(SUM(CASE WHEN c.ClaimStatus = N'Reimbursed' AND YEAR(ISNULL(c.ReimbursedDate, c.CreatedDate)) = YEAR(SYSUTCDATETIME()) THEN ISNULL(c.ApprovedAmount, c.ClaimAmount) ELSE 0 END), 0) AS TotalReimbursedYtd
    FROM dbo.tbl_ExpenseClaims c
    WHERE c.CompanyId = @CompanyId
      AND c.IsDeleted = 0
      AND (@EmployeeId IS NULL OR c.EmployeeId = @EmployeeId);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseClaim_GetById
    @CompanyId INT,
    @ClaimId   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ClaimId, CompanyId, EmployeeId, CategoryId, ClaimNumber, ExpenseDate,
        MerchantOrVendor, ClaimAmount, ApprovedAmount, CurrencyCode,
        ReceiptFileName, ReceiptStoredName, ClaimStatus
    FROM dbo.tbl_ExpenseClaims
    WHERE CompanyId = @CompanyId AND ClaimId = @ClaimId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseClaim_Submit
    @CompanyId         INT,
    @EmployeeId        INT,
    @CategoryId        INT,
    @ExpenseDate       DATE,
    @MerchantOrVendor  NVARCHAR(150),
    @ClaimAmount       DECIMAL(18,2),
    @Description       NVARCHAR(500) = NULL,
    @ReceiptFileName   NVARCHAR(255) = NULL,
    @ReceiptStoredName NVARCHAR(255) = NULL,
    @UserId            INT,
    @NewId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsActive BIT, @MaxLimit DECIMAL(18,2), @RequiresReceipt BIT;
    SELECT @IsActive = IsActive, @MaxLimit = MaxLimitPerClaim, @RequiresReceipt = RequiresReceipt
    FROM dbo.tbl_ExpenseCategories
    WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @IsActive IS NULL OR @IsActive = 0
    BEGIN
        THROW 50098, N'Selected expense category is not active.', 1;
    END

    IF @MaxLimit IS NOT NULL AND @ClaimAmount > @MaxLimit
    BEGIN
        THROW 50099, N'Claim amount exceeds the maximum limit allowed for this expense category.', 1;
    END

    IF @RequiresReceipt = 1 AND NULLIF(LTRIM(RTRIM(@ReceiptStoredName)), N'') IS NULL
    BEGIN
        THROW 50100, N'A receipt upload is required for this expense category.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        THROW 50101, N'Employee not found or inactive.', 1;
    END

    DECLARE @YearPrefix NVARCHAR(12) = CONCAT(N'EXP-', YEAR(SYSUTCDATETIME()), N'-');
    DECLARE @NextSeq INT = ISNULL((
        SELECT MAX(TRY_CAST(RIGHT(ClaimNumber, 4) AS INT))
        FROM dbo.tbl_ExpenseClaims
        WHERE CompanyId = @CompanyId AND ClaimNumber LIKE @YearPrefix + N'%'
    ), 1000) + 1;
    DECLARE @ClaimNumber NVARCHAR(30) = CONCAT(@YearPrefix, RIGHT(CONCAT(N'0000', @NextSeq), 4));

    INSERT INTO dbo.tbl_ExpenseClaims (
        CompanyId, EmployeeId, CategoryId, ClaimNumber, ExpenseDate, MerchantOrVendor,
        ClaimAmount, Description, ReceiptFileName, ReceiptStoredName, ClaimStatus, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, @CategoryId, @ClaimNumber, @ExpenseDate, @MerchantOrVendor,
        @ClaimAmount, @Description, @ReceiptFileName, @ReceiptStoredName, N'Submitted', @UserId
    );

    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExpenseClaim_Review
    @CompanyId        INT,
    @ClaimId          INT,
    @ActionStatus     NVARCHAR(20), -- Approved, Rejected, Reimbursed, Cancelled
    @ApprovedAmount   DECIMAL(18,2) = NULL,
    @ReviewerComments NVARCHAR(400) = NULL,
    @PaymentReference NVARCHAR(80) = NULL,
    @ReimbursedDate   DATE = NULL,
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus NVARCHAR(20), @ClaimAmt DECIMAL(18,2);
    SELECT @CurrentStatus = ClaimStatus, @ClaimAmt = ClaimAmount
    FROM dbo.tbl_ExpenseClaims
    WHERE ClaimId = @ClaimId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @CurrentStatus IS NULL THROW 50102, N'Expense claim not found.', 1;

    IF @ActionStatus = N'Approved'
    BEGIN
        IF @CurrentStatus <> N'Submitted' THROW 50103, N'Only submitted claims can be approved.', 1;
        DECLARE @FinalApproved DECIMAL(18,2) = ISNULL(@ApprovedAmount, @ClaimAmt);
        IF @FinalApproved <= 0 OR @FinalApproved > @ClaimAmt THROW 50104, N'Approved amount must be between 0.01 and the claimed amount.', 1;

        UPDATE dbo.tbl_ExpenseClaims
        SET ClaimStatus = N'Approved',
            ApprovedAmount = @FinalApproved,
            ReviewedBy = @UserId,
            ReviewedDate = SYSUTCDATETIME(),
            ReviewerComments = @ReviewerComments,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ClaimId = @ClaimId AND CompanyId = @CompanyId;
    END
    ELSE IF @ActionStatus = N'Rejected'
    BEGIN
        IF @CurrentStatus <> N'Submitted' THROW 50105, N'Only submitted claims can be rejected.', 1;

        UPDATE dbo.tbl_ExpenseClaims
        SET ClaimStatus = N'Rejected',
            ApprovedAmount = 0,
            ReviewedBy = @UserId,
            ReviewedDate = SYSUTCDATETIME(),
            ReviewerComments = @ReviewerComments,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ClaimId = @ClaimId AND CompanyId = @CompanyId;
    END
    ELSE IF @ActionStatus = N'Reimbursed'
    BEGIN
        IF @CurrentStatus <> N'Approved' THROW 50106, N'Only approved claims can be marked as reimbursed.', 1;

        UPDATE dbo.tbl_ExpenseClaims
        SET ClaimStatus = N'Reimbursed',
            ReimbursedDate = ISNULL(@ReimbursedDate, CAST(SYSUTCDATETIME() AS DATE)),
            PaymentReference = @PaymentReference,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ClaimId = @ClaimId AND CompanyId = @CompanyId;
    END
    ELSE IF @ActionStatus = N'Cancelled'
    BEGIN
        IF @CurrentStatus <> N'Submitted' THROW 50107, N'Only submitted claims can be cancelled.', 1;

        UPDATE dbo.tbl_ExpenseClaims
        SET ClaimStatus = N'Cancelled',
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ClaimId = @ClaimId AND CompanyId = @CompanyId;
    END
    ELSE
    BEGIN
        THROW 50108, N'Invalid expense claim status transition.', 1;
    END
END
GO
