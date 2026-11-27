# HRMS_DB Database Schema Reference

## Tables (Phase 1)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_Companies` | Multi-tenant company root | `CompanyId` (PK), `CompanyCode` (UQ) |
| `dbo.tbl_Branches` | Company branch locations | `BranchId` (PK), `CompanyId` (FK) |
| `dbo.tbl_Departments` | Hierarchical departments | `DepartmentId` (PK), `CompanyId` (FK), `ParentDepartmentId` (FK) |
| `dbo.tbl_Designations` | Job designations & grades | `DesignationId` (PK), `CompanyId` (FK) |
| `dbo.tbl_Employees` | Core employee master | `EmployeeId` (PK), `CompanyId`, `BranchId`, `DepartmentId`, `DesignationId` |
| `dbo.tbl_Roles` | Company-scoped roles | `RoleId` (PK), `CompanyId` (FK), `RoleCode` |
| `dbo.tbl_Permissions` | Module/Page/Action permissions | `PermissionId` (PK), `PermissionCode` (UQ) |
| `dbo.tbl_RolePermissions` | Role-to-Permission mapping | `RolePermissionId` (PK), `RoleId`, `PermissionId` |
| `dbo.tbl_Users` | Application login accounts | `UserId` (PK), `CompanyId`, `Username` (UQ), `Email` (UQ) |
| `dbo.tbl_UserRoles` | User-to-Role mapping | `UserRoleId` (PK), `UserId`, `RoleId` |
| `dbo.tbl_LoginHistory` | Login & failed-attempt audit | `LoginHistoryId` (PK), `UserId` |
| `dbo.tbl_AuditLogs` | Immutable enterprise audit trail | `AuditLogId` (PK), `CompanyId` |
| `dbo.tbl_Notifications` | In-app notification store | `NotificationId` (PK), `CompanyId`, `UserId` |
| `dbo.tbl_SystemSettings` | Configurable company rules | `SettingId` (PK), `CompanyId`, `SettingKey` |
| `dbo.tbl_Attendance` | Daily attendance records | `AttendanceId` (PK), `CompanyId`, `EmployeeId` |
| `dbo.tbl_LeaveTypes` | Leave policy types | `LeaveTypeId` (PK), `CompanyId` |
| `dbo.tbl_LeaveApplications` | Leave requests & status | `LeaveApplicationId` (PK), `CompanyId`, `EmployeeId` |
| `dbo.tbl_PayrollRuns` | Monthly payroll batch summary | `PayrollRunId` (PK), `CompanyId` |

## Stored Procedures (Phase 1)
- `sp_Auth_GetUserForLogin`, `sp_Auth_GetUserById`, `sp_Auth_GetUserRolesAndPermissions`
- `sp_Auth_RecordLoginAttempt`, `sp_Auth_UpdateUserLoginSuccess`, `sp_Auth_UpdateUserLockout`, `sp_Auth_UpdatePassword`
- `sp_Auth_HasAnyAdminUser`, `sp_Auth_CreateInitialAdmin`
- `sp_AuditLog_Insert`, `sp_AuditLog_GetRecent`
- `sp_Notification_GetByUserId`, `sp_Notification_MarkAsRead`, `sp_Notification_MarkAllAsRead`
- `sp_Dashboard_GetSummary`, `sp_Dashboard_GlobalSearch`

## Tables (Phase 2)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_JobGrades` | Pay grades & salary bands | `JobGradeId` (PK), `CompanyId` (FK), `GradeCode` |
| `dbo.tbl_CostCenters` | Financial cost centers | `CostCenterId` (PK), `CompanyId`, `BranchId` |
| `dbo.tbl_WorkLocations` | Physical/remote work sites | `WorkLocationId` (PK), `CompanyId`, `BranchId` |
| `dbo.tbl_JobPositions` | Organizational positions & budget | `JobPositionId` (PK), `DepartmentId`, `DesignationId`, `JobGradeId` |
| `dbo.tbl_Shifts` | Work shifts & grace/off rules | `ShiftId` (PK), `CompanyId`, `ShiftCode` |

