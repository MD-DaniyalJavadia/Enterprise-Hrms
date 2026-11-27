USE [HRMS_DB];
GO

-- ============================================================================
-- Initial Seed Data for Enterprise HRMS (Phase 1)
-- Note: The SuperAdmin user is created via the secure First-Run Setup screen
-- (/Account/Setup) so no plaintext credentials are stored in scripts or code.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01')
BEGIN
    INSERT INTO dbo.tbl_Companies (CompanyCode, CompanyName, LegalName, Email, Phone, City, Country, CurrencyCode, TimeZoneId)
    VALUES (N'ACME01', N'Acme Enterprise Corp', N'Acme Enterprise Corporation LLC', N'hr@acme-enterprise.example', N'+1-212-555-0100', N'New York', N'United States', 'USD', N'America/New_York');
END
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

-- Branches
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'HQ-NYC')
BEGIN
    INSERT INTO dbo.tbl_Branches (CompanyId, BranchCode, BranchName, Email, City, Country, IsHeadOffice)
    VALUES
        (@CompanyId, N'HQ-NYC', N'Headquarters - New York', N'nyc@acme-enterprise.example', N'New York', N'United States', 1),
        (@CompanyId, N'BR-SFO', N'West Coast Hub - San Francisco', N'sfo@acme-enterprise.example', N'San Francisco', N'United States', 0);
END

DECLARE @BranchNyc INT = (SELECT BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'HQ-NYC');
DECLARE @BranchSfo INT = (SELECT BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'BR-SFO');

-- Departments
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'ENG')
BEGIN
    INSERT INTO dbo.tbl_Departments (CompanyId, BranchId, DepartmentCode, DepartmentName, CostCenterCode)
    VALUES
        (@CompanyId, @BranchNyc, N'ENG', N'Engineering', N'CC-100'),
        (@CompanyId, @BranchNyc, N'HR',  N'Human Resources', N'CC-200'),
        (@CompanyId, @BranchNyc, N'FIN', N'Finance & Payroll', N'CC-300'),
        (@CompanyId, @BranchSfo, N'SLS', N'Enterprise Sales', N'CC-400'),
        (@CompanyId, @BranchSfo, N'OPS', N'Operations', N'CC-500');
END

-- Designations
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SWE-SR')
BEGIN
    INSERT INTO dbo.tbl_Designations (CompanyId, DesignationCode, DesignationTitle, JobGrade)
    VALUES
        (@CompanyId, N'SWE-SR', N'Senior Software Engineer', N'G6'),
        (@CompanyId, N'HR-MGR', N'HR Manager', N'G7'),
        (@CompanyId, N'FIN-AN', N'Payroll & Finance Specialist', N'G5'),
        (@CompanyId, N'SLS-EX', N'Enterprise Account Executive', N'G5'),
        (@CompanyId, N'OPS-LD', N'Operations Lead', N'G6');
END

-- Roles
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'SuperAdmin')
BEGIN
    INSERT INTO dbo.tbl_Roles (CompanyId, RoleCode, RoleName, Description, IsSystemRole)
    VALUES
        (@CompanyId, N'SuperAdmin',     N'Super Administrator', N'Full system access across all HRMS modules', 1),
        (@CompanyId, N'HRAdmin',        N'HR Administrator',    N'Full access to HR, Employee, Attendance, Leave, Recruitment', 1),
        (@CompanyId, N'PayrollOfficer', N'Payroll Officer',     N'Access to Payroll, Loans, Expenses, and Salary Reports', 1),
        (@CompanyId, N'Manager',        N'Department Manager',  N'Manager Portal, team approvals, and team reviews', 1),
        (@CompanyId, N'Employee',       N'Employee',            N'Employee Self-Service Portal access', 1);
END

