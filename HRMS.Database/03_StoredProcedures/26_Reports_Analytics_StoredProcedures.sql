USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 13: Reports, Analytics & Export Stored Procedures
-- ============================================================================

-- 1. Executive Cross-Module Analytics Hub (6 Result Sets)
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetAnalyticsHub
    @CompanyId    INT,
    @BranchId     INT = NULL,
    @DepartmentId INT = NULL,
    @FromDate     DATE = NULL,
    @ToDate       DATE = NULL,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToDate IS NULL SET @ToDate = CAST(SYSUTCDATETIME() AS DATE);
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -29, @ToDate);

    DECLARE @LatestPayrollYear INT, @LatestPayrollMonth INT, @MonthlyPayrollCost DECIMAL(18,2) = 0;
    SELECT TOP 1
        @LatestPayrollYear = PayrollYear,
        @LatestPayrollMonth = PayrollMonth,
        @MonthlyPayrollCost = TotalGrossPay
    FROM dbo.tbl_PayrollRuns
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY PayrollYear DESC, PayrollMonth DESC;

    -- Result Set 1: Executive KPIs
    DECLARE @ActiveCount INT = 0, @ProbationCount INT = 0, @SeparatedPeriodCount INT = 0;
    SELECT
        @ActiveCount = SUM(CASE WHEN e.IsActive = 1 AND e.EmploymentStatus IN (1, 2, 3) THEN 1 ELSE 0 END),
        @ProbationCount = SUM(CASE WHEN e.IsActive = 1 AND e.EmploymentStatus = 2 THEN 1 ELSE 0 END),
        @SeparatedPeriodCount = SUM(CASE WHEN e.ExitDate BETWEEN @FromDate AND @ToDate THEN 1 ELSE 0 END)
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId);

    DECLARE @AttTotal INT = 0, @AttOnTime INT = 0;
    SELECT
        @AttTotal = COUNT(1),
        @AttOnTime = SUM(CASE WHEN a.Status IN (N'Present', N'Late', N'HalfDay') THEN 1 ELSE 0 END)
    FROM dbo.tbl_Attendance a
    INNER JOIN dbo.tbl_Employees e ON a.EmployeeId = e.EmployeeId
    WHERE a.CompanyId = @CompanyId AND a.IsDeleted = 0
      AND a.AttendanceDate BETWEEN @FromDate AND @ToDate
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId);

    SELECT
        ISNULL(@ActiveCount, 0) AS ActiveHeadcount,
        ISNULL(@ProbationCount, 0) AS ProbationCount,
        CAST(CASE WHEN ISNULL(@ActiveCount, 0) > 0
                  THEN ROUND((CAST(ISNULL(@SeparatedPeriodCount, 0) AS DECIMAL(10,2)) / @ActiveCount) * 100.0, 1)
                  ELSE 0 END AS DECIMAL(5,1)) AS TurnoverRatePercent,
        ISNULL(@MonthlyPayrollCost, 0) AS MonthlyPayrollCost,
        CAST(CASE WHEN ISNULL(@AttTotal, 0) > 0
                  THEN ROUND((CAST(ISNULL(@AttOnTime, 0) AS DECIMAL(10,2)) / @AttTotal) * 100.0, 1)
                  ELSE 0 END AS DECIMAL(5,1)) AS AttendanceCompliancePercent,
        (
            SELECT COUNT(1) FROM dbo.tbl_JobRequisitions jr
            WHERE jr.CompanyId = @CompanyId AND jr.IsDeleted = 0
              AND jr.RequisitionStatus IN (N'Open', N'PendingApproval')
              AND (@BranchId IS NULL OR jr.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR jr.DepartmentId = @DepartmentId)
        ) AS OpenRequisitionsCount,
        ISNULL((
            SELECT SUM(la.TotalDays)
            FROM dbo.tbl_LeaveApplications la
            INNER JOIN dbo.tbl_Employees e ON la.EmployeeId = e.EmployeeId
            WHERE la.CompanyId = @CompanyId AND la.IsDeleted = 0
              AND la.ApprovalStatus = N'Approved'
              AND la.StartDate BETWEEN @FromDate AND @ToDate
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        ), 0) AS ApprovedLeaveDaysInPeriod,
        ISNULL((
            SELECT SUM(el.RemainingBalance)
            FROM dbo.tbl_EmployeeLoans el
            INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
            WHERE el.CompanyId = @CompanyId AND el.IsDeleted = 0 AND el.LoanStatus = N'Active'
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        ), 0) AS OutstandingLoanPortfolio,
        CAST(ISNULL((
            SELECT CASE WHEN COUNT(1) = 0 THEN 0
                        ELSE ROUND(SUM(CASE WHEN pa.AppraisalStatus IN (N'ManagerReviewed', N'Completed') THEN 100.0 ELSE 0.0 END) / COUNT(1), 1) END
            FROM dbo.tbl_PerformanceAppraisals pa
            INNER JOIN dbo.tbl_Employees e ON pa.EmployeeId = e.EmployeeId
            WHERE pa.CompanyId = @CompanyId AND pa.IsDeleted = 0
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        ), 0) AS DECIMAL(5,1)) AS AppraisalCompletionPercent,
        ISNULL((
            SELECT SUM(ast.PurchaseCost)
            FROM dbo.tbl_Assets ast
            WHERE ast.CompanyId = @CompanyId AND ast.IsDeleted = 0 AND ast.AssetStatus <> N'Retired'
              AND (@BranchId IS NULL OR ast.BranchId = @BranchId)
        ), 0) AS ActiveAssetPortfolioValue,
        (
            SELECT COUNT(1)
            FROM dbo.tbl_SeparationRequests sr
            INNER JOIN dbo.tbl_Employees e ON sr.EmployeeId = e.EmployeeId
            WHERE sr.CompanyId = @CompanyId AND sr.IsDeleted = 0
              AND sr.SeparationStatus NOT IN (N'Completed', N'Rejected', N'Withdrawn')
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        ) AS ActiveSeparationCases;

    -- Result Set 2: Department Workforce & Cost Breakdown
    SELECT
        d.DepartmentId,
        d.DepartmentName,
        COUNT(CASE WHEN e.IsActive = 1 THEN 1 END) AS ActiveHeadcount,
        ISNULL(SUM(CASE WHEN e.IsActive = 1 THEN e.BaseSalary ELSE 0 END), 0) AS AnnualBaseSalaryTotal,
        ISNULL((
            SELECT SUM(p.NetPay)
            FROM dbo.tbl_Payslips p
            INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
            WHERE p.CompanyId = @CompanyId AND p.IsDeleted = 0 AND pr.IsDeleted = 0
              AND p.DepartmentId = d.DepartmentId
              AND pr.PayrollYear = @LatestPayrollYear AND pr.PayrollMonth = @LatestPayrollMonth
              AND (@BranchId IS NULL OR p.BranchId = @BranchId)
        ), 0) AS LatestMonthlyNetPay,
        CAST(ISNULL(AVG(CASE WHEN e.IsActive = 1 THEN DATEDIFF(MONTH, e.HireDate, @ToDate) / 12.0 END), 0) AS DECIMAL(4,1)) AS AvgTenureYears,
        ISNULL((
            SELECT SUM(jr.OpeningsCount)
            FROM dbo.tbl_JobRequisitions jr
            WHERE jr.CompanyId = @CompanyId AND jr.IsDeleted = 0 AND jr.DepartmentId = d.DepartmentId
              AND jr.RequisitionStatus IN (N'Open', N'PendingApproval')
              AND (@BranchId IS NULL OR jr.BranchId = @BranchId)
        ), 0) AS OpenPositions
    FROM dbo.tbl_Departments d
    LEFT JOIN dbo.tbl_Employees e
        ON d.DepartmentId = e.DepartmentId AND e.IsDeleted = 0
        AND (@BranchId IS NULL OR e.BranchId = @BranchId)
    WHERE d.CompanyId = @CompanyId AND d.IsDeleted = 0 AND d.IsActive = 1
      AND (@DepartmentId IS NULL OR d.DepartmentId = @DepartmentId)
    GROUP BY d.DepartmentId, d.DepartmentName
    ORDER BY ActiveHeadcount DESC, d.DepartmentName;

    -- Result Set 3: 6-Month Payroll & Approved Expense Trend
    DECLARE @AnchorMonth DATE = DATEFROMPARTS(YEAR(@ToDate), MONTH(@ToDate), 1);
    ;WITH MonthNums AS (
        SELECT 5 AS Offset UNION ALL SELECT 4 UNION ALL SELECT 3 UNION ALL SELECT 2 UNION ALL SELECT 1 UNION ALL SELECT 0
    ),
    Windows AS (
        SELECT
            DATEADD(MONTH, -Offset, @AnchorMonth) AS MStart,
            EOMONTH(DATEADD(MONTH, -Offset, @AnchorMonth)) AS MEnd
        FROM MonthNums
    )
    SELECT
        FORMAT(w.MStart, 'MMM yyyy') AS PeriodLabel,
        ISNULL((
            SELECT TOP 1 pr.TotalGrossPay FROM dbo.tbl_PayrollRuns pr
            WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0
              AND pr.PayrollYear = YEAR(w.MStart) AND pr.PayrollMonth = MONTH(w.MStart)
        ), 0) AS TotalGrossPay,
        ISNULL((
            SELECT TOP 1 pr.TotalNetPay FROM dbo.tbl_PayrollRuns pr
            WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0
              AND pr.PayrollYear = YEAR(w.MStart) AND pr.PayrollMonth = MONTH(w.MStart)
        ), 0) AS TotalNetPay,
        ISNULL((
            SELECT TOP 1 pr.TotalTaxDeducted FROM dbo.tbl_PayrollRuns pr
            WHERE pr.CompanyId = @CompanyId AND pr.IsDeleted = 0
              AND pr.PayrollYear = YEAR(w.MStart) AND pr.PayrollMonth = MONTH(w.MStart)
        ), 0) AS TotalTaxDeducted,
        ISNULL((
            SELECT SUM(ISNULL(ec.ApprovedAmount, ec.ClaimAmount))
            FROM dbo.tbl_ExpenseClaims ec
            INNER JOIN dbo.tbl_Employees e ON ec.EmployeeId = e.EmployeeId
            WHERE ec.CompanyId = @CompanyId AND ec.IsDeleted = 0
              AND ec.ClaimStatus IN (N'Approved', N'Reimbursed')
              AND ec.ExpenseDate BETWEEN w.MStart AND w.MEnd
              AND (@BranchId IS NULL OR e.BranchId = @BranchId)
              AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        ), 0) AS ApprovedExpenseAmount
    FROM Windows w
    ORDER BY w.MStart ASC;

    -- Result Set 4: Attrition & Separation Breakdown
    SELECT
        sr.ReasonCategory,
        COUNT(1) AS CaseCount,
        CAST(ISNULL(AVG(CAST(ei.OverallSatisfactionRating AS DECIMAL(4,2))), 0) AS DECIMAL(4,2)) AS AvgSatisfaction
    FROM dbo.tbl_SeparationRequests sr
    INNER JOIN dbo.tbl_Employees e ON sr.EmployeeId = e.EmployeeId
    LEFT JOIN dbo.tbl_ExitInterviews ei ON sr.SeparationId = ei.SeparationId
    WHERE sr.CompanyId = @CompanyId AND sr.IsDeleted = 0
      AND sr.SeparationStatus NOT IN (N'Rejected', N'Withdrawn')
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    GROUP BY sr.ReasonCategory
    ORDER BY CaseCount DESC;

    -- Result Set 5: Saved Report Templates
    SELECT
        t.TemplateId,
        t.CompanyId,
        t.TemplateName,
        t.ReportKey,
        t.BranchId,
        b.BranchName,
        t.DepartmentId,
        d.DepartmentName,
        t.DateRangeDays,
        t.StatusFilter,
        t.Notes,
        t.IsShared,
        t.CreatedBy,
        t.CreatedDate
    FROM dbo.tbl_SavedReportTemplates t
    LEFT JOIN dbo.tbl_Branches b ON t.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Departments d ON t.DepartmentId = d.DepartmentId
    WHERE t.CompanyId = @CompanyId AND t.IsDeleted = 0
      AND (t.IsShared = 1 OR t.CreatedBy = @UserId)
    ORDER BY t.CreatedDate DESC, t.TemplateId DESC;

    -- Result Set 6: Recent Export Audit Log
    SELECT TOP 15
        ExportLogId,
        CompanyId,
        ReportKey,
        ReportTitle,
        ExportFormat,
        FilterSummary,
        RecordCount,
        ExportedBy,
        ExportedByName,
        ExportedDate
    FROM dbo.tbl_ReportExportLogs
    WHERE CompanyId = @CompanyId
    ORDER BY ExportedDate DESC, ExportLogId DESC;
