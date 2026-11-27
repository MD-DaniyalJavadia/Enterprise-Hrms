USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 12: Separation & Exit Management Seed Data
-- ============================================================================

INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'EXIT.MANAGE', N'Separation', N'Manage', N'Manage', N'Approve separations, manage exit clearance checklists, calculate final settlements, and finalize offboarding')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);
GO

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'EXIT.MANAGE') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'EXIT.VIEW') p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'PayrollOfficer', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

IF @CompanyId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_ExitClearanceTemplates WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_ExitClearanceTemplates (CompanyId, DepartmentScope, ItemName, Description, IsMandatory, DisplayOrder, IsActive)
        VALUES
            (@CompanyId, N'Manager',         N'Knowledge Transfer & Project Handover',     N'Complete documentation, codebase/client handover, and transition sign-off.', 1, 1, 1),
            (@CompanyId, N'IT',              N'Revoke SSO, Email & VPN Credentials',      N'Disable corporate email, Git, cloud IAM, and VPN access on last day.', 1, 2, 1),
            (@CompanyId, N'Assets',          N'Return Laptop, Charger & Peripherals',     N'Return all assigned hardware in tbl_AssetAllocations with condition check.', 1, 3, 1),
            (@CompanyId, N'AdminFacilities', N'Return Building Access Card & Parking Tag',N'Collect RFID access badge and keys before final release.', 1, 4, 1),
            (@CompanyId, N'Finance',         N'Clear Corporate Card & Loan/Expense Dues', N'Reconcile pending expense claims and active employee loans.', 1, 5, 1),
            (@CompanyId, N'HR',              N'Conduct Exit Interview & Confirm Address', N'Complete exit interview and verify forwarding address for tax/F&F docs.', 1, 6, 1);
    END

    -- Sample active resignation in progress for EMP-1008
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SeparationRequests WHERE CompanyId = @CompanyId)
    BEGIN
        DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
        DECLARE @Emp8 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1008');

        IF @Emp8 IS NOT NULL
        BEGIN
            DECLARE @SepId INT;
            EXEC dbo.sp_SeparationRequest_Submit
                @CompanyId = @CompanyId,
                @EmployeeId = @Emp8,
                @SeparationType = N'Resignation',
                @ReasonCategory = N'Relocation',
                @SubmissionDate = @Today,
                @NoticePeriodDays = 30,
                @RequestedLastWorkingDay = @Today,
                @IsNoticeWaived = 0,
                @EmployeeComments = N'Relocating overseas for family commitments. Happy to support full handover.',
                @HrComments = N'Approved for offboarding clearance.',
                @InitiateAsHrApproved = 1,
                @UserId = 1,
                @SavedId = @SepId OUTPUT;

            -- Mark Manager handover item cleared and save an exit interview sample
            DECLARE @MgrClrId INT = (SELECT TOP 1 ClearanceItemId FROM dbo.tbl_ExitClearanceItems WHERE SeparationId = @SepId AND DepartmentScope = N'Manager');
            IF @MgrClrId IS NOT NULL
            BEGIN
                EXEC dbo.sp_ExitClearance_UpdateItem
                    @CompanyId = @CompanyId,
                    @ClearanceItemId = @MgrClrId,
                    @ClearanceStatus = N'Cleared',
                    @RecoveryAmount = 0,
                    @Remarks = N'Documentation and sprint tasks handed over cleanly.',
                    @UserId = 1;
            END

            DECLARE @DummySetId INT;
            EXEC dbo.sp_FinalSettlement_CalculateAndSave
                @CompanyId = @CompanyId,
                @SeparationId = @SepId,
                @WorkedDaysInFinalMonth = NULL,
                @EncashableLeaveDays = NULL,
                @GratuityOrSeverance = 0,
                @BonusOrOtherEarnings = 500.00,
                @OutstandingLoanRecovery = NULL,
                @AssetOrNoticeRecovery = 0,
                @TaxAndOtherDeductions = 150.00,
                @Notes = N'Draft Full & Final calculation pending final IT & Finance sign-off.',
                @UserId = 1,
                @SavedId = @DummySetId OUTPUT;
        END
    END
END
GO