-- Permissions (All Modules)
INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'DASHBOARD.VIEW',     N'Dashboard',     N'Dashboard',    N'View',   N'View executive HRMS dashboard'),
    (N'ORG.VIEW',           N'Organization',  N'Company',      N'View',   N'View organization setup'),
    (N'ORG.MANAGE',         N'Organization',  N'Company',      N'Manage', N'Manage companies, branches, departments'),
    (N'EMPLOYEE.VIEW',      N'Employees',     N'Directory',    N'View',   N'View employee directory and profiles'),
    (N'EMPLOYEE.MANAGE',    N'Employees',     N'Profile',      N'Manage', N'Create and update employees'),
    (N'RECRUITMENT.VIEW',   N'Recruitment',   N'Requisitions', N'View',   N'View recruitment pipeline'),
    (N'ATTENDANCE.VIEW',    N'Attendance',    N'Daily',        N'View',   N'View attendance records'),
    (N'LEAVE.VIEW',         N'Leave',         N'Applications', N'View',   N'View leave applications'),
    (N'PAYROLL.VIEW',       N'Payroll',       N'Runs',         N'View',   N'View payroll summary and payslips'),
    (N'LOAN.VIEW',          N'Loans',         N'Applications', N'View',   N'View loans and advances'),
    (N'PERFORMANCE.VIEW',   N'Performance',   N'Appraisals',   N'View',   N'View KPIs and reviews'),
    (N'TRAINING.VIEW',      N'Training',      N'Programs',     N'View',   N'View training programs'),
    (N'ESS.VIEW',           N'SelfService',   N'Portal',       N'View',   N'Access Employee Self-Service portal'),
    (N'MANAGER.VIEW',       N'ManagerPortal', N'Dashboard',    N'View',   N'Access Manager portal'),
    (N'EXPENSE.VIEW',       N'Expenses',      N'Claims',       N'View',   N'View expense reimbursements'),
    (N'ASSET.VIEW',         N'Assets',        N'Inventory',    N'View',   N'View company asset allocations'),
    (N'DOCUMENT.VIEW',      N'Documents',     N'Repository',   N'View',   N'View HR documents and templates'),
    (N'EXIT.VIEW',          N'Separation',    N'Resignations', N'View',   N'View separation and clearance'),
    (N'REPORTS.VIEW',       N'Reports',       N'Analytics',    N'View',   N'View and export HRMS reports'),
    (N'ADMIN.VIEW',         N'Administration',N'Settings',     N'View',   N'View system administration and audit logs')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode
);

-- Assign all permissions to SuperAdmin
DECLARE @SuperAdminRoleId INT = (SELECT RoleId FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'SuperAdmin');
INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT @SuperAdminRoleId, p.PermissionId
FROM dbo.tbl_Permissions p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @SuperAdminRoleId AND rp.PermissionId = p.PermissionId
);

-- Sample Employees for Rich Phase 1 Dashboard Analytics
DECLARE @DepEng INT = (SELECT DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'ENG');
DECLARE @DepHr  INT = (SELECT DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'HR');
DECLARE @DepFin INT = (SELECT DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'FIN');
DECLARE @DepSls INT = (SELECT DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'SLS');
DECLARE @DepOps INT = (SELECT DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'OPS');

