USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 14 Seed Data: Permissions, System Settings, Notification Templates, Workflows
-- ============================================================================

DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE IsDeleted = 0 ORDER BY CompanyId);
IF @CompanyId IS NULL RETURN;

-- 1. Permissions
INSERT INTO dbo.tbl_Permissions (PermissionCode, ModuleName, PageName, ActionName, Description)
SELECT v.PermissionCode, v.ModuleName, v.PageName, v.ActionName, v.Description
FROM (VALUES
    (N'ADMIN.MANAGE',        N'Administration', N'Settings',      N'Manage', N'Manage system settings, users, roles, and permissions'),
    (N'WORKFLOW.VIEW',       N'Workflows',      N'Engine',        N'View',   N'View workflow definitions and active approval pipelines'),
    (N'WORKFLOW.MANAGE',     N'Workflows',      N'Engine',        N'Manage', N'Configure workflow definitions, steps, and advance approvals'),
    (N'NOTIFICATION.MANAGE', N'Notifications',  N'Center',        N'Manage', N'Broadcast announcements and manage notification templates')
) AS v(PermissionCode, ModuleName, PageName, ActionName, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Permissions p WHERE p.PermissionCode = v.PermissionCode);

DECLARE @SuperAdminRoleId INT = (SELECT RoleId FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'SuperAdmin');
DECLARE @HRAdminRoleId    INT = (SELECT RoleId FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'HRAdmin');
DECLARE @PayrollRoleId    INT = (SELECT RoleId FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'PayrollOfficer');
DECLARE @ManagerRoleId    INT = (SELECT RoleId FROM dbo.tbl_Roles WHERE CompanyId = @CompanyId AND RoleCode = N'Manager');

-- SuperAdmin gets all new permissions
INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT @SuperAdminRoleId, p.PermissionId
FROM dbo.tbl_Permissions p
WHERE p.PermissionCode IN (N'ADMIN.VIEW', N'ADMIN.MANAGE', N'WORKFLOW.VIEW', N'WORKFLOW.MANAGE', N'NOTIFICATION.MANAGE')
  AND @SuperAdminRoleId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @SuperAdminRoleId AND rp.PermissionId = p.PermissionId);

-- HRAdmin gets workflow & notification management + admin view
INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT @HRAdminRoleId, p.PermissionId
FROM dbo.tbl_Permissions p
WHERE p.PermissionCode IN (N'ADMIN.VIEW', N'WORKFLOW.VIEW', N'WORKFLOW.MANAGE', N'NOTIFICATION.MANAGE')
  AND @HRAdminRoleId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @HRAdminRoleId AND rp.PermissionId = p.PermissionId);

-- Manager gets WORKFLOW.VIEW
INSERT INTO dbo.tbl_RolePermissions (RoleId, PermissionId)
SELECT @ManagerRoleId, p.PermissionId
FROM dbo.tbl_Permissions p
WHERE p.PermissionCode IN (N'WORKFLOW.VIEW')
  AND @ManagerRoleId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.tbl_RolePermissions rp WHERE rp.RoleId = @ManagerRoleId AND rp.PermissionId = p.PermissionId);

-- 2. Default Enterprise System Settings
INSERT INTO dbo.tbl_SystemSettings (CompanyId, SettingKey, SettingValue, Category, DataType, IsSensitive, Description)
SELECT @CompanyId, v.SettingKey, v.SettingValue, v.Category, v.DataType, 0, v.Description
FROM (VALUES
    (N'Company.FiscalYearStartMonth',      N'1',     N'General',         N'Number',  N'Month number (1-12) when the corporate fiscal year begins'),
    (N'Company.DefaultDateFormat',         N'dd MMM yyyy', N'General',    N'Text',    N'Standard enterprise display format for dates across reports and UI'),
    (N'Security.MaxFailedLoginAttempts',   N'5',     N'Security',        N'Number',  N'Failed sign-in attempts before temporary account lockout'),
    (N'Security.AccountLockoutMinutes',    N'15',    N'Security',        N'Number',  N'Duration in minutes for automatic brute-force lockout'),
    (N'Security.SessionTimeoutMinutes',    N'480',   N'Security',        N'Number',  N'Idle authentication cookie lifetime in minutes'),
    (N'Security.RequirePasswordComplexity',N'true',  N'Security',        N'Boolean', N'Enforce uppercase, lowercase, numeric, and symbol rules'),
    (N'Attendance.DefaultGraceMinutes',    N'15',    N'AttendanceLeave', N'Number',  N'Default check-in grace window before marking status Late'),
    (N'Attendance.AllowEmployeeSelfCheckIn',N'true', N'AttendanceLeave', N'Boolean', N'Allow employees to clock in/out from the Self-Service Portal'),
    (N'Leave.AllowNegativeBalance',        N'false', N'AttendanceLeave', N'Boolean', N'Permit paid leave submission when remaining balance is zero'),
    (N'Payroll.AutoSyncEmployeeBaseSalary',N'true',  N'Payroll',         N'Boolean', N'Synchronize employee master BaseSalary upon Salary Structure updates'),
    (N'Payroll.ProrateMidMonthJoins',      N'true',  N'Payroll',         N'Boolean', N'Automatically prorate earnings for employees hired mid-period'),
    (N'Workflow.DefaultSlaHours',          N'24',    N'Workflow',        N'Number',  N'Default SLA response window per approval stage before escalation'),
    (N'Notifications.RetentionDays',       N'90',    N'Notifications',   N'Number',  N'Number of days to retain read in-app notifications'),
    (N'Notifications.EnableDigestSummary', N'true',  N'Notifications',   N'Boolean', N'Enable daily digest bundling for low-priority operational alerts')
) AS v(SettingKey, SettingValue, Category, DataType, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_SystemSettings s WHERE s.CompanyId = @CompanyId AND s.SettingKey = v.SettingKey
);

