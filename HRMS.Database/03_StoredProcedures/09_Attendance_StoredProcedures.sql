USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 4: Daily Attendance, Check-In/Out, Manual Marking & Monthly Reporting
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_Attendance_GetDailyRegister
    @CompanyId      INT,
    @AttendanceDate DATE,
    @BranchId       INT = NULL,
    @DepartmentId   INT = NULL,
    @Status         NVARCHAR(20) = NULL,
    @SearchTerm     NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Daily Attendance Register
    SELECT
        a.AttendanceId,
        a.CompanyId,
        a.BranchId,
        b.BranchName,
        a.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        des.DesignationTitle,
        ISNULL(a.ShiftId, e.ShiftId) AS ShiftId,
        s.ShiftName,
        a.AttendanceDate,
        a.CheckInTime,
        a.CheckOutTime,
        a.Status,
        a.LateMinutes,
        a.EarlyExitMinutes,
        a.WorkMinutes,
        a.OvertimeMinutes,
        a.Source,
        a.Remarks
    FROM dbo.tbl_Attendance a
    INNER JOIN dbo.tbl_Employees e ON a.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Branches b ON a.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(a.ShiftId, e.ShiftId) = s.ShiftId
    WHERE a.CompanyId = @CompanyId
      AND a.AttendanceDate = @AttendanceDate
      AND a.IsDeleted = 0
      AND e.IsDeleted = 0
      AND (@BranchId IS NULL OR a.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@Status IS NULL OR a.Status = @Status)
      AND (@SearchTerm IS NULL
           OR e.FullName LIKE N'%' + @SearchTerm + N'%'
           OR e.EmployeeCode LIKE N'%' + @SearchTerm + N'%')
    ORDER BY e.EmployeeCode;

    -- 2. Daily KPI Summary
    SELECT
        (SELECT COUNT(1) FROM dbo.tbl_Employees e
         WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
           AND (@BranchId IS NULL OR e.BranchId = @BranchId)
           AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)) AS TotalActiveEmployees,
        ISNULL(SUM(CASE WHEN a.Status = N'Present' THEN 1 ELSE 0 END), 0) AS PresentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Late' THEN 1 ELSE 0 END), 0) AS LateCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Absent' THEN 1 ELSE 0 END), 0) AS AbsentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'HalfDay' THEN 1 ELSE 0 END), 0) AS HalfDayCount,
        ISNULL(SUM(CASE WHEN a.Status = N'OnLeave' THEN 1 ELSE 0 END), 0) AS OnLeaveCount,
        ISNULL(SUM(a.OvertimeMinutes), 0) AS TotalOvertimeMinutes
    FROM dbo.tbl_Attendance a
    INNER JOIN dbo.tbl_Employees e ON a.EmployeeId = e.EmployeeId
    WHERE a.CompanyId = @CompanyId
      AND a.AttendanceDate = @AttendanceDate
      AND a.IsDeleted = 0
      AND (@BranchId IS NULL OR a.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId);
END
GO

