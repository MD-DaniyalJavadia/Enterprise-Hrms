USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 13: Reports, Analytics & Export Features Seed Data
-- ============================================================================

INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'REPORTS.EXPORT', N'Reports', N'Export', N'Export', N'Export HRMS reports to CSV/Excel and manage shared report templates')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);
GO

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'REPORTS.VIEW') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer', N'Manager')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'REPORTS.EXPORT') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

IF @CompanyId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SavedReportTemplates WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_SavedReportTemplates (
            CompanyId, TemplateName, ReportKey, BranchId, DepartmentId, DateRangeDays, StatusFilter, Notes, IsShared, CreatedBy
        )
        VALUES
            (@CompanyId, N'Active Headcount & Tenure Roster',        N'Workforce',           NULL, NULL, 30,  N'1',   N'Monthly executive headcount, job grade, and base salary audit.', 1, 1),
            (@CompanyId, N'Monthly Attendance & Leave Compliance',   N'AttendanceLeave',     NULL, NULL, 30,  NULL,   N'Tracks late arrivals, overtime hours, and remaining leave accruals.', 1, 1),
            (@CompanyId, N'Current Period Payroll Cost-to-Company',  N'PayrollCost',         NULL, NULL, 30,  NULL,   N'Department-level gross, tax, net pay, and employer contributions.', 1, 1),
            (@CompanyId, N'Quarterly Asset, Policy & Exit Audit',    N'FinancialCompliance', NULL, NULL, 90,  NULL,   N'Tracks active loans, expense claims, asset custody, and policy sign-offs.', 1, 1);
    END
END
GO
