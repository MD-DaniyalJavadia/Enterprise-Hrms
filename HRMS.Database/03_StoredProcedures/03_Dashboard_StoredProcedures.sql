USE [HRMS_DB];
GO

-- ============================================================================
-- sp_Dashboard_GetSummary
-- Returns 11 result sets consumed via Dapper QueryMultipleAsync
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetSummary
    @CompanyId INT,
    @BranchId  INT = NULL,
    @AsOfDate  DATE = NULL,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    DECLARE @MonthStart DATE = DATEFROMPARTS(YEAR(@AsOfDate), MONTH(@AsOfDate), 1);
    DECLARE @MonthEnd   DATE = EOMONTH(@AsOfDate);

    -- 1. KPI Summary
    SELECT
        COUNT(*) AS TotalEmployees,
        SUM(CASE WHEN e.EmploymentStatus IN (1, 2, 3) AND e.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveEmployees,
        SUM(CASE WHEN e.IsActive = 0 OR e.EmploymentStatus IN (4, 5, 6) THEN 1 ELSE 0 END) AS InactiveEmployees,
        SUM(CASE WHEN e.HireDate BETWEEN @MonthStart AND @MonthEnd THEN 1 ELSE 0 END) AS NewHiresThisMonth,
        SUM(CASE WHEN e.ExitDate BETWEEN @MonthStart AND @MonthEnd AND e.EmploymentStatus IN (4, 5) THEN 1 ELSE 0 END) AS ResignedEmployeesThisMonth,
        (
            SELECT COUNT(DISTINCT la.EmployeeId)
            FROM dbo.tbl_LeaveApplications la
            INNER JOIN dbo.tbl_Employees le ON la.EmployeeId = le.EmployeeId
            WHERE la.CompanyId = @CompanyId
              AND la.IsDeleted = 0
              AND la.ApprovalStatus = N'Approved'
              AND @AsOfDate BETWEEN la.StartDate AND la.EndDate
              AND (@BranchId IS NULL OR le.BranchId = @BranchId)
        ) AS EmployeesOnLeaveToday,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_Attendance a
            WHERE a.CompanyId = @CompanyId
              AND a.IsDeleted = 0
              AND a.AttendanceDate = @AsOfDate
              AND a.Status IN (N'Present', N'Late')
              AND (@BranchId IS NULL OR a.BranchId = @BranchId)
        ) AS PresentToday,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_Attendance a
            WHERE a.CompanyId = @CompanyId
              AND a.IsDeleted = 0
              AND a.AttendanceDate = @AsOfDate
              AND a.Status = N'Late'
              AND (@BranchId IS NULL OR a.BranchId = @BranchId)
        ) AS LateArrivalsToday,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_Attendance a
            WHERE a.CompanyId = @CompanyId
              AND a.IsDeleted = 0
              AND a.AttendanceDate = @AsOfDate
              AND a.Status = N'Absent'
              AND (@BranchId IS NULL OR a.BranchId = @BranchId)
        ) AS AbsentToday,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_LeaveApplications la
            INNER JOIN dbo.tbl_Employees le ON la.EmployeeId = le.EmployeeId
            WHERE la.CompanyId = @CompanyId
              AND la.IsDeleted = 0
              AND la.ApprovalStatus = N'Pending'
              AND (@BranchId IS NULL OR le.BranchId = @BranchId)
        ) AS PendingLeaveRequests
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND (@BranchId IS NULL OR e.BranchId = @BranchId);

    -- 2. Latest Payroll Summary
    SELECT TOP 1
        PeriodLabel,
        TotalGrossPay,
        TotalDeductions,
        TotalNetPay,
        ProcessedEmployees,
        PayrollStatus
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId
      AND IsDeleted = 0
    ORDER BY PayrollYear DESC, PayrollMonth DESC;

    -- 3. Active Branches for Filter Dropdown
    SELECT
        BranchId,
        BranchName
    FROM dbo.tbl_Branches
    WHERE CompanyId = @CompanyId
      AND IsActive = 1
      AND IsDeleted = 0
    ORDER BY IsHeadOffice DESC, BranchName;

    -- 4. Department-Wise Employee Statistics
    SELECT
        d.DepartmentName,
        COUNT(e.EmployeeId) AS EmployeeCount,
        SUM(CASE WHEN e.IsActive = 1 AND e.EmploymentStatus IN (1, 2, 3) THEN 1 ELSE 0 END) AS ActiveCount
    FROM dbo.tbl_Departments d
    LEFT JOIN dbo.tbl_Employees e
        ON d.DepartmentId = e.DepartmentId
        AND e.IsDeleted = 0
        AND (@BranchId IS NULL OR e.BranchId = @BranchId)
    WHERE d.CompanyId = @CompanyId
      AND d.IsActive = 1
      AND d.IsDeleted = 0
    GROUP BY d.DepartmentName
    ORDER BY EmployeeCount DESC, d.DepartmentName;

    -- 5. Gender Distribution
    SELECT
        e.Gender,
        COUNT(*) AS [Count]
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
    GROUP BY e.Gender;

    -- 6. Monthly Hiring Statistics (Last 6 Months)
    ;WITH Months AS (
        SELECT 0 AS Offset
        UNION ALL SELECT 1
        UNION ALL SELECT 2
        UNION ALL SELECT 3
        UNION ALL SELECT 4
        UNION ALL SELECT 5
    ),
    MonthWindows AS (
        SELECT
            DATEADD(MONTH, -Offset, @MonthStart) AS MStart,
            EOMONTH(DATEADD(MONTH, -Offset, @MonthStart)) AS MEnd
        FROM Months
    )
    SELECT
        FORMAT(mw.MStart, 'MMM yyyy') AS MonthLabel,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId
              AND e.IsDeleted = 0
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND e.HireDate BETWEEN mw.MStart AND mw.MEnd
        ) AS HiredCount,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId
              AND e.IsDeleted = 0
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND e.ExitDate BETWEEN mw.MStart AND mw.MEnd
        ) AS ResignedCount
    FROM MonthWindows mw
    ORDER BY mw.MStart ASC;

    -- 7. Attendance Analytics (Last 7 Days)
    ;WITH Days AS (
        SELECT 0 AS Offset
        UNION ALL SELECT 1
        UNION ALL SELECT 2
        UNION ALL SELECT 3
        UNION ALL SELECT 4
        UNION ALL SELECT 5
        UNION ALL SELECT 6
    )
    SELECT
        FORMAT(DATEADD(DAY, -d.Offset, @AsOfDate), 'MMM dd') AS DateLabel,
        ISNULL(SUM(CASE WHEN a.Status = N'Present' THEN 1 ELSE 0 END), 0) AS PresentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Late' THEN 1 ELSE 0 END), 0) AS LateCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Absent' THEN 1 ELSE 0 END), 0) AS AbsentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'OnLeave' THEN 1 ELSE 0 END), 0) AS OnLeaveCount
    FROM Days d
    LEFT JOIN dbo.tbl_Attendance a
        ON a.AttendanceDate = DATEADD(DAY, -d.Offset, @AsOfDate)
        AND a.CompanyId = @CompanyId
        AND a.IsDeleted = 0
        AND (@BranchId IS NULL OR a.BranchId = @BranchId)
    GROUP BY d.Offset
    ORDER BY DATEADD(DAY, -d.Offset, @AsOfDate) ASC;

    -- 8. Upcoming Birthdays (Next 30 Days)
    SELECT TOP 8
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        dep.DepartmentName,
        DATEFROMPARTS(
            CASE
                WHEN DATEFROMPARTS(YEAR(@AsOfDate), MONTH(e.DateOfBirth), CASE WHEN MONTH(e.DateOfBirth)=2 AND DAY(e.DateOfBirth)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.DateOfBirth) END) < @AsOfDate
                THEN YEAR(@AsOfDate) + 1
                ELSE YEAR(@AsOfDate)
            END,
            MONTH(e.DateOfBirth),
            CASE WHEN MONTH(e.DateOfBirth)=2 AND DAY(e.DateOfBirth)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.DateOfBirth) END
        ) AS EventDate,
        NULL AS YearsOfService
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments dep ON e.DepartmentId = dep.DepartmentId
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND DATEDIFF(DAY, @AsOfDate,
            DATEFROMPARTS(
                CASE
                    WHEN DATEFROMPARTS(YEAR(@AsOfDate), MONTH(e.DateOfBirth), CASE WHEN MONTH(e.DateOfBirth)=2 AND DAY(e.DateOfBirth)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.DateOfBirth) END) < @AsOfDate
                    THEN YEAR(@AsOfDate) + 1
                    ELSE YEAR(@AsOfDate)
                END,
                MONTH(e.DateOfBirth),
                CASE WHEN MONTH(e.DateOfBirth)=2 AND DAY(e.DateOfBirth)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.DateOfBirth) END
            )
          ) BETWEEN 0 AND 30
    ORDER BY EventDate ASC;

    -- 9. Upcoming Work Anniversaries (Next 30 Days)
    SELECT TOP 8
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        dep.DepartmentName,
        DATEFROMPARTS(
            CASE
                WHEN DATEFROMPARTS(YEAR(@AsOfDate), MONTH(e.HireDate), CASE WHEN MONTH(e.HireDate)=2 AND DAY(e.HireDate)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.HireDate) END) < @AsOfDate
                THEN YEAR(@AsOfDate) + 1
                ELSE YEAR(@AsOfDate)
            END,
            MONTH(e.HireDate),
            CASE WHEN MONTH(e.HireDate)=2 AND DAY(e.HireDate)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.HireDate) END
        ) AS EventDate,
        DATEDIFF(YEAR, e.HireDate, @AsOfDate) +
        CASE WHEN DATEFROMPARTS(YEAR(@AsOfDate), MONTH(e.HireDate), CASE WHEN MONTH(e.HireDate)=2 AND DAY(e.HireDate)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.HireDate) END) < @AsOfDate THEN 1 ELSE 0 END AS YearsOfService
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments dep ON e.DepartmentId = dep.DepartmentId
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND DATEDIFF(YEAR, e.HireDate, @AsOfDate) >= 1
      AND DATEDIFF(DAY, @AsOfDate,
            DATEFROMPARTS(
                CASE
                    WHEN DATEFROMPARTS(YEAR(@AsOfDate), MONTH(e.HireDate), CASE WHEN MONTH(e.HireDate)=2 AND DAY(e.HireDate)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.HireDate) END) < @AsOfDate
                    THEN YEAR(@AsOfDate) + 1
                    ELSE YEAR(@AsOfDate)
                END,
                MONTH(e.HireDate),
                CASE WHEN MONTH(e.HireDate)=2 AND DAY(e.HireDate)=29 AND (YEAR(@AsOfDate)%4<>0) THEN 28 ELSE DAY(e.HireDate) END
            )
          ) BETWEEN 0 AND 30
    ORDER BY EventDate ASC;

    -- 10. Recent Activities
    SELECT TOP 10
        AuditLogId,
        ISNULL(Username, N'System') AS Username,
        ActionType,
        ModuleName,
        ISNULL(Description, N'') AS Description,
        CreatedDate
    FROM dbo.tbl_AuditLogs
    WHERE CompanyId = @CompanyId
    ORDER BY CreatedDate DESC, AuditLogId DESC;

    -- 11. User Notifications
    SELECT TOP 10
        NotificationId,
        Title,
        Message,
        NotificationType,
        ActionUrl,
        IsRead,
        CreatedDate
    FROM dbo.tbl_Notifications
    WHERE CompanyId = @CompanyId
      AND UserId = @UserId
    ORDER BY IsRead ASC, CreatedDate DESC;