END
GO

-- 2. Workforce Headcount, Demographics & Compensation Report
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetWorkforceHeadcount
    @CompanyId        INT,
    @BranchId         INT = NULL,
    @DepartmentId     INT = NULL,
    @EmploymentStatus TINYINT = NULL,
    @Search           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        e.Gender,
        e.WorkEmail,
        b.BranchName,
        d.DepartmentName,
        desig.DesignationTitle,
        ISNULL(desig.JobGrade, N'-') AS JobGrade,
        ISNULL(mgr.FullName, N'-') AS ManagerName,
        e.HireDate,
        e.ExitDate,
        CAST(DATEDIFF(MONTH, e.HireDate, ISNULL(e.ExitDate, @Today)) / 12.0 AS DECIMAL(5,1)) AS TenureYears,
        CASE e.EmploymentStatus
            WHEN 1 THEN N'Active'
            WHEN 2 THEN N'Probation'
            WHEN 3 THEN N'OnLeave'
            WHEN 4 THEN N'Resigned'
            WHEN 5 THEN N'Terminated'
            ELSE N'Inactive'
        END AS EmploymentStatusLabel,
        e.BaseSalary,
        e.IsActive
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@EmploymentStatus IS NULL OR e.EmploymentStatus = @EmploymentStatus)
      AND (@Search IS NULL OR @Search = N'' OR
           e.EmployeeCode LIKE N'%' + @Search + N'%' OR
           e.FullName LIKE N'%' + @Search + N'%' OR
           e.WorkEmail LIKE N'%' + @Search + N'%')
    ORDER BY d.DepartmentName, e.FullName;
