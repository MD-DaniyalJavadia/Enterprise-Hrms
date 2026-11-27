using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Attendance;

namespace HRMS.Data.Repositories;

public sealed class AttendanceRepository : BaseRepository, IAttendanceRepository
{
    public AttendanceRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(IEnumerable<AttendanceRecord> Records, DailyAttendanceKpiViewModel Kpi)> GetDailyRegisterAsync(
        int companyId, DateTime attendanceDate, int? branchId = null, int? departmentId = null, string? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
        p.Add("@BranchId", branchId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("sp_Attendance_GetDailyRegister", p, commandType: CommandType.StoredProcedure);
        var records = (await multi.ReadAsync<AttendanceRecord>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<DailyAttendanceKpiViewModel>() ?? new DailyAttendanceKpiViewModel();
        return (records, kpi);
    }

    public Task CheckInAsync(int companyId, int employeeId, DateTime attendanceDate, DateTime checkInTime, string? checkInIp, string? remarks, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
        p.Add("@CheckInTime", checkInTime, DbType.DateTime2);
        p.Add("@CheckInIp", checkInIp, DbType.String, size: 45);
        p.Add("@Remarks", remarks, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Attendance_CheckIn", p);
    }

    public Task CheckOutAsync(int companyId, int employeeId, DateTime attendanceDate, DateTime checkOutTime, string? remarks, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
        p.Add("@CheckOutTime", checkOutTime, DbType.DateTime2);
        p.Add("@Remarks", remarks, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Attendance_CheckOut", p);
    }

    public Task MarkManualAsync(
        int companyId, int employeeId, DateTime attendanceDate, string status,
        DateTime? checkInTime, DateTime? checkOutTime, int? lateMinutes, int? overtimeMinutes,
        string source, string? remarks, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
        p.Add("@Status", status, DbType.String, size: 20);
        p.Add("@CheckInTime", checkInTime, DbType.DateTime2);
        p.Add("@CheckOutTime", checkOutTime, DbType.DateTime2);
        p.Add("@LateMinutes", lateMinutes, DbType.Int32);
        p.Add("@OvertimeMinutes", overtimeMinutes, DbType.Int32);
        p.Add("@Source", source, DbType.String, size: 30);
        p.Add("@Remarks", remarks, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_Attendance_MarkManual", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task<IEnumerable<MonthlyAttendanceReportItemViewModel>> GetMonthlyReportAsync(
        int companyId, int year, int month, int? branchId = null, int? departmentId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);
        p.Add("@Month", month, DbType.Int32);
        p.Add("@BranchId", branchId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        return QuerySpAsync<MonthlyAttendanceReportItemViewModel>("sp_Attendance_GetMonthlyReport", p);
    }

    public Task<IEnumerable<AttendanceRecord>> GetEmployeeMonthLogAsync(int companyId, int employeeId, int year, int month)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);
        p.Add("@Month", month, DbType.Int32);
        return QuerySpAsync<AttendanceRecord>("sp_Attendance_GetEmployeeMonthLog", p);
    }

    public Task<IEnumerable<ShiftAssignment>> GetShiftAssignmentsAsync(int companyId, int? employeeId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        return QuerySpAsync<ShiftAssignment>("sp_ShiftAssignment_GetAll", p);
    }

    public async Task<int> SaveShiftAssignmentAsync(int companyId, ShiftAssignmentFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@AssignmentId", model.AssignmentId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@ShiftId", model.ShiftId, DbType.Int32);
        p.Add("@EffectiveFrom", model.EffectiveFrom.Date, DbType.Date);
        p.Add("@EffectiveTo", model.EffectiveTo?.Date, DbType.Date);
        p.Add("@Notes", model.Notes, DbType.String, size: 300);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_ShiftAssignment_Save", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
        return p.Get<int>("@SavedId");
    }

    public Task DeleteShiftAssignmentAsync(int companyId, int assignmentId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@AssignmentId", assignmentId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_ShiftAssignment_Delete", p);
    }

    public Task<IEnumerable<Holiday>> GetHolidaysAsync(int companyId, int? year = null, int? branchId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);
        p.Add("@BranchId", branchId, DbType.Int32);
        return QuerySpAsync<Holiday>("sp_Holiday_GetAll", p);
    }

