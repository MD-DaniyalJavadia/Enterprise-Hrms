USE [HRMS_DB];
GO

-- ============================================================================
-- Leave Types CRUD Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_LeaveType_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LeaveTypeId, CompanyId, LeaveCode, LeaveName, AnnualQuota, IsPaid,
        AllowCarryForward, MaxCarryForwardDays, AllowEncashment, RequiresDocument,
        MaxConsecutiveDays, IsHalfDayAllowed, GenderRestriction, ColorHex, Description, IsActive
    FROM dbo.tbl_LeaveTypes
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY LeaveName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveType_Save
    @LeaveTypeId         INT = 0,
    @CompanyId           INT,
    @LeaveCode           NVARCHAR(20),
    @LeaveName           NVARCHAR(80),
    @AnnualQuota         DECIMAL(5,2),
    @IsPaid              BIT,
    @AllowCarryForward   BIT,
    @MaxCarryForwardDays DECIMAL(5,2),
    @AllowEncashment     BIT,
    @RequiresDocument    BIT,
    @MaxConsecutiveDays  INT = NULL,
    @IsHalfDayAllowed    BIT,
    @GenderRestriction   NVARCHAR(20) = NULL,
    @ColorHex            NVARCHAR(15),
    @Description         NVARCHAR(300) = NULL,
    @IsActive            BIT,
    @UserId              INT,
    @SavedId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_LeaveTypes WHERE CompanyId = @CompanyId AND LeaveCode = @LeaveCode AND LeaveTypeId <> @LeaveTypeId AND IsDeleted = 0)
        THROW 50401, 'A leave type with this code already exists.', 1;

    IF @LeaveTypeId = 0
    BEGIN
        INSERT INTO dbo.tbl_LeaveTypes (
            CompanyId, LeaveCode, LeaveName, AnnualQuota, IsPaid, AllowCarryForward, MaxCarryForwardDays,
            AllowEncashment, RequiresDocument, MaxConsecutiveDays, IsHalfDayAllowed, GenderRestriction,
            ColorHex, Description, IsActive, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @LeaveCode, @LeaveName, @AnnualQuota, @IsPaid, @AllowCarryForward, @MaxCarryForwardDays,
            @AllowEncashment, @RequiresDocument, @MaxConsecutiveDays, @IsHalfDayAllowed, @GenderRestriction,
            @ColorHex, @Description, @IsActive, 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_LeaveTypes
        SET LeaveCode = @LeaveCode,
            LeaveName = @LeaveName,
            AnnualQuota = @AnnualQuota,
            IsPaid = @IsPaid,
            AllowCarryForward = @AllowCarryForward,
            MaxCarryForwardDays = @MaxCarryForwardDays,
            AllowEncashment = @AllowEncashment,
            RequiresDocument = @RequiresDocument,
            MaxConsecutiveDays = @MaxConsecutiveDays,
            IsHalfDayAllowed = @IsHalfDayAllowed,
            GenderRestriction = @GenderRestriction,
            ColorHex = @ColorHex,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE LeaveTypeId = @LeaveTypeId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @LeaveTypeId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveType_Delete
    @CompanyId   INT,
    @LeaveTypeId INT,
    @UserId      INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_LeaveApplications WHERE CompanyId = @CompanyId AND LeaveTypeId = @LeaveTypeId AND IsDeleted = 0 AND ApprovalStatus IN (N'Pending', N'Approved'))
        THROW 50402, 'Cannot delete a leave type that has active or pending leave applications.', 1;

    UPDATE dbo.tbl_LeaveTypes
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE LeaveTypeId = @LeaveTypeId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Leave Balances & Annual Allocation Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_LeaveBalance_GetAll
    @CompanyId    INT,
    @LeaveYear    INT,
    @EmployeeId   INT = NULL,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        lb.LeaveBalanceId,
        lb.CompanyId,
        lb.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        lb.LeaveTypeId,
        lt.LeaveCode,
        lt.LeaveName,
        lt.ColorHex,
        lb.LeaveYear,
        lb.OpeningBalance,
        lb.AccruedDays,
        lb.CarryForwardDays,
        lb.UsedDays,
        lb.PendingDays,
        lb.EncashedDays,
        (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays) AS AvailableDays
    FROM dbo.tbl_LeaveBalances lb
    INNER JOIN dbo.tbl_Employees e ON lb.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LeaveTypes lt ON lb.LeaveTypeId = lt.LeaveTypeId
    WHERE lb.CompanyId = @CompanyId
      AND lb.LeaveYear = @LeaveYear
      AND e.IsDeleted = 0
      AND lt.IsDeleted = 0
      AND (@EmployeeId IS NULL OR lb.EmployeeId = @EmployeeId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY e.EmployeeCode, lt.LeaveName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveBalance_AllocateAnnual
    @CompanyId           INT,
    @LeaveYear           INT,
    @IncludeCarryForward BIT = 1,
    @UserId              INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Insert missing annual leave balance rows for all active employees & active leave types
    INSERT INTO dbo.tbl_LeaveBalances (
        CompanyId, EmployeeId, LeaveTypeId, LeaveYear,
        OpeningBalance, AccruedDays, CarryForwardDays, UsedDays, PendingDays, EncashedDays,
        CreatedBy, CreatedDate
    )
    SELECT
        @CompanyId,
        e.EmployeeId,
        lt.LeaveTypeId,
        @LeaveYear,
        lt.AnnualQuota,
        0,
        CASE
            WHEN @IncludeCarryForward = 1 AND lt.AllowCarryForward = 1 THEN
                ISNULL((
                    SELECT TOP 1
                        CASE
                            WHEN (prev.OpeningBalance + prev.AccruedDays + prev.CarryForwardDays - prev.UsedDays - prev.EncashedDays) > lt.MaxCarryForwardDays
                                THEN lt.MaxCarryForwardDays
                            WHEN (prev.OpeningBalance + prev.AccruedDays + prev.CarryForwardDays - prev.UsedDays - prev.EncashedDays) < 0
                                THEN 0
                            ELSE (prev.OpeningBalance + prev.AccruedDays + prev.CarryForwardDays - prev.UsedDays - prev.EncashedDays)
                        END
                    FROM dbo.tbl_LeaveBalances prev
                    WHERE prev.EmployeeId = e.EmployeeId AND prev.LeaveTypeId = lt.LeaveTypeId AND prev.LeaveYear = @LeaveYear - 1
                ), 0)
            ELSE 0
        END,
        0, 0, 0,
        @UserId, SYSUTCDATETIME()
    FROM dbo.tbl_Employees e
    CROSS JOIN dbo.tbl_LeaveTypes lt
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
      AND lt.CompanyId = @CompanyId AND lt.IsDeleted = 0 AND lt.IsActive = 1
      AND (lt.GenderRestriction IS NULL OR lt.GenderRestriction = N'' OR lt.GenderRestriction = N'All' OR lt.GenderRestriction = e.Gender)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.tbl_LeaveBalances existing
          WHERE existing.EmployeeId = e.EmployeeId AND existing.LeaveTypeId = lt.LeaveTypeId AND existing.LeaveYear = @LeaveYear
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveBalance_Adjust
    @CompanyId        INT,
    @LeaveBalanceId   INT,
    @OpeningBalance   DECIMAL(5,2),
    @AccruedDays      DECIMAL(5,2),
    @CarryForwardDays DECIMAL(5,2),
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_LeaveBalances
    SET OpeningBalance = @OpeningBalance,
        AccruedDays = @AccruedDays,
        CarryForwardDays = @CarryForwardDays,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE LeaveBalanceId = @LeaveBalanceId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Leave Encashment Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_LeaveEncashment_GetAll
    @CompanyId      INT,
    @ApprovalStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        enc.EncashmentId,
        enc.CompanyId,
        enc.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        enc.LeaveTypeId,
        lt.LeaveName,
        enc.LeaveYear,
        enc.DaysEncashed,
        enc.EncashmentAmount,
        enc.ApprovalStatus,
        enc.Remarks,
        u.FullName AS ReviewedByName,
        enc.ReviewedDate,
        enc.ReviewerComments,
        enc.CreatedDate
    FROM dbo.tbl_LeaveEncashments enc
    INNER JOIN dbo.tbl_Employees e ON enc.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LeaveTypes lt ON enc.LeaveTypeId = lt.LeaveTypeId
    LEFT JOIN dbo.tbl_Users u ON enc.ReviewedBy = u.UserId
    WHERE enc.CompanyId = @CompanyId
      AND enc.IsDeleted = 0
      AND (@ApprovalStatus IS NULL OR enc.ApprovalStatus = @ApprovalStatus)
    ORDER BY CASE WHEN enc.ApprovalStatus = N'Pending' THEN 0 ELSE 1 END, enc.EncashmentId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveEncashment_Submit
    @CompanyId    INT,
    @EmployeeId   INT,
    @LeaveTypeId  INT,
    @LeaveYear    INT,
    @DaysEncashed DECIMAL(5,2),
    @Remarks      NVARCHAR(300) = NULL,
    @UserId       INT,
    @NewId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AllowEncash BIT, @BaseSalary DECIMAL(18,2);
    SELECT @AllowEncash = AllowEncashment FROM dbo.tbl_LeaveTypes WHERE LeaveTypeId = @LeaveTypeId AND CompanyId = @CompanyId AND IsDeleted = 0;
    IF @AllowEncash IS NULL OR @AllowEncash = 0
        THROW 50411, 'Leave encashment is not enabled for this leave type.', 1;

    SELECT @BaseSalary = BaseSalary FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0;
    IF @BaseSalary IS NULL
        THROW 50412, 'Employee not found.', 1;

    DECLARE @Available DECIMAL(5,2) = (
        SELECT (OpeningBalance + AccruedDays + CarryForwardDays - UsedDays - PendingDays - EncashedDays)
        FROM dbo.tbl_LeaveBalances
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND LeaveTypeId = @LeaveTypeId AND LeaveYear = @LeaveYear
    );

    IF @Available IS NULL OR @Available < @DaysEncashed
        THROW 50413, 'Insufficient leave balance for encashment.', 1;

    DECLARE @Amount DECIMAL(18,2) = ROUND((@BaseSalary / 12.0 / 30.0) * @DaysEncashed, 2);

    INSERT INTO dbo.tbl_LeaveEncashments (
        CompanyId, EmployeeId, LeaveTypeId, LeaveYear, DaysEncashed, EncashmentAmount, ApprovalStatus, Remarks, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @EmployeeId, @LeaveTypeId, @LeaveYear, @DaysEncashed, @Amount, N'Pending', @Remarks, @UserId, SYSUTCDATETIME()
    );
    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LeaveEncashment_Review
    @CompanyId        INT,
    @EncashmentId     INT,
    @ApprovalStatus   NVARCHAR(20),
    @ReviewerComments NVARCHAR(400) = NULL,
    @ReviewedBy       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmpId INT, @TypeId INT, @Year INT, @Days DECIMAL(5,2), @CurStatus NVARCHAR(20);
    SELECT @EmpId = EmployeeId, @TypeId = LeaveTypeId, @Year = LeaveYear, @Days = DaysEncashed, @CurStatus = ApprovalStatus
    FROM dbo.tbl_LeaveEncashments
    WHERE EncashmentId = @EncashmentId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmpId IS NULL
        THROW 50414, 'Leave encashment request not found.', 1;
    IF @CurStatus <> N'Pending'
        THROW 50415, 'This encashment request has already been reviewed.', 1;

    IF @ApprovalStatus = N'Approved'
    BEGIN
        DECLARE @Available DECIMAL(5,2) = (
            SELECT (OpeningBalance + AccruedDays + CarryForwardDays - UsedDays - PendingDays - EncashedDays)
            FROM dbo.tbl_LeaveBalances
            WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @Year
        );
        IF @Available IS NULL OR @Available < @Days
            THROW 50413, 'Insufficient leave balance for encashment approval.', 1;

        UPDATE dbo.tbl_LeaveBalances
        SET EncashedDays = EncashedDays + @Days, ModifiedBy = @ReviewedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmpId AND LeaveTypeId = @TypeId AND LeaveYear = @Year;
    END

    UPDATE dbo.tbl_LeaveEncashments
    SET ApprovalStatus = @ApprovalStatus,
        ReviewedBy = @ReviewedBy,
        ReviewedDate = SYSUTCDATETIME(),
        ReviewerComments = @ReviewerComments,
        ModifiedBy = @ReviewedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EncashmentId = @EncashmentId AND CompanyId = @CompanyId;
END
GO