END
GO

-- 3. Attendance Punctuality & Leave Utilization Report
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetAttendanceLeaveSummary
    @CompanyId    INT,
    @BranchId     INT = NULL,
    @DepartmentId INT = NULL,
    @FromDate     DATE = NULL,
    @ToDate       DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToDate IS NULL SET @ToDate = CAST(SYSUTCDATETIME() AS DATE);
    IF @FromDate IS NULL SET @FromDate = DATEFROMPARTS(YEAR(@ToDate), MONTH(@ToDate), 1);
    DECLARE @LeaveYear INT = YEAR(@ToDate);

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        b.BranchName,
        d.DepartmentName,
        ISNULL(att.PresentDays, 0) AS PresentDays,
        ISNULL(att.LateDays, 0) AS LateDays,
        ISNULL(att.AbsentDays, 0) AS AbsentDays,
        ISNULL(att.LeaveDaysFromAttendance, 0) AS OnLeaveDays,
        CAST(ISNULL(att.TotalWorkHours, 0) AS DECIMAL(8,1)) AS TotalWorkHours,
        CAST(ISNULL(att.TotalOvertimeHours, 0) AS DECIMAL(8,1)) AS TotalOvertimeHours,
        ISNULL(att.TotalLateMinutes, 0) AS TotalLateMinutes,
        ISNULL(lv.ApprovedLeaveDays, 0) AS ApprovedLeaveDays,
        ISNULL(bal.RemainingLeaveBalance, 0) AS RemainingLeaveBalance
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    OUTER APPLY (
        SELECT
            SUM(CASE WHEN a.Status IN (N'Present', N'Late') THEN 1 ELSE 0 END) AS PresentDays,
            SUM(CASE WHEN a.Status = N'Late' THEN 1 ELSE 0 END) AS LateDays,
            SUM(CASE WHEN a.Status = N'Absent' THEN 1 ELSE 0 END) AS AbsentDays,
            SUM(CASE WHEN a.Status = N'OnLeave' THEN 1 ELSE 0 END) AS LeaveDaysFromAttendance,
            SUM(ISNULL(a.WorkMinutes, 0)) / 60.0 AS TotalWorkHours,
            SUM(ISNULL(a.OvertimeMinutes, 0)) / 60.0 AS TotalOvertimeHours,
            SUM(ISNULL(a.LateMinutes, 0)) AS TotalLateMinutes
        FROM dbo.tbl_Attendance a
        WHERE a.CompanyId = @CompanyId AND a.EmployeeId = e.EmployeeId AND a.IsDeleted = 0
          AND a.AttendanceDate BETWEEN @FromDate AND @ToDate
    ) att
    OUTER APPLY (
        SELECT SUM(la.TotalDays) AS ApprovedLeaveDays
        FROM dbo.tbl_LeaveApplications la
        WHERE la.CompanyId = @CompanyId AND la.EmployeeId = e.EmployeeId AND la.IsDeleted = 0
          AND la.ApprovalStatus = N'Approved'
          AND la.StartDate <= @ToDate AND la.EndDate >= @FromDate
    ) lv
    OUTER APPLY (
        SELECT SUM(CASE WHEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays) > 0
                        THEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays)
                        ELSE 0 END) AS RemainingLeaveBalance
        FROM dbo.tbl_LeaveBalances lb
        WHERE lb.CompanyId = @CompanyId AND lb.EmployeeId = e.EmployeeId AND lb.LeaveYear = @LeaveYear
    ) bal
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY d.DepartmentName, e.FullName;
END
GO