-- ============================================================================
-- Check-In (Self-Service or Admin Quick Punch)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Attendance_CheckIn
    @CompanyId      INT,
    @EmployeeId     INT,
    @AttendanceDate DATE,
    @CheckInTime    DATETIME2(0),
    @CheckInIp      NVARCHAR(45) = NULL,
    @Remarks        NVARCHAR(400) = NULL,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BranchId INT, @EmpShiftId INT;
    SELECT @BranchId = BranchId, @EmpShiftId = ShiftId
    FROM dbo.tbl_Employees
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1;

    IF @BranchId IS NULL
        THROW 50301, 'Active employee record not found.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Attendance WHERE EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate AND IsDeleted = 0 AND CheckInTime IS NOT NULL)
        THROW 50302, 'Check-in has already been recorded for this date.', 1;

    DECLARE @ShiftId INT;
    SELECT TOP 1 @ShiftId = sa.ShiftId
    FROM dbo.tbl_ShiftAssignments sa
    WHERE sa.CompanyId = @CompanyId AND sa.EmployeeId = @EmployeeId
      AND sa.IsDeleted = 0 AND sa.IsActive = 1
      AND @AttendanceDate >= sa.EffectiveFrom
      AND (sa.EffectiveTo IS NULL OR @AttendanceDate <= sa.EffectiveTo)
    ORDER BY sa.EffectiveFrom DESC, sa.AssignmentId DESC;

    IF @ShiftId IS NULL SET @ShiftId = @EmpShiftId;
    IF @ShiftId IS NULL
        SELECT TOP 1 @ShiftId = ShiftId FROM dbo.tbl_Shifts WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1 ORDER BY ShiftId;

    DECLARE @StartTime TIME(0) = '09:00:00', @GraceMinutes INT = 15;
    IF @ShiftId IS NOT NULL
    BEGIN
        SELECT @StartTime = StartTime, @GraceMinutes = GracePeriodMinutes
        FROM dbo.tbl_Shifts WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId;
    END

    DECLARE @ScheduledStart DATETIME2(0) = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), @StartTime), CAST(@AttendanceDate AS DATETIME2(0)));
    DECLARE @GraceCutoff    DATETIME2(0) = DATEADD(MINUTE, @GraceMinutes, @ScheduledStart);
    DECLARE @LateMinutes    INT = CASE WHEN @CheckInTime > @GraceCutoff THEN DATEDIFF(MINUTE, @ScheduledStart, @CheckInTime) ELSE 0 END;
    DECLARE @Status         NVARCHAR(20) = CASE WHEN @LateMinutes > 0 THEN N'Late' ELSE N'Present' END;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Attendance WHERE EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate AND IsDeleted = 0)
    BEGIN
        UPDATE dbo.tbl_Attendance
        SET ShiftId = @ShiftId,
            CheckInTime = @CheckInTime,
            Status = @Status,
            LateMinutes = @LateMinutes,
            Source = N'Web',
            Remarks = ISNULL(@Remarks, Remarks),
            CheckInIp = @CheckInIp,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate AND IsDeleted = 0;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tbl_Attendance (
            CompanyId, BranchId, EmployeeId, ShiftId, AttendanceDate, CheckInTime,
            Status, LateMinutes, OvertimeMinutes, WorkMinutes, EarlyExitMinutes,
            Source, Remarks, CheckInIp, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @BranchId, @EmployeeId, @ShiftId, @AttendanceDate, @CheckInTime,
            @Status, @LateMinutes, 0, 0, 0,
            N'Web', @Remarks, @CheckInIp, @UserId, SYSUTCDATETIME()
        );
    END
END
GO

