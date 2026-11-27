USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 9: Performance & Training Management Seed Data
-- ============================================================================

INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'PERFORMANCE.MANAGE', N'Performance', N'Manage', N'Manage', N'Configure appraisal cycles, assign KPI goals, and finalize manager evaluations'),
    (N'TRAINING.MANAGE',    N'Training',    N'Manage', N'Manage', N'Create training programs, enroll employees, and record training completion/certificates')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);
GO

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'PERFORMANCE.MANAGE', N'TRAINING.MANAGE')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.tbl_Roles r
CROSS JOIN (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode IN (N'PERFORMANCE.VIEW', N'TRAINING.VIEW')) p
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager', N'PayrollOfficer', N'Employee')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_AppraisalCycles WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @Yr INT = YEAR(SYSUTCDATETIME());
    INSERT INTO dbo.tbl_AppraisalCycles (CompanyId, CycleCode, CycleName, ReviewYear, StartDate, EndDate, SelfReviewDeadline, ManagerReviewDeadline, CycleStatus, Description)
    VALUES
        (@CompanyId, CONCAT(N'CY-', @Yr, N'-ANNUAL'), CONCAT(@Yr, N' Annual Performance Review'), @Yr, DATEFROMPARTS(@Yr, 1, 1), DATEFROMPARTS(@Yr, 12, 31), DATEFROMPARTS(@Yr, 12, 10), DATEFROMPARTS(@Yr, 12, 22), N'Active', N'Company-wide annual goal evaluation, competency review, and merit calibration.'),
        (@CompanyId, CONCAT(N'CY-', @Yr, N'-MID'),    CONCAT(@Yr, N' Mid-Year Check-In'),         @Yr, DATEFROMPARTS(@Yr, 1, 1), DATEFROMPARTS(@Yr, 6, 30),  DATEFROMPARTS(@Yr, 6, 20),  DATEFROMPARTS(@Yr, 6, 30),  N'Closed', N'Mid-year milestone alignment and coaching check-in.');

    DECLARE @ActiveCycleId INT = (SELECT TOP 1 CycleId FROM dbo.tbl_AppraisalCycles WHERE CompanyId = @CompanyId AND CycleStatus = N'Active');
    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @Emp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1002');
    DECLARE @Emp3 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');

    IF @ActiveCycleId IS NOT NULL AND @Emp1 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_PerformanceGoals (CompanyId, CycleId, EmployeeId, GoalTitle, KpiMetric, WeightPercent, TargetValue, AchievedValue, ProgressPercent, GoalStatus, ManagerNotes)
        VALUES
            (@CompanyId, @ActiveCycleId, @Emp1, N'Platform Uptime & API Latency', N'99.95% SLA & p95 < 180ms', 50.00, N'99.95%', N'99.97%', 100, N'Completed', N'Exceeded SLA targets across Q1-Q4.'),
            (@CompanyId, @ActiveCycleId, @Emp1, N'Zero-Downtime Payroll Engine Migration', N'Cutover with 0 billing incidents', 50.00, N'100% Rollout', N'85% Complete', 85, N'InProgress', N'On track for year-end completion.');

        INSERT INTO dbo.tbl_PerformanceAppraisals (
            CompanyId, CycleId, EmployeeId, ReviewerEmployeeId, SelfRating, SelfComments, SelfSubmittedDate,
            ManagerRating, FinalRating, RatingBand, Strengths, ImprovementAreas, ManagerComments,
            RecommendedIncrementPercent, AppraisalStatus, CompletedDate
        )
        VALUES
            (@CompanyId, @ActiveCycleId, @Emp1, @Emp3, 4.60, N'Led core platform modernization and mentored 3 engineers.', SYSUTCDATETIME(),
             4.70, 4.65, N'Outstanding', N'System architecture, reliability ownership, cross-team mentoring.', N'Delegate routine code reviews earlier.', N'Consistent top-tier engineering leadership.',
             8.50, N'Completed', SYSUTCDATETIME());
    END

    IF @ActiveCycleId IS NOT NULL AND @Emp2 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_PerformanceGoals (CompanyId, CycleId, EmployeeId, GoalTitle, KpiMetric, WeightPercent, TargetValue, AchievedValue, ProgressPercent, GoalStatus)
        VALUES
            (@CompanyId, @ActiveCycleId, @Emp2, N'Automated Tax Compliance Audit Readiness', N'100% accuracy across monthly payroll runs', 60.00, N'12 Clean Runs', N'10 Clean Runs', 80, N'InProgress');

        INSERT INTO dbo.tbl_PerformanceAppraisals (
            CompanyId, CycleId, EmployeeId, ReviewerEmployeeId, SelfRating, SelfComments, SelfSubmittedDate, AppraisalStatus
        )
        VALUES
            (@CompanyId, @ActiveCycleId, @Emp2, @Emp1, 4.20, N'Streamlined monthly payroll reconciliation and automated statutory exports.', SYSUTCDATETIME(), N'SelfSubmitted');
    END

    IF @ActiveCycleId IS NOT NULL AND @Emp3 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_PerformanceAppraisals (CompanyId, CycleId, EmployeeId, ReviewerEmployeeId, AppraisalStatus)
        VALUES (@CompanyId, @ActiveCycleId, @Emp3, @Emp1, N'NotStarted');
    END