## Stored Procedures (Phase 2)
- `sp_Company_GetAll`, `sp_Company_GetById`, `sp_Company_Insert`, `sp_Company_Update`, `sp_Company_Delete`
- `sp_Branch_GetAll`, `sp_Branch_GetById`, `sp_Branch_Insert`, `sp_Branch_Update`, `sp_Branch_Delete`
- `sp_Department_GetAll`, `sp_Department_GetById`, `sp_Department_Insert`, `sp_Department_Update`, `sp_Department_Delete`
- `sp_Designation_GetAll`, `sp_Designation_GetById`, `sp_Designation_Insert`, `sp_Designation_Update`, `sp_Designation_Delete`
- `sp_JobGrade_GetAll`, `sp_JobGrade_Save`, `sp_JobGrade_Delete`
- `sp_JobPosition_GetAll`, `sp_JobPosition_Save`, `sp_JobPosition_Delete`
- `sp_CostCenter_GetAll`, `sp_CostCenter_Save`, `sp_CostCenter_Delete`
- `sp_WorkLocation_GetAll`, `sp_WorkLocation_Save`, `sp_WorkLocation_Delete`
- `sp_Shift_GetAll`, `sp_Shift_Save`, `sp_Shift_Delete`
- `sp_Organization_GetOrgChart`

## Tables (Phase 3)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_Employees` *(extended)* | Work placement, personal, bank & tax fields | `WorkLocationId`, `JobPositionId`, `ShiftId`, `BankAccountNumber`, `TaxIdentificationNumber` |
| `dbo.tbl_EmployeeEmergencyContacts` | Emergency contacts per employee | `EmergencyContactId` (PK), `CompanyId`, `EmployeeId` (FK) |
| `dbo.tbl_EmployeeFamilyMembers` | Family & benefit dependents | `FamilyMemberId` (PK), `CompanyId`, `EmployeeId` (FK) |
| `dbo.tbl_EmployeeEducation` | Academic qualifications | `EducationId` (PK), `CompanyId`, `EmployeeId` (FK) |
| `dbo.tbl_EmployeeExperience` | Prior employment history | `ExperienceId` (PK), `CompanyId`, `EmployeeId` (FK) |
| `dbo.tbl_EmployeeSkills` | Skills & professional certifications | `SkillId` (PK), `CompanyId`, `EmployeeId` (FK) |
| `dbo.tbl_EmployeeDocuments` | Private document metadata & storage reference | `DocumentId` (PK), `CompanyId`, `EmployeeId` (FK), `StoredFileName` |
| `dbo.tbl_EmployeeEmploymentHistory` | Promotions, transfers & status timeline | `HistoryId` (PK), `CompanyId`, `EmployeeId` (FK), `EventType`, `EffectiveDate` |
| `dbo.tbl_EmployeeSalaryHistory` | Compensation revision ledger | `SalaryHistoryId` (PK), `CompanyId`, `EmployeeId` (FK), `PreviousSalary`, `NewSalary` |

## Stored Procedures (Phase 3)
- `sp_Employee_GenerateNextCode`, `sp_Employee_GetAll`, `sp_Employee_GetById`, `sp_Employee_Insert`, `sp_Employee_Update`, `sp_Employee_Delete`, `sp_Employee_RecordCareerAction`
- `sp_EmployeeEmergencyContact_Insert`, `sp_EmployeeEmergencyContact_Delete`
- `sp_EmployeeFamily_Insert`, `sp_EmployeeFamily_Delete`
- `sp_EmployeeEducation_Insert`, `sp_EmployeeEducation_Delete`
- `sp_EmployeeExperience_Insert`, `sp_EmployeeExperience_Delete`
- `sp_EmployeeSkill_Insert`, `sp_EmployeeSkill_Delete`
- `sp_EmployeeDocument_Insert`, `sp_EmployeeDocument_GetById`, `sp_EmployeeDocument_Delete`

