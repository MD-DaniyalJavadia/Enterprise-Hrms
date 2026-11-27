# Changelog

## [1.0.0-phase1] - 2026-11-25
### Added
- Created `HRMS.sln` with `HRMS.Models`, `HRMS.Data`, `HRMS.Business`, and `HRMS.Web` (.NET 8).
- Created `HRMS.Database` scripts for `HRMS_DB` core multi-tenant tables, RBAC/security tables, operational summary tables, stored procedures, and initial seed data.
- Implemented `Pbkdf2PasswordHasher`, `AuthService`, `DashboardService`, `AuditLogService`, and `NotificationService`.
- Implemented Cookie Authentication, login rate limiting, account lockout after 5 failed attempts, Anti-Forgery CSRF protection, and `[HasPermission]` authorization.
- Built `_Layout.cshtml`, `_Sidebar.cshtml`, `_TopNavbar.cshtml`, `_AuthLayout.cshtml`, `Account/*` views, `Error/Index.cshtml`, and `Dashboard/Index.cshtml` with Chart.js and Light/Dark theme support.

## [1.1.0-phase2] - 2026-11-25
### Added
- Created `04_Organization_Extended_Tables.sql` (`tbl_JobGrades`, `tbl_CostCenters`, `tbl_WorkLocations`, `tbl_JobPositions`, `tbl_Shifts`).
- Added stored procedures `04_Company_Branch_StoredProcedures.sql`, `05_Department_Designation_StoredProcedures.sql`, `06_OrgStructure_StoredProcedures.sql`, and seed data `02_OrganizationSeedData.sql`.
- Implemented `ICompanyRepository`, `IBranchRepository`, `IDepartmentRepository`, `IDesignationRepository`, `IOrgStructureRepository` and their business services with circular-hierarchy checks, salary-band validation, and audit logging.
- Built `CompanyController`, `BranchController`, `DepartmentController`, `DesignationController`, and `OrganizationController` with Razor views, DataTables, Select2, SweetAlert2 confirmations, formula-safe CSV export, and interactive Org Chart views.

## [1.2.0-phase3] - 2026-11-25
### Added
- Created `05_Employee_Profile_Tables.sql` extending `dbo.tbl_Employees` and adding `tbl_EmployeeEmergencyContacts`, `tbl_EmployeeFamilyMembers`, `tbl_EmployeeEducation`, `tbl_EmployeeExperience`, `tbl_EmployeeSkills`, `tbl_EmployeeDocuments`, `tbl_EmployeeEmploymentHistory`, and `tbl_EmployeeSalaryHistory`.
- Added stored procedures `07_Employee_Master_StoredProcedures.sql` (`sp_Employee_GenerateNextCode`, `sp_Employee_GetAll`, `sp_Employee_GetById`, `sp_Employee_Insert`, `sp_Employee_Update`, `sp_Employee_Delete`, `sp_Employee_RecordCareerAction`), `08_Employee_SubModules_StoredProcedures.sql`, and seed data `03_EmployeeSeedData.sql`.
- Implemented `IEmployeeRepository`/`EmployeeRepository` (Dapper multi-result-set and transaction support), `IEmployeeService`/`EmployeeService` (including CSV bulk import parser), and `FileStorageHelper` (image/document validation and confined storage).
- Built `EmployeeController`, `wwwroot/js/employee.js`, and complete Razor views (`Index`, `Directory`, `Create`, `Edit`, `_EmployeeForm`, `Details`, `IdCard`, `Import`) plus sidebar navigation updates.