END

IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    INSERT INTO dbo.tbl_TrainingPrograms (
        CompanyId, ProgramCode, ProgramTitle, Category, DeliveryMode, DurationHours,
        TrainerOrProvider, StartDate, EndDate, MaxParticipants, LocationOrUrl, IsMandatory, ProgramStatus, Description
    )
    VALUES
        (@CompanyId, N'TRN-SEC-101', N'Enterprise Security & Data Privacy Compliance', N'Compliance', N'Online',    3.5, N'InfoSec Governance Team', DATEADD(DAY, -15, @Today), DATEADD(DAY, 15, @Today), 100, N'https://lms.acme-enterprise.example/security-101', 1, N'Ongoing', N'Annual mandatory training covering secure coding, GDPR, and phishing defense.'),
        (@CompanyId, N'TRN-ARC-201', N'Distributed SQL Server & High-Throughput Dapper', N'Technical',  N'Workshop', 12.0, N'Staff Engineering Guild', DATEADD(DAY, 10, @Today),  DATEADD(DAY, 12, @Today), 25,  N'HQ Auditorium B', 0, N'Planned', N'Hands-on tuning workshop for stored procedures, indexing, and connection pooling.'),
        (@CompanyId, N'TRN-LDR-301', N'People Leadership & Coaching Excellence',          N'Leadership', N'Hybrid',    8.0, N'Talent & OD Partners',    DATEADD(DAY, -40, @Today), DATEADD(DAY, -38, @Today), 20, N'Executive Boardroom', 0, N'Completed', N'Practical coaching frameworks for new and existing department managers.');

    DECLARE @ProgSec INT = (SELECT TOP 1 ProgramId FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId AND ProgramCode = N'TRN-SEC-101');
    DECLARE @ProgArc INT = (SELECT TOP 1 ProgramId FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId AND ProgramCode = N'TRN-ARC-201');
    DECLARE @ProgLdr INT = (SELECT TOP 1 ProgramId FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId AND ProgramCode = N'TRN-LDR-301');
    DECLARE @TEmp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @TEmp2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1002');
    DECLARE @TEmp3 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');

    IF @ProgSec IS NOT NULL AND @TEmp1 IS NOT NULL AND @TEmp2 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_TrainingEnrollments (CompanyId, ProgramId, EmployeeId, AttendanceStatus, AssessmentScore, CertificateNumber, CompletionDate, FeedbackRating, FeedbackNotes)
        VALUES
            (@CompanyId, @ProgSec, @TEmp1, N'Completed', 96.00, N'CERT-SEC-2026-001', DATEADD(DAY, -5, @Today), 5, N'Clear and practical security scenarios.'),
            (@CompanyId, @ProgSec, @TEmp2, N'Enrolled',  NULL,  NULL,                 NULL,                     NULL, NULL);
    END

    IF @ProgArc IS NOT NULL AND @TEmp1 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_TrainingEnrollments (CompanyId, ProgramId, EmployeeId, AttendanceStatus)
        VALUES (@CompanyId, @ProgArc, @TEmp1, N'Enrolled');
    END

    IF @ProgLdr IS NOT NULL AND @TEmp3 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_TrainingEnrollments (CompanyId, ProgramId, EmployeeId, AttendanceStatus, AssessmentScore, CertificateNumber, CompletionDate, FeedbackRating, FeedbackNotes)
        VALUES (@CompanyId, @ProgLdr, @TEmp3, N'Completed', 92.50, N'CERT-LDR-2026-014', DATEADD(DAY, -38, @Today), 5, N'Valuable 1-on-1 feedback roleplays.');
    END
END
GO