-- 3. Default Notification Templates
INSERT INTO dbo.tbl_NotificationTemplates (
    CompanyId, TemplateCode, TemplateName, ModuleName, EventTrigger,
    SubjectTemplate, BodyTemplate, DefaultType, DefaultPriority, SendInApp, SendEmailDigest, IsActive
)
SELECT @CompanyId, v.TemplateCode, v.TemplateName, v.ModuleName, v.EventTrigger,
       v.SubjectTemplate, v.BodyTemplate, v.DefaultType, v.DefaultPriority, 1, v.SendEmailDigest, 1
FROM (VALUES
    (N'LEAVE_DECISION',     N'Leave Request Decision',         N'Leave',       N'OnApproved',    N'Leave Request {{Status}}: {{Period}}',         N'Your leave application for {{Period}} ({{Days}} day(s)) has been {{Status}}.', N'Success', N'Normal', 0),
    (N'PAYSLIP_PUBLISHED',  N'Monthly Payslip Published',      N'Payroll',     N'OnApproved',    N'Payslip Ready for {{PeriodLabel}}',            N'Your monthly salary statement for {{PeriodLabel}} is now available in Self-Service.', N'Info', N'Normal', 1),
    (N'LOAN_DISBURSED',     N'Loan Approval & Disbursement',   N'Loan',        N'OnApproved',    N'Loan {{LoanNumber}} Disbursed',                N'Your loan application {{LoanNumber}} has been approved and the repayment schedule is active.', N'Success', N'High', 0),
    (N'EXIT_CLEARANCE_DUE', N'Separation Clearance Reminder',  N'Exit',        N'OnEscalated',   N'Action Required: Exit Clearance {{CaseNo}}',   N'Pending clearance items require sign-off prior to the employee last working day.', N'Warning', N'High', 1),
    (N'WF_SLA_ESCALATION',  N'Workflow SLA Escalation Notice', N'Workflow',    N'OnEscalated',   N'Escalated Approval: {{ReferenceNumber}}',      N'Request {{ReferenceNumber}} exceeded its step SLA window and was escalated for review.', N'Danger', N'Urgent', 1)
) AS v(TemplateCode, TemplateName, ModuleName, EventTrigger, SubjectTemplate, BodyTemplate, DefaultType, DefaultPriority, SendEmailDigest)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_NotificationTemplates t WHERE t.CompanyId = @CompanyId AND t.TemplateCode = v.TemplateCode
);

