USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 10: Employee Self-Service & Manager Portal Seed Data
-- ============================================================================

-- Grant ESS.VIEW to Employee & Manager roles; MANAGER.VIEW to Manager role
INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'ESS.VIEW') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'MANAGER.VIEW') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01' AND IsDeleted = 0);

IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001'); -- Elena (Eng Lead)
    DECLARE @Emp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1002');
    DECLARE @Emp3 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003'); -- Sophia (HR Mgr)
    DECLARE @Emp4 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1004');
    DECLARE @Emp5 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1005');
    DECLARE @Emp6 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1006');
    DECLARE @Emp7 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1007');
    DECLARE @Emp8 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1008');

    -- Establish clean reporting hierarchy for Org Chart & Manager Portal if not already assigned
    IF @Emp3 IS NOT NULL AND @Emp1 IS NOT NULL
    BEGIN
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @Emp3 WHERE EmployeeId = @Emp1 AND ReportingManagerId IS NULL;
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @Emp1 WHERE EmployeeId IN (@Emp2, @Emp8) AND ReportingManagerId IS NULL;
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @Emp3 WHERE EmployeeId IN (@Emp4, @Emp7) AND ReportingManagerId IS NULL;
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @Emp7 WHERE EmployeeId IN (@Emp5, @Emp6) AND ReportingManagerId IS NULL;
    END

    -- Sample Profile Update Requests for ESS & Manager Portal
    IF @Emp2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_ProfileUpdateRequests WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_ProfileUpdateRequests (
            CompanyId, EmployeeId, RequestType, FieldSummary,
            RequestedMobilePhone, RequestedPersonalEmail, RequestedAddress, RequestedCity,
            EmployeeNotes, ApprovalStatus
        )
        VALUES (
            @CompanyId, @Emp2, N'ContactInfo', N'Update personal mobile number & residential city',
            N'+1-212-555-0188', N'marcus.vance.personal@example.com', N'415 Madison Ave, Apt 12B', N'New York',
            N'Relocated apartments this month; please update payroll & HR contact records.', N'Pending'
        );

        IF @Emp4 IS NOT NULL
        BEGIN
            INSERT INTO dbo.tbl_ProfileUpdateRequests (
                CompanyId, EmployeeId, RequestType, FieldSummary,
                RequestedEmergencyContactName, RequestedEmergencyRelationship, RequestedEmergencyPhone,
                EmployeeNotes, ApprovalStatus, ReviewedDate, ReviewerComments
            )
            VALUES (
                @CompanyId, @Emp4, N'EmergencyContact', N'Add primary emergency contact (Spouse)',
                N'Hannah Kim', N'Spouse', N'+1-212-555-0144',
                N'Updated emergency contact number.', N'Approved', SYSUTCDATETIME(), N'Verified and synced to employee emergency contacts.'
            );
        END
    END
END
GO