## [1.3.0-phase4] - 2026-11-25
### Added
- Created `06_Attendance_Shift_Tables.sql` extending `dbo.tbl_Attendance` and creating `tbl_ShiftAssignments`, `tbl_Holidays`, `tbl_AttendanceRegularizations`, and `tbl_OvertimeRequests`.
- Added stored procedures `09_Attendance_StoredProcedures.sql` (`sp_Attendance_GetDailyRegister`, `sp_Attendance_CheckIn`, `sp_Attendance_CheckOut`, `sp_Attendance_MarkManual`, `sp_Attendance_GetMonthlyReport`, `sp_Attendance_GetEmployeeMonthLog`) and `10_Shift_Holiday_Regularization_StoredProcedures.sql` (`sp_ShiftAssignment_*`, `sp_Holiday_*`, `sp_AttendanceRegularization_*`, `sp_OvertimeRequest_*`).
- Added seed script `04_AttendanceSeedData.sql` with `ATTENDANCE.MANAGE` permission, shift roster assignments, company holidays, historical attendance logs, and sample regularization/overtime requests.
- Implemented `IAttendanceRepository`/`AttendanceRepository`, `IAttendanceService`/`AttendanceService` (including CSV attendance import), `AttendanceController`, `wwwroot/js/attendance.js`, and 7 Razor views (`Index`, `MonthlyReport`, `EmployeeCalendar`, `Roster`, `Holidays`, `Regularizations`, `Import`).

## [1.4.0-phase5] - 2026-11-25
### Added
- Created `07_Leave_Management_Tables.sql` extending `tbl_LeaveTypes` and `tbl_LeaveApplications` and creating `tbl_LeaveBalances` and `tbl_LeaveEncashments`.
- Added stored procedures `11_Leave_Types_Balances_StoredProcedures.sql` (`sp_LeaveType_*`, `sp_LeaveBalance_*`, `sp_LeaveEncashment_*`) and `12_Leave_Applications_StoredProcedures.sql` (`sp_LeaveApplication_GetAll`, `sp_LeaveApplication_GetById`, `sp_LeaveApplication_Submit`, `sp_LeaveApplication_Review`, `sp_LeaveApplication_Cancel`, `sp_Leave_GetCalendarEvents`).
- Added seed script `05_LeaveSeedData.sql` with `LEAVE.MANAGE` permission, standard enterprise leave policies (`AL`, `SL`, `CL`, `ML`, `LWP`), annual leave balance allocations, reconciled usage counts, and a sample encashment request.
- Extended `FileStorageHelper` with `SaveLeaveAttachmentAsync` and `ResolveLeaveAttachmentPath` for private attachment storage under `App_Data/LeaveAttachments`.
- Implemented `ILeaveRepository`/`LeaveRepository`, `ILeaveService`/`LeaveService`, `LeaveController`, `wwwroot/js/leave.js`, and 5 Razor views (`Index`, `Balances`, `Types`, `Calendar`, `Encashment`) plus sidebar navigation updates.

## [1.5.0-phase6] - 2026-11-25
### Added
- Created `08_Payroll_Management_Tables.sql` extending `tbl_PayrollRuns` and creating `tbl_SalaryComponents`, `tbl_TaxSlabs`, `tbl_EmployeeSalaryStructures`, `tbl_EmployeeSalaryStructureItems`, `tbl_Payslips`, and `tbl_PayslipLines`.
- Added stored procedures `13_Payroll_Setup_StoredProcedures.sql` (`sp_SalaryComponent_*`, `sp_TaxSlab_*`, `sp_EmployeeSalaryStructure_*`) and `14_Payroll_Processing_StoredProcedures.sql` (`sp_PayrollRun_GetAll`, `sp_PayrollRun_Generate`, `sp_PayrollRun_GetDetails`, `sp_PayrollRun_UpdateStatus`, `sp_PayrollRun_Delete`, `sp_Payslip_Adjust`, `sp_Payslip_GetById`, `sp_Payroll_GetBankTransferList`).
- Added seed script `06_PayrollSeedData.sql` with `PAYROLL.MANAGE` permission, standard earnings/deductions/employer contribution components, progressive annual tax slabs, employee salary structures, and itemized payslips for the current period.
- Implemented `IPayrollRepository`/`PayrollRepository`, `IPayrollService`/`PayrollService`, `PayrollController`, `wwwroot/js/payroll.js`, and 5 Razor views (`Index`, `Details`, `Payslip`, `Structures`, `Components`) plus sidebar navigation and DI updates.

