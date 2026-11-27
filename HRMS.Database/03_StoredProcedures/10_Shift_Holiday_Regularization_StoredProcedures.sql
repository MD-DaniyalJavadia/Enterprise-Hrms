USE [HRMS_DB];
GO

-- ============================================================================
-- Shift Assignments / Roster Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_ShiftAssignment_GetAll
    @CompanyId  INT,
    @EmployeeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sa.AssignmentId,
        sa.CompanyId,
        sa.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        sa.ShiftId,
        s.ShiftCode,
        s.ShiftName,
        CONVERT(VARCHAR(5), s.StartTime, 108) AS StartTimeText,
        CONVERT(VARCHAR(5), s.EndTime, 108) AS EndTimeText,
        s.WeeklyOffDays,
        sa.EffectiveFrom,
        sa.EffectiveTo,
        sa.Notes,
        sa.IsActive
    FROM dbo.tbl_ShiftAssignments sa
    INNER JOIN dbo.tbl_Employees e ON sa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Shifts s ON sa.ShiftId = s.ShiftId
    WHERE sa.CompanyId = @CompanyId
      AND sa.IsDeleted = 0
      AND e.IsDeleted = 0
      AND (@EmployeeId IS NULL OR sa.EmployeeId = @EmployeeId)
    ORDER BY sa.EffectiveFrom DESC, e.EmployeeCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ShiftAssignment_Save
    @AssignmentId  INT = 0,
    @CompanyId     INT,
    @EmployeeId    INT,
    @ShiftId       INT,
    @EffectiveFrom DATE,
    @EffectiveTo   DATE = NULL,
    @Notes         NVARCHAR(300) = NULL,
    @UserId        INT,
    @SavedId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @EffectiveTo IS NOT NULL AND @EffectiveTo < @EffectiveFrom
        THROW 50311, 'Effective To date cannot be earlier than Effective From date.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50312, 'Employee not found in company.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Shifts WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50313, 'Shift not found in company.', 1;

    IF @AssignmentId = 0
    BEGIN
        INSERT INTO dbo.tbl_ShiftAssignments (CompanyId, EmployeeId, ShiftId, EffectiveFrom, EffectiveTo, Notes, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @EmployeeId, @ShiftId, @EffectiveFrom, @EffectiveTo, @Notes, 1, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_ShiftAssignments
        SET EmployeeId = @EmployeeId,
            ShiftId = @ShiftId,
            EffectiveFrom = @EffectiveFrom,
            EffectiveTo = @EffectiveTo,
            Notes = @Notes,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE AssignmentId = @AssignmentId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @AssignmentId;
    END

    -- Keep employee's default ShiftId synced when assignment is active today
    IF @EffectiveFrom <= CAST(GETDATE() AS DATE) AND (@EffectiveTo IS NULL OR @EffectiveTo >= CAST(GETDATE() AS DATE))
    BEGIN
        UPDATE dbo.tbl_Employees
        SET ShiftId = @ShiftId, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ShiftAssignment_Delete
    @CompanyId    INT,
    @AssignmentId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_ShiftAssignments
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE AssignmentId = @AssignmentId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Holiday Calendar Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Holiday_GetAll
    @CompanyId INT,
    @Year      INT = NULL,
    @BranchId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.HolidayId,
        h.CompanyId,
        h.BranchId,
        ISNULL(b.BranchName, N'All Branches (Company-Wide)') AS BranchName,
        h.HolidayName,
        h.HolidayDate,
        h.HolidayType,
        h.Description,
        h.IsActive
    FROM dbo.tbl_Holidays h
    LEFT JOIN dbo.tbl_Branches b ON h.BranchId = b.BranchId
    WHERE h.CompanyId = @CompanyId
      AND h.IsDeleted = 0
      AND (@Year IS NULL OR YEAR(h.HolidayDate) = @Year)
      AND (@BranchId IS NULL OR h.BranchId IS NULL OR h.BranchId = @BranchId)
    ORDER BY h.HolidayDate ASC, h.HolidayName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Holiday_Save
    @HolidayId   INT = 0,
    @CompanyId   INT,
    @BranchId    INT = NULL,
    @HolidayName NVARCHAR(120),
    @HolidayDate DATE,
    @HolidayType NVARCHAR(30),
    @Description NVARCHAR(300) = NULL,
    @IsActive    BIT,
    @UserId      INT,
    @SavedId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_Holidays
        WHERE CompanyId = @CompanyId AND HolidayDate = @HolidayDate
          AND ISNULL(BranchId, 0) = ISNULL(@BranchId, 0)
          AND HolidayName = @HolidayName AND HolidayId <> @HolidayId AND IsDeleted = 0
    )
        THROW 50321, 'This holiday is already configured for the selected date and branch.', 1;

    IF @HolidayId = 0
    BEGIN
        INSERT INTO dbo.tbl_Holidays (CompanyId, BranchId, HolidayName, HolidayDate, HolidayType, Description, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @BranchId, @HolidayName, @HolidayDate, @HolidayType, @Description, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_Holidays
        SET BranchId = @BranchId, HolidayName = @HolidayName, HolidayDate = @HolidayDate,
            HolidayType = @HolidayType, Description = @Description, IsActive = @IsActive,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE HolidayId = @HolidayId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @HolidayId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Holiday_Delete
    @CompanyId INT,
    @HolidayId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Holidays
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE HolidayId = @HolidayId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Attendance Regularization Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_AttendanceRegularization_GetAll
    @CompanyId      INT,
    @EmployeeId     INT = NULL,
    @ApprovalStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RegularizationId,
        r.CompanyId,
        r.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        r.AttendanceDate,
        r.RequestedCheckIn,
        r.RequestedCheckOut,
        r.RequestedStatus,
        r.Reason,
        r.ApprovalStatus,
        r.ReviewedBy,
        u.FullName AS ReviewedByName,
        r.ReviewedDate,
        r.ReviewerComments,
        r.CreatedDate
    FROM dbo.tbl_AttendanceRegularizations r
    INNER JOIN dbo.tbl_Employees e ON r.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_Users u ON r.ReviewedBy = u.UserId
    WHERE r.CompanyId = @CompanyId
      AND r.IsDeleted = 0
      AND (@EmployeeId IS NULL OR r.EmployeeId = @EmployeeId)
      AND (@ApprovalStatus IS NULL OR r.ApprovalStatus = @ApprovalStatus)
    ORDER BY CASE WHEN r.ApprovalStatus = N'Pending' THEN 0 ELSE 1 END, r.AttendanceDate DESC, r.RegularizationId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AttendanceRegularization_Submit
    @CompanyId         INT,
    @EmployeeId        INT,
    @AttendanceDate    DATE,
    @RequestedCheckIn  DATETIME2(0) = NULL,
    @RequestedCheckOut DATETIME2(0) = NULL,
    @RequestedStatus   NVARCHAR(20),
    @Reason            NVARCHAR(500),
    @UserId            INT,
    @NewId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50331, 'Employee not found in company.', 1;

    IF @RequestedCheckIn IS NOT NULL AND @RequestedCheckOut IS NOT NULL AND @RequestedCheckOut < @RequestedCheckIn
        THROW 50304, 'Requested check-out cannot be earlier than check-in.', 1;

    INSERT INTO dbo.tbl_AttendanceRegularizations (
        CompanyId, EmployeeId, AttendanceDate, RequestedCheckIn, RequestedCheckOut,
        RequestedStatus, Reason, ApprovalStatus, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @EmployeeId, @AttendanceDate, @RequestedCheckIn, @RequestedCheckOut,
        @RequestedStatus, @Reason, N'Pending', @UserId, SYSUTCDATETIME()
    );
    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AttendanceRegularization_Review
    @CompanyId        INT,
    @RegularizationId INT,
    @ApprovalStatus   NVARCHAR(20), -- Approved or Rejected
    @ReviewerComments NVARCHAR(400) = NULL,
    @ReviewedBy       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmployeeId INT, @AttendanceDate DATE, @ReqIn DATETIME2(0), @ReqOut DATETIME2(0),
            @ReqStatus NVARCHAR(20), @Reason NVARCHAR(500), @CurrentStatus NVARCHAR(20);

    SELECT
        @EmployeeId = EmployeeId,
        @AttendanceDate = AttendanceDate,
        @ReqIn = RequestedCheckIn,
        @ReqOut = RequestedCheckOut,
        @ReqStatus = RequestedStatus,
        @Reason = Reason,
        @CurrentStatus = ApprovalStatus
    FROM dbo.tbl_AttendanceRegularizations
    WHERE RegularizationId = @RegularizationId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmployeeId IS NULL
        THROW 50332, 'Regularization request not found.', 1;

    IF @CurrentStatus <> N'Pending'
        THROW 50333, 'This regularization request has already been reviewed.', 1;

    UPDATE dbo.tbl_AttendanceRegularizations
    SET ApprovalStatus = @ApprovalStatus,
        ReviewedBy = @ReviewedBy,
        ReviewedDate = SYSUTCDATETIME(),
        ReviewerComments = @ReviewerComments,
        ModifiedBy = @ReviewedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE RegularizationId = @RegularizationId AND CompanyId = @CompanyId;

    IF @ApprovalStatus = N'Approved'
    BEGIN
        DECLARE @RemarkText NVARCHAR(400) = LEFT(CONCAT(N'Regularized: ', @Reason), 400);
        EXEC dbo.sp_Attendance_MarkManual
            @CompanyId = @CompanyId,
            @EmployeeId = @EmployeeId,
            @AttendanceDate = @AttendanceDate,
            @Status = @ReqStatus,
            @CheckInTime = @ReqIn,
            @CheckOutTime = @ReqOut,
            @LateMinutes = 0,
            @OvertimeMinutes = NULL,
            @Source = N'Regularization',
            @Remarks = @RemarkText,
            @UserId = @ReviewedBy;
    END
END
GO

-- ============================================================================
-- Overtime Request Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_OvertimeRequest_GetAll
    @CompanyId      INT,
    @EmployeeId     INT = NULL,
    @ApprovalStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OvertimeRequestId,
        o.CompanyId,
        o.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        o.OvertimeDate,
        o.RequestedHours,
        o.ApprovedHours,
        o.Reason,
        o.ApprovalStatus,
        o.ReviewedBy,
        u.FullName AS ReviewedByName,
        o.ReviewedDate,
        o.ReviewerComments,
        o.CreatedDate
    FROM dbo.tbl_OvertimeRequests o
    INNER JOIN dbo.tbl_Employees e ON o.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_Users u ON o.ReviewedBy = u.UserId
    WHERE o.CompanyId = @CompanyId
      AND o.IsDeleted = 0
      AND (@EmployeeId IS NULL OR o.EmployeeId = @EmployeeId)
      AND (@ApprovalStatus IS NULL OR o.ApprovalStatus = @ApprovalStatus)
    ORDER BY CASE WHEN o.ApprovalStatus = N'Pending' THEN 0 ELSE 1 END, o.OvertimeDate DESC, o.OvertimeRequestId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_OvertimeRequest_Submit
    @CompanyId      INT,
    @EmployeeId     INT,
    @OvertimeDate   DATE,
    @RequestedHours DECIMAL(5,2),
    @Reason         NVARCHAR(500),
    @UserId         INT,
    @NewId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50341, 'Employee not found in company.', 1;

    IF @RequestedHours <= 0 OR @RequestedHours > 24
        THROW 50342, 'Requested overtime hours must be between 0.25 and 24.', 1;

    INSERT INTO dbo.tbl_OvertimeRequests (
        CompanyId, EmployeeId, OvertimeDate, RequestedHours, ApprovedHours, Reason, ApprovalStatus, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @EmployeeId, @OvertimeDate, @RequestedHours, 0, @Reason, N'Pending', @UserId, SYSUTCDATETIME()
    );
    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_OvertimeRequest_Review
    @CompanyId         INT,
    @OvertimeRequestId INT,
    @ApprovalStatus    NVARCHAR(20), -- Approved or Rejected
    @ApprovedHours     DECIMAL(5,2) = 0,
    @ReviewerComments  NVARCHAR(400) = NULL,
    @ReviewedBy        INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmployeeId INT, @OvertimeDate DATE, @CurrentStatus NVARCHAR(20);
    SELECT @EmployeeId = EmployeeId, @OvertimeDate = OvertimeDate, @CurrentStatus = ApprovalStatus
    FROM dbo.tbl_OvertimeRequests
    WHERE OvertimeRequestId = @OvertimeRequestId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmployeeId IS NULL
        THROW 50343, 'Overtime request not found.', 1;

    IF @CurrentStatus <> N'Pending'
        THROW 50344, 'This overtime request has already been reviewed.', 1;

    DECLARE @FinalHours DECIMAL(5,2) = CASE WHEN @ApprovalStatus = N'Approved' THEN @ApprovedHours ELSE 0 END;

    UPDATE dbo.tbl_OvertimeRequests
    SET ApprovalStatus = @ApprovalStatus,
        ApprovedHours = @FinalHours,
        ReviewedBy = @ReviewedBy,
        ReviewedDate = SYSUTCDATETIME(),
        ReviewerComments = @ReviewerComments,
        ModifiedBy = @ReviewedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE OvertimeRequestId = @OvertimeRequestId AND CompanyId = @CompanyId;

    IF @ApprovalStatus = N'Approved'
    BEGIN
        UPDATE dbo.tbl_Attendance
        SET OvertimeMinutes = CAST(@FinalHours * 60 AS INT),
            ModifiedBy = @ReviewedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND AttendanceDate = @OvertimeDate AND IsDeleted = 0;
    END
END
GO