-- 4. Payroll Register & Cost-to-Company Report
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetPayrollCostReport
    @CompanyId    INT,
    @PayrollYear  INT = NULL,
    @PayrollMonth INT = NULL,
    @BranchId     INT = NULL,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PayrollYear IS NULL OR @PayrollMonth IS NULL
    BEGIN
        SELECT TOP 1
            @PayrollYear = ISNULL(@PayrollYear, PayrollYear),
            @PayrollMonth = ISNULL(@PayrollMonth, PayrollMonth)
        FROM dbo.tbl_PayrollRuns
        WHERE CompanyId = @CompanyId AND IsDeleted = 0
        ORDER BY PayrollYear DESC, PayrollMonth DESC;
    END

    SELECT
        pr.PeriodLabel,
        pr.PayrollYear,
        pr.PayrollMonth,
        pr.PayrollStatus,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        b.BranchName,
        d.DepartmentName,
        desig.DesignationTitle,
        p.WorkingDaysInMonth,
        p.PaidDays,
        p.BasicPay,
        p.TotalAllowances,
        p.OvertimePay,
        p.LeaveEncashmentPay,
        p.BonusAmount,
        p.GrossPay,
        p.IncomeTax,
        p.StatutoryDeductions,
        p.OtherDeductions + p.UnpaidAbsenceDeduction AS OtherAndAbsenceDeductions,
        p.TotalDeductions,
        p.NetPay,
        p.EmployerContributions,
        (p.GrossPay + p.EmployerContributions) AS CostToCompany,
        p.PaymentStatus
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
    INNER JOIN dbo.tbl_Employees e ON p.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Branches b ON p.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON p.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON p.DesignationId = desig.DesignationId
    WHERE p.CompanyId = @CompanyId AND p.IsDeleted = 0 AND pr.IsDeleted = 0
      AND (@PayrollYear IS NULL OR pr.PayrollYear = @PayrollYear)
      AND (@PayrollMonth IS NULL OR pr.PayrollMonth = @PayrollMonth)
      AND (@BranchId IS NULL OR p.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR p.DepartmentId = @DepartmentId)
    ORDER BY pr.PayrollYear DESC, pr.PayrollMonth DESC, d.DepartmentName, e.FullName;