## [1.6.0-phase7] - 2026-11-25
### Added
- Created `09_Recruitment_Tables.sql` (`tbl_JobRequisitions`, `tbl_Candidates`, `tbl_JobApplications`, `tbl_Interviews`, `tbl_JobOffers`).
- Added stored procedures `15_Recruitment_Requisitions_StoredProcedures.sql` (`sp_JobRequisition_GetAll`, `sp_JobRequisition_Save`, `sp_JobRequisition_UpdateStatus`, `sp_JobRequisition_Delete`, `sp_Candidate_GetAll`, `sp_Candidate_Save`, `sp_Candidate_GetResume`, `sp_Candidate_Delete`) and `16_Recruitment_Pipeline_StoredProcedures.sql` (`sp_JobApplication_GetPipeline`, `sp_JobApplication_Submit`, `sp_JobApplication_MoveStage`, `sp_Interview_GetAll`, `sp_Interview_Schedule`, `sp_Interview_SubmitFeedback`, `sp_JobOffer_GetAll`, `sp_JobOffer_Save`, `sp_JobOffer_UpdateStatus`, `sp_JobOffer_ConvertToEmployee`).
- Added seed script `07_RecruitmentSeedData.sql` with `RECRUITMENT.MANAGE` permission, sample job requisitions, candidates, pipeline applications, scheduled/completed interviews, and an accepted job offer ready for onboarding.
- Extended `FileStorageHelper` with `SaveCandidateResumeAsync` and `ResolveCandidateResumePath` for private resume storage under `App_Data/CandidateResumes`.
- Implemented `IRecruitmentRepository`/`RecruitmentRepository`, `IRecruitmentService`/`RecruitmentService`, `RecruitmentController`, `wwwroot/js/recruitment.js`, and 5 Razor views (`Index`, `Candidates`, `Pipeline`, `Interviews`, `Offers`) plus sidebar navigation and DI updates.

## [1.7.0-phase8] - 2026-11-25
### Added
- Created `10_Loan_Expense_Tables.sql` (`tbl_LoanTypes`, `tbl_EmployeeLoans`, `tbl_LoanRepaymentSchedule`, `tbl_ExpenseCategories`, `tbl_ExpenseClaims`).
- Added stored procedures `17_Loan_Management_StoredProcedures.sql` (`sp_LoanType_*`, `sp_EmployeeLoan_GetAll`, `sp_EmployeeLoan_GetById`, `sp_EmployeeLoan_Submit`, `sp_EmployeeLoan_Review`, `sp_EmployeeLoan_RecordRepayment`) and `18_Expense_Management_StoredProcedures.sql` (`sp_ExpenseCategory_*`, `sp_ExpenseClaim_GetAll`, `sp_ExpenseClaim_GetById`, `sp_ExpenseClaim_Submit`, `sp_ExpenseClaim_Review`).
- Added seed script `08_LoanExpenseSeedData.sql` with `LOAN.MANAGE` and `EXPENSE.MANAGE` permissions, loan/advance policies, expense categories, sample loans with EMI schedule, and sample expense claims.
- Extended `FileStorageHelper` with `SaveExpenseReceiptAsync` and `ResolveExpenseReceiptPath` for private receipt storage under `App_Data/ExpenseReceipts`.
- Implemented `ILoanExpenseRepository`/`LoanExpenseRepository`, `ILoanExpenseService`/`LoanExpenseService`, `LoanController`, `ExpenseController`, `wwwroot/js/loan-expense.js`, and 5 Razor views (`Loan/Index`, `Loan/Details`, `Loan/Types`, `Expense/Index`, `Expense/Categories`) plus sidebar navigation and DI updates.