END
GO

-- ============================================================================
-- sp_Dashboard_GlobalSearch
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GlobalSearch
    @CompanyId  INT,
    @SearchTerm NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LikeTerm NVARCHAR(104) = N'%' + @SearchTerm + N'%';

    SELECT TOP 8
        N'Employee' AS Category,
        e.FullName AS Title,
        CONCAT(e.EmployeeCode, N' • ', d.DepartmentName) AS Subtitle,
        CONCAT(N'/Employee/Details/', e.EmployeeId) AS Url,
        N'bi-person-badge' AS IconClass
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND (
          e.FullName LIKE @LikeTerm
          OR e.EmployeeCode LIKE @LikeTerm
          OR e.WorkEmail LIKE @LikeTerm
      )

    UNION ALL

    SELECT TOP 4
        N'Department' AS Category,
        d.DepartmentName AS Title,
        CONCAT(N'Code: ', d.DepartmentCode) AS Subtitle,
        N'/Department/Index' AS Url,
        N'bi-diagram-3' AS IconClass
    FROM dbo.tbl_Departments d
    WHERE d.CompanyId = @CompanyId
      AND d.IsDeleted = 0
      AND (d.DepartmentName LIKE @LikeTerm OR d.DepartmentCode LIKE @LikeTerm);
END
GO
