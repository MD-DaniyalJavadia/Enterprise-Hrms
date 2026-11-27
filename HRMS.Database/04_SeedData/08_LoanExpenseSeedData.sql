USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 8: Loan, Salary Advance & Expense Management Seed Data
-- ============================================================================

INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'LOAN.MANAGE',    N'Loans',    N'Manage', N'Manage', N'Approve/disburse employee loans, configure loan policies, and record repayments'),
    (N'EXPENSE.MANAGE', N'Expenses', N'Manage', N'Manage', N'Approve/reject/reimburse expense claims and configure expense categories')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);
GO

DECLARE @LoanViewId   INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'LOAN.VIEW');
DECLARE @LoanManageId INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'LOAN.MANAGE');
DECLARE @ExpViewId    INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'EXPENSE.VIEW');
DECLARE @ExpManageId  INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'EXPENSE.MANAGE');

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'LOAN.MANAGE', N'EXPENSE.MANAGE')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'PayrollOfficer', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'LOAN.VIEW', N'EXPENSE.VIEW')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'PayrollOfficer', N'HRAdmin', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_LoanTypes WHERE CompanyId = @CompanyId)
BEGIN
    INSERT INTO dbo.tbl_LoanTypes (CompanyId, TypeCode, TypeName, Category, MaxAmount, DefaultInterestRate, MaxTenureMonths, RequiresGuarantor, Description)
    VALUES
        (@CompanyId, N'SAL-ADV', N'Salary Advance',            N'SalaryAdvance', 5000.00,  0.00,  3,  0, N'Interest-free short-term salary advance recovered over 1-3 months.'),
        (@CompanyId, N'PER-LN',  N'Employee Personal Loan',    N'Loan',          25000.00, 4.50,  24, 1, N'Low-interest employee welfare loan with monthly EMI repayment.'),
        (@CompanyId, N'EDU-LN',  N'Education & Upskilling Loan', N'Loan',        15000.00, 2.00,  18, 0, N'Subsidized tuition and certification assistance loan.');
END

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_ExpenseCategories WHERE CompanyId = @CompanyId)
BEGIN
    INSERT INTO dbo.tbl_ExpenseCategories (CompanyId, CategoryCode, CategoryName, MaxLimitPerClaim, RequiresReceipt, Description)
    VALUES
        (@CompanyId, N'TRV', N'Travel & Airfare',          2500.00, 1, N'Flights, rail tickets, airport transfers, and mileage.'),
        (@CompanyId, N'HTL', N'Lodging & Business Meals',  1000.00, 1, N'Hotel stays and per-diem meals during approved business travel.'),
        (@CompanyId, N'EQP', N'Home Office & Equipment',   1500.00, 1, N'Monitors, ergonomic accessories, and licensed software.'),
        (@CompanyId, N'CLN', N'Client Hospitality',         500.00, 0, N'Approved client lunches and relationship meetings.');