## Tables (Phase 4)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_Attendance` *(extended)* | Daily attendance with shift & duration metrics | `ShiftId`, `WorkMinutes`, `EarlyExitMinutes`, `Source`, `CheckInIp` |
| `dbo.tbl_ShiftAssignments` | Effective-dated employee shift roster | `AssignmentId` (PK), `CompanyId`, `EmployeeId`, `ShiftId`, `EffectiveFrom` |
| `dbo.tbl_Holidays` | Company and branch-specific holiday calendar | `HolidayId` (PK), `CompanyId`, `BranchId`, `HolidayDate`, `HolidayType` |
| `dbo.tbl_AttendanceRegularizations` | Missed-punch / status correction requests | `RegularizationId` (PK), `CompanyId`, `EmployeeId`, `ApprovalStatus` |
| `dbo.tbl_OvertimeRequests` | Overtime claims and approvals | `OvertimeRequestId` (PK), `CompanyId`, `EmployeeId`, `RequestedHours`, `ApprovedHours` |

## Stored Procedures (Phase 4)
- `sp_Attendance_GetDailyRegister`, `sp_Attendance_CheckIn`, `sp_Attendance_CheckOut`, `sp_Attendance_MarkManual`, `sp_Attendance_GetMonthlyReport`, `sp_Attendance_GetEmployeeMonthLog`
- `sp_ShiftAssignment_GetAll`, `sp_ShiftAssignment_Save`, `sp_ShiftAssignment_Delete`
- `sp_Holiday_GetAll`, `sp_Holiday_Save`, `sp_Holiday_Delete`
- `sp_AttendanceRegularization_GetAll`, `sp_AttendanceRegularization_Submit`, `sp_AttendanceRegularization_Review`
- `sp_OvertimeRequest_GetAll`, `sp_OvertimeRequest_Submit`, `sp_OvertimeRequest_Review`

## Tables (Phase 5)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_LeaveTypes` *(extended)* | Enterprise leave policy configuration | `AllowCarryForward`, `MaxCarryForwardDays`, `AllowEncashment`, `RequiresDocument`, `MaxConsecutiveDays`, `IsHalfDayAllowed`, `GenderRestriction`, `ColorHex` |
| `dbo.tbl_LeaveApplications` *(extended)* | Leave requests, attachments & review details | `IsHalfDay`, `HalfDaySession`, `ContactDuringLeave`, `AttachmentStoredName`, `ReviewerComments`, `CancelledDate` |
| `dbo.tbl_LeaveBalances` | Annual per-employee leave balance ledger | `LeaveBalanceId` (PK), `CompanyId`, `EmployeeId`, `LeaveTypeId`, `LeaveYear`, `OpeningBalance`, `AccruedDays`, `CarryForwardDays`, `UsedDays`, `PendingDays`, `EncashedDays` |
| `dbo.tbl_LeaveEncashments` | Leave encashment claims & approvals | `EncashmentId` (PK), `CompanyId`, `EmployeeId`, `LeaveTypeId`, `LeaveYear`, `DaysEncashed`, `EncashmentAmount`, `ApprovalStatus` |

## Stored Procedures (Phase 5)
- `sp_LeaveType_GetAll`, `sp_LeaveType_Save`, `sp_LeaveType_Delete`
- `sp_LeaveBalance_GetAll`, `sp_LeaveBalance_AllocateAnnual`, `sp_LeaveBalance_Adjust`
- `sp_LeaveEncashment_GetAll`, `sp_LeaveEncashment_Submit`, `sp_LeaveEncashment_Review`
- `sp_LeaveApplication_GetAll`, `sp_LeaveApplication_GetById`, `sp_LeaveApplication_Submit`, `sp_LeaveApplication_Review`, `sp_LeaveApplication_Cancel`, `sp_Leave_GetCalendarEvents`

