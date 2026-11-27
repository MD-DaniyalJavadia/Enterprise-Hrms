USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 11: Asset & Document Management Seed Data
-- ============================================================================

INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'ASSET.MANAGE',    N'Assets',    N'Manage', N'Manage', N'Manage asset inventory, issue/return allocations, categories, and maintenance logs'),
    (N'DOCUMENT.MANAGE', N'Documents', N'Manage', N'Manage', N'Publish HR policies, manage document categories, and administer the employee document vault')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);
GO

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'ASSET.MANAGE', N'DOCUMENT.MANAGE')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'ASSET.VIEW', N'DOCUMENT.VIEW')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager', N'PayrollOfficer', N'Employee')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @BranchNyc INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'HQ-NYC');
    DECLARE @BranchSfo INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'BR-SFO');
    DECLARE @DepEng INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'ENG');
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @Emp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1002');
    DECLARE @Emp3 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');
    DECLARE @Emp4 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1004');

    -- Asset Categories & Assets
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_AssetCategories WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_AssetCategories (CompanyId, CategoryCode, CategoryName, AssetClass, RequiresSerialNumber, DefaultDepreciationMonths, Description, IsActive)
        VALUES
            (@CompanyId, N'CAT-LAP', N'Laptops & Workstations',   N'IT',         1, 36, N'Corporate developer laptops and executive ultrabooks.', 1),
            (@CompanyId, N'CAT-MON', N'Monitors & Peripherals',   N'IT',         1, 48, N'External 4K displays, docking stations, and webcams.', 1),
            (@CompanyId, N'CAT-MOB', N'Corporate Mobile Devices', N'Mobile',     1, 24, N'Company-managed smartphones and tablets.', 1),
            (@CompanyId, N'CAT-ACC', N'Access & Security Badges', N'AccessCard', 1, 60, N'RFID facility access badges and hardware security keys.', 1),
            (@CompanyId, N'CAT-FUR', N'Ergonomic Furniture',      N'Furniture',  0, 60, N'Standing desks and ergonomic office seating.', 1);

        DECLARE @CatLap INT = (SELECT CategoryId FROM dbo.tbl_AssetCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'CAT-LAP');
        DECLARE @CatMon INT = (SELECT CategoryId FROM dbo.tbl_AssetCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'CAT-MON');
        DECLARE @CatMob INT = (SELECT CategoryId FROM dbo.tbl_AssetCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'CAT-MOB');
        DECLARE @CatAcc INT = (SELECT CategoryId FROM dbo.tbl_AssetCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'CAT-ACC');

        INSERT INTO dbo.tbl_Assets (
            CompanyId, CategoryId, BranchId, AssetTag, AssetName, Brand, ModelNumber, SerialNumber,
            PurchaseDate, PurchaseCost, WarrantyExpiryDate, VendorName, ConditionStatus, AssetStatus, CurrentEmployeeId, Notes
        )
        VALUES
            (@CompanyId, @CatLap, @BranchNyc, N'AST-2026-0001', N'MacBook Pro 16" M3 Pro',    N'Apple',  N'MRW13LL/A', N'C02Z91K8MD6R', DATEADD(MONTH, -10, @Today), 2699.00, DATEADD(MONTH, 26, @Today), N'CDW Enterprise', N'Good',    N'Assigned',    @Emp1, N'Primary engineering workstation.'),
            (@CompanyId, @CatLap, @BranchNyc, N'AST-2026-0002', N'Dell XPS 15 9530',           N'Dell',   N'XPS-9530',  N'DL9530-88421', DATEADD(MONTH, -8, @Today),  2150.00, DATEADD(DAY, 25, @Today),   N'Dell Direct',    N'Good',    N'Assigned',    @Emp2, N'Warranty renewal approaching.'),
            (@CompanyId, @CatMon, @BranchNyc, N'AST-2026-0003', N'Dell UltraSharp 27" 4K USB-C', N'Dell', N'U2723QE',   N'CN-0U2723-119', DATEADD(MONTH, -6, @Today),  649.00, DATEADD(MONTH, 30, @Today), N'Dell Direct',    N'New',     N'Available',   NULL,  N'Stocked in NYC IT Cage.'),
            (@CompanyId, @CatMob, @BranchSfo, N'AST-2026-0004', N'iPhone 15 Pro 256GB',        N'Apple',  N'A2848',     N'F2LZN992Q18R', DATEADD(MONTH, -4, @Today),  1099.00, DATEADD(MONTH, 20, @Today), N'Verizon Biz',    N'New',     N'Available',   NULL,  N'Ready for sales team onboarding.'),
            (@CompanyId, @CatLap, @BranchSfo, N'AST-2026-0005', N'ThinkPad X1 Carbon Gen 11',  N'Lenovo', N'21HM000US', N'PF4X1C-77310', DATEADD(MONTH, -14, @Today), 1890.00, DATEADD(MONTH, 22, @Today), N'Insight Direct', N'Fair',    N'Maintenance', NULL,  N'Battery replacement in progress.'),
            (@CompanyId, @CatAcc, @BranchNyc, N'AST-2026-0006', N'HID iCLASS HQ Access Badge', N'HID',    N'iCLASS-SE', N'HID-1004-NYC', DATEADD(MONTH, -4, @Today),  45.00,   NULL,                      N'Securitas',      N'Good',    N'Assigned',    @Emp4, N'Floor 14 & server room clearance.');

        DECLARE @Ast1 INT = (SELECT AssetId FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId AND AssetTag = N'AST-2026-0001');
        DECLARE @Ast2 INT = (SELECT AssetId FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId AND AssetTag = N'AST-2026-0002');
        DECLARE @Ast5 INT = (SELECT AssetId FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId AND AssetTag = N'AST-2026-0005');
        DECLARE @Ast6 INT = (SELECT AssetId FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId AND AssetTag = N'AST-2026-0006');

        IF @Emp1 IS NOT NULL AND @Ast1 IS NOT NULL
            INSERT INTO dbo.tbl_AssetAllocations (CompanyId, AssetId, EmployeeId, AssignedDate, ConditionAtIssue, AllocationStatus, IssueRemarks, EmployeeAcknowledged, AcknowledgedDate)
            VALUES (@CompanyId, @Ast1, @Emp1, DATEADD(MONTH, -10, @Today), N'New', N'Active', N'Issued with USB-C dock and carrying sleeve.', 1, DATEADD(MONTH, -10, SYSUTCDATETIME()));

        IF @Emp2 IS NOT NULL AND @Ast2 IS NOT NULL
            INSERT INTO dbo.tbl_AssetAllocations (CompanyId, AssetId, EmployeeId, AssignedDate, ConditionAtIssue, AllocationStatus, IssueRemarks, EmployeeAcknowledged)
            VALUES (@CompanyId, @Ast2, @Emp2, DATEADD(MONTH, -8, @Today), N'Good', N'Active', N'Assigned for finance & payroll automation.', 0);

        IF @Emp4 IS NOT NULL AND @Ast6 IS NOT NULL
            INSERT INTO dbo.tbl_AssetAllocations (CompanyId, AssetId, EmployeeId, AssignedDate, ConditionAtIssue, AllocationStatus, IssueRemarks, EmployeeAcknowledged)
            VALUES (@CompanyId, @Ast6, @Emp4, DATEADD(MONTH, -4, @Today), N'New', N'Active', N'NYC Headquarters access card.', 1);

        IF @Ast5 IS NOT NULL
            INSERT INTO dbo.tbl_AssetMaintenanceLogs (CompanyId, AssetId, MaintenanceType, ServiceDate, VendorOrTechnician, Cost, MaintenanceStatus, Description)
            VALUES (@CompanyId, @Ast5, N'Repair', DATEADD(DAY, -2, @Today), N'Lenovo Premier Support', 185.00, N'InProgress', N'Replacing degraded battery cell and running thermal diagnostics.');
    END

    -- Document Categories & Company Documents
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_DocumentCategories WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_DocumentCategories (CompanyId, CategoryCode, CategoryName, ScopeType, RequiresAcknowledgment, DefaultVisibility, RetentionMonths, Description, IsActive)
        VALUES
            (@CompanyId, N'DOC-POL', N'HR Policies & Handbooks', N'CompanyPolicy', 1, N'AllEmployees',       84, N'Official company employee handbook, code of conduct, and workplace policies.', 1),
            (@CompanyId, N'DOC-SEC', N'Security & Compliance',   N'Compliance',    1, N'AllEmployees',       60, N'Information security, data privacy, and statutory compliance standards.', 1),
            (@CompanyId, N'DOC-TMP', N'HR Templates & Forms',    N'Template',      0, N'AllEmployees',       36, N'Standard expense, travel, NDA, and manager review templates.', 1),
            (@CompanyId, N'DOC-MGR', N'Leadership & HR Guides',  N'Guide',         0, N'ManagersOnly',       36, N'Manager coaching playbooks and compensation calibration guidelines.', 1);

        DECLARE @DCatPol INT = (SELECT CategoryId FROM dbo.tbl_DocumentCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'DOC-POL');
        DECLARE @DCatSec INT = (SELECT CategoryId FROM dbo.tbl_DocumentCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'DOC-SEC');
        DECLARE @DCatTmp INT = (SELECT CategoryId FROM dbo.tbl_DocumentCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'DOC-TMP');
        DECLARE @DCatMgr INT = (SELECT CategoryId FROM dbo.tbl_DocumentCategories WHERE CompanyId = @CompanyId AND CategoryCode = N'DOC-MGR');

        INSERT INTO dbo.tbl_CompanyDocuments (
            CompanyId, CategoryId, DepartmentId, DocumentCode, Title, VersionNumber, Visibility,
            RequiresAcknowledgment, EffectiveDate, ExpiryDate, OriginalFileName, StoredFileName,
            ContentType, FileSizeBytes, Description, DocumentStatus
        )
        VALUES
            (@CompanyId, @DCatPol, NULL, N'POL-2026-001', N'Global Employee Handbook',                  N'2.1', N'AllEmployees',       1, DATEFROMPARTS(YEAR(@Today), 1, 1), NULL,                      N'Global_Employee_Handbook_v2.1.pdf',  N'seed_handbook.pdf',    N'application/pdf', 1420000, N'Core employment standards, workplace ethics, benefits, and leave guidelines.', N'Published'),
            (@CompanyId, @DCatSec, NULL, N'SEC-2026-002', N'Information Security & Acceptable Use Policy', N'1.4', N'AllEmployees',     1, DATEFROMPARTS(YEAR(@Today), 2, 1), DATEADD(DAY, 20, @Today),  N'InfoSec_Acceptable_Use_v1.4.pdf',    N'seed_infosec.pdf',     N'application/pdf', 845000,  N'Mandatory device encryption, password hygiene, and data handling rules.', N'Published'),
            (@CompanyId, @DCatTmp, @DepEng, N'ENG-2026-003', N'Engineering On-Call & Incident Runbook', N'1.2', N'DepartmentSpecific', 0, DATEADD(MONTH, -2, @Today),        NULL,                      N'Engineering_Oncall_Runbook_v1.2.pdf',N'seed_runbook.pdf',     N'application/pdf', 512000,  N'Standard operating procedures for production incident escalation.', N'Published'),
            (@CompanyId, @DCatMgr, NULL, N'MGR-2026-004', N'Manager Performance Calibration Playbook',   N'1.0', N'ManagersOnly',       0, DATEADD(MONTH, -1, @Today),        NULL,                      N'Manager_Calibration_Playbook.pdf',   N'seed_playbook.pdf',    N'application/pdf', 620000,  N'Structured evaluation rubric and merit allocation guide for people managers.', N'Published'),
            (@CompanyId, @DCatPol, NULL, N'HR-2026-005',  N'Executive Compensation Governance Charter',  N'1.0', N'HrOnly',             0, DATEFROMPARTS(YEAR(@Today), 1, 1), NULL,                      N'Compensation_Governance_Charter.pdf',N'seed_charter.pdf',     N'application/pdf', 390000,  N'Confidential HR-only governance reference.', N'Published');

        DECLARE @DocPol INT = (SELECT DocumentId FROM dbo.tbl_CompanyDocuments WHERE CompanyId = @CompanyId AND DocumentCode = N'POL-2026-001');
        DECLARE @DocSec INT = (SELECT DocumentId FROM dbo.tbl_CompanyDocuments WHERE CompanyId = @CompanyId AND DocumentCode = N'SEC-2026-002');

        IF @DocPol IS NOT NULL AND @Emp1 IS NOT NULL
            INSERT INTO dbo.tbl_DocumentAcknowledgments (CompanyId, DocumentId, EmployeeId, AcknowledgedVersion, Remarks)
            VALUES (@CompanyId, @DocPol, @Emp1, N'2.1', N'Reviewed and acknowledged during annual refresh.');

        IF @DocPol IS NOT NULL AND @Emp3 IS NOT NULL
            INSERT INTO dbo.tbl_DocumentAcknowledgments (CompanyId, DocumentId, EmployeeId, AcknowledgedVersion, Remarks)
            VALUES (@CompanyId, @DocPol, @Emp3, N'2.1', N'Signed off.');

        IF @DocSec IS NOT NULL AND @Emp1 IS NOT NULL
            INSERT INTO dbo.tbl_DocumentAcknowledgments (CompanyId, DocumentId, EmployeeId, AcknowledgedVersion, Remarks)
            VALUES (@CompanyId, @DocSec, @Emp1, N'1.4', N'Completed security compliance sign-off.');
    END
END
GO
