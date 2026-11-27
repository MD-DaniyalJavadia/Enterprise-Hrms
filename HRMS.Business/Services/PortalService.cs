using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Attendance;
using HRMS.Models.ViewModels.Leave;
using HRMS.Models.ViewModels.LoanExpense;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Models.ViewModels.Portal;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class PortalService : IPortalService
{
    private static readonly HashSet<string> ValidRequestTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ContactInfo", "Address", "BankDetails", "EmergencyContact"
    };

    private static readonly HashSet<string> ValidApprovalDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approved", "Rejected"
    };

    private readonly IPortalRepository _portalRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILeaveService _leaveService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILoanExpenseService _loanExpenseService;
    private readonly IPerformanceTrainingService _performanceService;
    private readonly IAuditLogService _auditLogService;

    public PortalService(
        IPortalRepository portalRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILeaveService leaveService,
        IAttendanceService attendanceService,
        ILoanExpenseService loanExpenseService,
        IPerformanceTrainingService performanceService,
        IAuditLogService auditLogService)
    {
        _portalRepository = portalRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _leaveService = leaveService;
        _attendanceService = attendanceService;
        _loanExpenseService = loanExpenseService;
        _performanceService = performanceService;
        _auditLogService = auditLogService;
    }

    public async Task<EssDashboardViewModel> GetEssDashboardAsync(int companyId, int? employeeId, bool isSuperAdminPreview = false)
    {
        var previewEmployees = isSuperAdminPreview
            ? (await _employeeRepository.GetAllAsync(companyId, status: 1)).ToList()
            : new();

        int? resolvedEmpId = employeeId;
        if (!resolvedEmpId.HasValue && isSuperAdminPreview && previewEmployees.Count > 0)
        {
            resolvedEmpId = previewEmployees[0].EmployeeId;
        }

        if (!resolvedEmpId.HasValue)
        {
            return new EssDashboardViewModel
            {
                HasEmployeeProfile = false,
                IsSuperAdminPreview = isSuperAdminPreview,
                PreviewEmployees = previewEmployees
            };
        }

        var vm = await _portalRepository.GetEssDashboardAsync(companyId, resolvedEmpId.Value, DateTime.Today);
        vm.IsSuperAdminPreview = isSuperAdminPreview;
        vm.PreviewEmployees = previewEmployees;

        if (vm.HasEmployeeProfile)
        {
            vm.LeaveTypes = (await _leaveService.GetLeaveTypesAsync(companyId)).Where(x => x.IsActive).ToList();
            vm.LoanTypes = await _loanExpenseService.GetLoanTypesAsync(companyId, includeInactive: false);
            vm.ExpenseCategories = await _loanExpenseService.GetExpenseCategoriesAsync(companyId, includeInactive: false);
        }

        return vm;
    }

    public async Task<EssMyProfilePageViewModel> GetEssMyProfileAsync(int companyId, int? employeeId, bool isSuperAdminPreview = false)
    {
        var previewEmployees = isSuperAdminPreview
            ? (await _employeeRepository.GetAllAsync(companyId, status: 1)).ToList()
            : new();

        int? resolvedEmpId = employeeId;
        if (!resolvedEmpId.HasValue && isSuperAdminPreview && previewEmployees.Count > 0)
        {
            resolvedEmpId = previewEmployees[0].EmployeeId;
        }

        if (!resolvedEmpId.HasValue)
        {
            return new EssMyProfilePageViewModel
            {
                HasEmployeeProfile = false,
                IsSuperAdminPreview = isSuperAdminPreview,
                PreviewEmployees = previewEmployees
            };
        }

        var fullProfile = await _employeeRepository.GetProfileByIdAsync(companyId, resolvedEmpId.Value);
        var essDash = await _portalRepository.GetEssDashboardAsync(companyId, resolvedEmpId.Value, DateTime.Today);

        return new EssMyProfilePageViewModel
        {
            HasEmployeeProfile = fullProfile is not null,
            IsSuperAdminPreview = isSuperAdminPreview,
            FullProfile = fullProfile,
            ProfileRequests = essDash.ProfileRequests,
            PreviewEmployees = previewEmployees
        };
    }

    public async Task<EssAttendancePageViewModel> GetEssAttendanceAsync(int companyId, int? employeeId, int year, int month, bool isSuperAdminPreview = false)
    {
        var previewEmployees = isSuperAdminPreview
            ? (await _employeeRepository.GetAllAsync(companyId, status: 1)).ToList()
            : new();

        int? resolvedEmpId = employeeId;
        if (!resolvedEmpId.HasValue && isSuperAdminPreview && previewEmployees.Count > 0)
        {
            resolvedEmpId = previewEmployees[0].EmployeeId;
        }

        if (!resolvedEmpId.HasValue)
        {
            return new EssAttendancePageViewModel
            {
                HasEmployeeProfile = false,
                IsSuperAdminPreview = isSuperAdminPreview,
                Year = year,
                Month = month,
                PreviewEmployees = previewEmployees
            };
        }

        var page = await _portalRepository.GetEssAttendanceAsync(companyId, resolvedEmpId.Value, year, month);
        var dash = await _portalRepository.GetEssDashboardAsync(companyId, resolvedEmpId.Value, DateTime.Today);
        page.Profile = dash.Profile;
        page.TodayAttendance = dash.TodayAttendance;
        page.HasEmployeeProfile = dash.HasEmployeeProfile;
        page.IsSuperAdminPreview = isSuperAdminPreview;
        page.PreviewEmployees = previewEmployees;
        return page;
    }

    public async Task<EssPayslipsPageViewModel> GetEssPayslipsAsync(int companyId, int? employeeId, int? year = null, bool isSuperAdminPreview = false)
    {
        var previewEmployees = isSuperAdminPreview
            ? (await _employeeRepository.GetAllAsync(companyId, status: 1)).ToList()
            : new();

        int? resolvedEmpId = employeeId;
        if (!resolvedEmpId.HasValue && isSuperAdminPreview && previewEmployees.Count > 0)
        {
            resolvedEmpId = previewEmployees[0].EmployeeId;
        }

        if (!resolvedEmpId.HasValue)
        {
            return new EssPayslipsPageViewModel
            {
                HasEmployeeProfile = false,
                IsSuperAdminPreview = isSuperAdminPreview,
                Year = year,
                PreviewEmployees = previewEmployees
            };
        }

        var dash = await _portalRepository.GetEssDashboardAsync(companyId, resolvedEmpId.Value, DateTime.Today);
        var payslips = await _portalRepository.GetEssPayslipsAsync(companyId, resolvedEmpId.Value, year);

        return new EssPayslipsPageViewModel
        {
            HasEmployeeProfile = dash.HasEmployeeProfile,
            IsSuperAdminPreview = isSuperAdminPreview,
            EmployeeId = resolvedEmpId.Value,
            Year = year,
            Profile = dash.Profile,
            Payslips = payslips,
            PreviewEmployees = previewEmployees
        };
    }

    public async Task<OperationResult<int>> SubmitProfileUpdateRequestAsync(
        int companyId, int employeeId, ProfileUpdateRequestFormViewModel model, int userId, string username)
    {
        if (!ValidRequestTypes.Contains(model.RequestType))
        {
            return OperationResult<int>.Failure("Invalid profile update request type.");
        }

        model.RequestType = ValidRequestTypes.First(t => string.Equals(t, model.RequestType, StringComparison.OrdinalIgnoreCase));

        bool hasAnyChange = model.RequestType switch
        {
            "ContactInfo" => !string.IsNullOrWhiteSpace(model.RequestedPhone) ||
                             !string.IsNullOrWhiteSpace(model.RequestedMobilePhone) ||
                             !string.IsNullOrWhiteSpace(model.RequestedPersonalEmail),
            "Address" => !string.IsNullOrWhiteSpace(model.RequestedAddress) ||
                         !string.IsNullOrWhiteSpace(model.RequestedCity) ||
                         !string.IsNullOrWhiteSpace(model.RequestedCountry),
            "BankDetails" => !string.IsNullOrWhiteSpace(model.RequestedBankName) ||
                             !string.IsNullOrWhiteSpace(model.RequestedBankAccountTitle) ||
                             !string.IsNullOrWhiteSpace(model.RequestedBankAccountNumber) ||
                             !string.IsNullOrWhiteSpace(model.RequestedBankRoutingCode),
            "EmergencyContact" => !string.IsNullOrWhiteSpace(model.RequestedEmergencyContactName) &&
                                  !string.IsNullOrWhiteSpace(model.RequestedEmergencyPhone),
            _ => false
        };

        if (!hasAnyChange)
        {
            return OperationResult<int>.Failure("Please enter at least one updated value for the selected change category.");
        }

        try
        {
            var id = await _portalRepository.SubmitProfileUpdateRequestAsync(companyId, employeeId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "SelfService", "ProfileUpdateRequest", id.ToString(),
                $"Submitted {model.RequestType} profile change request: {model.FieldSummary}.");
            return OperationResult<int>.Success(id, "Profile update request submitted for manager/HR review.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> CancelProfileUpdateRequestAsync(
        int companyId, int requestId, int employeeId, int userId, string username)
    {
        try
        {
            await _portalRepository.CancelProfileUpdateRequestAsync(companyId, requestId, employeeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "SelfService", "ProfileUpdateRequest", requestId.ToString(),
                $"Cancelled pending profile update request #{requestId}.");
            return OperationResult.Success("Profile update request cancelled.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int companyId, int? managerEmployeeId, bool isCompanyWide)
        => _portalRepository.GetManagerDashboardAsync(companyId, managerEmployeeId, isCompanyWide, DateTime.Today);

    public async Task<ManagerTeamPageViewModel> GetManagerTeamAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, int? departmentId = null, string? searchTerm = null)
    {
        var members = await _portalRepository.GetManagerTeamRosterAsync(companyId, managerEmployeeId, isCompanyWide, departmentId, searchTerm);
        var departments = (await _departmentRepository.GetAllAsync(companyId)).ToList();

        return new ManagerTeamPageViewModel
        {
            IsCompanyWideView = isCompanyWide,
            FilterDepartmentId = departmentId,
            FilterSearch = searchTerm,
            Members = members,
            Departments = departments
        };
    }

    public async Task<ManagerApprovalsPageViewModel> GetManagerApprovalsAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, string? category = null)
    {
        var items = await _portalRepository.GetManagerPendingApprovalsAsync(companyId, managerEmployeeId, isCompanyWide, category);
        var dash = await _portalRepository.GetManagerDashboardAsync(companyId, managerEmployeeId, isCompanyWide, DateTime.Today);

        return new ManagerApprovalsPageViewModel
        {
            IsCompanyWideView = isCompanyWide,
            FilterCategory = category,
            Items = items,
            PendingAppraisals = dash.PendingAppraisals
        };
    }

    public async Task<bool> CanManagerAccessEmployeeAsync(int companyId, int? managerEmployeeId, bool isCompanyWide, int targetEmployeeId)
    {
        if (isCompanyWide)
        {
            return true;
        }

        if (!managerEmployeeId.HasValue || managerEmployeeId.Value <= 0)
        {
            return false;
        }

        return await _portalRepository.VerifyManagerTeamMemberAsync(companyId, managerEmployeeId.Value, targetEmployeeId);
    }

    public Task<int?> GetRequestOwnerEmployeeIdAsync(int companyId, string category, int requestId)
        => _portalRepository.GetManagerRequestOwnerAsync(companyId, category, requestId);

    public async Task<OperationResult> ProcessManagerApprovalAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, ManagerApprovalActionViewModel model, int userId, string username)
    {
        if (!ValidApprovalDecisions.Contains(model.Decision))
        {
            return OperationResult.Failure("Decision must be Approved or Rejected.");
        }

        var normalizedDecision = ValidApprovalDecisions.First(d => string.Equals(d, model.Decision, StringComparison.OrdinalIgnoreCase));
        var ownerEmployeeId = await _portalRepository.GetManagerRequestOwnerAsync(companyId, model.Category, model.RequestId);
        if (!ownerEmployeeId.HasValue)
        {
            return OperationResult.Failure("Requested item was not found or has already been removed.");
        }

        var allowed = await CanManagerAccessEmployeeAsync(companyId, managerEmployeeId, isCompanyWide, ownerEmployeeId.Value);
        if (!allowed)
        {
            return OperationResult.Failure("Access denied: you can only review requests belonging to your reporting team.");
        }

        switch (model.Category)
        {
            case "Leave":
                return await _leaveService.ReviewApplicationAsync(companyId, new LeaveReviewViewModel
                {
                    LeaveApplicationId = model.RequestId,
                    ApprovalStatus = normalizedDecision,
                    ReviewerComments = model.ReviewerComments
                }, userId, username);

            case "Regularization":
                return await _attendanceService.ReviewRegularizationAsync(companyId, new RegularizationReviewViewModel
                {
                    RegularizationId = model.RequestId,
                    ApprovalStatus = normalizedDecision,
                    ReviewerComments = model.ReviewerComments
                }, userId, username);

            case "Overtime":
                return await _attendanceService.ReviewOvertimeAsync(companyId, new OvertimeReviewViewModel
                {
                    OvertimeRequestId = model.RequestId,
                    ApprovalStatus = normalizedDecision,
                    ApprovedHours = normalizedDecision == "Approved" ? (model.NumericOverride ?? 1.0m) : 0m,
                    ReviewerComments = model.ReviewerComments
                }, userId, username);

            case "Loan":
                return await _loanExpenseService.ReviewLoanAsync(companyId, new LoanReviewViewModel
                {
                    LoanId = model.RequestId,
                    ActionStatus = normalizedDecision == "Approved" ? "Active" : "Rejected",
                    ReviewerComments = model.ReviewerComments
                }, userId, username);

            case "Expense":
                return await _loanExpenseService.ReviewExpenseClaimAsync(companyId, new ExpenseReviewViewModel
                {
                    ClaimId = model.RequestId,
                    ActionStatus = normalizedDecision,
                    ApprovedAmount = normalizedDecision == "Approved" ? model.NumericOverride : null,
                    ReviewerComments = model.ReviewerComments
                }, userId, username);

            case "ProfileUpdate":
                try
                {
                    await _portalRepository.ReviewProfileUpdateRequestAsync(companyId, new ProfileUpdateReviewViewModel
                    {
                        RequestId = model.RequestId,
                        ApprovalStatus = normalizedDecision,
                        ReviewerComments = model.ReviewerComments
                    }, userId);

                    await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                        "ManagerPortal", "ProfileUpdateRequest", model.RequestId.ToString(),
                        $"{normalizedDecision} profile update request #{model.RequestId}.");
                    return OperationResult.Success($"Profile update request {normalizedDecision.ToLowerInvariant()} successfully.");
                }
                catch (SqlException ex) when (ex.Number >= 50000)
                {
                    return OperationResult.Failure(ex.Message);
                }

            default:
                return OperationResult.Failure("Unsupported approval category.");
        }
    }

    public async Task<OperationResult> SubmitTeamAppraisalReviewAsync(
        int companyId, int? managerEmployeeId, bool isCompanyWide, ManagerAppraisalReviewViewModel model, int userId, string username)
    {
        var ownerEmployeeId = await _portalRepository.GetManagerRequestOwnerAsync(companyId, "Appraisal", model.AppraisalId);
        if (!ownerEmployeeId.HasValue)
        {
            return OperationResult.Failure("Appraisal record not found.");
        }

        var allowed = await CanManagerAccessEmployeeAsync(companyId, managerEmployeeId, isCompanyWide, ownerEmployeeId.Value);
        if (!allowed)
        {
            return OperationResult.Failure("Access denied: you can only review appraisals for members of your reporting team.");
        }

        return await _performanceService.SubmitManagerReviewAsync(companyId, model, userId, username);
    }
}