## Tables (Phase 6)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_PayrollRuns` *(extended)* | Monthly payroll batch header & lifecycle | `PayDate`, `TotalEmployerContributions`, `TotalTaxDeducted`, `ApprovedBy`, `LockedBy`, `PaidDate` |
| `dbo.tbl_SalaryComponents` | Configurable earnings, deductions & contributions | `ComponentId` (PK), `CompanyId`, `ComponentCode`, `ComponentType`, `CalculationType`, `IsTaxable`, `IsStatutory` |
| `dbo.tbl_TaxSlabs` | Progressive annual income tax brackets | `TaxSlabId` (PK), `CompanyId`, `TaxYear`, `MinAnnualIncome`, `MaxAnnualIncome`, `TaxRatePercent` |
| `dbo.tbl_EmployeeSalaryStructures` | Employee compensation package headers | `SalaryStructureId` (PK), `CompanyId`, `EmployeeId`, `EffectiveFrom`, `BasicSalaryMonthly`, `HourlyOvertimeRate` |
| `dbo.tbl_EmployeeSalaryStructureItems` | Component amounts/percentages per structure | `StructureItemId` (PK), `SalaryStructureId` (FK), `ComponentId` (FK), `CalculatedMonthlyAmount` |
| `dbo.tbl_Payslips` | Monthly employee payslip calculations | `PayslipId` (PK), `PayrollRunId` (FK), `EmployeeId` (FK), `GrossPay`, `IncomeTax`, `TotalDeductions`, `NetPay`, `PaymentStatus` |
| `dbo.tbl_PayslipLines` | Itemized earnings, deductions & contributions | `PayslipLineId` (PK), `PayslipId` (FK), `ComponentCode`, `ComponentType`, `Amount` |

## Stored Procedures (Phase 6)
- `sp_SalaryComponent_GetAll`, `sp_SalaryComponent_Save`, `sp_SalaryComponent_Delete`
- `sp_TaxSlab_GetAll`, `sp_TaxSlab_Save`, `sp_TaxSlab_Delete`
- `sp_EmployeeSalaryStructure_GetAll`, `sp_EmployeeSalaryStructure_GetByEmployee`, `sp_EmployeeSalaryStructure_Save`
- `sp_PayrollRun_GetAll`, `sp_PayrollRun_Generate`, `sp_PayrollRun_GetDetails`, `sp_PayrollRun_UpdateStatus`, `sp_PayrollRun_Delete`
- `sp_Payslip_Adjust`, `sp_Payslip_GetById`, `sp_Payroll_GetBankTransferList`

## Tables (Phase 7)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_JobRequisitions` | Job openings & hiring requests | `RequisitionId` (PK), `CompanyId`, `RequisitionCode`, `BranchId`, `DepartmentId`, `DesignationId`, `RequisitionStatus`, `OpeningsCount` |
| `dbo.tbl_Candidates` | Talent pool & resume vault metadata | `CandidateId` (PK), `CompanyId`, `FullName` (computed), `Email`, `Source`, `ResumeFileName`, `ResumeStoredName` |
| `dbo.tbl_JobApplications` | Applicant pipeline tracking | `ApplicationId` (PK), `CompanyId`, `RequisitionId` (FK), `CandidateId` (FK), `PipelineStage`, `MatchScore`, `HiredEmployeeId` |
| `dbo.tbl_Interviews` | Interview schedules & scorecards | `InterviewId` (PK), `CompanyId`, `ApplicationId` (FK), `InterviewRound`, `InterviewMode`, `ScheduledStart`, `InterviewStatus`, `OverallRecommendation` |
| `dbo.tbl_JobOffers` | Job offers & onboarding conversion | `OfferId` (PK), `CompanyId`, `ApplicationId` (FK), `OfferCode`, `OfferedSalaryAnnual`, `JoiningDate`, `OfferStatus` |

## Stored Procedures (Phase 7)
- `sp_JobRequisition_GetAll`, `sp_JobRequisition_Save`, `sp_JobRequisition_UpdateStatus`, `sp_JobRequisition_Delete`
- `sp_Candidate_GetAll`, `sp_Candidate_Save`, `sp_Candidate_GetResume`, `sp_Candidate_Delete`
- `sp_JobApplication_GetPipeline`, `sp_JobApplication_Submit`, `sp_JobApplication_MoveStage`
- `sp_Interview_GetAll`, `sp_Interview_Schedule`, `sp_Interview_SubmitFeedback`
- `sp_JobOffer_GetAll`, `sp_JobOffer_Save`, `sp_JobOffer_UpdateStatus`, `sp_JobOffer_ConvertToEmployee`