END
GO

-- 5. Recruitment Funnel & Talent Performance/Training Report (2 Result Sets)
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetRecruitmentTalentReport
    @CompanyId    INT,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Recruitment Pipeline Funnel per Requisition
    SELECT
        jr.RequisitionId,
        jr.RequisitionCode,
        jr.JobTitle,
        d.DepartmentName,
        b.BranchName,
        jr.OpeningsCount,
        jr.RequisitionStatus,
        jr.TargetHireDate,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = jr.RequisitionId AND ja.IsDeleted = 0) AS TotalApplicants,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = jr.RequisitionId AND ja.IsDeleted = 0 AND ja.PipelineStage IN (N'Interview', N'Assessment')) AS InterviewStageCount,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = jr.RequisitionId AND ja.IsDeleted = 0 AND ja.PipelineStage = N'Offer') AS OfferStageCount,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = jr.RequisitionId AND ja.IsDeleted = 0 AND ja.PipelineStage = N'Hired') AS HiredCount
    FROM dbo.tbl_JobRequisitions jr
    INNER JOIN dbo.tbl_Departments d ON jr.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Branches b ON jr.BranchId = b.BranchId
    WHERE jr.CompanyId = @CompanyId AND jr.IsDeleted = 0
      AND (@DepartmentId IS NULL OR jr.DepartmentId = @DepartmentId)
    ORDER BY jr.CreatedDate DESC;

    -- Result Set 2: Talent Performance & Training Completion Summary per Employee
    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        desig.DesignationTitle,
        ISNULL(ac.CycleName, N'No Active Cycle') AS LatestCycleName,
        ISNULL(pa.AppraisalStatus, N'NotStarted') AS AppraisalStatus,
        pa.SelfRating,
        pa.ManagerRating,
        pa.FinalRating,
        ISNULL(pa.RatingBand, N'-') AS RatingBand,
        ISNULL(trn.CompletedTrainings, 0) AS CompletedTrainings,
        ISNULL(trn.TotalEnrolledTrainings, 0) AS TotalEnrolledTrainings,
        CAST(ISNULL(trn.AvgAssessmentScore, 0) AS DECIMAL(5,1)) AS AvgTrainingScore
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    OUTER APPLY (
        SELECT TOP 1 pa1.*, ac1.CycleName
        FROM dbo.tbl_PerformanceAppraisals pa1
        INNER JOIN dbo.tbl_AppraisalCycles ac1 ON pa1.CycleId = ac1.CycleId
        WHERE pa1.CompanyId = @CompanyId AND pa1.EmployeeId = e.EmployeeId AND pa1.IsDeleted = 0
        ORDER BY ac1.ReviewYear DESC, pa1.AppraisalId DESC
    ) pa
    LEFT JOIN dbo.tbl_AppraisalCycles ac ON pa.CycleId = ac.CycleId
    OUTER APPLY (
        SELECT
            COUNT(1) AS TotalEnrolledTrainings,
            SUM(CASE WHEN te.AttendanceStatus = N'Completed' THEN 1 ELSE 0 END) AS CompletedTrainings,
            AVG(te.AssessmentScore) AS AvgAssessmentScore
        FROM dbo.tbl_TrainingEnrollments te
        WHERE te.CompanyId = @CompanyId AND te.EmployeeId = e.EmployeeId AND te.IsDeleted = 0
    ) trn
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0 AND e.IsActive = 1
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY d.DepartmentName, e.FullName;
END
GO

