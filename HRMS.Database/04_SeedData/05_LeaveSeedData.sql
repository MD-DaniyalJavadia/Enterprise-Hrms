USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 5: Leave Management Seed Data
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions WHERE PermissionCode = N'LEAVE.MANAGE')
BEGIN
    INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
    VALUES (N'LEAVE.MANAGE', N'Leave', N'Manage', N'Manage', N'Manage leave types, allocations, approvals, and encashments');
END
GO

DECLARE @LeaveManageId INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'LEAVE.MANAGE');
DECLARE @LeaveViewId   INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'LEAVE.VIEW');

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @LeaveManageId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @LeaveManageId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @LeaveViewId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND @LeaveViewId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @LeaveViewId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');
IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @Year INT = YEAR(GETDATE());

    -- Update existing AL leave type with Phase 5 policy attributes
    UPDATE dbo.tbl_LeaveTypes
    SET AllowCarryForward = 1, MaxCarryForwardDays = 5.00, AllowEncashment = 1,
        IsHalfDayAllowed = 1, ColorHex = N'#2563eb',
        Description = N'Paid annual vacation leave with carry-forward and encashment eligibility.'
    WHERE CompanyId = @CompanyId AND LeaveCode = N'AL';

    -- Seed additional standard enterprise leave types
    INSERT INTO dbo.tbl_LeaveTypes (
        CompanyId, LeaveCode, LeaveName, AnnualQuota, IsPaid, AllowCarryForward, MaxCarryForwardDays,
        AllowEncashment, RequiresDocument, MaxConsecutiveDays, IsHalfDayAllowed, GenderRestriction, ColorHex, Description
    )
    SELECT @CompanyId, v.LeaveCode, v.LeaveName, v.AnnualQuota, v.IsPaid, v.AllowCarryForward, v.MaxCarryForwardDays,
           v.AllowEncashment, v.RequiresDocument, v.MaxConsecutiveDays, v.IsHalfDayAllowed, v.GenderRestriction, v.ColorHex, v.Description
    FROM (VALUES
        (N'SL',  N'Sick Leave',        10.00, 1, 0, 0.00, 0, 1, 10, 1, NULL,     N'#dc2626', N'Medical and recovery leave; supporting certificate required.'),
        (N'CL',  N'Casual Leave',       7.00, 1, 0, 0.00, 0, 0, 3,  1, NULL,     N'#0ea5e9', N'Short-notice personal leave up to 3 consecutive days.'),
        (N'ML',  N'Maternity Leave',   90.00, 1, 0, 0.00, 0, 1, 90, 0, N'Female',N'#9333ea', N'Statutory maternity leave policy.'),
        (N'LWP', N'Leave Without Pay', 30.00, 0, 0, 0.00, 0, 0, 30, 1, NULL,     N'#64748b', N'Unpaid personal leave when paid balances are exhausted.')
    ) AS v(LeaveCode, LeaveName, AnnualQuota, IsPaid, AllowCarryForward, MaxCarryForwardDays, AllowEncashment, RequiresDocument, MaxConsecutiveDays, IsHalfDayAllowed, GenderRestriction, ColorHex, Description)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.tbl_LeaveTypes lt WHERE lt.CompanyId = @CompanyId AND lt.LeaveCode = v.LeaveCode AND lt.IsDeleted = 0
    );

    -- Allocate Annual Leave Balances for current year
    EXEC dbo.sp_LeaveBalance_AllocateAnnual @CompanyId = @CompanyId, @LeaveYear = @Year, @IncludeCarryForward = 0, @UserId = 1;

    -- Reconcile UsedDays / PendingDays with existing seeded applications
    UPDATE lb
    SET UsedDays = ISNULL((
            SELECT SUM(la.TotalDays) FROM dbo.tbl_LeaveApplications la
            WHERE la.EmployeeId = lb.EmployeeId AND la.LeaveTypeId = lb.LeaveTypeId
              AND YEAR(la.StartDate) = @Year AND la.ApprovalStatus = N'Approved' AND la.IsDeleted = 0
        ), 0),
        PendingDays = ISNULL((
            SELECT SUM(la.TotalDays) FROM dbo.tbl_LeaveApplications la
            WHERE la.EmployeeId = lb.EmployeeId AND la.LeaveTypeId = lb.LeaveTypeId
              AND YEAR(la.StartDate) = @Year AND la.ApprovalStatus = N'Pending' AND la.IsDeleted = 0
        ), 0)
    FROM dbo.tbl_LeaveBalances lb
    WHERE lb.CompanyId = @CompanyId AND lb.LeaveYear = @Year;

    -- Sample Leave Encashment Request
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @AlId INT = (SELECT TOP 1 LeaveTypeId FROM dbo.tbl_LeaveTypes WHERE CompanyId = @CompanyId AND LeaveCode = N'AL' AND IsDeleted = 0);

    IF @Emp1 IS NOT NULL AND @AlId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_LeaveEncashments WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_LeaveEncashments (CompanyId, EmployeeId, LeaveTypeId, LeaveYear, DaysEncashed, EncashmentAmount, ApprovalStatus, Remarks)
        VALUES (@CompanyId, @Emp1, @AlId, @Year, 2.00, 750.00, N'Pending', N'Year-end partial annual leave encashment request');
    END
END
GO
