USE [HRMS_DB];
GO

-- ============================================================================
-- Job Grades Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_JobGrade_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT JobGradeId, CompanyId, GradeCode, GradeName, LevelOrder, MinSalary, MaxSalary, Description, IsActive, CreatedDate
    FROM dbo.tbl_JobGrades
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY LevelOrder, GradeCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobGrade_Save
    @JobGradeId  INT = 0,
    @CompanyId   INT,
    @GradeCode   NVARCHAR(20),
    @GradeName   NVARCHAR(100),
    @LevelOrder  INT,
    @MinSalary   DECIMAL(18,2),
    @MaxSalary   DECIMAL(18,2),
    @Description NVARCHAR(300) = NULL,
    @IsActive    BIT,
    @UserId      INT,
    @SavedId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_JobGrades WHERE CompanyId = @CompanyId AND GradeCode = @GradeCode AND JobGradeId <> @JobGradeId AND IsDeleted = 0)
        THROW 50141, 'Job grade code already exists.', 1;

    IF @JobGradeId = 0
    BEGIN
        INSERT INTO dbo.tbl_JobGrades (CompanyId, GradeCode, GradeName, LevelOrder, MinSalary, MaxSalary, Description, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @GradeCode, @GradeName, @LevelOrder, @MinSalary, @MaxSalary, @Description, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_JobGrades
        SET GradeCode = @GradeCode, GradeName = @GradeName, LevelOrder = @LevelOrder,
            MinSalary = @MinSalary, MaxSalary = @MaxSalary, Description = @Description,
            IsActive = @IsActive, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE JobGradeId = @JobGradeId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @JobGradeId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobGrade_Delete
    @CompanyId  INT,
    @JobGradeId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_JobPositions WHERE CompanyId = @CompanyId AND JobGradeId = @JobGradeId AND IsDeleted = 0)
        THROW 50142, 'Cannot delete a job grade linked to active job positions.', 1;

    UPDATE dbo.tbl_JobGrades
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE JobGradeId = @JobGradeId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Job Positions Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_JobPosition_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jp.JobPositionId, jp.CompanyId, jp.DepartmentId, d.DepartmentName,
        jp.DesignationId, des.DesignationTitle, jp.JobGradeId, jg.GradeCode,
        jp.ReportsToPositionId, rjp.PositionTitle AS ReportsToPositionTitle,
        jp.PositionCode, jp.PositionTitle, jp.HeadcountBudget, jp.Description, jp.IsActive
    FROM dbo.tbl_JobPositions jp
    INNER JOIN dbo.tbl_Departments d ON jp.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON jp.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_JobGrades jg ON jp.JobGradeId = jg.JobGradeId
    LEFT JOIN dbo.tbl_JobPositions rjp ON jp.ReportsToPositionId = rjp.JobPositionId
    WHERE jp.CompanyId = @CompanyId AND jp.IsDeleted = 0
    ORDER BY jp.PositionTitle;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobPosition_Save
    @JobPositionId       INT = 0,
    @CompanyId           INT,
    @DepartmentId        INT,
    @DesignationId       INT,
    @JobGradeId          INT = NULL,
    @ReportsToPositionId INT = NULL,
    @PositionCode        NVARCHAR(25),
    @PositionTitle       NVARCHAR(120),
    @HeadcountBudget     INT,
    @Description         NVARCHAR(400) = NULL,
    @IsActive            BIT,
    @UserId              INT,
    @SavedId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @JobPositionId > 0 AND @ReportsToPositionId = @JobPositionId
        THROW 50151, 'A position cannot report to itself.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_JobPositions WHERE CompanyId = @CompanyId AND PositionCode = @PositionCode AND JobPositionId <> @JobPositionId AND IsDeleted = 0)
        THROW 50152, 'Job position code already exists.', 1;

    IF @JobPositionId = 0
    BEGIN
        INSERT INTO dbo.tbl_JobPositions (CompanyId, DepartmentId, DesignationId, JobGradeId, ReportsToPositionId, PositionCode, PositionTitle, HeadcountBudget, Description, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @DepartmentId, @DesignationId, @JobGradeId, @ReportsToPositionId, @PositionCode, @PositionTitle, @HeadcountBudget, @Description, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_JobPositions
        SET DepartmentId = @DepartmentId, DesignationId = @DesignationId, JobGradeId = @JobGradeId,
            ReportsToPositionId = @ReportsToPositionId, PositionCode = @PositionCode, PositionTitle = @PositionTitle,
            HeadcountBudget = @HeadcountBudget, Description = @Description, IsActive = @IsActive,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE JobPositionId = @JobPositionId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @JobPositionId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobPosition_Delete
    @CompanyId     INT,
    @JobPositionId INT,
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_JobPositions
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE JobPositionId = @JobPositionId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Cost Centers Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_CostCenter_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT cc.CostCenterId, cc.CompanyId, cc.BranchId, b.BranchName, cc.CostCenterCode, cc.CostCenterName, cc.Description, cc.IsActive
    FROM dbo.tbl_CostCenters cc
    LEFT JOIN dbo.tbl_Branches b ON cc.BranchId = b.BranchId
    WHERE cc.CompanyId = @CompanyId AND cc.IsDeleted = 0
    ORDER BY cc.CostCenterCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CostCenter_Save
    @CostCenterId   INT = 0,
    @CompanyId      INT,
    @BranchId       INT = NULL,
    @CostCenterCode NVARCHAR(30),
    @CostCenterName NVARCHAR(120),
    @Description    NVARCHAR(300) = NULL,
    @IsActive       BIT,
    @UserId         INT,
    @SavedId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_CostCenters WHERE CompanyId = @CompanyId AND CostCenterCode = @CostCenterCode AND CostCenterId <> @CostCenterId AND IsDeleted = 0)
        THROW 50161, 'Cost center code already exists.', 1;

    IF @CostCenterId = 0
    BEGIN
        INSERT INTO dbo.tbl_CostCenters (CompanyId, BranchId, CostCenterCode, CostCenterName, Description, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @BranchId, @CostCenterCode, @CostCenterName, @Description, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_CostCenters
        SET BranchId = @BranchId, CostCenterCode = @CostCenterCode, CostCenterName = @CostCenterName,
            Description = @Description, IsActive = @IsActive, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE CostCenterId = @CostCenterId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @CostCenterId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CostCenter_Delete
    @CompanyId    INT,
    @CostCenterId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_CostCenters
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CostCenterId = @CostCenterId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Work Locations Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_WorkLocation_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT wl.WorkLocationId, wl.CompanyId, wl.BranchId, b.BranchName, wl.LocationCode, wl.LocationName,
           wl.AddressLine1, wl.City, wl.Country, wl.TimeZoneId, wl.IsRemote, wl.IsActive
    FROM dbo.tbl_WorkLocations wl
    LEFT JOIN dbo.tbl_Branches b ON wl.BranchId = b.BranchId
    WHERE wl.CompanyId = @CompanyId AND wl.IsDeleted = 0
    ORDER BY wl.LocationName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkLocation_Save
    @WorkLocationId INT = 0,
    @CompanyId      INT,
    @BranchId       INT = NULL,
    @LocationCode   NVARCHAR(20),
    @LocationName   NVARCHAR(120),
    @AddressLine1   NVARCHAR(250) = NULL,
    @City           NVARCHAR(80) = NULL,
    @Country        NVARCHAR(80) = NULL,
    @TimeZoneId     NVARCHAR(60),
    @IsRemote       BIT,
    @IsActive       BIT,
    @UserId         INT,
    @SavedId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_WorkLocations WHERE CompanyId = @CompanyId AND LocationCode = @LocationCode AND WorkLocationId <> @WorkLocationId AND IsDeleted = 0)
        THROW 50171, 'Work location code already exists.', 1;

    IF @WorkLocationId = 0
    BEGIN
        INSERT INTO dbo.tbl_WorkLocations (CompanyId, BranchId, LocationCode, LocationName, AddressLine1, City, Country, TimeZoneId, IsRemote, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @BranchId, @LocationCode, @LocationName, @AddressLine1, @City, @Country, @TimeZoneId, @IsRemote, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_WorkLocations
        SET BranchId = @BranchId, LocationCode = @LocationCode, LocationName = @LocationName,
            AddressLine1 = @AddressLine1, City = @City, Country = @Country, TimeZoneId = @TimeZoneId,
            IsRemote = @IsRemote, IsActive = @IsActive, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE WorkLocationId = @WorkLocationId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @WorkLocationId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_WorkLocation_Delete
    @CompanyId      INT,
    @WorkLocationId INT,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_WorkLocations
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE WorkLocationId = @WorkLocationId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Shifts Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Shift_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ShiftId, CompanyId, ShiftCode, ShiftName,
        CONVERT(VARCHAR(5), StartTime, 108) AS StartTimeText,
        CONVERT(VARCHAR(5), EndTime, 108) AS EndTimeText,
        GracePeriodMinutes, BreakMinutes, WorkingHours, IsNightShift, WeeklyOffDays, IsActive
    FROM dbo.tbl_Shifts
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY StartTime, ShiftName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Shift_Save
    @ShiftId            INT = 0,
    @CompanyId          INT,
    @ShiftCode          NVARCHAR(20),
    @ShiftName          NVARCHAR(100),
    @StartTime          TIME(0),
    @EndTime            TIME(0),
    @GracePeriodMinutes INT,
    @BreakMinutes       INT,
    @WorkingHours       DECIMAL(4,2),
    @IsNightShift       BIT,
    @WeeklyOffDays      NVARCHAR(60),
    @IsActive           BIT,
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Shifts WHERE CompanyId = @CompanyId AND ShiftCode = @ShiftCode AND ShiftId <> @ShiftId AND IsDeleted = 0)
        THROW 50181, 'Shift code already exists.', 1;

    IF @ShiftId = 0
    BEGIN
        INSERT INTO dbo.tbl_Shifts (CompanyId, ShiftCode, ShiftName, StartTime, EndTime, GracePeriodMinutes, BreakMinutes, WorkingHours, IsNightShift, WeeklyOffDays, IsActive, IsDeleted, CreatedBy, CreatedDate)
        VALUES (@CompanyId, @ShiftCode, @ShiftName, @StartTime, @EndTime, @GracePeriodMinutes, @BreakMinutes, @WorkingHours, @IsNightShift, @WeeklyOffDays, @IsActive, 0, @UserId, SYSUTCDATETIME());
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_Shifts
        SET ShiftCode = @ShiftCode, ShiftName = @ShiftName, StartTime = @StartTime, EndTime = @EndTime,
            GracePeriodMinutes = @GracePeriodMinutes, BreakMinutes = @BreakMinutes, WorkingHours = @WorkingHours,
            IsNightShift = @IsNightShift, WeeklyOffDays = @WeeklyOffDays, IsActive = @IsActive,
            ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @ShiftId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Shift_Delete
    @CompanyId INT,
    @ShiftId   INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Shifts
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE ShiftId = @ShiftId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Organization Chart & Reporting Hierarchy Stored Procedure
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetOrgChart
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Department Hierarchy
    SELECT
        d.DepartmentId,
        d.ParentDepartmentId,
        d.DepartmentCode,
        d.DepartmentName,
        b.BranchName,
        (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.DepartmentId = d.DepartmentId AND e.IsDeleted = 0 AND e.IsActive = 1) AS Headcount
    FROM dbo.tbl_Departments d
    LEFT JOIN dbo.tbl_Branches b ON d.BranchId = b.BranchId
    WHERE d.CompanyId = @CompanyId AND d.IsDeleted = 0 AND d.IsActive = 1
    ORDER BY d.DepartmentName;

    -- 2. Employee Reporting Hierarchy
    SELECT
        e.EmployeeId,
        e.ReportingManagerId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        des.DesignationTitle,
        b.BranchName
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
    ORDER BY e.ReportingManagerId, e.FullName;
END
GO
