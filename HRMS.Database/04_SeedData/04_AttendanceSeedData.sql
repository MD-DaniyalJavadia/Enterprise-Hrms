USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 4: Attendance & Shift Management Seed Data
-- ============================================================================

-- 1. Seed ATTENDANCE.MANAGE permission and grant to SuperAdmin / HRAdmin
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions WHERE PermissionCode = N'ATTENDANCE.MANAGE')
BEGIN
    INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
    VALUES (N'ATTENDANCE.MANAGE', N'Attendance', N'Manage', N'Manage', N'Manage daily attendance, shift rosters, holidays, and approvals');
END
GO

DECLARE @ManagePermId INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'ATTENDANCE.MANAGE');
DECLARE @ViewPermId   INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'ATTENDANCE.VIEW');

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @ManagePermId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @ManagePermId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @ViewPermId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager', N'Employee')
  AND r.IsDeleted = 0
  AND @ViewPermId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @ViewPermId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');
IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @Year  INT  = YEAR(@Today);
    DECLARE @DayShiftId INT = (SELECT TOP 1 ShiftId FROM dbo.tbl_Shifts WHERE CompanyId = @CompanyId AND IsDeleted = 0 ORDER BY ShiftId);

    -- 2. Assign Default Shift Roster for existing active employees
    IF @DayShiftId IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_ShiftAssignments (CompanyId, EmployeeId, ShiftId, EffectiveFrom, Notes, IsActive)
        SELECT @CompanyId, e.EmployeeId, ISNULL(e.ShiftId, @DayShiftId), DATEFROMPARTS(@Year, 1, 1), N'Initial annual roster assignment', 1
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 FROM dbo.tbl_ShiftAssignments sa
              WHERE sa.CompanyId = @CompanyId AND sa.EmployeeId = e.EmployeeId AND sa.IsDeleted = 0
          );

        UPDATE dbo.tbl_Employees
        SET ShiftId = @DayShiftId
        WHERE CompanyId = @CompanyId AND ShiftId IS NULL AND IsDeleted = 0;
    END

    -- 3. Seed Sample Company Holidays for Current Year
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Holidays WHERE CompanyId = @CompanyId AND YEAR(HolidayDate) = @Year)
    BEGIN
        INSERT INTO dbo.tbl_Holidays (CompanyId, BranchId, HolidayName, HolidayDate, HolidayType, Description, IsActive)
        VALUES
            (@CompanyId, NULL, N'New Year''s Day',         DATEFROMPARTS(@Year, 1, 1),   N'Public',  N'Statutory public holiday', 1),
            (@CompanyId, NULL, N'Memorial Day',           DATEFROMPARTS(@Year, 5, 25),  N'Public',  N'National observance', 1),
            (@CompanyId, NULL, N'Independence Day',       DATEFROMPARTS(@Year, 7, 4),   N'Public',  N'Federal holiday', 1),
            (@CompanyId, NULL, N'Labor Day',              DATEFROMPARTS(@Year, 9, 7),   N'Public',  N'National holiday', 1),
            (@CompanyId, NULL, N'Thanksgiving Day',       DATEFROMPARTS(@Year, 11, 26), N'Public',  N'Annual company closure', 1),
            (@CompanyId, NULL, N'Year-End Company Day',   DATEFROMPARTS(@Year, 12, 25), N'Company', N'Winter holiday closure', 1);
    END

    -- 4. Seed Historical Attendance (past 4 weekdays) for rich Monthly Report & Dashboard charts
    DECLARE @Offset INT = 1;
    WHILE @Offset <= 4
    BEGIN
        DECLARE @TargetDate DATE = DATEADD(DAY, -@Offset, @Today);
        IF DATENAME(WEEKDAY, @TargetDate) NOT IN (N'Saturday', N'Sunday')
        BEGIN
            INSERT INTO dbo.tbl_Attendance (
                CompanyId, BranchId, EmployeeId, ShiftId, AttendanceDate, CheckInTime, CheckOutTime,
                Status, LateMinutes, OvertimeMinutes, WorkMinutes, Source
            )
            SELECT
                e.CompanyId,
                e.BranchId,
                e.EmployeeId,
                ISNULL(e.ShiftId, @DayShiftId),
                @TargetDate,
                DATEADD(MINUTE, CASE WHEN e.EmployeeCode = N'EMP-1006' AND @Offset = 2 THEN 565 ELSE 538 END, CAST(@TargetDate AS DATETIME2(0))),
                DATEADD(MINUTE, CASE WHEN e.EmployeeCode = N'EMP-1001' AND @Offset = 1 THEN 1140 ELSE 1080 END, CAST(@TargetDate AS DATETIME2(0))),
                CASE WHEN e.EmployeeCode = N'EMP-1006' AND @Offset = 2 THEN N'Late' ELSE N'Present' END,
                CASE WHEN e.EmployeeCode = N'EMP-1006' AND @Offset = 2 THEN 25 ELSE 0 END,
                CASE WHEN e.EmployeeCode = N'EMP-1001' AND @Offset = 1 THEN 60 ELSE 0 END,
                CASE WHEN e.EmployeeCode = N'EMP-1001' AND @Offset = 1 THEN 602 ELSE 542 END,
                N'Web'
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.tbl_Attendance a
                  WHERE a.EmployeeId = e.EmployeeId AND a.AttendanceDate = @TargetDate
              );
        END
        SET @Offset = @Offset + 1;
    END

    -- 5. Seed Sample Pending Regularization & Overtime Request
    DECLARE @Emp8 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1008');
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');

    IF @Emp8 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_AttendanceRegularizations WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_AttendanceRegularizations (
            CompanyId, EmployeeId, AttendanceDate, RequestedCheckIn, RequestedCheckOut,
            RequestedStatus, Reason, ApprovalStatus
        )
        VALUES (
            @CompanyId, @Emp8, @Today,
            DATEADD(HOUR, 9, CAST(@Today AS DATETIME2(0))),
            DATEADD(HOUR, 18, CAST(@Today AS DATETIME2(0))),
            N'Present', N'Attended client onboarding session off-site; missed badge punch.', N'Pending'
        );
    END

    IF @Emp1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_OvertimeRequests WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_OvertimeRequests (
            CompanyId, EmployeeId, OvertimeDate, RequestedHours, ApprovedHours, Reason, ApprovalStatus
        )
        VALUES (
            @CompanyId, @Emp1, DATEADD(DAY, -1, @Today), 1.50, 0.00,
            N'Production release cutover & database index verification.', N'Pending'
        );
    END
END
GO