-- 6. Financial (Loans & Expenses) & Asset/Exit Compliance Report (2 Result Sets)
CREATE OR ALTER PROCEDURE dbo.sp_Reports_GetFinancialComplianceReport
    @CompanyId    INT,
    @DepartmentId INT = NULL,
    @FromDate     DATE = NULL,
    @ToDate       DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToDate IS NULL SET @ToDate = CAST(SYSUTCDATETIME() AS DATE);
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -89, @ToDate);

    -- Result Set 1: Loans & Expenses by Employee
    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        ISNULL(ln.ActiveLoansCount, 0) AS ActiveLoansCount,
        ISNULL(ln.TotalLoanPrincipal, 0) AS TotalLoanPrincipal,
        ISNULL(ln.RemainingLoanBalance, 0) AS RemainingLoanBalance,
        ISNULL(expn.ClaimCount, 0) AS ExpenseClaimCount,
        ISNULL(expn.TotalClaimedAmount, 0) AS TotalClaimedAmount,
        ISNULL(expn.ApprovedExpenseAmount, 0) AS ApprovedExpenseAmount
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    OUTER APPLY (
        SELECT
            COUNT(CASE WHEN el.LoanStatus = N'Active' THEN 1 END) AS ActiveLoansCount,
            SUM(CASE WHEN el.LoanStatus IN (N'Active', N'Closed') THEN el.PrincipalAmount ELSE 0 END) AS TotalLoanPrincipal,
            SUM(CASE WHEN el.LoanStatus = N'Active' THEN el.RemainingBalance ELSE 0 END) AS RemainingLoanBalance
        FROM dbo.tbl_EmployeeLoans el
        WHERE el.CompanyId = @CompanyId AND el.EmployeeId = e.EmployeeId AND el.IsDeleted = 0
    ) ln
    OUTER APPLY (
        SELECT
            COUNT(1) AS ClaimCount,
            SUM(ec.ClaimAmount) AS TotalClaimedAmount,
            SUM(CASE WHEN ec.ClaimStatus IN (N'Approved', N'Reimbursed') THEN ISNULL(ec.ApprovedAmount, ec.ClaimAmount) ELSE 0 END) AS ApprovedExpenseAmount
        FROM dbo.tbl_ExpenseClaims ec
        WHERE ec.CompanyId = @CompanyId AND ec.EmployeeId = e.EmployeeId AND ec.IsDeleted = 0
          AND ec.ExpenseDate BETWEEN @FromDate AND @ToDate
    ) expn
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (ISNULL(ln.TotalLoanPrincipal, 0) > 0 OR ISNULL(expn.ClaimCount, 0) > 0 OR e.IsActive = 1)
    ORDER BY RemainingLoanBalance DESC, ApprovedExpenseAmount DESC, e.FullName;

    -- Result Set 2: Asset Custody, Policy Sign-Off & Separation Compliance by Employee
    DECLARE @MandatoryPolicyCount INT = (
        SELECT COUNT(1) FROM dbo.tbl_CompanyDocuments
        WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND DocumentStatus = N'Published' AND RequiresAcknowledgment = 1
    );

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        ISNULL(ast.AssignedAssetCount, 0) AS AssignedAssetCount,
        ISNULL(ast.AssignedAssetValue, 0) AS AssignedAssetValue,
        ISNULL(ack.AcknowledgedPolicies, 0) AS AcknowledgedPoliciesCount,
        ISNULL(@MandatoryPolicyCount, 0) AS MandatoryPoliciesCount,
        ISNULL(sr.SeparationNumber, N'-') AS SeparationNumber,
        ISNULL(sr.SeparationStatus, N'ActiveEmployee') AS SeparationStatus,
        ISNULL(fs.SettlementStatus, N'-') AS SettlementStatus,
        fs.NetPayableAmount AS SettlementNetAmount
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    OUTER APPLY (
        SELECT
            COUNT(1) AS AssignedAssetCount,
            SUM(a.PurchaseCost) AS AssignedAssetValue
        FROM dbo.tbl_AssetAllocations aa
        INNER JOIN dbo.tbl_Assets a ON aa.AssetId = a.AssetId
        WHERE aa.CompanyId = @CompanyId AND aa.EmployeeId = e.EmployeeId
          AND aa.AllocationStatus = N'Active' AND aa.IsDeleted = 0 AND a.IsDeleted = 0
    ) ast
    OUTER APPLY (
        SELECT COUNT(DISTINCT da.DocumentId) AS AcknowledgedPolicies
        FROM dbo.tbl_DocumentAcknowledgments da
        INNER JOIN dbo.tbl_CompanyDocuments cd ON da.DocumentId = cd.DocumentId
        WHERE da.CompanyId = @CompanyId AND da.EmployeeId = e.EmployeeId
          AND cd.IsDeleted = 0 AND cd.DocumentStatus = N'Published' AND cd.RequiresAcknowledgment = 1
    ) ack
    OUTER APPLY (
        SELECT TOP 1 sr1.SeparationId, sr1.SeparationNumber, sr1.SeparationStatus
        FROM dbo.tbl_SeparationRequests sr1
        WHERE sr1.CompanyId = @CompanyId AND sr1.EmployeeId = e.EmployeeId AND sr1.IsDeleted = 0
        ORDER BY sr1.SeparationId DESC
    ) sr
    LEFT JOIN dbo.tbl_FinalSettlements fs ON sr.SeparationId = fs.SeparationId AND fs.IsDeleted = 0
    WHERE e.CompanyId = @CompanyId AND e.IsDeleted = 0
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
    ORDER BY AssignedAssetCount DESC, e.FullName;