## [1.8.0-phase9] - 2026-11-25
### Added
- Created `11_Performance_Training_Tables.sql` (`tbl_AppraisalCycles`, `tbl_PerformanceGoals`, `tbl_PerformanceAppraisals`, `tbl_TrainingPrograms`, `tbl_TrainingEnrollments`).
- Added stored procedures `19_Performance_Management_StoredProcedures.sql` (`sp_AppraisalCycle_*`, `sp_PerformanceGoal_*`, `sp_PerformanceAppraisal_GetAll`, `sp_PerformanceAppraisal_InitCycle`, `sp_PerformanceAppraisal_SubmitSelf`, `sp_PerformanceAppraisal_SubmitManagerReview`) and `20_Training_Management_StoredProcedures.sql` (`sp_TrainingProgram_*`, `sp_TrainingEnrollment_*`).
- Added seed script `09_PerformanceTrainingSeedData.sql` with `PERFORMANCE.MANAGE` and `TRAINING.MANAGE` permissions, appraisal cycles, sample KPI goals, appraisals, training courses, and certifications.
- Implemented `IPerformanceTrainingRepository`/`PerformanceTrainingRepository`, `IPerformanceTrainingService`/`PerformanceTrainingService`, `PerformanceController`, `TrainingController`, `wwwroot/js/performance-training.js`, and 5 Razor views (`Performance/Index`, `Performance/Goals`, `Performance/Cycles`, `Training/Index`, `Training/Enrollments`) plus sidebar navigation and DI updates.

## [1.9.0-phase10] - 2026-11-25
### Added
- Created `12_SelfService_Portal_Tables.sql` (`tbl_ProfileUpdateRequests`) and stored procedures `21_SelfService_Portal_StoredProcedures.sql` (`sp_ESS_GetDashboard`, `sp_ESS_GetMyPayslips`, `sp_ESS_GetMyAttendance`, `sp_ProfileUpdateRequest_Submit`, `sp_ProfileUpdateRequest_Cancel`, `sp_ProfileUpdateRequest_Review`) and `22_Manager_Portal_StoredProcedures.sql` (`sp_Manager_GetDashboard`, `sp_Manager_GetTeamRoster`, `sp_Manager_GetPendingApprovals`, `sp_Manager_GetRequestOwner`, `sp_Manager_VerifyTeamMember`).
- Added seed script `10_SelfServiceManagerSeedData.sql` granting `ESS.VIEW` and `MANAGER.VIEW` permissions to `Employee`/`Manager` roles, configuring the employee reporting hierarchy (`ReportingManagerId`), and seeding sample profile update requests.
- Implemented `IPortalRepository`/`PortalRepository`, `IPortalService`/`PortalService`, `SelfServiceController`, `ManagerPortalController`, `wwwroot/js/portal.js`, and 7 Razor views (`SelfService/Index`, `SelfService/MyProfile`, `SelfService/MyAttendance`, `SelfService/MyPayslips`, `ManagerPortal/Index`, `ManagerPortal/Team`, `ManagerPortal/Approvals`).
- Updated `_Sidebar.cshtml`, `DependencyInjection.cs`, `_ViewImports.cshtml`, `Payroll/Payslip.cshtml`, and `AccountController.cs` role-aware landing redirects.