-- ============================================================================
-- Check-Out (Computes WorkMinutes, EarlyExitMinutes & OvertimeMinutes)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Attendance_CheckOut
    @CompanyId      INT,
    @EmployeeId     INT,
    @AttendanceDate DATE,
    @CheckOutTime   DATETIME2(0),
    @Remarks        NVARCHAR(400) = NULL,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AttendanceId BIGINT, @CheckInTime DATETIME2(0), @ShiftId INT;
    SELECT @AttendanceId = AttendanceId, @CheckInTime = CheckInTime, @ShiftId = ShiftId
    FROM dbo.tbl_Attendance
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate AND IsDeleted = 0;

    IF @AttendanceId IS NULL OR @CheckInTime IS NULL
        THROW 50303, 'No check-in record found for this employee on the selected date.', 1;

    IF @CheckOutTime < @CheckInTime
        THROW 50304, 'Check-out time cannot be earlier than check-in time.', 1;

    DECLARE @RequiredWorkMinutes INT = 480, @EndTime TIME(0) = '18:00:00', @IsNightShift BIT = 0;
    IF @ShiftId IS NOT NULL
    BEGIN
        SELECT @RequiredWorkMinutes = CAST(WorkingHours * 60 AS INT), @EndTime = EndTime, @IsNightShift = IsNightShift
        FROM dbo.tbl_Shifts WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId;
    END

    DECLARE @RawMinutes INT = DATEDIFF(MINUTE, @CheckInTime, @CheckOutTime);
    DECLARE @ShiftEndDate DATE = CASE WHEN @IsNightShift = 1 THEN DATEADD(DAY, 1, @AttendanceDate) ELSE @AttendanceDate END;
    DECLARE @ScheduledEnd DATETIME2(0) = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), @EndTime), CAST(@ShiftEndDate AS DATETIME2(0)));

    DECLARE @EarlyExitMinutes INT = CASE WHEN @CheckOutTime < @ScheduledEnd THEN DATEDIFF(MINUTE, @CheckOutTime, @ScheduledEnd) ELSE 0 END;
    DECLARE @OvertimeMinutes  INT = CASE WHEN @RawMinutes > @RequiredWorkMinutes THEN @RawMinutes - @RequiredWorkMinutes ELSE 0 END;

    UPDATE dbo.tbl_Attendance
    SET CheckOutTime = @CheckOutTime,
        WorkMinutes = @RawMinutes,
        EarlyExitMinutes = @EarlyExitMinutes,
        OvertimeMinutes = @OvertimeMinutes,
        Remarks = ISNULL(@Remarks, Remarks),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AttendanceId = @AttendanceId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Manual Attendance Upsert (HR Marking & Bulk Import)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Attendance_MarkManual
    @CompanyId      INT,
    @EmployeeId     INT,
    @AttendanceDate DATE,
    @Status         NVARCHAR(20),
    @CheckInTime    DATETIME2(0) = NULL,
    @CheckOutTime   DATETIME2(0) = NULL,
    @LateMinutes    INT = NULL,
    @OvertimeMinutes INT = NULL,
    @Source         NVARCHAR(30) = N'Manual',
    @Remarks        NVARCHAR(400) = NULL,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BranchId INT, @EmpShiftId INT;
    SELECT @BranchId = BranchId, @EmpShiftId = ShiftId
    FROM dbo.tbl_Employees
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @BranchId IS NULL
        THROW 50305, 'Employee not found in company.', 1;

    IF @CheckInTime IS NOT NULL AND @CheckOutTime IS NOT NULL AND @CheckOutTime < @CheckInTime
        THROW 50304, 'Check-out time cannot be earlier than check-in time.', 1;

    DECLARE @ShiftId INT;
    SELECT TOP 1 @ShiftId = sa.ShiftId
    FROM dbo.tbl_ShiftAssignments sa
    WHERE sa.CompanyId = @CompanyId AND sa.EmployeeId = @EmployeeId
      AND sa.IsDeleted = 0 AND sa.IsActive = 1
      AND @AttendanceDate >= sa.EffectiveFrom
      AND (sa.EffectiveTo IS NULL OR @AttendanceDate <= sa.EffectiveTo)
    ORDER BY sa.EffectiveFrom DESC, sa.AssignmentId DESC;
    IF @ShiftId IS NULL SET @ShiftId = @EmpShiftId;

    DECLARE @WorkMin INT = 0, @CalcLate INT = ISNULL(@LateMinutes, 0), @CalcOT INT = ISNULL(@OvertimeMinutes, 0), @EarlyExit INT = 0;
    DECLARE @StartTime TIME(0) = '09:00:00', @EndTime TIME(0) = '18:00:00', @GraceMin INT = 15, @ReqMin INT = 480, @IsNight BIT = 0;

    IF @ShiftId IS NOT NULL
    BEGIN
        SELECT @StartTime = StartTime, @EndTime = EndTime, @GraceMin = GracePeriodMinutes,
               @ReqMin = CAST(WorkingHours * 60 AS INT), @IsNight = IsNightShift
        FROM dbo.tbl_Shifts WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId;
    END

    IF @CheckInTime IS NOT NULL AND @LateMinutes IS NULL
    BEGIN
        DECLARE @SchedStart DATETIME2(0) = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), @StartTime), CAST(@AttendanceDate AS DATETIME2(0)));
        SET @CalcLate = CASE WHEN @CheckInTime > DATEADD(MINUTE, @GraceMin, @SchedStart) THEN DATEDIFF(MINUTE, @SchedStart, @CheckInTime) ELSE 0 END;
    END

    IF @CheckInTime IS NOT NULL AND @CheckOutTime IS NOT NULL
    BEGIN
        SET @WorkMin = DATEDIFF(MINUTE, @CheckInTime, @CheckOutTime);
        DECLARE @SchedEnd DATETIME2(0) = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME(0)), @EndTime), CAST(CASE WHEN @IsNight = 1 THEN DATEADD(DAY, 1, @AttendanceDate) ELSE @AttendanceDate END AS DATETIME2(0)));
        SET @EarlyExit = CASE WHEN @CheckOutTime < @SchedEnd THEN DATEDIFF(MINUTE, @CheckOutTime, @SchedEnd) ELSE 0 END;
        IF @OvertimeMinutes IS NULL
            SET @CalcOT = CASE WHEN @WorkMin > @ReqMin THEN @WorkMin - @ReqMin ELSE 0 END;
    END

    IF EXISTS (SELECT 1 FROM dbo.tbl_Attendance WHERE EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate)
    BEGIN
        UPDATE dbo.tbl_Attendance
        SET BranchId = @BranchId,
            ShiftId = @ShiftId,
            CheckInTime = @CheckInTime,
            CheckOutTime = @CheckOutTime,
            Status = @Status,
            LateMinutes = @CalcLate,
            OvertimeMinutes = @CalcOT,
            WorkMinutes = @WorkMin,
            EarlyExitMinutes = @EarlyExit,
            Source = @Source,
            Remarks = @Remarks,
            IsDeleted = 0,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmployeeId AND AttendanceDate = @AttendanceDate;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tbl_Attendance (
            CompanyId, BranchId, EmployeeId, ShiftId, AttendanceDate, CheckInTime, CheckOutTime,
            Status, LateMinutes, OvertimeMinutes, WorkMinutes, EarlyExitMinutes,
            Source, Remarks, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @BranchId, @EmployeeId, @ShiftId, @AttendanceDate, @CheckInTime, @CheckOutTime,
            @Status, @CalcLate, @CalcOT, @WorkMin, @EarlyExit,
            @Source, @Remarks, @UserId, SYSUTCDATETIME()
        );
    END