-- 4. Default Approval Workflows, Steps & Sample Instances
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_WorkflowDefinitions WHERE CompanyId = @CompanyId AND WorkflowCode = N'WF-LEAVE-STD')
BEGIN
    INSERT INTO dbo.tbl_WorkflowDefinitions (CompanyId, WorkflowCode, WorkflowName, ModuleName, MinAmountThreshold, AutoEscalateHours, Description, IsActive)
    VALUES
        (@CompanyId, N'WF-LEAVE-STD',    N'Standard Two-Tier Leave Approval',       N'Leave',      NULL,     48, N'Line manager reviews first; HR Admin performs policy verification for extended leave.', 1),
        (@CompanyId, N'WF-LOAN-EXEC',    N'High-Value Loan & Advance Approval',     N'Loan',       2500.00,  36, N'Requires Reporting Manager, HR Review, and Payroll Officer disbursement clearance.', 1),
        (@CompanyId, N'WF-EXIT-CLEAR',   N'Offboarding & Separation Clearance Flow',N'Separation', NULL,     72, N'Structured Manager sign-off followed by HR & Finance final settlement verification.', 1);

    DECLARE @WfLeave INT = (SELECT WorkflowId FROM dbo.tbl_WorkflowDefinitions WHERE CompanyId = @CompanyId AND WorkflowCode = N'WF-LEAVE-STD');
    DECLARE @WfLoan  INT = (SELECT WorkflowId FROM dbo.tbl_WorkflowDefinitions WHERE CompanyId = @CompanyId AND WorkflowCode = N'WF-LOAN-EXEC');
    DECLARE @WfExit  INT = (SELECT WorkflowId FROM dbo.tbl_WorkflowDefinitions WHERE CompanyId = @CompanyId AND WorkflowCode = N'WF-EXIT-CLEAR');

    INSERT INTO dbo.tbl_WorkflowSteps (CompanyId, WorkflowId, StepOrder, StepName, ApproverType, ApproverRoleId, EscalationRoleId, SlaHours, CanDelegate, IsMandatory)
    VALUES
        (@CompanyId, @WfLeave, 1, N'Direct Manager Approval',      N'ReportingManager', @ManagerRoleId, @HRAdminRoleId, 24, 1, 1),
        (@CompanyId, @WfLeave, 2, N'HR Policy Verification',       N'RoleBased',        @HRAdminRoleId, @SuperAdminRoleId, 24, 1, 1),

        (@CompanyId, @WfLoan,  1, N'Line Manager Endorsement',     N'ReportingManager', @ManagerRoleId, @HRAdminRoleId, 24, 1, 1),
        (@CompanyId, @WfLoan,  2, N'HR Eligibility Check',         N'RoleBased',        @HRAdminRoleId, @SuperAdminRoleId, 24, 1, 1),
        (@CompanyId, @WfLoan,  3, N'Payroll Disbursement Sign-off',N'RoleBased',        @PayrollRoleId, @SuperAdminRoleId, 24, 0, 1),

        (@CompanyId, @WfExit,  1, N'Manager Handover Sign-Off',    N'ReportingManager', @ManagerRoleId, @HRAdminRoleId, 48, 1, 1),
        (@CompanyId, @WfExit,  2, N'HR Clearance Audit',           N'RoleBased',        @HRAdminRoleId, @SuperAdminRoleId, 48, 1, 1),
        (@CompanyId, @WfExit,  3, N'Finance F&F Settlement Check', N'RoleBased',        @PayrollRoleId, @SuperAdminRoleId, 24, 0, 1);

    DECLARE @SampleUserId INT = (SELECT TOP 1 UserId FROM dbo.tbl_Users WHERE CompanyId = @CompanyId AND IsDeleted = 0 ORDER BY UserId);
    DECLARE @SampleEmpId  INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND IsDeleted = 0 ORDER BY EmployeeId);

    INSERT INTO dbo.tbl_WorkflowInstances (
        CompanyId, WorkflowId, ModuleName, EntityId, ReferenceNumber,
        RequestedByUserId, RequestedByEmployeeId, CurrentStepOrder, TotalSteps,
        InstanceStatus, DueAtUtc, Summary
    )
    VALUES
        (@CompanyId, @WfLeave, N'Leave',      N'1', N'LV-2026-0101', @SampleUserId, @SampleEmpId, 2, 2, N'InProgress', DATEADD(HOUR, 18, SYSUTCDATETIME()), N'Annual leave request awaiting final HR verification'),
        (@CompanyId, @WfLoan,  N'Loan',       N'1', N'LN-2026-0001', @SampleUserId, @SampleEmpId, 2, 3, N'InProgress', DATEADD(HOUR, 20, SYSUTCDATETIME()), N'Personal medical advance above $2,500 threshold'),
        (@CompanyId, @WfExit,  N'Separation', N'1', N'EXT-2026-0001',@SampleUserId, @SampleEmpId, 3, 3, N'Approved',   DATEADD(DAY, -1, SYSUTCDATETIME()),  N'Exit handover and clearance audit completed');

    DECLARE @Inst1 BIGINT = (SELECT TOP 1 InstanceId FROM dbo.tbl_WorkflowInstances WHERE CompanyId = @CompanyId AND ReferenceNumber = N'LV-2026-0101');
    DECLARE @Inst2 BIGINT = (SELECT TOP 1 InstanceId FROM dbo.tbl_WorkflowInstances WHERE CompanyId = @CompanyId AND ReferenceNumber = N'LN-2026-0001');

    IF @Inst1 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_WorkflowActionLogs (CompanyId, InstanceId, StepOrder, StepName, ActionTaken, ActorUserId, ActorName, Comments)
        VALUES
            (@CompanyId, @Inst1, 1, N'Direct Manager Approval', N'Submitted', @SampleUserId, N'System Seed', N'Leave application submitted for approval.'),
            (@CompanyId, @Inst1, 1, N'Direct Manager Approval', N'Approved',  @SampleUserId, N'Department Manager', N'Team coverage confirmed.');
    END
    IF @Inst2 IS NOT NULL
    BEGIN
        INSERT INTO dbo.tbl_WorkflowActionLogs (CompanyId, InstanceId, StepOrder, StepName, ActionTaken, ActorUserId, ActorName, Comments)
        VALUES
            (@CompanyId, @Inst2, 1, N'Line Manager Endorsement', N'Submitted', @SampleUserId, N'System Seed', N'Loan request routed to multi-stage workflow.');
    END
END
GO
