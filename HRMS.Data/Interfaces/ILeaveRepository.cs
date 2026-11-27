using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Leave;

namespace HRMS.Data.Interfaces;

public interface ILeaveRepository
{
    Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(int companyId);
    Task<int> SaveLeaveTypeAsync(int companyId, LeaveTypeFormViewModel model, int userId);
    Task DeleteLeaveTypeAsync(int companyId, int leaveTypeId, int userId);

    Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(int companyId, int leaveYear, int? employeeId = null, int? departmentId = null);
    Task AllocateAnnualBalancesAsync(int companyId, int leaveYear, bool includeCarryForward, int userId);
    Task AdjustLeaveBalanceAsync(int companyId, LeaveBalanceAdjustViewModel model, int userId);

    Task<(IEnumerable<LeaveApplication> Applications, LeaveKpiSummaryViewModel Kpi)> GetLeaveApplicationsAsync(
        int companyId, int? employeeId = null, int? departmentId = null, int? leaveTypeId = null, string? approvalStatus = null);
    Task<LeaveApplication?> GetLeaveApplicationByIdAsync(int companyId, int leaveApplicationId);
    Task<int> SubmitLeaveApplicationAsync(int companyId, LeaveApplyViewModel model, int userId);
    Task ReviewLeaveApplicationAsync(int companyId, LeaveReviewViewModel model, int reviewedBy);
    Task CancelLeaveApplicationAsync(int companyId, int leaveApplicationId, int userId);

    Task<IEnumerable<LeaveCalendarEventViewModel>> GetCalendarEventsAsync(int companyId, int year, int month, int? departmentId = null);

    Task<IEnumerable<LeaveEncashment>> GetLeaveEncashmentsAsync(int companyId, string? approvalStatus = null);
    Task<int> SubmitLeaveEncashmentAsync(int companyId, LeaveEncashmentSubmitViewModel model, int userId);
    Task ReviewLeaveEncashmentAsync(int companyId, LeaveEncashmentReviewViewModel model, int reviewedBy);
}
