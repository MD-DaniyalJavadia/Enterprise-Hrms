USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 10: Employee Self-Service Portal Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_ESS_GetDashboard
    @CompanyId  INT,
    @EmployeeId INT,
    @AsOfDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    DECLARE @CurrentYear INT = YEAR(@AsOfDate);

    -- Resolve active shift for employee
    DECLARE @ShiftId INT;
    SELECT TOP 1 @ShiftId = sa.ShiftId
    FROM dbo.tbl_ShiftAssignments sa
    WHERE sa.CompanyId = @CompanyId AND sa.EmployeeId = @EmployeeId
      AND sa.IsDeleted = 0 AND sa.IsActive = 1
      AND @AsOfDate >= sa.EffectiveFrom
      AND (sa.EffectiveTo IS NULL OR @AsOfDate <= sa.EffectiveTo)
    ORDER BY sa.EffectiveFrom DESC, sa.AssignmentId DESC;

    -- 1. Employee 360 Summary
    SELECT
        e.EmployeeId,
        e.CompanyId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.FullName,
        e.Gender,
        e.DateOfBirth,
        e.WorkEmail,
        e.PersonalEmail,
        e.Phone,
        e.MobilePhone,
        e.CurrentAddress,
        e.PermanentAddress,
        e.City,
        e.Country,
        e.HireDate,
        e.ConfirmationDate,
        e.EmploymentStatus,
        e.PhotoPath,
        e.BankName,
        e.BankAccountTitle,
        e.BankAccountNumber,
        e.BankRoutingCode,
        e.TaxIdentificationNumber,
        e.TaxFilingStatus,
        b.BranchName,
        d.DepartmentName,
        desig.DesignationTitle,
        mgr.EmployeeId AS ReportingManagerId,
        mgr.FullName AS ReportingManagerName,
        mgr.WorkEmail AS ReportingManagerEmail,
        ISNULL(s.ShiftName, N'Standard Day Shift') AS ShiftName,
        CONVERT(VARCHAR(5), ISNULL(s.StartTime, CAST('09:00:00' AS TIME(0))), 108) AS ShiftStartTime,
        CONVERT(VARCHAR(5), ISNULL(s.EndTime, CAST('18:00:00' AS TIME(0))), 108) AS ShiftEndTime,
        ISNULL(s.WeeklyOffDays, N'Saturday,Sunday') AS WeeklyOffDays,
        c.CurrencyCode
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Companies c ON e.CompanyId = c.CompanyId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(@ShiftId, e.ShiftId) = s.ShiftId
    WHERE e.CompanyId = @CompanyId AND e.EmployeeId = @EmployeeId AND e.IsDeleted = 0;

    -- 2. Today's Attendance
    SELECT TOP 1
        a.AttendanceId,
        a.AttendanceDate,
        a.CheckInTime,
        a.CheckOutTime,
        a.Status,
        a.LateMinutes,
        a.EarlyExitMinutes,
        a.WorkMinutes,
        a.OvertimeMinutes,
        ISNULL(s.ShiftName, N'Standard Shift') AS ShiftName,
        a.Remarks
    FROM dbo.tbl_Attendance a
    LEFT JOIN dbo.tbl_Shifts s ON a.ShiftId = s.ShiftId
    WHERE a.CompanyId = @CompanyId AND a.EmployeeId = @EmployeeId AND a.AttendanceDate = @AsOfDate AND a.IsDeleted = 0;

    -- 3. Leave Balances (Current Year)
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
        ISNULL(lt.ColorHex, N'#2563eb') AS ColorHex,
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
    WHERE lb.CompanyId = @CompanyId AND lb.EmployeeId = @EmployeeId AND lb.LeaveYear = @CurrentYear AND lt.IsDeleted = 0
    ORDER BY lt.LeaveName;

    -- 4. Recent Leave Applications
    SELECT TOP 10
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
        ISNULL(lt.ColorHex, N'#2563eb') AS ColorHex,
        la.StartDate,
        la.EndDate,
        la.TotalDays,
        la.IsHalfDay,
        la.HalfDaySession,
        la.Reason,
        la.ContactDuringLeave,
        la.AttachmentFileName,
        la.AttachmentStoredName,
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
    WHERE la.CompanyId = @CompanyId AND la.EmployeeId = @EmployeeId AND la.IsDeleted = 0
    ORDER BY la.StartDate DESC, la.LeaveApplicationId DESC;

    -- 5. Recent Payslips
    SELECT TOP 6
        p.PayslipId,
        p.PayrollRunId,
        pr.PayrollYear,
        pr.PayrollMonth,
        pr.PeriodLabel,
        pr.PayDate,
        p.WorkingDaysInMonth,
        p.PaidDays,
        p.BasicPay,
        p.TotalAllowances,
        p.GrossPay,
        p.TotalDeductions,
        p.NetPay,
        p.PaymentStatus,
        pr.PayrollStatus,
        c.CurrencyCode
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
    INNER JOIN dbo.tbl_Companies c ON p.CompanyId = c.CompanyId
    WHERE p.CompanyId = @CompanyId AND p.EmployeeId = @EmployeeId AND p.IsDeleted = 0 AND pr.IsDeleted = 0
    ORDER BY pr.PayrollYear DESC, pr.PayrollMonth DESC, p.PayslipId DESC;

    -- 6. Active & Recent Loans
    SELECT TOP 6
        el.LoanId,
        el.CompanyId,
        el.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        el.LoanTypeId,
        lt.TypeCode AS LoanTypeCode,
        lt.TypeName AS LoanTypeName,
        lt.Category AS LoanCategory,
        el.LoanNumber,
        el.PrincipalAmount,
        el.InterestRatePercent,
        el.TenureMonths,
        el.MonthlyInstallment,
        el.TotalPayableAmount,
        el.PaidAmount,
        el.RemainingBalance,
        el.Purpose,
        el.RepaymentStartDate,
        el.LoanStatus,
        el.ApprovedDate,
        el.DisbursedDate,
        el.ReviewerComments,
        el.CreatedDate
    FROM dbo.tbl_EmployeeLoans el
    INNER JOIN dbo.tbl_Employees e ON el.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_LoanTypes lt ON el.LoanTypeId = lt.LoanTypeId
    WHERE el.CompanyId = @CompanyId AND el.EmployeeId = @EmployeeId AND el.IsDeleted = 0
    ORDER BY el.CreatedDate DESC, el.LoanId DESC;

    -- 7. Recent Expense Claims
    SELECT TOP 6
        c.ClaimId,
        c.CompanyId,
        c.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        c.CategoryId,
        ec.CategoryCode,
        ec.CategoryName,
        c.ClaimNumber,
        c.ExpenseDate,
        c.MerchantOrVendor,
        c.ClaimAmount,
        c.ApprovedAmount,
        c.CurrencyCode,
        c.Description,
        c.ReceiptFileName,
        c.ReceiptStoredName,
        c.ClaimStatus,
        c.ReviewedDate,
        c.ReviewerComments,
        c.ReimbursedDate,
        c.PaymentReference,
        c.CreatedDate
    FROM dbo.tbl_ExpenseClaims c
    INNER JOIN dbo.tbl_Employees e ON c.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_ExpenseCategories ec ON c.CategoryId = ec.CategoryId
    WHERE c.CompanyId = @CompanyId AND c.EmployeeId = @EmployeeId AND c.IsDeleted = 0
    ORDER BY c.ExpenseDate DESC, c.ClaimId DESC;

    -- 8. Performance Appraisals
    SELECT TOP 4
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
    INNER JOIN dbo.tbl_AppraisalCycles ac ON pa.CycleId = ac.CycleId
    INNER JOIN dbo.tbl_Employees e ON pa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees rev ON pa.ReviewerEmployeeId = rev.EmployeeId
    WHERE pa.CompanyId = @CompanyId AND pa.EmployeeId = @EmployeeId AND pa.IsDeleted = 0
    ORDER BY ac.ReviewYear DESC, pa.AppraisalId DESC;

    -- 9. Performance Goals
    SELECT TOP 8
        pg.GoalId,
        pg.CompanyId,
        pg.CycleId,
        ac.CycleCode,
        ac.CycleName,
        pg.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        pg.GoalTitle,
        pg.KpiMetric,
        pg.WeightPercent,
        pg.TargetValue,
        pg.AchievedValue,
        pg.ProgressPercent,
        pg.GoalStatus,
        pg.ManagerNotes
    FROM dbo.tbl_PerformanceGoals pg
    INNER JOIN dbo.tbl_AppraisalCycles ac ON pg.CycleId = ac.CycleId
    INNER JOIN dbo.tbl_Employees e ON pg.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE pg.CompanyId = @CompanyId AND pg.EmployeeId = @EmployeeId AND pg.IsDeleted = 0
    ORDER BY ac.ReviewYear DESC, pg.GoalId DESC;

    -- 10. Training Enrollments
    SELECT TOP 8
        te.EnrollmentId,
        te.CompanyId,
        te.ProgramId,
        tp.ProgramCode,
        tp.ProgramTitle,
        tp.Category,
        tp.StartDate,
        tp.EndDate,
        te.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        te.EnrollmentDate,
        te.AttendanceStatus,
        te.AssessmentScore,
        te.CertificateNumber,
        te.CompletionDate,
        te.FeedbackRating,
        te.FeedbackNotes
    FROM dbo.tbl_TrainingEnrollments te
    INNER JOIN dbo.tbl_TrainingPrograms tp ON te.ProgramId = tp.ProgramId
    INNER JOIN dbo.tbl_Employees e ON te.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE te.CompanyId = @CompanyId AND te.EmployeeId = @EmployeeId AND te.IsDeleted = 0
    ORDER BY tp.StartDate DESC, te.EnrollmentId DESC;

    -- 11. Profile Update Requests
    SELECT TOP 10
        pr.RequestId,
        pr.CompanyId,
        pr.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        pr.RequestType,
        pr.FieldSummary,
        pr.RequestedPhone,
        pr.RequestedMobilePhone,
        pr.RequestedPersonalEmail,
        pr.RequestedAddress,
        pr.RequestedCity,
        pr.RequestedCountry,
        pr.RequestedBankName,
        pr.RequestedBankAccountTitle,
        pr.RequestedBankAccountNumber,
        pr.RequestedBankRoutingCode,
        pr.RequestedEmergencyContactName,
        pr.RequestedEmergencyRelationship,
        pr.RequestedEmergencyPhone,
        pr.EmployeeNotes,
        pr.ApprovalStatus,
        pr.ReviewedBy,
        u.FullName AS ReviewedByName,
        pr.ReviewedDate,
        pr.ReviewerComments,
        pr.CreatedDate
    FROM dbo.tbl_ProfileUpdateRequests pr
    INNER JOIN dbo.tbl_Employees e ON pr.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_Users u ON pr.ReviewedBy = u.UserId
    WHERE pr.CompanyId = @CompanyId AND pr.EmployeeId = @EmployeeId AND pr.IsDeleted = 0
    ORDER BY pr.CreatedDate DESC, pr.RequestId DESC;

    -- 12. Upcoming Company Holidays (Next 5)
    SELECT TOP 5
        h.HolidayId,
        h.CompanyId,
        h.BranchId,
        ISNULL(b.BranchName, N'All Branches') AS BranchName,
        h.HolidayName,
        h.HolidayDate,
        h.HolidayType,
        h.Description,
        h.IsActive
    FROM dbo.tbl_Holidays h
    LEFT JOIN dbo.tbl_Branches b ON h.BranchId = b.BranchId
    WHERE h.CompanyId = @CompanyId AND h.IsDeleted = 0 AND h.IsActive = 1 AND h.HolidayDate >= @AsOfDate
    ORDER BY h.HolidayDate ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ESS_GetMyPayslips
    @CompanyId  INT,
    @EmployeeId INT,
    @Year       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PayslipId,
        p.PayrollRunId,
        pr.PayrollYear,
        pr.PayrollMonth,
        pr.PeriodLabel,
        pr.PayDate,
        p.WorkingDaysInMonth,
        p.PaidDays,
        p.BasicPay,
        p.TotalAllowances,
        p.GrossPay,
        p.TotalDeductions,
        p.NetPay,
        p.PaymentStatus,
        pr.PayrollStatus,
        c.CurrencyCode
    FROM dbo.tbl_Payslips p
    INNER JOIN dbo.tbl_PayrollRuns pr ON p.PayrollRunId = pr.PayrollRunId
    INNER JOIN dbo.tbl_Companies c ON p.CompanyId = c.CompanyId
    WHERE p.CompanyId = @CompanyId
      AND p.EmployeeId = @EmployeeId
      AND p.IsDeleted = 0
      AND pr.IsDeleted = 0
      AND (@Year IS NULL OR pr.PayrollYear = @Year)
    ORDER BY pr.PayrollYear DESC, pr.PayrollMonth DESC, p.PayslipId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ESS_GetMyAttendance
    @CompanyId  INT,
    @EmployeeId INT,
    @Year       INT,
    @Month      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate   DATE = EOMONTH(@StartDate);

    -- 1. Attendance Log for Month
    SELECT
        a.AttendanceId,
        a.CompanyId,
        a.BranchId,
        b.BranchName,
        a.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        desig.DesignationTitle,
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
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Shifts s ON ISNULL(a.ShiftId, e.ShiftId) = s.ShiftId
    WHERE a.CompanyId = @CompanyId AND a.EmployeeId = @EmployeeId AND a.IsDeleted = 0
      AND a.AttendanceDate BETWEEN @StartDate AND @EndDate
    ORDER BY a.AttendanceDate DESC;

    -- 2. My Attendance Regularizations
    SELECT TOP 25
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
    WHERE r.CompanyId = @CompanyId AND r.EmployeeId = @EmployeeId AND r.IsDeleted = 0
    ORDER BY r.CreatedDate DESC, r.RegularizationId DESC;

    -- 3. My Overtime Requests
    SELECT TOP 25
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
    WHERE o.CompanyId = @CompanyId AND o.EmployeeId = @EmployeeId AND o.IsDeleted = 0
    ORDER BY o.CreatedDate DESC, o.OvertimeRequestId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProfileUpdateRequest_Submit
    @CompanyId                      INT,
    @EmployeeId                     INT,
    @RequestType                    NVARCHAR(40),
    @FieldSummary                   NVARCHAR(200),
    @RequestedPhone                 NVARCHAR(30) = NULL,
    @RequestedMobilePhone           NVARCHAR(30) = NULL,
    @RequestedPersonalEmail         NVARCHAR(120) = NULL,
    @RequestedAddress               NVARCHAR(300) = NULL,
    @RequestedCity                  NVARCHAR(80) = NULL,
    @RequestedCountry               NVARCHAR(80) = NULL,
    @RequestedBankName              NVARCHAR(120) = NULL,
    @RequestedBankAccountTitle      NVARCHAR(120) = NULL,
    @RequestedBankAccountNumber     NVARCHAR(50) = NULL,
    @RequestedBankRoutingCode       NVARCHAR(40) = NULL,
    @RequestedEmergencyContactName  NVARCHAR(120) = NULL,
    @RequestedEmergencyRelationship NVARCHAR(50) = NULL,
    @RequestedEmergencyPhone        NVARCHAR(30) = NULL,
    @EmployeeNotes                  NVARCHAR(500) = NULL,
    @UserId                         INT,
    @NewId                          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1)
        THROW 50701, N'Active employee record not found.', 1;

    INSERT INTO dbo.tbl_ProfileUpdateRequests (
        CompanyId, EmployeeId, RequestType, FieldSummary,
        RequestedPhone, RequestedMobilePhone, RequestedPersonalEmail,
        RequestedAddress, RequestedCity, RequestedCountry,
        RequestedBankName, RequestedBankAccountTitle, RequestedBankAccountNumber, RequestedBankRoutingCode,
        RequestedEmergencyContactName, RequestedEmergencyRelationship, RequestedEmergencyPhone,
        EmployeeNotes, ApprovalStatus, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, @RequestType, @FieldSummary,
        @RequestedPhone, @RequestedMobilePhone, @RequestedPersonalEmail,
        @RequestedAddress, @RequestedCity, @RequestedCountry,
        @RequestedBankName, @RequestedBankAccountTitle, @RequestedBankAccountNumber, @RequestedBankRoutingCode,
        @RequestedEmergencyContactName, @RequestedEmergencyRelationship, @RequestedEmergencyPhone,
        @EmployeeNotes, N'Pending', @UserId
    );

    SET @NewId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProfileUpdateRequest_Cancel
    @CompanyId  INT,
    @RequestId  INT,
    @EmployeeId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status NVARCHAR(20);
    SELECT @Status = ApprovalStatus
    FROM dbo.tbl_ProfileUpdateRequests
    WHERE RequestId = @RequestId AND CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 50702, N'Profile update request not found.', 1;

    IF @Status <> N'Pending'
        THROW 50703, N'Only pending profile update requests can be cancelled.', 1;

    UPDATE dbo.tbl_ProfileUpdateRequests
    SET ApprovalStatus = N'Cancelled',
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE RequestId = @RequestId AND CompanyId = @CompanyId AND EmployeeId = @EmployeeId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProfileUpdateRequest_Review
    @CompanyId        INT,
    @RequestId        INT,
    @ApprovalStatus   NVARCHAR(20), -- Approved or Rejected
    @ReviewerComments NVARCHAR(400) = NULL,
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmpId INT, @CurStatus NVARCHAR(20), @ReqType NVARCHAR(40),
            @Phone NVARCHAR(30), @Mobile NVARCHAR(30), @Email NVARCHAR(120),
            @Addr NVARCHAR(300), @City NVARCHAR(80), @Country NVARCHAR(80),
            @BankName NVARCHAR(120), @BankTitle NVARCHAR(120), @BankAcct NVARCHAR(50), @BankRouting NVARCHAR(40),
            @EmgName NVARCHAR(120), @EmgRel NVARCHAR(50), @EmgPhone NVARCHAR(30);

    SELECT
        @EmpId = EmployeeId,
        @CurStatus = ApprovalStatus,
        @ReqType = RequestType,
        @Phone = NULLIF(LTRIM(RTRIM(RequestedPhone)), N''),
        @Mobile = NULLIF(LTRIM(RTRIM(RequestedMobilePhone)), N''),
        @Email = NULLIF(LTRIM(RTRIM(RequestedPersonalEmail)), N''),
        @Addr = NULLIF(LTRIM(RTRIM(RequestedAddress)), N''),
        @City = NULLIF(LTRIM(RTRIM(RequestedCity)), N''),
        @Country = NULLIF(LTRIM(RTRIM(RequestedCountry)), N''),
        @BankName = NULLIF(LTRIM(RTRIM(RequestedBankName)), N''),
        @BankTitle = NULLIF(LTRIM(RTRIM(RequestedBankAccountTitle)), N''),
        @BankAcct = NULLIF(LTRIM(RTRIM(RequestedBankAccountNumber)), N''),
        @BankRouting = NULLIF(LTRIM(RTRIM(RequestedBankRoutingCode)), N''),
        @EmgName = NULLIF(LTRIM(RTRIM(RequestedEmergencyContactName)), N''),
        @EmgRel = NULLIF(LTRIM(RTRIM(RequestedEmergencyRelationship)), N''),
        @EmgPhone = NULLIF(LTRIM(RTRIM(RequestedEmergencyPhone)), N'')
    FROM dbo.tbl_ProfileUpdateRequests
    WHERE RequestId = @RequestId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @EmpId IS NULL
        THROW 50702, N'Profile update request not found.', 1;

    IF @CurStatus <> N'Pending'
        THROW 50704, N'This profile update request has already been processed.', 1;

    BEGIN TRAN;

    UPDATE dbo.tbl_ProfileUpdateRequests
    SET ApprovalStatus = @ApprovalStatus,
        ReviewedBy = @UserId,
        ReviewedDate = SYSUTCDATETIME(),
        ReviewerComments = @ReviewerComments,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE RequestId = @RequestId AND CompanyId = @CompanyId;

    IF @ApprovalStatus = N'Approved'
    BEGIN
        UPDATE dbo.tbl_Employees
        SET Phone = COALESCE(@Phone, Phone),
            MobilePhone = COALESCE(@Mobile, MobilePhone),
            PersonalEmail = COALESCE(@Email, PersonalEmail),
            CurrentAddress = COALESCE(@Addr, CurrentAddress),
            City = COALESCE(@City, City),
            Country = COALESCE(@Country, Country),
            BankName = COALESCE(@BankName, BankName),
            BankAccountTitle = COALESCE(@BankTitle, BankAccountTitle),
            BankAccountNumber = COALESCE(@BankAcct, BankAccountNumber),
            BankRoutingCode = COALESCE(@BankRouting, BankRoutingCode),
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EmployeeId = @EmpId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @ReqType = N'EmergencyContact' AND @EmgName IS NOT NULL AND @EmgPhone IS NOT NULL
        BEGIN
            UPDATE dbo.tbl_EmployeeEmergencyContacts
            SET IsPrimary = 0
            WHERE EmployeeId = @EmpId AND CompanyId = @CompanyId AND IsDeleted = 0;

            INSERT INTO dbo.tbl_EmployeeEmergencyContacts (
                CompanyId, EmployeeId, ContactName, Relationship, PrimaryPhone, IsPrimary, CreatedBy
            )
            VALUES (
                @CompanyId, @EmpId, @EmgName, ISNULL(@EmgRel, N'Family'), @EmgPhone, 1, @UserId
            );
        END
    END

    COMMIT TRAN;
END
GO