## Tables (Phase 8)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_LoanTypes` | Loan & salary advance policy types | `LoanTypeId` (PK), `CompanyId`, `TypeCode`, `Category`, `MaxAmount`, `DefaultInterestRate`, `MaxTenureMonths` |
| `dbo.tbl_EmployeeLoans` | Employee loan & advance applications | `LoanId` (PK), `CompanyId`, `EmployeeId` (FK), `LoanTypeId` (FK), `LoanNumber`, `PrincipalAmount`, `MonthlyInstallment`, `RemainingBalance`, `LoanStatus` |
| `dbo.tbl_LoanRepaymentSchedule` | Monthly EMI amortization ledger | `ScheduleId` (PK), `CompanyId`, `LoanId` (FK), `InstallmentNumber`, `DueDate`, `InstallmentAmount`, `PaymentStatus`, `PayrollRunId` |
| `dbo.tbl_ExpenseCategories` | Configurable expense policy categories | `CategoryId` (PK), `CompanyId`, `CategoryCode`, `MaxLimitPerClaim`, `RequiresReceipt` |
| `dbo.tbl_ExpenseClaims` | Employee expense reimbursement claims | `ClaimId` (PK), `CompanyId`, `EmployeeId` (FK), `CategoryId` (FK), `ClaimNumber`, `ClaimAmount`, `ApprovedAmount`, `ReceiptStoredName`, `ClaimStatus` |

## Stored Procedures (Phase 8)
- `sp_LoanType_GetAll`, `sp_LoanType_Save`, `sp_LoanType_Delete`
- `sp_EmployeeLoan_GetAll`, `sp_EmployeeLoan_GetById`, `sp_EmployeeLoan_Submit`, `sp_EmployeeLoan_Review`, `sp_EmployeeLoan_RecordRepayment`
- `sp_ExpenseCategory_GetAll`, `sp_ExpenseCategory_Save`, `sp_ExpenseCategory_Delete`
- `sp_ExpenseClaim_GetAll`, `sp_ExpenseClaim_GetById`, `sp_ExpenseClaim_Submit`, `sp_ExpenseClaim_Review`

## Tables (Phase 9)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_AppraisalCycles` | Performance review periods & deadlines | `CycleId` (PK), `CompanyId`, `CycleCode`, `ReviewYear`, `StartDate`, `EndDate`, `CycleStatus` |
| `dbo.tbl_PerformanceGoals` | Weighted employee KPIs / OKRs | `GoalId` (PK), `CompanyId`, `CycleId` (FK), `EmployeeId` (FK), `WeightPercent`, `ProgressPercent`, `GoalStatus` |
| `dbo.tbl_PerformanceAppraisals` | Self & manager evaluation records | `AppraisalId` (PK), `CompanyId`, `CycleId` (FK), `EmployeeId` (FK), `SelfRating`, `ManagerRating`, `FinalRating`, `RatingBand`, `AppraisalStatus` |
| `dbo.tbl_TrainingPrograms` | Training catalog & capacity rules | `ProgramId` (PK), `CompanyId`, `ProgramCode`, `Category`, `DeliveryMode`, `MaxParticipants`, `IsMandatory`, `ProgramStatus` |
| `dbo.tbl_TrainingEnrollments` | Employee enrollments & certificates | `EnrollmentId` (PK), `CompanyId`, `ProgramId` (FK), `EmployeeId` (FK), `AttendanceStatus`, `AssessmentScore`, `CertificateNumber` |