END
GO

-- ============================================================================
-- Monthly Attendance Report (Employee-Wise Rollup)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Attendance_GetMonthlyReport
    @CompanyId    INT,
    @Year         INT,
    @Month        INT,
    @BranchId     INT = NULL,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate   DATE = EOMONTH(@StartDate);

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        b.BranchName,
        d.DepartmentName,
        des.DesignationTitle,
        ISNULL(SUM(CASE WHEN a.Status IN (N'Present', N'Late') THEN 1 ELSE 0 END), 0) AS PresentDays,
        ISNULL(SUM(CASE WHEN a.Status = N'Late' THEN 1 ELSE 0 END), 0) AS LateDays,
        ISNULL(SUM(CASE WHEN a.Status = N'Absent' THEN 1 ELSE 0 END), 0) AS AbsentDays,
        ISNULL(SUM(CASE WHEN a.Status = N'HalfDay' THEN 1 ELSE 0 END), 0) AS HalfDays,
        ISNULL(SUM(CASE WHEN a.Status = N'OnLeave' THEN 1 ELSE 0 END), 0) AS LeaveDays,
        ISNULL(SUM(a.WorkMinutes), 0) AS TotalWorkMinutes,
        ISNULL(SUM(a.LateMinutes), 0) AS TotalLateMinutes,
        ISNULL(SUM(a.OvertimeMinutes), 0) AS TotalOvertimeMinutes
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_Attendance a
        ON a.EmployeeId = e.EmployeeId
        AND a.CompanyId = @CompanyId
        AND a.IsDeleted = 0
        AND a.AttendanceDate BETWEEN @StartDate AND @EndDate
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    GROUP BY e.EmployeeId, e.EmployeeCode, e.FullName, b.BranchName, d.DepartmentName, des.DesignationTitle
    ORDER BY e.EmployeeCode;
END
GO

-- ============================================================================
-- Individual Employee Monthly Attendance Log & Totals
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Attendance_GetEmployeeMonthLog
    @CompanyId  INT,
    @EmployeeId INT,
    @Year       INT,
    @Month      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate   DATE = EOMONTH(@StartDate);

    SELECT
        a.AttendanceId,
        a.CompanyId,
        a.BranchId,
        b.BranchName,
        a.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        des.DesignationTitle,
        ISNULL(a.ShiftId, e.ShiftId) AS ShiftId,
        s.ShiftName,
        a.AttendanceDate,
        a.CheckInTime,
        a.CheckOutTime,
        a.Status,
        a.LateMinutes,
        a.EarlyExitMinutes,
        a.WorkMinutes,
        a.OvertimeMinutes,
        a.Source,
        a.Remarks
    FROM dbo.tbl_Attendance a
    INNER JOIN dbo.tbl_Employees e ON a.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Branches b ON a.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(a.ShiftId, e.ShiftId) = s.ShiftId
    WHERE a.CompanyId = @CompanyId
      AND a.EmployeeId = @EmployeeId
      AND a.IsDeleted = 0
      AND a.AttendanceDate BETWEEN @StartDate AND @EndDate
    ORDER BY a.AttendanceDate ASC;
END
GO
