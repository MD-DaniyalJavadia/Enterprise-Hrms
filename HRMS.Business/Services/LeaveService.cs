using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Leave;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAuditLogService _auditLogService;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IAttendanceRepository attendanceRepository,
        IAuditLogService auditLogService)
    {
        _leaveRepository = leaveRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _attendanceRepository = attendanceRepository;
        _auditLogService = auditLogService;
    }

    public async Task<LeaveApplicationsPageViewModel> GetApplicationsPageAsync(
        int companyId, int? employeeId = null, int? departmentId = null, int? leaveTypeId = null,
        string? approvalStatus = null, int? currentUserEmployeeId = null)
    {
        var (apps, kpi) = await _leaveRepository.GetLeaveApplicationsAsync(
            companyId, employeeId, departmentId, leaveTypeId, approvalStatus);

        return new LeaveApplicationsPageViewModel
        {
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            LeaveTypeId = leaveTypeId,
            ApprovalStatus = approvalStatus,
            CurrentUserEmployeeId = currentUserEmployeeId,
            Kpi = kpi,
            Applications = apps.ToList(),
            LeaveTypes = (await _leaveRepository.GetLeaveTypesAsync(companyId)).Where(t => t.IsActive).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public Task<LeaveApplication?> GetApplicationByIdAsync(int companyId, int leaveApplicationId)
        => _leaveRepository.GetLeaveApplicationByIdAsync(companyId, leaveApplicationId);

    public async Task<OperationResult<int>> SubmitApplicationAsync(int companyId, LeaveApplyViewModel model, int userId, string username)
    {
        if (model.EndDate.Date < model.StartDate.Date)
        {
            return OperationResult<int>.Failure("End Date cannot be earlier than Start Date.");
        }

        if (model.IsHalfDay && model.EndDate.Date != model.StartDate.Date)
        {
            return OperationResult<int>.Failure("Half-day leave must be for a single day.");
        }

        try
        {
            var id = await _leaveRepository.SubmitLeaveApplicationAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Leave", "Application", id.ToString(),
                $"Submitted leave application #{id} for employee ID {model.EmployeeId} ({model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}).");
            return OperationResult<int>.Success(id, "Leave application submitted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewApplicationAsync(int companyId, LeaveReviewViewModel model, int reviewedBy, string username)
    {
        if (!string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model.ApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("Decision must be Approved or Rejected.");
        }

        try
        {
            model.ApprovalStatus = string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : "Rejected";

            await _leaveRepository.ReviewLeaveApplicationAsync(companyId, model, reviewedBy);
            await _auditLogService.LogAsync(companyId, reviewedBy, username,
                model.ApprovalStatus == "Approved" ? AuditActionType.Approve.ToString() : AuditActionType.Reject.ToString(),
                "Leave", "Application", model.LeaveApplicationId.ToString(),
                $"Marked leave application #{model.LeaveApplicationId} as {model.ApprovalStatus}.");
            return OperationResult.Success($"Leave application {model.ApprovalStatus.ToLowerInvariant()}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> CancelApplicationAsync(int companyId, int leaveApplicationId, int userId, string username)
    {
        try
        {
            await _leaveRepository.CancelLeaveApplicationAsync(companyId, leaveApplicationId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Leave", "Cancellation", leaveApplicationId.ToString(),
                $"Cancelled leave application #{leaveApplicationId}.");
            return OperationResult.Success("Leave application cancelled and balance restored.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<List<LeaveType>> GetLeaveTypesAsync(int companyId)
        => (await _leaveRepository.GetLeaveTypesAsync(companyId)).ToList();

    public async Task<OperationResult<int>> SaveLeaveTypeAsync(int companyId, LeaveTypeFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _leaveRepository.SaveLeaveTypeAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.LeaveTypeId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Leave", "LeaveType", id.ToString(),
                $"Saved leave type '{model.LeaveName}' ({model.LeaveCode}).");
            return OperationResult<int>.Success(id, "Leave policy saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteLeaveTypeAsync(int companyId, int leaveTypeId, int userId, string username)
    {
        try
        {
            await _leaveRepository.DeleteLeaveTypeAsync(companyId, leaveTypeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Leave", "LeaveType", leaveTypeId.ToString(), $"Deleted leave type ID {leaveTypeId}.");
            return OperationResult.Success("Leave type removed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<LeaveBalancesPageViewModel> GetBalancesPageAsync(int companyId, int leaveYear, int? employeeId = null, int? departmentId = null)
    {
        return new LeaveBalancesPageViewModel
        {
            LeaveYear = leaveYear,
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            Balances = (await _leaveRepository.GetLeaveBalancesAsync(companyId, leaveYear, employeeId, departmentId)).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public async Task<OperationResult> AllocateAnnualBalancesAsync(int companyId, int leaveYear, bool includeCarryForward, int userId, string username)
    {
        try
        {
            await _leaveRepository.AllocateAnnualBalancesAsync(companyId, leaveYear, includeCarryForward, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Leave", "BalanceAllocation", leaveYear.ToString(),
                $"Allocated annual leave balances for year {leaveYear} (CarryForward={includeCarryForward}).");
            return OperationResult.Success($"Leave balances allocated for {leaveYear}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> AdjustBalanceAsync(int companyId, LeaveBalanceAdjustViewModel model, int userId, string username)
    {
        await _leaveRepository.AdjustLeaveBalanceAsync(companyId, model, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
            "Leave", "BalanceAdjust", model.LeaveBalanceId.ToString(),
            $"Adjusted leave balance #{model.LeaveBalanceId} (Opening={model.OpeningBalance}, Accrued={model.AccruedDays}, Carry={model.CarryForwardDays}).");
        return OperationResult.Success("Leave balance updated.");
    }

    public async Task<LeaveCalendarPageViewModel> GetCalendarPageAsync(int companyId, int year, int month, int? departmentId = null)
    {
        return new LeaveCalendarPageViewModel
        {
            Year = year,
            Month = month,
            DepartmentId = departmentId,
            Events = (await _leaveRepository.GetCalendarEventsAsync(companyId, year, month, departmentId)).ToList(),
            Holidays = (await _attendanceRepository.GetHolidaysAsync(companyId, year)).Where(h => h.IsActive).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public async Task<LeaveEncashmentPageViewModel> GetEncashmentPageAsync(int companyId, string? filterStatus = null, int? currentUserEmployeeId = null)
    {
        return new LeaveEncashmentPageViewModel
        {
            FilterStatus = filterStatus,
            CurrentUserEmployeeId = currentUserEmployeeId,
            Encashments = (await _leaveRepository.GetLeaveEncashmentsAsync(companyId, filterStatus)).ToList(),
            EncashableTypes = (await _leaveRepository.GetLeaveTypesAsync(companyId)).Where(t => t.IsActive && t.AllowEncashment).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult<int>> SubmitEncashmentAsync(int companyId, LeaveEncashmentSubmitViewModel model, int userId, string username)
    {
        try
        {
            var id = await _leaveRepository.SubmitLeaveEncashmentAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Leave", "Encashment", id.ToString(),
                $"Submitted encashment for {model.DaysEncashed} day(s) for employee ID {model.EmployeeId}.");
            return OperationResult<int>.Success(id, "Leave encashment request submitted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewEncashmentAsync(int companyId, LeaveEncashmentReviewViewModel model, int reviewedBy, string username)
    {
        if (!string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model.ApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("Decision must be Approved or Rejected.");
        }

        try
        {
            model.ApprovalStatus = string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : "Rejected";

            await _leaveRepository.ReviewLeaveEncashmentAsync(companyId, model, reviewedBy);
            await _auditLogService.LogAsync(companyId, reviewedBy, username,
                model.ApprovalStatus == "Approved" ? AuditActionType.Approve.ToString() : AuditActionType.Reject.ToString(),
                "Leave", "Encashment", model.EncashmentId.ToString(),
                $"Marked leave encashment #{model.EncashmentId} as {model.ApprovalStatus}.");
            return OperationResult.Success($"Leave encashment {model.ApprovalStatus.ToLowerInvariant()}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
