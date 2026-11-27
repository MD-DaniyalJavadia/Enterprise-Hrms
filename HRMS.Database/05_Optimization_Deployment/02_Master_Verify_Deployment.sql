USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 15: Post-Deployment Schema, Stored Procedure & Seed Verification Script
-- ============================================================================
SET NOCOUNT ON;

PRINT N'=== Enterprise HRMS Deployment Verification ===';

-- 1. Verify Core & Module Tables
DECLARE @ExpectedTables TABLE (TableName SYSNAME NOT NULL);
INSERT INTO @ExpectedTables (TableName) VALUES
    (N'tbl_Companies'), (N'tbl_Branches'), (N'tbl_Departments'), (N'tbl_Designations'), (N'tbl_Employees'),
    (N'tbl_Roles'), (N'tbl_Permissions'), (N'tbl_RolePermissions'), (N'tbl_Users'), (N'tbl_UserRoles'),
    (N'tbl_LoginHistory'), (N'tbl_AuditLogs'), (N'tbl_Notifications'), (N'tbl_SystemSettings'),
    (N'tbl_Attendance'), (N'tbl_LeaveTypes'), (N'tbl_LeaveApplications'), (N'tbl_LeaveBalances'), (N'tbl_LeaveEncashments'),
    (N'tbl_PayrollRuns'), (N'tbl_SalaryComponents'), (N'tbl_TaxSlabs'), (N'tbl_EmployeeSalaryStructures'), (N'tbl_Payslips'),
    (N'tbl_JobRequisitions'), (N'tbl_Candidates'), (N'tbl_JobApplications'), (N'tbl_Interviews'), (N'tbl_JobOffers'),
    (N'tbl_LoanTypes'), (N'tbl_EmployeeLoans'), (N'tbl_LoanRepaymentSchedule'), (N'tbl_ExpenseCategories'), (N'tbl_ExpenseClaims'),
    (N'tbl_AppraisalCycles'), (N'tbl_PerformanceGoals'), (N'tbl_PerformanceAppraisals'), (N'tbl_TrainingPrograms'), (N'tbl_TrainingEnrollments'),
    (N'tbl_ProfileUpdateRequests'), (N'tbl_Assets'), (N'tbl_AssetAllocations'), (N'tbl_CompanyDocuments'), (N'tbl_DocumentAcknowledgments'),
    (N'tbl_SeparationRequests'), (N'tbl_ExitClearanceItems'), (N'tbl_FinalSettlements'),
    (N'tbl_SavedReportTemplates'), (N'tbl_ReportExportLogs'),
    (N'tbl_NotificationTemplates'), (N'tbl_WorkflowDefinitions'), (N'tbl_WorkflowSteps'), (N'tbl_WorkflowInstances'), (N'tbl_WorkflowActionLogs');

SELECT
    e.TableName,
    CASE WHEN OBJECT_ID(N'dbo.' + e.TableName, N'U') IS NOT NULL THEN N'OK' ELSE N'MISSING' END AS TableStatus
FROM @ExpectedTables e
WHERE OBJECT_ID(N'dbo.' + e.TableName, N'U') IS NULL;

-- 2. Summary Counts
SELECT
    (SELECT COUNT(1) FROM sys.tables WHERE is_ms_shipped = 0) AS TotalUserTables,
    (SELECT COUNT(1) FROM sys.procedures WHERE is_ms_shipped = 0 AND name LIKE N'sp[_]%') AS TotalHrmsStoredProcedures,
    (SELECT COUNT(1) FROM dbo.tbl_Permissions) AS SeededPermissions,
    (SELECT COUNT(1) FROM dbo.tbl_Roles WHERE IsDeleted = 0) AS SeededRoles,
    (SELECT COUNT(1) FROM dbo.tbl_SystemSettings) AS SeededSystemSettings;

-- 3. Execute Health Check
EXEC dbo.sp_System_HealthCheck @CompanyId = 1;
GO
