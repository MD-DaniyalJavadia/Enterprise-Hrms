USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 7: Recruitment Management Seed Data
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions WHERE PermissionCode = N'RECRUITMENT.MANAGE')
BEGIN
    INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
    VALUES (N'RECRUITMENT.MANAGE', N'Recruitment', N'Manage', N'Manage', N'Manage job requisitions, candidates, interviews, offers, and onboarding');
END
GO

DECLARE @RecManageId INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'RECRUITMENT.MANAGE');
DECLARE @RecViewId   INT = (SELECT PermissionId FROM dbo.tbl_Permissions WHERE PermissionCode = N'RECRUITMENT.VIEW');

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @RecManageId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin')
  AND r.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @RecManageId);

INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, @RecViewId
FROM dbo.tbl_Roles r
WHERE r.RoleCode IN (N'SuperAdmin', N'HRAdmin', N'Manager')
  AND r.IsDeleted = 0
  AND @RecViewId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = r.RoleId AND rp.PermissionId = @RecViewId);
GO

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01');
IF @CompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_JobRequisitions WHERE CompanyId = @CompanyId)
BEGIN
    DECLARE @BranchNyc INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'HQ-NYC');
    DECLARE @BranchSfo INT = (SELECT TOP 1 BranchId FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = N'BR-SFO');
    DECLARE @DepEng INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'ENG');
    DECLARE @DepHr  INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'HR');
    DECLARE @DepSls INT = (SELECT TOP 1 DepartmentId FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = N'SLS');
    DECLARE @DesEng INT = (SELECT TOP 1 DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SWE-SR');
    DECLARE @DesHr  INT = (SELECT TOP 1 DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'HR-MGR');
    DECLARE @DesSls INT = (SELECT TOP 1 DesignationId FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = N'SLS-EX');
    DECLARE @Mgr1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    DECLARE @Mgr2 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1003');
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    INSERT INTO dbo.tbl_JobRequisitions (
        CompanyId, RequisitionCode, JobTitle, BranchId, DepartmentId, DesignationId, HiringManagerId,
        EmploymentType, WorkMode, OpeningsCount, MinSalary, MaxSalary, ExperienceMinYears, ExperienceMaxYears,
        TargetHireDate, RequisitionStatus, JobSummary, RequiredSkills, ApprovedBy, ApprovedDate
    )
    VALUES
        (@CompanyId, N'REQ-1001', N'Senior .NET Platform Engineer', @BranchNyc, @DepEng, @DesEng, @Mgr1, N'Full-Time', N'Hybrid', 2, 125000, 155000, 5, 9, DATEADD(DAY, 30, @Today), N'Open', N'Build enterprise APIs and distributed SQL Server data pipelines.', N'C#, .NET 8, Dapper, SQL Server, Azure', 1, SYSUTCDATETIME()),
        (@CompanyId, N'REQ-1002', N'Talent Acquisition Partner', @BranchNyc, @DepHr, @DesHr, @Mgr2, N'Full-Time', N'On-Site', 1, 95000, 115000, 3, 6, DATEADD(DAY, 21, @Today), N'Open', N'Lead technical recruiting pipelines across engineering and product.', N'Full-Cycle Recruiting, ATS, Sourcing', 1, SYSUTCDATETIME()),
        (@CompanyId, N'REQ-1003', N'Enterprise Account Executive', @BranchSfo, @DepSls, @DesSls, NULL, N'Full-Time', N'Remote', 2, 100000, 130000, 4, 8, DATEADD(DAY, 45, @Today), N'PendingApproval', N'Drive Fortune 500 SaaS expansion on the West Coast.', N'B2B SaaS Sales, Negotiation, CRM', NULL, NULL);

    INSERT INTO dbo.tbl_Candidates (
        CompanyId, FirstName, LastName, Email, Phone, CurrentCompany, CurrentJobTitle,
        TotalExperienceYears, ExpectedSalary, Skills, Source, Notes
    )
    VALUES
        (@CompanyId, N'Julian',  N'Mercer',  N'julian.mercer@candidate.example',  N'+1-917-555-0141', N'CloudScale Inc.', N'Staff Software Engineer', 7.5, 145000, N'C#, .NET 8, SQL Server, Dapper, Microservices', N'LinkedIn', N'Strong SQL optimization background.'),
        (@CompanyId, N'Hannah',  N'Lin',     N'hannah.lin@candidate.example',     N'+1-415-555-0192', N'FinCore Systems', N'Senior Backend Developer', 5.0, 132000, N'C#, ASP.NET Core, REST APIs, Redis', N'Referral', N'Referred by Elena Rostova.'),
        (@CompanyId, N'Gabriel', N'Ortiz',   N'gabriel.ortiz@candidate.example',  N'+1-646-555-0134', N'PeopleFirst Corp', N'Technical Recruiter', 4.5, 105000, N'Technical Sourcing, Employer Branding, ATS', N'CareerPortal', N'Great executive presence.'),
        (@CompanyId, N'Priya',   N'Nair',    N'priya.nair@candidate.example',     N'+1-212-555-0177', N'Vertex Analytics', N'Software Engineer II', 3.5, 122000, N'C#, SQL Server, JavaScript, Docker', N'Direct', N'Solid fundamentals; scheduled for screening.');

    DECLARE @Req1 INT = (SELECT RequisitionId FROM dbo.tbl_JobRequisitions WHERE CompanyId = @CompanyId AND RequisitionCode = N'REQ-1001');
    DECLARE @Req2 INT = (SELECT RequisitionId FROM dbo.tbl_JobRequisitions WHERE CompanyId = @CompanyId AND RequisitionCode = N'REQ-1002');
    DECLARE @C1 INT = (SELECT CandidateId FROM dbo.tbl_Candidates WHERE CompanyId = @CompanyId AND Email = N'julian.mercer@candidate.example');
    DECLARE @C2 INT = (SELECT CandidateId FROM dbo.tbl_Candidates WHERE CompanyId = @CompanyId AND Email = N'hannah.lin@candidate.example');
    DECLARE @C3 INT = (SELECT CandidateId FROM dbo.tbl_Candidates WHERE CompanyId = @CompanyId AND Email = N'gabriel.ortiz@candidate.example');
    DECLARE @C4 INT = (SELECT CandidateId FROM dbo.tbl_Candidates WHERE CompanyId = @CompanyId AND Email = N'priya.nair@candidate.example');

    INSERT INTO dbo.tbl_JobApplications (CompanyId, RequisitionId, CandidateId, PipelineStage, MatchScore, StageUpdatedDate)
    VALUES
        (@CompanyId, @Req1, @C1, N'Offer',     94, SYSUTCDATETIME()),
        (@CompanyId, @Req1, @C2, N'Interview', 88, SYSUTCDATETIME()),
        (@CompanyId, @Req2, @C3, N'Screening', 91, SYSUTCDATETIME()),
        (@CompanyId, @Req1, @C4, N'Applied',   79, SYSUTCDATETIME());

    DECLARE @App1 INT = (SELECT ApplicationId FROM dbo.tbl_JobApplications WHERE RequisitionId = @Req1 AND CandidateId = @C1);
    DECLARE @App2 INT = (SELECT ApplicationId FROM dbo.tbl_JobApplications WHERE RequisitionId = @Req1 AND CandidateId = @C2);

    INSERT INTO dbo.tbl_Interviews (
        CompanyId, ApplicationId, InterviewRound, InterviewMode, ScheduledStart, DurationMinutes,
        LocationOrLink, InterviewerEmployeeId, InterviewStatus, TechnicalScore, CommunicationScore, OverallRecommendation, FeedbackNotes
    )
    VALUES
        (@CompanyId, @App1, N'Final Panel', N'Video', DATEADD(DAY, -3, SYSUTCDATETIME()), 60, N'https://meet.acme.example/panel-101', @Mgr1, N'Completed', 5, 5, N'StrongHire', N'Outstanding system design and SQL performance expertise.'),
        (@CompanyId, @App2, N'Technical Round', N'Video', DATEADD(DAY, 2, DATEADD(HOUR, 14, CAST(@Today AS DATETIME2(0)))), 45, N'https://meet.acme.example/tech-204', @Mgr1, N'Scheduled', NULL, NULL, NULL, NULL);

    INSERT INTO dbo.tbl_JobOffers (
        CompanyId, ApplicationId, OfferCode, OfferedSalaryAnnual, JoiningDate, OfferExpiryDate,
        EmploymentType, OfferStatus, BenefitsSummary, SentDate, RespondedDate
    )
    VALUES
        (@CompanyId, @App1, N'OFR-1001', 145000.00, DATEADD(DAY, 14, @Today), DATEADD(DAY, 7, @Today), N'Full-Time', N'Accepted', N'Full health/dental coverage, 401k match, 20 days annual PTO.', DATEADD(DAY, -2, SYSUTCDATETIME()), SYSUTCDATETIME());
END
GO