## Stored Procedures (Phase 9)
- `sp_AppraisalCycle_GetAll`, `sp_AppraisalCycle_Save`, `sp_AppraisalCycle_Delete`
- `sp_PerformanceGoal_GetAll`, `sp_PerformanceGoal_Save`, `sp_PerformanceGoal_UpdateProgress`, `sp_PerformanceGoal_Delete`
- `sp_PerformanceAppraisal_GetAll`, `sp_PerformanceAppraisal_InitCycle`, `sp_PerformanceAppraisal_SubmitSelf`, `sp_PerformanceAppraisal_SubmitManagerReview`
- `sp_TrainingProgram_GetAll`, `sp_TrainingProgram_Save`, `sp_TrainingProgram_Delete`
- `sp_TrainingEnrollment_GetAll`, `sp_TrainingEnrollment_Enroll`, `sp_TrainingEnrollment_UpdateOutcome`, `sp_TrainingEnrollment_Delete`

## Tables (Phase 10)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_ProfileUpdateRequests` | Self-service personal, address, bank & emergency contact change requests | `RequestId` (PK), `CompanyId`, `EmployeeId` (FK), `RequestType`, `ApprovalStatus`, `ReviewedBy` |

## Stored Procedures (Phase 10)
- `sp_ESS_GetDashboard`, `sp_ESS_GetMyPayslips`, `sp_ESS_GetMyAttendance`
- `sp_ProfileUpdateRequest_Submit`, `sp_ProfileUpdateRequest_Cancel`, `sp_ProfileUpdateRequest_Review`
- `sp_Manager_GetDashboard`, `sp_Manager_GetTeamRoster`, `sp_Manager_GetPendingApprovals`, `sp_Manager_GetRequestOwner`, `sp_Manager_VerifyTeamMember`

## Tables (Phase 11)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_AssetCategories` | Asset taxonomy & depreciation defaults | `CategoryId` (PK), `CompanyId`, `CategoryCode`, `AssetClass`, `RequiresSerialNumber` |
| `dbo.tbl_Assets` | Corporate asset inventory & status | `AssetId` (PK), `CompanyId`, `CategoryId` (FK), `BranchId`, `AssetTag`, `AssetStatus`, `CurrentEmployeeId` |
| `dbo.tbl_AssetAllocations` | Asset custody, returns & employee sign-offs | `AllocationId` (PK), `CompanyId`, `AssetId` (FK), `EmployeeId` (FK), `AllocationStatus`, `EmployeeAcknowledged` |
| `dbo.tbl_AssetMaintenanceLogs` | Hardware repairs, upgrades & preventive service | `MaintenanceId` (PK), `CompanyId`, `AssetId` (FK), `MaintenanceType`, `MaintenanceStatus`, `Cost` |
| `dbo.tbl_DocumentCategories` | Document taxonomy & retention policies | `CategoryId` (PK), `CompanyId`, `CategoryCode`, `ScopeType`, `DefaultVisibility`, `RetentionMonths` |
| `dbo.tbl_CompanyDocuments` | Versioned HR policies & templates repository | `DocumentId` (PK), `CompanyId`, `CategoryId` (FK), `DepartmentId`, `DocumentCode`, `VersionNumber`, `Visibility`, `DocumentStatus` |
| `dbo.tbl_DocumentAcknowledgments` | Policy read & sign-off compliance ledger | `AcknowledgmentId` (PK), `CompanyId`, `DocumentId` (FK), `EmployeeId` (FK), `AcknowledgedVersion`, `AcknowledgedDate` |

## Stored Procedures (Phase 11)
- `sp_AssetCategory_GetAll`, `sp_AssetCategory_Save`, `sp_AssetCategory_Delete`
- `sp_Asset_GetAll`, `sp_Asset_GetById`, `sp_Asset_Save`, `sp_Asset_Delete`
- `sp_AssetAllocation_GetAll`, `sp_AssetAllocation_Assign`, `sp_AssetAllocation_Return`, `sp_AssetAllocation_Acknowledge`
- `sp_AssetMaintenance_GetAll`, `sp_AssetMaintenance_Save`, `sp_AssetMaintenance_Delete`
- `sp_DocumentCategory_GetAll`, `sp_DocumentCategory_Save`, `sp_DocumentCategory_Delete`
- `sp_CompanyDocument_GetAll`, `sp_CompanyDocument_GetById`, `sp_CompanyDocument_Save`, `sp_CompanyDocument_Delete`
- `sp_CompanyDocument_Acknowledge`, `sp_CompanyDocument_GetAcknowledgments`, `sp_EmployeeVault_GetAll`