## [1.10.0-phase11] - 2026-11-25
### Added
- Created `13_Asset_Document_Tables.sql` (`tbl_AssetCategories`, `tbl_Assets`, `tbl_AssetAllocations`, `tbl_AssetMaintenanceLogs`, `tbl_DocumentCategories`, `tbl_CompanyDocuments`, `tbl_DocumentAcknowledgments`).
- Added stored procedures `23_Asset_Management_StoredProcedures.sql` (`sp_AssetCategory_*`, `sp_Asset_GetAll`, `sp_Asset_GetById`, `sp_Asset_Save`, `sp_Asset_Delete`, `sp_AssetAllocation_GetAll`, `sp_AssetAllocation_Assign`, `sp_AssetAllocation_Return`, `sp_AssetAllocation_Acknowledge`, `sp_AssetMaintenance_*`) and `24_Document_Management_StoredProcedures.sql` (`sp_DocumentCategory_*`, `sp_CompanyDocument_GetAll`, `sp_CompanyDocument_GetById`, `sp_CompanyDocument_Save`, `sp_CompanyDocument_Delete`, `sp_CompanyDocument_Acknowledge`, `sp_CompanyDocument_GetAcknowledgments`, `sp_EmployeeVault_GetAll`).
- Added seed script `11_AssetDocumentSeedData.sql` with `ASSET.MANAGE` and `DOCUMENT.MANAGE` permissions, role mappings, asset categories, IT/access assets, active allocations, maintenance log, document categories, versioned HR policies, and sample acknowledgments.
- Extended `FileStorageHelper` with `SaveCompanyDocumentAsync` and `ResolveCompanyDocumentPath` for private storage under `App_Data/CompanyDocuments`.
- Implemented `IAssetDocumentRepository`/`AssetDocumentRepository`, `IAssetDocumentService`/`AssetDocumentService`, `AssetController`, `DocumentController`, `wwwroot/js/asset-document.js`, and 9 Razor views (`Asset/Index`, `Asset/Details`, `Asset/Allocations`, `Asset/Maintenance`, `Asset/Categories`, `Document/Index`, `Document/Acknowledgments`, `Document/EmployeeVault`, `Document/Categories`) plus sidebar navigation and DI updates.

## [1.11.0-phase12] - 2026-11-26
### Added
- Created `14_Separation_Exit_Tables.sql` (`tbl_SeparationRequests`, `tbl_ExitClearanceTemplates`, `tbl_ExitClearanceItems`, `tbl_ExitInterviews`, `tbl_FinalSettlements`).
- Added stored procedures `25_Separation_Exit_StoredProcedures.sql` (`sp_SeparationRequest_GetAll`, `sp_SeparationRequest_GetById`, `sp_SeparationRequest_Submit`, `sp_SeparationRequest_Review`, `sp_SeparationRequest_FinalizeExit`, `sp_ExitClearance_GetTemplates`, `sp_ExitClearance_SaveTemplate`, `sp_ExitClearance_DeleteTemplate`, `sp_ExitClearance_UpdateItem`, `sp_ExitInterview_GetAll`, `sp_ExitInterview_Save`, `sp_FinalSettlement_CalculateAndSave`, `sp_FinalSettlement_UpdateStatus`).
- Added seed script `12_SeparationExitSeedData.sql` with `EXIT.MANAGE` permission, role mappings, default multi-department clearance checklist templates, and a sample separation case with draft Full & Final settlement.
- Implemented `IExitRepository`/`ExitRepository`, `IExitService`/`ExitService`, `ExitController`, `wwwroot/js/exit.js`, and 4 Razor views (`Exit/Index`, `Exit/Details`, `Exit/Interviews`, `Exit/Templates`) plus sidebar navigation, `_ViewImports.cshtml`, and DI updates.

## [1.12.0-phase13] - 2026-11-26
### Added
- Created `15_Reports_Analytics_Tables.sql` (`tbl_SavedReportTemplates`, `tbl_ReportExportLogs`).
- Added stored procedures `26_Reports_Analytics_StoredProcedures.sql` (`sp_Reports_GetAnalyticsHub`, `sp_Reports_GetWorkforceHeadcount`, `sp_Reports_GetAttendanceLeaveSummary`, `sp_Reports_GetPayrollCostReport`, `sp_Reports_GetRecruitmentTalentReport`, `sp_Reports_GetFinancialComplianceReport`, `sp_ReportTemplate_Save`, `sp_ReportTemplate_Delete`, `sp_ReportExportLog_Insert`).
- Added seed script `13_ReportsAnalyticsSeedData.sql` introducing `REPORTS.EXPORT` permission, role grants (`REPORTS.VIEW` / `REPORTS.EXPORT`), and 4 shared report presets.
- Implemented `IReportRepository`/`ReportRepository`, `IReportService`/`ReportService`, `ReportsController`, `wwwroot/js/reports.js`, and 6 Razor views (`Reports/Index`, `Reports/Workforce`, `Reports/AttendanceLeave`, `Reports/PayrollCost`, `Reports/TalentRecruitment`, `Reports/FinancialCompliance`) with 8 formula-safe CSV export endpoints, AJAX Analytics Hub filtering, Chart.js visualizations, Print/PDF export logging, print-friendly stylesheet rules, and `_Sidebar.cshtml` updates.