END
GO

-- 7. Saved Report Template CRUD + Export Audit Logging
CREATE OR ALTER PROCEDURE dbo.sp_ReportTemplate_Save
    @TemplateId    INT,
    @CompanyId     INT,
    @TemplateName  NVARCHAR(120),
    @ReportKey     NVARCHAR(50),
    @BranchId      INT = NULL,
    @DepartmentId  INT = NULL,
    @DateRangeDays INT = 30,
    @StatusFilter  NVARCHAR(40) = NULL,
    @Notes         NVARCHAR(300) = NULL,
    @IsShared      BIT = 1,
    @UserId        INT,
    @SavedId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @TemplateId = 0
    BEGIN
        INSERT INTO dbo.tbl_SavedReportTemplates (
            CompanyId, TemplateName, ReportKey, BranchId, DepartmentId, DateRangeDays, StatusFilter, Notes, IsShared, CreatedBy
        )
        VALUES (
            @CompanyId, @TemplateName, @ReportKey, @BranchId, @DepartmentId, @DateRangeDays, @StatusFilter, @Notes, @IsShared, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_SavedReportTemplates
        SET TemplateName = @TemplateName,
            ReportKey = @ReportKey,
            BranchId = @BranchId,
            DepartmentId = @DepartmentId,
            DateRangeDays = @DateRangeDays,
            StatusFilter = @StatusFilter,
            Notes = @Notes,
            IsShared = @IsShared,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50181, N'Saved report template not found.', 1;
        SET @SavedId = @TemplateId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReportTemplate_Delete
    @CompanyId  INT,
    @TemplateId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_SavedReportTemplates
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReportExportLog_Insert
    @CompanyId      INT,
    @ReportKey      NVARCHAR(50),
    @ReportTitle    NVARCHAR(150),
    @ExportFormat   NVARCHAR(20),
    @FilterSummary  NVARCHAR(300) = NULL,
    @RecordCount    INT,
    @ExportedBy     INT,
    @ExportedByName NVARCHAR(100),
    @IpAddress      NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tbl_ReportExportLogs (
        CompanyId, ReportKey, ReportTitle, ExportFormat, FilterSummary, RecordCount, ExportedBy, ExportedByName, IpAddress
    )
    VALUES (
        @CompanyId, @ReportKey, @ReportTitle, @ExportFormat, @FilterSummary, @RecordCount, @ExportedBy, @ExportedByName, @IpAddress
    );
END
GO