## Tables (Phase 12)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_SeparationRequests` | Employee resignation, termination & offboarding lifecycle | `SeparationId` (PK), `CompanyId`, `EmployeeId` (FK), `SeparationNumber`, `SeparationType`, `NoticePeriodDays`, `RequestedLastWorkingDay`, `ApprovedLastWorkingDay`, `SeparationStatus`, `IsRehireEligible` |
| `dbo.tbl_ExitClearanceTemplates` | Reusable multi-department clearance checklist templates | `TemplateId` (PK), `CompanyId`, `DepartmentScope`, `ItemName`, `IsMandatory`, `DisplayOrder`, `IsActive` |
| `dbo.tbl_ExitClearanceItems` | Per-separation departmental clearance sign-off items | `ClearanceItemId` (PK), `CompanyId`, `SeparationId` (FK), `DepartmentScope`, `ItemName`, `ClearanceStatus`, `RecoveryAmount` |
| `dbo.tbl_ExitInterviews` | Structured exit interview ratings & retention feedback | `ExitInterviewId` (PK), `CompanyId`, `SeparationId` (FK), `EmployeeId` (FK), `OverallSatisfactionRating`, `WouldRecommendCompany`, `PrimaryLeavingReason`, `IsConfidential` |
| `dbo.tbl_FinalSettlements` | Full & Final settlement earnings, recoveries & payout status | `SettlementId` (PK), `CompanyId`, `SeparationId` (FK), `EmployeeId` (FK), `SettlementNumber`, `GrossEarnings`, `TotalDeductions`, `NetPayableAmount`, `SettlementStatus` |

## Stored Procedures (Phase 12)
- `sp_SeparationRequest_GetAll`, `sp_SeparationRequest_GetById`, `sp_SeparationRequest_Submit`, `sp_SeparationRequest_Review`, `sp_SeparationRequest_FinalizeExit`
- `sp_ExitClearance_GetTemplates`, `sp_ExitClearance_SaveTemplate`, `sp_ExitClearance_DeleteTemplate`, `sp_ExitClearance_UpdateItem`
- `sp_ExitInterview_GetAll`, `sp_ExitInterview_Save`
- `sp_FinalSettlement_CalculateAndSave`, `sp_FinalSettlement_UpdateStatus`

## Tables (Phase 13)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_SavedReportTemplates` | Shared and personal report filter presets | `TemplateId` (PK), `CompanyId`, `TemplateName`, `ReportKey`, `BranchId`, `DepartmentId`, `DateRangeDays`, `StatusFilter`, `IsShared` |
| `dbo.tbl_ReportExportLogs` | Audit trail of CSV and Print/PDF report exports | `ExportLogId` (PK), `CompanyId`, `ReportKey`, `ReportTitle`, `ExportFormat`, `FilterSummary`, `RecordCount`, `ExportedBy`, `ExportedDate` |

## Stored Procedures (Phase 13)
- `sp_Reports_GetAnalyticsHub`, `sp_Reports_GetWorkforceHeadcount`, `sp_Reports_GetAttendanceLeaveSummary`, `sp_Reports_GetPayrollCostReport`, `sp_Reports_GetRecruitmentTalentReport`, `sp_Reports_GetFinancialComplianceReport`
- `sp_ReportTemplate_Save`, `sp_ReportTemplate_Delete`, `sp_ReportExportLog_Insert`