DECLARE @DesEng INT = (SELECT DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SWE-SR');
DECLARE @DesHr  INT = (SELECT DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'HR-MGR');
DECLARE @DesFin INT = (SELECT DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'FIN-AN');
DECLARE @DesSls INT = (SELECT DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SLS-EX');
DECLARE @DesOps INT = (SELECT DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'OPS-LD');

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    INSERT INTO dbo.tbl_Employees (CompanyId, BranchId, DepartmentId, DesignationId, EmployeeCode, FirstName, LastName, Gender, DateOfBirth, WorkEmail, HireDate, EmploymentStatus, BaseSalary, IsActive)
    VALUES
        (@CompanyId, @BranchNyc, @DepEng, @DesEng, N'EMP-1001', N'Elena',   N'Rostova',   N'Female', DATEADD(YEAR, -31, DATEADD(DAY, 5, @Today)),  N'elena.rostova@acme-enterprise.example',   DATEADD(YEAR, -3, DATEADD(DAY, 7, @Today)),  1, 135000, 1),
        (@CompanyId, @BranchNyc, @DepEng, @DesEng, N'EMP-1002', N'Marcus',  N'Vance',     N'Male',   DATEADD(YEAR, -29, DATEADD(DAY, 12, @Today)), N'marcus.vance@acme-enterprise.example',    DATEADD(YEAR, -2, DATEADD(DAY, 15, @Today)), 1, 128000, 1),
        (@CompanyId, @BranchNyc, @DepHr,  @DesHr,  N'EMP-1003', N'Sophia',  N'Patel',     N'Female', DATEADD(YEAR, -35, DATEADD(DAY, -40, @Today)),N'sophia.patel@acme-enterprise.example',    DATEADD(YEAR, -4, DATEADD(DAY, 3, @Today)),  1, 118000, 1),
        (@CompanyId, @BranchNyc, @DepFin, @DesFin, N'EMP-1004', N'David',   N'Kim',       N'Male',   DATEADD(YEAR, -33, DATEADD(DAY, 19, @Today)), N'david.kim@acme-enterprise.example',       DATEADD(MONTH, -4, @Today),                  1, 98000,  1),
        (@CompanyId, @BranchSfo, @DepSls, @DesSls, N'EMP-1005', N'Claire',  N'Bennett',   N'Female', DATEADD(YEAR, -27, DATEADD(DAY, -90, @Today)),N'claire.bennett@acme-enterprise.example',  DATEADD(DAY, -10, @Today),                   2, 92000,  1),
        (@CompanyId, @BranchSfo, @DepSls, @DesSls, N'EMP-1006', N'Liam',    N'OConnor',   N'Male',   DATEADD(YEAR, -30, DATEADD(DAY, 2, @Today)),  N'liam.oconnor@acme-enterprise.example',    DATEADD(MONTH, -2, @Today),                  1, 95000,  1),
        (@CompanyId, @BranchSfo, @DepOps, @DesOps, N'EMP-1007', N'Nadia',   N'Al-Mansoor',N'Female', DATEADD(YEAR, -34, DATEADD(DAY, -15, @Today)),N'nadia.almansoor@acme-enterprise.example', DATEADD(YEAR, -1, DATEADD(DAY, 20, @Today)), 1, 110000, 1),
        (@CompanyId, @BranchNyc, @DepEng, @DesEng, N'EMP-1008', N'Ethan',   N'Brooks',    N'Male',   DATEADD(YEAR, -26, DATEADD(DAY, -22, @Today)),N'ethan.brooks@acme-enterprise.example',    DATEADD(DAY, -5, @Today),                    2, 115000, 1);

    -- Sample Leave Type & Pending Leave
    INSERT INTO dbo.tbl_LeaveTypes (CompanyId, LeaveCode, LeaveName, AnnualQuota, IsPaid)
    VALUES (@CompanyId, N'AL', N'Annual Leave', 20.00, 1);

    DECLARE @LeaveTypeId INT = SCOPE_IDENTITY();
    DECLARE @Emp4 INT = (SELECT EmployeeId FROM dbo.tbl_Employees WHERE EmployeeCode = N'EMP-1004');
    DECLARE @Emp7 INT = (SELECT EmployeeId FROM dbo.tbl_Employees WHERE EmployeeCode = N'EMP-1007');

    INSERT INTO dbo.tbl_LeaveApplications (CompanyId, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays, Reason, ApprovalStatus)
    VALUES
        (@CompanyId, @Emp4, @LeaveTypeId, DATEADD(DAY, 3, @Today), DATEADD(DAY, 5, @Today), 3.0, N'Family travel', N'Pending'),
        (@CompanyId, @Emp7, @LeaveTypeId, @Today, @Today, 1.0, N'Personal appointment', N'Approved');

    -- Sample Today's Attendance
    INSERT INTO dbo.tbl_Attendance (CompanyId, BranchId, EmployeeId, AttendanceDate, CheckInTime, Status, LateMinutes)
    SELECT
        e.CompanyId,
        e.BranchId,
        e.EmployeeId,
        @Today,
        CASE WHEN e.EmployeeCode IN (N'EMP-1007') THEN NULL ELSE DATEADD(HOUR, 9, CAST(@Today AS DATETIME2(0))) END,
        CASE
            WHEN e.EmployeeCode = N'EMP-1007' THEN N'OnLeave'
            WHEN e.EmployeeCode = N'EMP-1006' THEN N'Late'
            WHEN e.EmployeeCode = N'EMP-1008' THEN N'Absent'
            ELSE N'Present'
        END,
        CASE WHEN e.EmployeeCode = N'EMP-1006' THEN 22 ELSE 0 END
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId;

    -- Sample Latest Payroll Run
    INSERT INTO dbo.tbl_PayrollRuns (CompanyId, PayrollYear, PayrollMonth, PeriodLabel, TotalGrossPay, TotalDeductions, TotalNetPay, ProcessedEmployees, PayrollStatus)
    VALUES (@CompanyId, YEAR(@Today), MONTH(@Today), FORMAT(@Today, 'MMMM yyyy'), 74000.00, 14800.00, 59200.00, 8, N'Approved');
END
GO
