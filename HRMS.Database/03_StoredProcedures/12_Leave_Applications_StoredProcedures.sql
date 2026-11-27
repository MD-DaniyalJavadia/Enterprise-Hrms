USE [HRMS_DB];
GO

-- ============================================================================
-- Leave Applications, Approvals, Cancellation & Team Calendar SPs
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_LeaveApplication_GetAll
    @CompanyId      INT,
    @EmployeeId     INT = NULL,
    @DepartmentId   INT = NULL,
    @LeaveTypeId    INT = NULL,
    @ApprovalStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Applications List
    SELECT
        la.LeaveApplicationId,
        la.CompanyId,
        la.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        b.BranchName,
        la.LeaveTypeId,
        lt.LeaveCode,
        lt.LeaveName,
        lt.ColorHex,
        la.StartDate,
        la.EndDate,
        la.TotalDays,
        la.IsHalfDay,
        la.HalfDaySession,
        la.Reason,
        la.ContactDuringLeave,
        la.AttachmentFileName,
        la.ApprovalStatus,
        la.ApprovedBy,
        u.FullName AS ApprovedByName,
        la.ApprovedDate,
        la.ReviewerComments,
        la.CreatedDate
    FROM dbo.tbl_LeaveApplications la
    INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
    LEFT JOIN dbo.tbl_Users u ON la.ApprovedBy = u.UserId
    WHERE la.CompanyId = @CompanyId
      AND la.IsDeleted = 0
      AND (@EmployeeId IS NULL OR la.EmployeeId = @EmployeeId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@LeaveTypeId IS NULL OR la.LeaveTypeId = @LeaveTypeId)
      AND (@ApprovalStatus IS NULL OR la.ApprovalStatus = @ApprovalStatus)
    ORDER BY CASE WHEN la.ApprovalStatus = N'Pending' THEN 0 ELSE 1 END, la.StartDate DESC, la.LeaveApplicationId DESC;

    -- 2. Summary KPIs
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    SELECT
        ISNULL(SUM(CASE WHEN ApprovalStatus = N'Pending' THEN 1 ELSE 0 END), 0) AS PendingCount,
        ISNULL(SUM(CASE WHEN ApprovalStatus = N'Approved' AND YEAR(StartDate) = YEAR(@Today) AND MONTH(StartDate) = MONTH(@Today) THEN 1 ELSE 0 END), 0) AS ApprovedThisMonthCount,
        ISNULL(SUM(CASE WHEN ApprovalStatus = N'Approved' AND @Today BETWEEN StartDate AND EndDate THEN 1 ELSE 0 END), 0) AS OnLeaveTodayCount,
        ISNULL(SUM(CASE WHEN ApprovalStatus = N'Rejected' THEN 1 ELSE 0 END), 0) AS RejectedCount
    FROM dbo.tbl_LeaveApplications
    WHERE CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveApplication_GetById
    @CompanyId          INT,
    @LeaveApplicationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        LeaveApplicationId, CompanyId, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays,
        ApprovalStatus, AttachmentFileName, AttachmentStoredName
    FROM dbo.tbl_LeaveApplications
    WHERE LeaveApplicationId = @LeaveApplicationId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveApplication_Submit
    @CompanyId            INT,
    @EmployeeId           INT,
    @LeaveTypeId          INT,
    @StartDate            DATE,
    @EndDate              DATE,
    @IsHalfDay            BIT = 0,
    @HalfDaySession       NVARCHAR(20) = NULL,
    @Reason               NVARCHAR(500),
    @ContactDuringLeave   NVARCHAR(60) = NULL,
    @AttachmentFileName   NVARCHAR(255) = NULL,
    @AttachmentStoredName NVARCHAR(255) = NULL,
    @UserId               INT,
    @NewId                INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @EndDate < @StartDate
        THROW 50421, 'Leave End Date cannot be earlier than Start Date.', 1;

    IF @IsHalfDay = 1 AND @EndDate <> @StartDate
        THROW 50422, 'Half-day leave must start and end on the same date.', 1;

    DECLARE @Gender NVARCHAR(20);
    SELECT @Gender = Gender FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1;
    IF @Gender IS NULL
        THROW 50423, 'Active employee record not found.', 1;

    DECLARE @AnnualQuota DECIMAL(5,2), @RequiresDoc BIT, @MaxConsec INT, @HalfAllowed BIT, @GenderRest NVARCHAR(20);
    SELECT
        @AnnualQuota = AnnualQuota,
        @RequiresDoc = RequiresDocument,
        @MaxConsec = MaxConsecutiveDays,
        @HalfAllowed = IsHalfDayAllowed,
        @GenderRest = GenderRestriction
    FROM dbo.tbl_LeaveTypes
    WHERE LeaveTypeId = @LeaveTypeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1;

    IF @AnnualQuota IS NULL
        THROW 50424, 'Selected leave type is inactive or not found.', 1;

    IF @IsHalfDay = 1 AND @HalfAllowed = 0
        THROW 50425, 'Half-day requests are not permitted for this leave type.', 1;

    IF @GenderRest IS NOT NULL AND @GenderRest <> N'' AND @GenderRest <> N'All' AND @GenderRest <> @Gender
        THROW 50426, 'This leave type is restricted to a different gender eligibility policy.', 1;

    IF @RequiresDoc = 1 AND (@AttachmentStoredName IS NULL OR @AttachmentStoredName = N'')
        THROW 50427, 'A supporting document (e.g., medical certificate) is required for this leave type.', 1;

    -- Overlapping leave check
    IF EXISTS (
        SELECT 1 FROM dbo.tbl_LeaveApplications
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
          AND ApprovalStatus IN (N'Pending', N'Approved')
          AND @StartDate <= EndDate AND @EndDate >= StartDate
    )
        THROW 50428, 'An overlapping pending or approved leave request already exists for these dates.', 1;

    DECLARE @TotalDays DECIMAL(5,2) = CASE WHEN @IsHalfDay = 1 THEN 0.50 ELSE CAST(DATEDIFF(DAY, @StartDate, @EndDate) + 1 AS DECIMAL(5,2)) END;

    IF @MaxConsec IS NOT NULL AND @TotalDays > @MaxConsec
        THROW 50429, 'Requested leave duration exceeds the maximum consecutive days allowed for this leave type.', 1;

    DECLARE @LeaveYear INT = YEAR(@StartDate);
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_LeaveBalances WHERE EmployeeId = @EmployeeId AND LeaveTypeId = @LeaveTypeId AND LeaveYear = @LeaveYear)
    BEGIN
        INSERT INTO dbo.tbl_LeaveBalances (CompanyId, EmployeeId, LeaveTypeId, LeaveYear, OpeningBalance, CreatedBy)
        VALUES (@CompanyId, @EmployeeId, @LeaveTypeId, @LeaveYear, @AnnualQuota, @UserId);
    END

    DECLARE @Available DECIMAL(5,2) = (
        SELECT (OpeningBalance + AccruedDays + CarryForwardDays - UsedDays - PendingDays - EncashedDays)
        FROM dbo.tbl_LeaveBalances
        WHERE EmployeeId = @EmployeeId AND LeaveTypeId = @LeaveTypeId AND LeaveYear = @LeaveYear
    );

    IF @Available < @TotalDays
        THROW 50430, 'Insufficient leave balance for the selected leave type and year.', 1;

    INSERT INTO dbo.tbl_LeaveApplications (
        CompanyId, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays,
        IsHalfDay, HalfDaySession, Reason, ContactDuringLeave,
        AttachmentFileName, AttachmentStoredName, ApprovalStatus, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @EmployeeId, @LeaveTypeId, @StartDate, @EndDate, @TotalDays,
        @IsHalfDay, @HalfDaySession, @Reason, @ContactDuringLeave,
        @AttachmentFileName, @AttachmentStoredName, N'Pending', @UserId, SYSUTCDATETIME()
    );
    SET @NewId = SCOPE_IDENTITY();

    UPDATE dbo.tbl_LeaveBalances
    SET PendingDays = PendingDays + @TotalDays, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND LeaveTypeId = @LeaveTypeId AND LeaveYear = @LeaveYear;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveApplication_Review
    @CompanyId          INT,
    @LeaveApplicationId INT,
    @ApprovalStatus     NVARCHAR(20), -- Approved or Rejected
    @ReviewerComments   NVARCHAR(400) = NULL,
    @ReviewedBy         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmpId INT, @TypeId INT, @Start DATE, @End DATE, @Days DECIMAL(5,2), @IsHalf BIT, @CurStatus NVARCHAR(20), @Reason NVARCHAR(500);
    SELECT
        @EmpId = EmployeeId, @TypeId = LeaveTypeId, @Start = StartDate, @End = EndDate,
        @Days = TotalDays, @IsHalf = IsHalfDay, @CurStatus = ApprovalStatus, @Reason = Reason
    FROM dbo.tbl_LeaveApplications
    WHERE LeaveApplicationId = @LeaveApplicationId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmpId IS NULL
        THROW 50431, 'Leave application not found.', 1;
    IF @CurStatus <> N'Pending'
        THROW 50432, 'This leave application has already been processed.', 1;

    DECLARE @LeaveYear INT = YEAR(@Start);

    UPDATE dbo.tbl_LeaveApplications
    SET ApprovalStatus = @ApprovalStatus,
        ApprovedBy = @ReviewedBy,
        ApprovedDate = SYSUTCDATETIME(),
        ReviewerComments = @ReviewerComments,
        ModifiedBy = @ReviewedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE LeaveApplicationId = @LeaveApplicationId AND CompanyId = @CompanyId;

    IF @ApprovalStatus = N'Approved'
    BEGIN
        UPDATE dbo.tbl_LeaveBalances
        SET PendingDays = CASE WHEN PendingDays >= @Days THEN PendingDays - @Days ELSE 0 END,
            UsedDays = UsedDays + @Days,
            ModifiedBy = @ReviewedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @LeaveYear;

        -- Sync approved leave dates to dbo.tbl_Attendance
        DECLARE @AttStatus NVARCHAR(20) = CASE WHEN @IsHalf = 1 THEN N'HalfDay' ELSE N'OnLeave' END;
        DECLARE @D DATE = @Start;
        DECLARE @Remark NVARCHAR(400) = LEFT(CONCAT(N'Leave Approved: ', @Reason), 400);

        WHILE @D <= @End
        BEGIN
            IF DATENAME(WEEKDAY, @D) NOT IN (N'Saturday', N'Sunday')
            BEGIN
                EXEC dbo.sp_Attendance_MarkManual
                    @CompanyId = @CompanyId,
                    @EmployeeId = @EmpId,
                    @AttendanceDate = @D,
                    @Status = @AttStatus,
                    @CheckInTime = NULL,
                    @CheckOutTime = NULL,
                    @LateMinutes = 0,
                    @OvertimeMinutes = 0,
                    @Source = N'LeaveSync',
                    @Remarks = @Remark,
                    @UserId = @ReviewedBy;
            END
            SET @D = DATEADD(DAY, 1, @D);
        END
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_LeaveBalances
        SET PendingDays = CASE WHEN PendingDays >= @Days THEN PendingDays - @Days ELSE 0 END,
            ModifiedBy = @ReviewedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @LeaveYear;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveApplication_Cancel
    @CompanyId          INT,
    @LeaveApplicationId INT,
    @UserId             INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmpId INT, @TypeId INT, @Start DATE, @End DATE, @Days DECIMAL(5,2), @CurStatus NVARCHAR(20);
    SELECT @EmpId = EmployeeId, @TypeId = LeaveTypeId, @Start = StartDate, @End = EndDate, @Days = TotalDays, @CurStatus = ApprovalStatus
    FROM dbo.tbl_LeaveApplications
    WHERE LeaveApplicationId = @LeaveApplicationId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmpId IS NULL
        THROW 50431, 'Leave application not found.', 1;
    IF @CurStatus NOT IN (N'Pending', N'Approved')
        THROW 50433, 'Only pending or approved leave requests can be cancelled.', 1;

    DECLARE @LeaveYear INT = YEAR(@Start);

    IF @CurStatus = N'Pending'
    BEGIN
        UPDATE dbo.tbl_LeaveBalances
        SET PendingDays = CASE WHEN PendingDays >= @Days THEN PendingDays - @Days ELSE 0 END,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @LeaveYear;
    END
    ELSE IF @CurStatus = N'Approved'
    BEGIN
        UPDATE dbo.tbl_LeaveBalances
        SET UsedDays = CASE WHEN UsedDays >= @Days THEN UsedDays - @Days ELSE 0 END,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @LeaveYear;

        UPDATE dbo.tbl_Attendance
        SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId
          AND AttendanceDate BETWEEN @Start AND @End
          AND Source = N'LeaveSync';
    END

    UPDATE dbo.tbl_LeaveApplications
    SET ApprovalStatus = N'Cancelled',
        CancelledDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE LeaveApplicationId = @LeaveApplicationId AND CompanyId = @CompanyId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Leave_GetCalendarEvents
    @CompanyId    INT,
    @Year         INT,
    @Month        INT,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MonthStart DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @MonthEnd   DATE = EOMONTH(@MonthStart);

    SELECT
        la.LeaveApplicationId,
        la.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        lt.LeaveCode,
        lt.LeaveName,
        lt.ColorHex,
        la.StartDate,
        la.EndDate,
        la.TotalDays,
        la.IsHalfDay,
        la.ApprovalStatus
    FROM dbo.tbl_LeaveApplications la
    INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
    WHERE la.CompanyId = @CompanyId
      AND la.IsDeleted = 0
      AND la.ApprovalStatus IN (N'Approved', N'Pending')
      AND la.StartDate <= @MonthEnd
      AND la.EndDate >= @MonthStart
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY la.StartDate, e.FullName;
END
GO