## Tables (Phase 14)
| Table | Purpose | Key Columns |
|---|---|---|
| `dbo.tbl_Notifications` *(extended)* | In-app notifications with module, priority & expiry | `ModuleName`, `Priority`, `CreatedBy`, `ExpiresAtUtc` |
| `dbo.tbl_SystemSettings` *(extended)* | Categorized & typed enterprise configuration | `Category`, `DataType`, `IsSensitive` |
| `dbo.tbl_NotificationTemplates` | Reusable alert & digest message templates | `TemplateId` (PK), `CompanyId`, `TemplateCode`, `ModuleName`, `EventTrigger`, `DefaultType`, `DefaultPriority` |
| `dbo.tbl_WorkflowDefinitions` | Multi-stage approval rules & SLA thresholds | `WorkflowId` (PK), `CompanyId`, `WorkflowCode`, `ModuleName`, `DepartmentId`, `MinAmountThreshold`, `AutoEscalateHours` |
| `dbo.tbl_WorkflowSteps` | Ordered approval stages & escalation routing | `WorkflowStepId` (PK), `WorkflowId` (FK), `StepOrder`, `ApproverType`, `ApproverRoleId`, `ApproverEmployeeId`, `SlaHours` |
| `dbo.tbl_WorkflowInstances` | Active and historical workflow execution records | `InstanceId` (PK), `WorkflowId` (FK), `ReferenceNumber`, `CurrentStepOrder`, `TotalSteps`, `InstanceStatus`, `DueAtUtc` |
| `dbo.tbl_WorkflowActionLogs` | Stage transition, escalation & delegation audit trail | `ActionLogId` (PK), `InstanceId` (FK), `StepOrder`, `ActionTaken`, `ActorUserId`, `ActionDate` |

## Stored Procedures (Phase 14)
- `sp_Notification_GetInbox`, `sp_Notification_SendBroadcast`, `sp_Notification_Delete`
- `sp_NotificationTemplate_GetAll`, `sp_NotificationTemplate_Save`, `sp_NotificationTemplate_Delete`
- `sp_WorkflowDefinition_GetAll`, `sp_WorkflowDefinition_Save`, `sp_WorkflowDefinition_Delete`
- `sp_WorkflowStep_Save`, `sp_WorkflowStep_Delete`
- `sp_WorkflowInstance_GetAll`, `sp_WorkflowInstance_Initiate`, `sp_WorkflowInstance_Advance`
- `sp_AuditLog_Search`, `sp_SystemSettings_GetAll`, `sp_SystemSettings_Upsert`
- `sp_Admin_GetRolesAndPermissions`, `sp_Admin_SaveRolePermissions`, `sp_Admin_GetUsers`, `sp_Admin_CreateOrUpdateUser`, `sp_Admin_ToggleUserStatus`

## Performance Indexes & Operations Procedures (Phase 15)
- **Covering Nonclustered Indexes** (`05_Optimization_Deployment/01_Performance_Indexes_And_HealthCheck.sql`):
  - `IX_tbl_Employees_Manager_Active` (`tbl_Employees`)
  - `IX_tbl_Attendance_Emp_Date` (`tbl_Attendance`)
  - `IX_tbl_LeaveApplications_Emp_Status` (`tbl_LeaveApplications`)
  - `IX_tbl_Payslips_Emp_Run` (`tbl_Payslips`)
  - `IX_tbl_AuditLogs_Module_Action_Date` (`tbl_AuditLogs`)
  - `IX_tbl_LoginHistory_AttemptedAt_Success` (`tbl_LoginHistory`)
  - `IX_tbl_ExpenseClaims_Company_Status` (`tbl_ExpenseClaims`)
  - `IX_tbl_AssetAllocations_Employee_Status` (`tbl_AssetAllocations`)
  - `IX_tbl_SeparationRequests_Company_Status` (`tbl_SeparationRequests`)
  - `IX_tbl_WorkflowInstances_Workflow_Status` (`tbl_WorkflowInstances`)
- **Stored Procedures**:
  - `sp_System_HealthCheck` — Returns live database connectivity, engine metadata, schema counts, and tenant operational counts
  - `sp_System_MaintenanceCleanup` — Enforces minimum retention floors and prunes aged read/expired notifications and login telemetry while preserving immutable `tbl_AuditLogs`
- **Verification Script**: `05_Optimization_Deployment/02_Master_Verify_Deployment.sql`