## [1.13.0-phase14] - 2026-11-26
### Added
- Created `16_Notifications_Workflow_SystemAdmin_Tables.sql` extending `tbl_Notifications` and `tbl_SystemSettings` and adding `tbl_NotificationTemplates`, `tbl_WorkflowDefinitions`, `tbl_WorkflowSteps`, `tbl_WorkflowInstances`, and `tbl_WorkflowActionLogs`.
- Added stored procedures `27_Notifications_Workflow_SystemAdmin_StoredProcedures.sql` (`sp_Notification_GetInbox`, `sp_Notification_SendBroadcast`, `sp_Notification_Delete`, `sp_NotificationTemplate_*`, `sp_WorkflowDefinition_*`, `sp_WorkflowStep_*`, `sp_WorkflowInstance_*`, `sp_AuditLog_Search`, `sp_SystemSettings_*`, `sp_Admin_*`).
- Added seed script `14_NotificationsWorkflowAdminSeedData.sql` with `ADMIN.MANAGE`, `WORKFLOW.VIEW`, `WORKFLOW.MANAGE`, and `NOTIFICATION.MANAGE` permissions, 14 default system settings, 5 notification templates, and 3 multi-step approval workflows.
- Implemented `ISystemAdminRepository`/`SystemAdminRepository`, `ISystemAdminService`/`SystemAdminService`, `NotificationsController`, `WorkflowController`, `SystemAdminController`, `wwwroot/js/system-admin.js`, and 5 Razor views (`Notifications/Index`, `Workflow/Index`, `SystemAdmin/AuditLogs`, `SystemAdmin/Settings`, `SystemAdmin/AccessControl`) plus `_Sidebar.cshtml`, `_TopNavbar.cshtml`, `_ViewImports.cshtml`, and DI updates.

## [1.14.0-phase15] - 2026-11-26
### Added
- Created `HRMS.Database/05_Optimization_Deployment/01_Performance_Indexes_And_HealthCheck.sql` (10 covering nonclustered indexes, `sp_System_HealthCheck`, `sp_System_MaintenanceCleanup`) and `02_Master_Verify_Deployment.sql`.
- Added `HealthController` (`/health`), `SystemAdminController.SystemHealth`, `SystemAdminController.RunMaintenance`, and `Views/SystemAdmin/SystemHealth.cshtml` with sidebar navigation wiring.
- Hardened `HRMS.Web/Program.cs` with Brotli/Gzip response compression, reverse-proxy `ForwardedHeaders`, static asset caching headers, `X-Frame-Options`, `Permissions-Policy`, and `Content-Security-Policy`; added optional command timeout support in `BaseRepository.cs`.
- Created `HRMS.Tests` xUnit test project (`PasswordHasherAndCsvTests`, `AuthorizationAndClaimsTests`, `AuthServiceTests`, `SystemAdminServiceTests`, `BusinessValidationTests`) and registered it in `HRMS.sln`.
- Created `Dockerfile`, `docker-compose.yml`, `HRMS.Web/appsettings.Production.json`, `DEPLOYMENT_GUIDE.md`, `SECURITY_REVIEW.md`, and `walkthrough.md`.
