USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 2 Seed Data: Job Grades, Cost Centers, Work Locations, Positions, Shifts
-- ============================================================================
DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01' AND IsDeleted = 0);
IF @CompanyId IS NOT NULL
BEGIN
    DECLARE @BranchNyc INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'HQ-NYC');
    DECLARE @BranchSfo INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'BR-SFO');

    -- Job Grades
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_JobGrades WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_JobGrades (CompanyId, GradeCode, GradeName, LevelOrder, MinSalary, MaxSalary, Description)
        VALUES
            (@CompanyId, N'G5', N'Professional II',   5,  75000.00, 110000.00, N'Mid-level specialists and account executives'),
            (@CompanyId, N'G6', N'Senior Lead',       6, 105000.00, 145000.00, N'Senior engineers and operations leads'),
            (@CompanyId, N'G7', N'Management',        7, 125000.00, 180000.00, N'Department managers and HR leads');
    END

    -- Cost Centers
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_CostCenters WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_CostCenters (CompanyId, BranchId, CostCenterCode, CostCenterName, Description)
        VALUES
            (@CompanyId, @BranchNyc, N'CC-100', N'R&D Engineering',     N'Core software engineering and cloud operations'),
            (@CompanyId, @BranchNyc, N'CC-200', N'People & Culture',    N'Human resources, talent acquisition, and L&D'),
            (@CompanyId, @BranchNyc, N'CC-300', N'Corporate Finance',   N'Accounting, treasury, and payroll operations'),
            (@CompanyId, @BranchSfo, N'CC-400', N'Commercial Revenue',  N'Enterprise sales and customer success'),
            (@CompanyId, @BranchSfo, N'CC-500', N'Business Operations', N'Facilities, logistics, and shared services');
    END

    -- Work Locations
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_WorkLocations WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_WorkLocations (CompanyId, BranchId, LocationCode, LocationName, AddressLine1, City, Country, TimeZoneId, IsRemote)
        VALUES
            (@CompanyId, @BranchNyc, N'LOC-NYC1', N'Manhattan Tower Floor 14', N'350 Fifth Avenue', N'New York', N'United States', N'America/New_York', 0),
            (@CompanyId, @BranchSfo, N'LOC-SFO1', N'SoMa Innovation Hub',      N'525 Market Street', N'San Francisco', N'United States', N'America/Los_Angeles', 0),
            (@CompanyId, NULL,       N'LOC-REM',  N'US Remote Workforce',      NULL, NULL, N'United States', N'UTC', 1);
    END

    -- Job Positions
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_JobPositions WHERE CompanyId = @CompanyId)
    BEGIN
        DECLARE @DepEng INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'ENG');
        DECLARE @DepHr  INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'HR');
        DECLARE @DesEng INT = (SELECT TOP 1 DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SWE-SR');
        DECLARE @DesHr  INT = (SELECT TOP 1 DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'HR-MGR');
        DECLARE @G6     INT = (SELECT TOP 1 JobGradeId FROM dbo.tbl_JobGrades WHERE CompanyId = @CompanyId AND GradeCode = N'G6');
        DECLARE @G7     INT = (SELECT TOP 1 JobGradeId FROM dbo.tbl_JobGrades WHERE CompanyId = @CompanyId AND GradeCode = N'G7');

        INSERT INTO dbo.tbl_JobPositions (CompanyId, DepartmentId, DesignationId, JobGradeId, PositionCode, PositionTitle, HeadcountBudget, Description)
        VALUES
            (@CompanyId, @DepHr,  @DesHr,  @G7, N'POS-HR-01',  N'Head of Human Resources',    1, N'Leads corporate HR strategy and compliance'),
            (@CompanyId, @DepEng, @DesEng, @G6, N'POS-ENG-01', N'Principal Platform Engineer', 4, N'Leads core HRMS & enterprise systems architecture');
    END

    -- Shifts
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Shifts WHERE CompanyId = @CompanyId)
    BEGIN
        INSERT INTO dbo.tbl_Shifts (CompanyId, ShiftCode, ShiftName, StartTime, EndTime, GracePeriodMinutes, BreakMinutes, WorkingHours, IsNightShift, WeeklyOffDays)
        VALUES
            (@CompanyId, N'DAY-STD', N'Standard Corporate Shift', '09:00:00', '18:00:00', 15, 60, 8.00, 0, N'Saturday,Sunday'),
            (@CompanyId, N'FLX-AM',  N'Early Flex Shift',         '08:00:00', '17:00:00', 15, 60, 8.00, 0, N'Saturday,Sunday'),
            (@CompanyId, N'OPS-NGT', N'Global Support Night',     '22:00:00', '06:00:00', 10, 45, 7.25, 1, N'Saturday,Sunday');
    END

    -- Link Reporting Hierarchy for Sample Employees so Org Chart renders multi-level tree
    DECLARE @HrLead INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');
    DECLARE @EngLead INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');

    IF @HrLead IS NOT NULL AND @EngLead IS NOT NULL
    BEGIN
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @HrLead  WHERE CompanyId = @CompanyId AND EmployeeCode IN (N'EMP-1001', N'EMP-1004', N'EMP-1007') AND ReportingManagerId IS NULL;
        UPDATE dbo.tbl_Employees SET ReportingManagerId = @EngLead WHERE CompanyId = @CompanyId AND EmployeeCode IN (N'EMP-1002', N'EMP-1008') AND ReportingManagerId IS NULL;
        UPDATE dbo.tbl_Employees SET ReportingManagerId = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1007') WHERE CompanyId = @CompanyId AND EmployeeCode IN (N'EMP-1005', N'EMP-1006') AND ReportingManagerId IS NULL;
    END
END
GO
