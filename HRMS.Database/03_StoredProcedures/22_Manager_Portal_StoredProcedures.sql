USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 10: Manager Portal Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_Manager_GetDashboard
    @CompanyId         INT,
    @ManagerEmployeeId INT = NULL,
    @IsCompanyWide     BIT = 0,
    @AsOfDate          DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    DECLARE @Team TABLE (
        EmployeeId     INT NOT NULL PRIMARY KEY,
        ReportingLevel INT NOT NULL
    );

    IF @IsCompanyWide = 1
    BEGIN
        INSERT INTO @Team (EmployeeId, ReportingLevel)
        SELECT e.EmployeeId, 1
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1;
    END
    ELSE IF @ManagerEmployeeId IS NOT NULL
    BEGIN
        ;WITH TeamCte AS (
            SELECT e.EmployeeId, 1 AS Lvl
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId AND e.ReportingManagerId = @ManagerEmployeeId
              AND e.IsDeleted = 0 AND e.IsActive = 1 AND e.EmployeeId <> @ManagerEmployeeId
            UNION ALL
            SELECT c.EmployeeId, t.Lvl + 1
            FROM dbo.tbl_Employees c
            INNER JOIN TeamCte t ON c.ReportingManagerId = t.EmployeeId
            WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0 AND c.IsActive = 1 AND t.Lvl < 10
        )
        INSERT INTO @Team (EmployeeId, ReportingLevel)
        SELECT EmployeeId, MIN(Lvl)
        FROM TeamCte
        GROUP BY EmployeeId;
    END

    -- 1. KPI Summary
    SELECT
        (SELECT COUNT(1) FROM @Team) AS TeamHeadcount,
        (SELECT COUNT(1) FROM @Team t INNER JOIN dbo.tbl_Attendance a ON a.EmployeeId = t.EmployeeId
         WHERE a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0 AND a.Status IN (N'Present', N'Late')) AS PresentTodayCount,
        (SELECT COUNT(1) FROM @Team t INNER JOIN dbo.tbl_Attendance a ON a.EmployeeId = t.EmployeeId
         WHERE a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0 AND a.Status = N'Late') AS LateTodayCount,
        (SELECT COUNT(1) FROM @Team t INNER JOIN dbo.tbl_Attendance a ON a.EmployeeId = t.EmployeeId
         WHERE a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0 AND a.Status = N'OnLeave') AS OnLeaveTodayCount,
        (SELECT COUNT(1) FROM @Team t INNER JOIN dbo.tbl_Attendance a ON a.EmployeeId = t.EmployeeId
         WHERE a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0 AND a.Status = N'Absent') AS AbsentTodayCount,
        (SELECT COUNT(1) FROM dbo.tbl_LeaveApplications la INNER JOIN @Team t ON la.EmployeeId = t.EmployeeId
         WHERE la.CompanyId = @CompanyId AND la.IsDeleted = 0 AND la.ApprovalStatus = N'Pending') AS PendingLeaveCount,
        (SELECT COUNT(1) FROM dbo.tbl_AttendanceRegularizations ar INNER JOIN @Team t ON ar.EmployeeId = t.EmployeeId
         WHERE ar.CompanyId = @CompanyId AND ar.IsDeleted = 0 AND ar.ApprovalStatus = N'Pending') AS PendingRegularizationCount,
        (SELECT COUNT(1) FROM dbo.tbl_OvertimeRequests ot INNER JOIN @Team t ON ot.EmployeeId = t.EmployeeId
         WHERE ot.CompanyId = @CompanyId AND ot.IsDeleted = 0 AND ot.ApprovalStatus = N'Pending') AS PendingOvertimeCount,
        (SELECT COUNT(1) FROM dbo.tbl_EmployeeLoans el INNER JOIN @Team t ON el.EmployeeId = t.EmployeeId
         WHERE el.CompanyId = @CompanyId AND el.IsDeleted = 0 AND el.LoanStatus = N'Pending') AS PendingLoanCount,
        (SELECT COUNT(1) FROM dbo.tbl_ExpenseClaims ec INNER JOIN @Team t ON ec.EmployeeId = t.EmployeeId
         WHERE ec.CompanyId = @CompanyId AND ec.IsDeleted = 0 AND ec.ClaimStatus = N'Submitted') AS PendingExpenseCount,
        (SELECT COUNT(1) FROM dbo.tbl_ProfileUpdateRequests pr INNER JOIN @Team t ON pr.EmployeeId = t.EmployeeId
         WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0 AND pr.ApprovalStatus = N'Pending') AS PendingProfileRequestCount,
        (SELECT COUNT(1) FROM dbo.tbl_PerformanceAppraisals pa INNER JOIN @Team t ON pa.EmployeeId = t.EmployeeId
         WHERE pa.CompanyId = @CompanyId AND pa.IsDeleted = 0 AND pa.AppraisalStatus IN (N'NotStarted', N'SelfSubmitted')) AS PendingAppraisalReviewCount;

    -- 2. Team Today Attendance Snapshot
    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        desig.DesignationTitle,
        b.BranchName,
        e.WorkEmail,
        e.Phone,
        e.HireDate,
        e.EmploymentStatus,
        t.ReportingLevel,
        mgr.FullName AS ManagerName,
        ISNULL(s.ShiftName, N'Standard Shift') AS ShiftName,
        ISNULL(a.Status, N'NotMarked') AS TodayStatus,
        a.CheckInTime,
        a.CheckOutTime,
        ISNULL(a.LateMinutes, 0) AS LateMinutes,
        ISNULL(a.WorkMinutes, 0) AS WorkMinutes,
        e.PhotoPath,
        0 AS OpenGoalsCount,
        0 AS PendingLeaveCount
    FROM @Team t
    INNER JOIN dbo.tbl_Employees e ON t.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    LEFT JOIN dbo.tbl_Attendance a ON a.EmployeeId = e.EmployeeId AND a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(a.ShiftId, e.ShiftId) = s.ShiftId
    ORDER BY t.ReportingLevel ASC, e.FullName ASC;

    -- 3. Unified Pending Approvals Queue (Top 25)
    SELECT TOP 25 * FROM (
        SELECT
            N'Leave' AS Category,
            la.LeaveApplicationId AS RequestId,
            la.EmployeeId,
            e.EmployeeCode,
            e.FullName AS EmployeeName,
            d.DepartmentName,
            lt.LeaveName AS Title,
            CONCAT(FORMAT(la.StartDate, 'dd MMM'), N' - ', FORMAT(la.EndDate, 'dd MMM yyyy'), N' (', la.TotalDays, N'd)') AS Summary,
            la.Reason AS Details,
            CAST(la.TotalDays AS DECIMAL(18,2)) AS NumericValue,
            CASE WHEN la.AttachmentStoredName IS NOT NULL AND la.AttachmentStoredName <> N'' THEN 1 ELSE 0 END AS HasAttachment,
            la.CreatedDate AS SubmittedDate
        FROM dbo.tbl_LeaveApplications la
        INNER JOIN @Team t ON la.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
        WHERE la.CompanyId = @CompanyId AND la.IsDeleted = 0 AND la.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Regularization' AS Category,
            ar.RegularizationId AS RequestId,
            ar.EmployeeId,
            e.EmployeeCode,
            e.FullName,
            d.DepartmentName,
            CONCAT(N'Attendance Correction (', ar.RequestedStatus, N')') AS Title,
            FORMAT(ar.AttendanceDate, 'dd MMM yyyy') AS Summary,
            ar.Reason AS Details,
            CAST(0 AS DECIMAL(18,2)) AS NumericValue,
            0 AS HasAttachment,
            ar.CreatedDate
        FROM dbo.tbl_AttendanceRegularizations ar
        INNER JOIN @Team t ON ar.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ar.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE ar.CompanyId = @CompanyId AND ar.IsDeleted = 0 AND ar.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Overtime' AS Category,
            ot.OvertimeRequestId AS RequestId,
            ot.EmployeeId,
            e.EmployeeCode,
            e.FullName,
            d.DepartmentName,
            N'Overtime Claim' AS Title,
            CONCAT(FORMAT(ot.OvertimeDate, 'dd MMM yyyy'), N' • ', ot.RequestedHours, N' hrs') AS Summary,
            ot.Reason AS Details,
            ot.RequestedHours AS NumericValue,
            0 AS HasAttachment,
            ot.CreatedDate
        FROM dbo.tbl_OvertimeRequests ot
        INNER JOIN @Team t ON ot.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ot.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE ot.CompanyId = @CompanyId AND ot.IsDeleted = 0 AND ot.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Loan' AS Category,
            el.LoanId AS RequestId,
            el.EmployeeId,
            e.EmployeeCode,
            e.FullName,
            d.DepartmentName,
            CONCAT(lt.TypeName, N' (', el.LoanNumber, N')') AS Title,
            CONCAT(FORMAT(el.PrincipalAmount, 'N2'), N' over ', el.TenureMonths, N' mo') AS Summary,
            ISNULL(el.Purpose, N'') AS Details,
            el.PrincipalAmount AS NumericValue,
            0 AS HasAttachment,
            el.CreatedDate
        FROM dbo.tbl_EmployeeLoans el
        INNER JOIN @Team t ON el.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_LoanTypes lt ON el.LoanTypeId = lt.LoanTypeId
        WHERE el.CompanyId = @CompanyId AND el.IsDeleted = 0 AND el.LoanStatus = N'Pending'

        UNION ALL

        SELECT
            N'Expense' AS Category,
            ec.ClaimId AS RequestId,
            ec.EmployeeId,
            e.EmployeeCode,
            e.FullName,
            d.DepartmentName,
            CONCAT(cat.CategoryName, N' (', ec.ClaimNumber, N')') AS Title,
            CONCAT(ec.MerchantOrVendor, N' • ', FORMAT(ec.ClaimAmount, 'N2'), N' ', ec.CurrencyCode) AS Summary,
            ISNULL(ec.Description, N'') AS Details,
            ec.ClaimAmount AS NumericValue,
            CASE WHEN ec.ReceiptStoredName IS NOT NULL AND ec.ReceiptStoredName <> N'' THEN 1 ELSE 0 END AS HasAttachment,
            ec.CreatedDate
        FROM dbo.tbl_ExpenseClaims ec
        INNER JOIN @Team t ON ec.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ec.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_ExpenseCategories cat ON ec.CategoryId = cat.CategoryId
        WHERE ec.CompanyId = @CompanyId AND ec.IsDeleted = 0 AND ec.ClaimStatus = N'Submitted'

        UNION ALL

        SELECT
            N'ProfileUpdate' AS Category,
            pr.RequestId AS RequestId,
            pr.EmployeeId,
            e.EmployeeCode,
            e.FullName,
            d.DepartmentName,
            CONCAT(N'Profile Change: ', pr.RequestType) AS Title,
            pr.FieldSummary AS Summary,
            ISNULL(pr.EmployeeNotes, N'') AS Details,
            CAST(0 AS DECIMAL(18,2)) AS NumericValue,
            0 AS HasAttachment,
            pr.CreatedDate
        FROM dbo.tbl_ProfileUpdateRequests pr
        INNER JOIN @Team t ON pr.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON pr.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0 AND pr.ApprovalStatus = N'Pending'
    ) unified
    ORDER BY unified.SubmittedDate DESC;

    -- 4. Team Performance Appraisals Pending Manager Action
    SELECT TOP 15
        pa.AppraisalId,
        pa.CompanyId,
        pa.CycleId,
        ac.CycleCode,
        ac.CycleName,
        pa.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        desig.DesignationTitle,
        pa.ReviewerEmployeeId,
        rev.FullName AS ReviewerName,
        pa.SelfRating,
        pa.SelfComments,
        pa.SelfSubmittedDate,
        pa.ManagerRating,
        pa.FinalRating,
        pa.RatingBand,
        pa.Strengths,
        pa.ImprovementAreas,
        pa.ManagerComments,
        pa.RecommendedIncrementPercent,
        pa.AppraisalStatus,
        pa.CompletedDate
    FROM dbo.tbl_PerformanceAppraisals pa
    INNER JOIN @Team t ON pa.EmployeeId = t.EmployeeId
    INNER JOIN dbo.tbl_AppraisalCycles ac ON pa.CycleId = ac.CycleId
    INNER JOIN dbo.tbl_Employees e ON pa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees rev ON pa.ReviewerEmployeeId = rev.EmployeeId
    WHERE pa.CompanyId = @CompanyId AND pa.IsDeleted = 0
      AND pa.AppraisalStatus IN (N'NotStarted', N'SelfSubmitted')
    ORDER BY CASE WHEN pa.AppraisalStatus = N'SelfSubmitted' THEN 0 ELSE 1 END, e.FullName;

    -- 5. Team Department Breakdown (Chart.js)
    SELECT
        d.DepartmentName,
        COUNT(t.EmployeeId) AS Headcount,
        SUM(CASE WHEN a.Status IN (N'Present', N'Late') THEN 1 ELSE 0 END) AS PresentCount,
        SUM(CASE WHEN a.Status = N'OnLeave' THEN 1 ELSE 0 END) AS OnLeaveCount
    FROM @Team t
    INNER JOIN dbo.tbl_Employees e ON t.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_Attendance a ON a.EmployeeId = e.EmployeeId AND a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0
    GROUP BY d.DepartmentName
    ORDER BY Headcount DESC;

    -- 6. Upcoming Team Approved Leaves (Next 14 Days)
    SELECT TOP 12
        la.LeaveApplicationId,
        la.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        lt.LeaveName,
        ISNULL(lt.ColorHex, N'#2563eb') AS ColorHex,
        la.StartDate,
        la.EndDate,
        la.TotalDays,
        la.ApprovalStatus
    FROM dbo.tbl_LeaveApplications la
    INNER JOIN @Team t ON la.EmployeeId = t.EmployeeId
    INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
    WHERE la.CompanyId = @CompanyId AND la.IsDeleted = 0
      AND la.ApprovalStatus = N'Approved'
      AND la.EndDate >= @AsOfDate
      AND la.StartDate <= DATEADD(DAY, 14, @AsOfDate)
    ORDER BY la.StartDate ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Manager_GetTeamRoster
    @CompanyId         INT,
    @ManagerEmployeeId INT = NULL,
    @IsCompanyWide     BIT = 0,
    @DepartmentId      INT = NULL,
    @SearchTerm        NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @Team TABLE (
        EmployeeId     INT NOT NULL PRIMARY KEY,
        ReportingLevel INT NOT NULL
    );

    IF @IsCompanyWide = 1
    BEGIN
        INSERT INTO @Team (EmployeeId, ReportingLevel)
        SELECT e.EmployeeId, 1
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1;
    END
    ELSE IF @ManagerEmployeeId IS NOT NULL
    BEGIN
        ;WITH TeamCte AS (
            SELECT e.EmployeeId, 1 AS Lvl
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId AND e.ReportingManagerId = @ManagerEmployeeId
              AND e.IsDeleted = 0 AND e.IsActive = 1 AND e.EmployeeId <> @ManagerEmployeeId
            UNION ALL
            SELECT c.EmployeeId, t.Lvl + 1
            FROM dbo.tbl_Employees c
            INNER JOIN TeamCte t ON c.ReportingManagerId = t.EmployeeId
            WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0 AND c.IsActive = 1 AND t.Lvl < 10
        )
        INSERT INTO @Team (EmployeeId, ReportingLevel)
        SELECT EmployeeId, MIN(Lvl)
        FROM TeamCte
        GROUP BY EmployeeId;
    END

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        desig.DesignationTitle,
        b.BranchName,
        e.WorkEmail,
        e.Phone,
        e.HireDate,
        e.EmploymentStatus,
        t.ReportingLevel,
        mgr.FullName AS ManagerName,
        ISNULL(s.ShiftName, N'Standard Shift') AS ShiftName,
        ISNULL(a.Status, N'NotMarked') AS TodayStatus,
        a.CheckInTime,
        a.CheckOutTime,
        ISNULL(a.LateMinutes, 0) AS LateMinutes,
        ISNULL(a.WorkMinutes, 0) AS WorkMinutes,
        e.PhotoPath,
        (SELECT COUNT(1) FROM dbo.tbl_PerformanceGoals pg WHERE pg.EmployeeId = e.EmployeeId AND pg.CompanyId = @CompanyId AND pg.IsDeleted = 0 AND pg.GoalStatus <> N'Completed') AS OpenGoalsCount,
        (SELECT COUNT(1) FROM dbo.tbl_LeaveApplications la WHERE la.EmployeeId = e.EmployeeId AND la.CompanyId = @CompanyId AND la.IsDeleted = 0 AND la.ApprovalStatus = N'Pending') AS PendingLeaveCount
    FROM @Team t
    INNER JOIN dbo.tbl_Employees e ON t.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    LEFT JOIN dbo.tbl_Attendance a ON a.EmployeeId = e.EmployeeId AND a.CompanyId = @CompanyId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(a.ShiftId, e.ShiftId) = s.ShiftId
    WHERE (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@SearchTerm IS NULL OR e.FullName LIKE N'%' + @SearchTerm + N'%' OR e.EmployeeCode LIKE N'%' + @SearchTerm + N'%' OR desig.DesignationTitle LIKE N'%' + @SearchTerm + N'%')
    ORDER BY t.ReportingLevel ASC, e.FullName ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Manager_GetPendingApprovals
    @CompanyId         INT,
    @ManagerEmployeeId INT = NULL,
    @IsCompanyWide     BIT = 0,
    @Category          NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Team TABLE (EmployeeId INT NOT NULL PRIMARY KEY);

    IF @IsCompanyWide = 1
    BEGIN
        INSERT INTO @Team (EmployeeId)
        SELECT EmployeeId FROM dbo.tbl_Employees
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1;
    END
    ELSE IF @ManagerEmployeeId IS NOT NULL
    BEGIN
        ;WITH TeamCte AS (
            SELECT e.EmployeeId, 1 AS Lvl
            FROM dbo.tbl_Employees e
            WHERE e.CompanyId = @CompanyId AND e.ReportingManagerId = @ManagerEmployeeId
              AND e.IsDeleted = 0 AND e.IsActive = 1 AND e.EmployeeId <> @ManagerEmployeeId
            UNION ALL
            SELECT c.EmployeeId, t.Lvl + 1
            FROM dbo.tbl_Employees c
            INNER JOIN TeamCte t ON c.ReportingManagerId = t.EmployeeId
            WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0 AND c.IsActive = 1 AND t.Lvl < 10
        )
        INSERT INTO @Team (EmployeeId)
        SELECT DISTINCT EmployeeId FROM TeamCte;
    END

    SELECT * FROM (
        SELECT
            N'Leave' AS Category,
            la.LeaveApplicationId AS RequestId,
            la.EmployeeId,
            e.EmployeeCode,
            e.FullName AS EmployeeName,
            d.DepartmentName,
            lt.LeaveName AS Title,
            CONCAT(FORMAT(la.StartDate, 'dd MMM'), N' - ', FORMAT(la.EndDate, 'dd MMM yyyy'), N' (', la.TotalDays, N'd)') AS Summary,
            la.Reason AS Details,
            CAST(la.TotalDays AS DECIMAL(18,2)) AS NumericValue,
            CASE WHEN la.AttachmentStoredName IS NOT NULL AND la.AttachmentStoredName <> N'' THEN 1 ELSE 0 END AS HasAttachment,
            la.CreatedDate AS SubmittedDate
        FROM dbo.tbl_LeaveApplications la
        INNER JOIN @Team t ON la.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_LeaveTypes lt ON la.LeaveTypeId = lt.LeaveTypeId
        WHERE la.CompanyId = @CompanyId AND la.IsDeleted = 0 AND la.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Regularization', ar.RegularizationId, ar.EmployeeId, e.EmployeeCode, e.FullName, d.DepartmentName,
            CONCAT(N'Attendance Correction (', ar.RequestedStatus, N')'),
            FORMAT(ar.AttendanceDate, 'dd MMM yyyy'),
            ar.Reason, CAST(0 AS DECIMAL(18,2)), 0, ar.CreatedDate
        FROM dbo.tbl_AttendanceRegularizations ar
        INNER JOIN @Team t ON ar.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ar.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE ar.CompanyId = @CompanyId AND ar.IsDeleted = 0 AND ar.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Overtime', ot.OvertimeRequestId, ot.EmployeeId, e.EmployeeCode, e.FullName, d.DepartmentName,
            N'Overtime Claim',
            CONCAT(FORMAT(ot.OvertimeDate, 'dd MMM yyyy'), N' • ', ot.RequestedHours, N' hrs'),
            ot.Reason, ot.RequestedHours, 0, ot.CreatedDate
        FROM dbo.tbl_OvertimeRequests ot
        INNER JOIN @Team t ON ot.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ot.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE ot.CompanyId = @CompanyId AND ot.IsDeleted = 0 AND ot.ApprovalStatus = N'Pending'

        UNION ALL

        SELECT
            N'Loan', el.LoanId, el.EmployeeId, e.EmployeeCode, e.FullName, d.DepartmentName,
            CONCAT(lt.TypeName, N' (', el.LoanNumber, N')'),
            CONCAT(FORMAT(el.PrincipalAmount, 'N2'), N' over ', el.TenureMonths, N' mo'),
            ISNULL(el.Purpose, N''), el.PrincipalAmount, 0, el.CreatedDate
        FROM dbo.tbl_EmployeeLoans el
        INNER JOIN @Team t ON el.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_LoanTypes lt ON el.LoanTypeId = lt.LoanTypeId
        WHERE el.CompanyId = @CompanyId AND el.IsDeleted = 0 AND el.LoanStatus = N'Pending'

        UNION ALL

        SELECT
            N'Expense', ec.ClaimId, ec.EmployeeId, e.EmployeeCode, e.FullName, d.DepartmentName,
            CONCAT(cat.CategoryName, N' (', ec.ClaimNumber, N')'),
            CONCAT(ec.MerchantOrVendor, N' • ', FORMAT(ec.ClaimAmount, 'N2'), N' ', ec.CurrencyCode),
            ISNULL(ec.Description, N''), ec.ClaimAmount,
            CASE WHEN ec.ReceiptStoredName IS NOT NULL AND ec.ReceiptStoredName <> N'' THEN 1 ELSE 0 END,
            ec.CreatedDate
        FROM dbo.tbl_ExpenseClaims ec
        INNER JOIN @Team t ON ec.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON ec.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        INNER JOIN dbo.tbl_ExpenseCategories cat ON ec.CategoryId = cat.CategoryId
        WHERE ec.CompanyId = @CompanyId AND ec.IsDeleted = 0 AND ec.ClaimStatus = N'Submitted'

        UNION ALL

        SELECT
            N'ProfileUpdate', pr.RequestId, pr.EmployeeId, e.EmployeeCode, e.FullName, d.DepartmentName,
            CONCAT(N'Profile Change: ', pr.RequestType),
            pr.FieldSummary,
            ISNULL(pr.EmployeeNotes, N''), CAST(0 AS DECIMAL(18,2)), 0, pr.CreatedDate
        FROM dbo.tbl_ProfileUpdateRequests pr
        INNER JOIN @Team t ON pr.EmployeeId = t.EmployeeId
        INNER JOIN dbo.tbl_Employees e ON pr.EmployeeId = e.EmployeeId
        INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
        WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0 AND pr.ApprovalStatus = N'Pending'
    ) allPending
    WHERE (@Category IS NULL OR allPending.Category = @Category)
    ORDER BY allPending.SubmittedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Manager_GetRequestOwner
    @CompanyId INT,
    @Category  NVARCHAR(30),
    @RequestId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Category = N'Leave'
        SELECT EmployeeId FROM dbo.tbl_LeaveApplications WHERE CompanyId = @CompanyId AND LeaveApplicationId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'Regularization'
        SELECT EmployeeId FROM dbo.tbl_AttendanceRegularizations WHERE CompanyId = @CompanyId AND RegularizationId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'Overtime'
        SELECT EmployeeId FROM dbo.tbl_OvertimeRequests WHERE CompanyId = @CompanyId AND OvertimeRequestId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'Loan'
        SELECT EmployeeId FROM dbo.tbl_EmployeeLoans WHERE CompanyId = @CompanyId AND LoanId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'Expense'
        SELECT EmployeeId FROM dbo.tbl_ExpenseClaims WHERE CompanyId = @CompanyId AND ClaimId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'ProfileUpdate'
        SELECT EmployeeId FROM dbo.tbl_ProfileUpdateRequests WHERE CompanyId = @CompanyId AND RequestId = @RequestId AND IsDeleted = 0;
    ELSE IF @Category = N'Appraisal'
        SELECT EmployeeId FROM dbo.tbl_PerformanceAppraisals WHERE CompanyId = @CompanyId AND AppraisalId = @RequestId AND IsDeleted = 0;
    ELSE
        SELECT CAST(NULL AS INT) AS EmployeeId WHERE 1 = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Manager_VerifyTeamMember
    @CompanyId         INT,
    @ManagerEmployeeId INT,
    @TargetEmployeeId  INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ManagerEmployeeId IS NULL OR @TargetEmployeeId IS NULL OR @ManagerEmployeeId = @TargetEmployeeId
    BEGIN
        SELECT CAST(0 AS BIT) AS IsTeamMember;
        RETURN;
    END

    ;WITH TeamCte AS (
        SELECT e.EmployeeId, 1 AS Lvl
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId AND e.ReportingManagerId = @ManagerEmployeeId
          AND e.IsDeleted = 0 AND e.IsActive = 1 AND e.EmployeeId <> @ManagerEmployeeId
        UNION ALL
        SELECT c.EmployeeId, t.Lvl + 1
        FROM dbo.tbl_Employees c
        INNER JOIN TeamCte t ON c.ReportingManagerId = t.EmployeeId
        WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0 AND c.IsActive = 1 AND t.Lvl < 10
    )
    SELECT CASE WHEN EXISTS (SELECT 1 FROM TeamCte WHERE EmployeeId = @TargetEmployeeId)
                THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsTeamMember;
END
GO
