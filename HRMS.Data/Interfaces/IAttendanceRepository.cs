using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Attendance;

namespace HRMS.Data.Interfaces;

public interface IAttendanceRepository
{
    Task<(IEnumerable<AttendanceRecord> Records, DailyAttendanceKpiViewModel Kpi)> GetDailyRegisterAsync(
        int companyId, DateTime attendanceDate, int? branchId = null, int? departmentId = null, string? status = null, string? searchTerm = null);

    Task CheckInAsync(int companyId, int employeeId, DateTime attendanceDate, DateTime checkInTime, string? checkInIp, string? remarks, int userId);
    Task CheckOutAsync(int companyId, int employeeId, DateTime attendanceDate, DateTime checkOutTime, string? remarks, int userId);
    Task MarkManualAsync(int companyId, int employeeId, DateTime attendanceDate, string status, DateTime? checkInTime, DateTime? checkOutTime, int? lateMinutes, int? overtimeMinutes, string source, string? remarks, int userId);

    Task<IEnumerable<MonthlyAttendanceReportItemViewModel>> GetMonthlyReportAsync(int companyId, int year, int month, int? branchId = null, int? departmentId = null);
    Task<IEnumerable<AttendanceRecord>> GetEmployeeMonthLogAsync(int companyId, int employeeId, int year, int month);

    Task<IEnumerable<ShiftAssignment>> GetShiftAssignmentsAsync(int companyId, int? employeeId = null);
    Task<int> SaveShiftAssignmentAsync(int companyId, ShiftAssignmentFormViewModel model, int userId);
    Task DeleteShiftAssignmentAsync(int companyId, int assignmentId, int userId);

    Task<IEnumerable<Holiday>> GetHolidaysAsync(int companyId, int? year = null, int? branchId = null);
    Task<int> SaveHolidayAsync(int companyId, HolidayFormViewModel model, int userId);
    Task DeleteHolidayAsync(int companyId, int holidayId, int userId);

    Task<IEnumerable<AttendanceRegularization>> GetRegularizationsAsync(int companyId, int? employeeId = null, string? approvalStatus = null);
    Task<int> SubmitRegularizationAsync(int companyId, RegularizationSubmitViewModel model, int userId);
    Task ReviewRegularizationAsync(int companyId, RegularizationReviewViewModel model, int reviewedBy);

    Task<IEnumerable<OvertimeRequest>> GetOvertimeRequestsAsync(int companyId, int? employeeId = null, string? approvalStatus = null);
    Task<int> SubmitOvertimeAsync(int companyId, OvertimeSubmitViewModel model, int userId);
    Task ReviewOvertimeAsync(int companyId, OvertimeReviewViewModel model, int reviewedBy);
}