END

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_EmployeeLoans WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @Emp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1002');
    DECLARE @TypePer INT = (SELECT TOP 1 LoanTypeId FROM dbo.tbl_LoanTypes WHERE CompanyId = @CompanyId AND TypeCode = N'PER-LN');
    DECLARE @TypeAdv INT = (SELECT TOP 1 LoanTypeId FROM dbo.tbl_LoanTypes WHERE CompanyId = @CompanyId AND TypeCode = N'SAL-ADV');
    DECLARE @Yr INT = YEAR(SYSUTCDATETIME());
    DECLARE @LoanStart DATE = DATEFROMPARTS(@Yr, MONTH(SYSUTCDATETIME()), 1);

    IF @Emp1 IS NOT NULL AND @TypePer IS NOT NULL
    BEGIN
        -- Active 6-month loan with 1 paid installment
        INSERT INTO dbo.tbl_EmployeeLoans (
            CompanyId, EmployeeId, LoanTypeId, LoanNumber, PrincipalAmount, InterestRatePercent,
            TenureMonths, MonthlyInstallment, TotalPayableAmount, PaidAmount, RemainingBalance,
            Purpose, RepaymentStartDate, LoanStatus, ApprovedBy, ApprovedDate, DisbursedDate, ReviewerComments
        )
        VALUES (
            @CompanyId, @Emp1, @TypePer, CONCAT(N'LN-', @Yr, N'-1001'), 6000.00, 4.50,
            6, 1022.50, 6135.00, 1022.50, 5112.50,
            N'Home relocation and family transition costs', @LoanStart, N'Active', 1, SYSUTCDATETIME(), DATEADD(DAY, -15, @LoanStart), N'Approved per employee welfare policy.'
        );

        DECLARE @ActiveLoanId INT = SCOPE_IDENTITY();
        INSERT INTO dbo.tbl_LoanRepaymentSchedule (CompanyId, LoanId, InstallmentNumber, DueDate, PrincipalComponent, InterestComponent, InstallmentAmount, PaidAmount, PaymentStatus, PaidDate, PaymentReference)
        VALUES
            (@CompanyId, @ActiveLoanId, 1, @LoanStart,                    1000.00, 22.50, 1022.50, 1022.50, N'Paid',    @LoanStart, N'PAY-EMI-01'),
            (@CompanyId, @ActiveLoanId, 2, DATEADD(MONTH, 1, @LoanStart), 1000.00, 22.50, 1022.50,    0.00, N'Pending', NULL,       NULL),
            (@CompanyId, @ActiveLoanId, 3, DATEADD(MONTH, 2, @LoanStart), 1000.00, 22.50, 1022.50,    0.00, N'Pending', NULL,       NULL),
            (@CompanyId, @ActiveLoanId, 4, DATEADD(MONTH, 3, @LoanStart), 1000.00, 22.50, 1022.50,    0.00, N'Pending', NULL,       NULL),
            (@CompanyId, @ActiveLoanId, 5, DATEADD(MONTH, 4, @LoanStart), 1000.00, 22.50, 1022.50,    0.00, N'Pending', NULL,       NULL),
            (@CompanyId, @ActiveLoanId, 6, DATEADD(MONTH, 5, @LoanStart), 1000.00, 22.50, 1022.50,    0.00, N'Pending', NULL,       NULL);
    END

    IF @Emp2 IS NOT NULL AND @TypeAdv IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_EmployeeLoans (
            CompanyId, EmployeeId, LoanTypeId, LoanNumber, PrincipalAmount, InterestRatePercent,
            TenureMonths, MonthlyInstallment, TotalPayableAmount, PaidAmount, RemainingBalance,
            Purpose, RepaymentStartDate, LoanStatus
        )
        VALUES (
            @CompanyId, @Emp2, @TypeAdv, CONCAT(N'LN-', @Yr, N'-1002'), 1500.00, 0.00,
            3, 500.00, 1500.00, 0.00, 1500.00,
            N'Short-term travel & medical advance', DATEADD(MONTH, 1, @LoanStart), N'Pending'
        );
    END
END

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_ExpenseClaims WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @ExpEmp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @ExpEmp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');
    DECLARE @CatTrv  INT = (SELECT TOP 1 CategoryId FROM dbo.tbl_ExpenseCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'TRV');
    DECLARE @CatCln  INT = (SELECT TOP 1 CategoryId FROM dbo.tbl_ExpenseCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'CLN');
    DECLARE @CatEqp  INT = (SELECT TOP 1 CategoryId FROM dbo.tbl_ExpenseCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'EQP');
    DECLARE @EYear   INT = YEAR(SYSUTCDATETIME());
    DECLARE @Today   DATE = CAST(SYSUTCDATETIME() AS DATE);

    IF @ExpEmp1 IS NOT NULL AND @CatCln IS NOT NULL AND @CatEqp IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_ExpenseClaims (
            CompanyId, EmployeeId, CategoryId, ClaimNumber, ExpenseDate, MerchantOrVendor,
            ClaimAmount, ApprovedAmount, Description, ClaimStatus, ReviewedBy, ReviewedDate, ReviewerComments, ReimbursedDate, PaymentReference
        )
        VALUES
            (@CompanyId, @ExpEmp1, @CatCln, CONCAT(N'EXP-', @EYear, N'-1001'), DATEADD(DAY, -12, @Today), N'Blue Ribbon Bistro NYC', 245.50, 245.50, N'Architecture review lunch with enterprise client team.', N'Reimbursed', 1, SYSUTCDATETIME(), N'Approved as submitted.', DATEADD(DAY, -5, @Today), N'ACH-88412'),
            (@CompanyId, @ExpEmp1, @CatEqp, CONCAT(N'EXP-', @EYear, N'-1002'), DATEADD(DAY, -4, @Today),  N'Dell Enterprise Store',   480.00, 480.00, N'27-inch USB-C monitor for home office workstation.',       N'Approved',   1, SYSUTCDATETIME(), N'Verified under remote equipment allowance.', NULL, NULL);
    END

    IF @ExpEmp2 IS NOT NULL AND @CatTrv IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_ExpenseClaims (
            CompanyId, EmployeeId, CategoryId, ClaimNumber, ExpenseDate, MerchantOrVendor,
            ClaimAmount, Description, ClaimStatus
        )
        VALUES
            (@CompanyId, @ExpEmp2, @CatTrv, CONCAT(N'EXP-', @EYear, N'-1003'), DATEADD(DAY, -2, @Today), N'Delta Air Lines', 640.00, N'Round-trip flight NYC <-> SFO for quarterly leadership summit.', N'Submitted');
    END
END
GO
