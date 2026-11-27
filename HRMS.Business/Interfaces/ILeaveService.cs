using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Leave;

namespace HRMS.Business.Interfaces;

public interface ILeaveService
{
    Task<LeaveApplicationsPageViewModel> GetApplicationsPageAsync(
        int companyId, int? employeeId = null, int? departmentId = null, int? leaveTypeId = null, string? approvalStatus = null, int? currentUserEmployeeId = null);
    Task<LeaveApplication?> GetApplicationByIdAsync(int companyId, int leaveApplicationId);
    Task<OperationResult<int>> SubmitApplicationAsync(int companyId, LeaveApplyViewModel model, int userId, string username);
    Task<OperationResult> ReviewApplicationAsync(int companyId, LeaveReviewViewModel model, int reviewedBy, string username);
    Task<OperationResult> CancelApplicationAsync(int companyId, int leaveApplicationId, int userId, string username);

    Task<List<LeaveType>> GetLeaveTypesAsync(int companyId);
    Task<OperationResult<int>> SaveLeaveTypeAsync(int companyId, LeaveTypeFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteLeaveTypeAsync(int companyId, int leaveTypeId, int userId, string username);

    Task<LeaveBalancesPageViewModel> GetBalancesPageAsync(int companyId, int leaveYear, int? employeeId = null, int? departmentId = null);
    Task<OperationResult> AllocateAnnualBalancesAsync(int companyId, int leaveYear, bool includeCarryForward, int userId, string username);
    Task<OperationResult> AdjustBalanceAsync(int companyId, LeaveBalanceAdjustViewModel model, int userId, string username);

    Task<LeaveCalendarPageViewModel> GetCalendarPageAsync(int companyId, int year, int month, int? departmentId = null);

    Task<LeaveEncashmentPageViewModel> GetEncashmentPageAsync(int companyId, string? filterStatus = null, int? currentUserEmployeeId = null);
    Task<OperationResult<int>> SubmitEncashmentAsync(int companyId, LeaveEncashmentSubmitViewModel model, int userId, string username);
    Task<OperationResult> ReviewEncashmentAsync(int companyId, LeaveEncashmentReviewViewModel model, int reviewedBy, string username);
}
