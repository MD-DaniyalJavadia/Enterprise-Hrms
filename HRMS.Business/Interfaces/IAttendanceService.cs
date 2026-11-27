using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Attendance;

namespace HRMS.Business.Interfaces;

public interface IAttendanceService
{
    Task<DailyAttendancePageViewModel> GetDailyRegisterPageAsync(
        int companyId, DateTime attendanceDate, int? branchId = null, int? departmentId = null, string? status = null, string? searchTerm = null, int? currentUserEmployeeId = null);

    Task<OperationResult> CheckInAsync(int companyId, QuickPunchViewModel model, string? ipAddress, int userId, string username);
    Task<OperationResult> CheckOutAsync(int companyId, QuickPunchViewModel model, int userId, string username);
    Task<OperationResult> MarkManualAsync(int companyId, ManualAttendanceViewModel model, int userId, string username);

    Task<MonthlyAttendanceReportPageViewModel> GetMonthlyReportPageAsync(int companyId, int year, int month, int? branchId = null, int? departmentId = null);
    Task<EmployeeCalendarViewModel> GetEmployeeCalendarAsync(int companyId, int? employeeId, int year, int month);

    Task<ShiftRosterPageViewModel> GetShiftRosterPageAsync(int companyId, int? employeeId = null);
    Task<OperationResult<int>> SaveShiftAssignmentAsync(int companyId, ShiftAssignmentFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteShiftAssignmentAsync(int companyId, int assignmentId, int userId, string username);

    Task<HolidayPageViewModel> GetHolidayPageAsync(int companyId, int year, int? branchId = null);
    Task<OperationResult<int>> SaveHolidayAsync(int companyId, HolidayFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteHolidayAsync(int companyId, int holidayId, int userId, string username);

    Task<RegularizationsPageViewModel> GetRegularizationsPageAsync(int companyId, string? filterStatus = null, int? currentUserEmployeeId = null);
    Task<OperationResult<int>> SubmitRegularizationAsync(int companyId, RegularizationSubmitViewModel model, int userId, string username);
    Task<OperationResult> ReviewRegularizationAsync(int companyId, RegularizationReviewViewModel model, int reviewedBy, string username);

    Task<OperationResult<int>> SubmitOvertimeAsync(int companyId, OvertimeSubmitViewModel model, int userId, string username);
    Task<OperationResult> ReviewOvertimeAsync(int companyId, OvertimeReviewViewModel model, int reviewedBy, string username);

    Task<AttendanceImportResultViewModel> ImportFromCsvStreamAsync(int companyId, Stream csvStream, int userId, string username);
}