    public async Task<int> SaveHolidayAsync(int companyId, HolidayFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@HolidayId", model.HolidayId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@BranchId", model.BranchId, DbType.Int32);
        p.Add("@HolidayName", model.HolidayName.Trim(), DbType.String, size: 120);
        p.Add("@HolidayDate", model.HolidayDate.Date, DbType.Date);
        p.Add("@HolidayType", model.HolidayType, DbType.String, size: 30);
        p.Add("@Description", model.Description, DbType.String, size: 300);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Holiday_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteHolidayAsync(int companyId, int holidayId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@HolidayId", holidayId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Holiday_Delete", p);
    }

    public Task<IEnumerable<AttendanceRegularization>> GetRegularizationsAsync(int companyId, int? employeeId = null, string? approvalStatus = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@ApprovalStatus", string.IsNullOrWhiteSpace(approvalStatus) ? null : approvalStatus.Trim(), DbType.String, size: 20);
        return QuerySpAsync<AttendanceRegularization>("sp_AttendanceRegularization_GetAll", p);
    }

    public async Task<int> SubmitRegularizationAsync(int companyId, RegularizationSubmitViewModel model, int userId)
    {
        DateTime? reqIn = model.RequestedCheckIn.HasValue ? model.AttendanceDate.Date.Add(model.RequestedCheckIn.Value) : null;
        DateTime? reqOut = model.RequestedCheckOut.HasValue ? model.AttendanceDate.Date.Add(model.RequestedCheckOut.Value) : null;

        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@AttendanceDate", model.AttendanceDate.Date, DbType.Date);
        p.Add("@RequestedCheckIn", reqIn, DbType.DateTime2);
        p.Add("@RequestedCheckOut", reqOut, DbType.DateTime2);
        p.Add("@RequestedStatus", model.RequestedStatus, DbType.String, size: 20);
        p.Add("@Reason", model.Reason.Trim(), DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_AttendanceRegularization_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task ReviewRegularizationAsync(int companyId, RegularizationReviewViewModel model, int reviewedBy)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@RegularizationId", model.RegularizationId, DbType.Int32);
        p.Add("@ApprovalStatus", model.ApprovalStatus, DbType.String, size: 20);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@ReviewedBy", reviewedBy, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_AttendanceRegularization_Review", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task<IEnumerable<OvertimeRequest>> GetOvertimeRequestsAsync(int companyId, int? employeeId = null, string? approvalStatus = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@ApprovalStatus", string.IsNullOrWhiteSpace(approvalStatus) ? null : approvalStatus.Trim(), DbType.String, size: 20);
        return QuerySpAsync<OvertimeRequest>("sp_OvertimeRequest_GetAll", p);
    }

    public async Task<int> SubmitOvertimeAsync(int companyId, OvertimeSubmitViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@OvertimeDate", model.OvertimeDate.Date, DbType.Date);
        p.Add("@RequestedHours", model.RequestedHours, DbType.Decimal);
        p.Add("@Reason", model.Reason.Trim(), DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_OvertimeRequest_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task ReviewOvertimeAsync(int companyId, OvertimeReviewViewModel model, int reviewedBy)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@OvertimeRequestId", model.OvertimeRequestId, DbType.Int32);
        p.Add("@ApprovalStatus", model.ApprovalStatus, DbType.String, size: 20);
        p.Add("@ApprovedHours", model.ApprovedHours, DbType.Decimal);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@ReviewedBy", reviewedBy, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_OvertimeRequest_Review", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }
}
