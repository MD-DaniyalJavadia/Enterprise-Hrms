using System.Globalization;
using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Attendance;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class AttendanceService : IAttendanceService
{
    private static readonly HashSet<string> ValidAttendanceStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Present", "Late", "Absent", "HalfDay", "OnLeave"
    };

    private static readonly HashSet<string> ValidHolidayTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Public", "Company", "Optional"
    };

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IAuditLogService _auditLogService;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IDepartmentRepository departmentRepository,
        IOrgStructureRepository orgStructureRepository,
        IAuditLogService auditLogService)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _departmentRepository = departmentRepository;
        _orgStructureRepository = orgStructureRepository;
        _auditLogService = auditLogService;
    }

    public async Task<DailyAttendancePageViewModel> GetDailyRegisterPageAsync(
        int companyId, DateTime attendanceDate, int? branchId = null, int? departmentId = null,
        string? status = null, string? searchTerm = null, int? currentUserEmployeeId = null)
    {
        var (records, kpi) = await _attendanceRepository.GetDailyRegisterAsync(
            companyId, attendanceDate, branchId, departmentId, status, searchTerm);

        return new DailyAttendancePageViewModel
        {
            AttendanceDate = attendanceDate.Date,
            BranchId = branchId,
            DepartmentId = departmentId,
            Status = status,
            SearchTerm = searchTerm,
            CurrentUserEmployeeId = currentUserEmployeeId,
            Kpi = kpi,
            Records = records.ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult> CheckInAsync(int companyId, QuickPunchViewModel model, string? ipAddress, int userId, string username)
    {
        try
        {
            var now = DateTime.Now;
            await _attendanceRepository.CheckInAsync(companyId, model.EmployeeId, now.Date, now, ipAddress, model.Remarks, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Attendance", "CheckIn", model.EmployeeId.ToString(), $"Recorded check-in for employee ID {model.EmployeeId} at {now:HH:mm}.");
            return OperationResult.Success($"Check-in recorded at {now:hh:mm tt}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> CheckOutAsync(int companyId, QuickPunchViewModel model, int userId, string username)
    {
        try
        {
            var now = DateTime.Now;
            await _attendanceRepository.CheckOutAsync(companyId, model.EmployeeId, now.Date, now, model.Remarks, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Attendance", "CheckOut", model.EmployeeId.ToString(), $"Recorded check-out for employee ID {model.EmployeeId} at {now:HH:mm}.");
            return OperationResult.Success($"Check-out recorded at {now:hh:mm tt}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> MarkManualAsync(int companyId, ManualAttendanceViewModel model, int userId, string username)
    {
        if (!ValidAttendanceStatuses.Contains(model.Status))
        {
            return OperationResult.Failure("Invalid attendance status selected.");
        }

        DateTime? checkIn = model.CheckInTime.HasValue ? model.AttendanceDate.Date.Add(model.CheckInTime.Value) : null;
        DateTime? checkOut = model.CheckOutTime.HasValue ? model.AttendanceDate.Date.Add(model.CheckOutTime.Value) : null;

        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value)
        {
            return OperationResult.Failure("Check-out time cannot be earlier than check-in time.");
        }

        try
        {
            await _attendanceRepository.MarkManualAsync(
                companyId, model.EmployeeId, model.AttendanceDate.Date, model.Status,
                checkIn, checkOut, model.LateMinutes, model.OvertimeMinutes, "Manual", model.Remarks, userId);

            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Attendance", "ManualEntry", model.EmployeeId.ToString(),
                $"Marked {model.Status} for employee ID {model.EmployeeId} on {model.AttendanceDate:yyyy-MM-dd}.");
            return OperationResult.Success("Attendance record saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<MonthlyAttendanceReportPageViewModel> GetMonthlyReportPageAsync(
        int companyId, int year, int month, int? branchId = null, int? departmentId = null)
    {
        return new MonthlyAttendanceReportPageViewModel
        {
            Year = year,
            Month = month,
            BranchId = branchId,
            DepartmentId = departmentId,
            Items = (await _attendanceRepository.GetMonthlyReportAsync(companyId, year, month, branchId, departmentId)).ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public async Task<EmployeeCalendarViewModel> GetEmployeeCalendarAsync(int companyId, int? employeeId, int year, int month)
    {
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();
        var selected = employeeId.HasValue
            ? employees.FirstOrDefault(e => e.EmployeeId == employeeId.Value)
            : employees.FirstOrDefault();

        var vm = new EmployeeCalendarViewModel
        {
            Year = year,
            Month = month,
            Employees = employees,
            Holidays = (await _attendanceRepository.GetHolidaysAsync(companyId, year)).Where(h => h.IsActive).ToList()
        };

        if (selected is not null)
        {
            vm.EmployeeId = selected.EmployeeId;
            vm.EmployeeCode = selected.EmployeeCode;
            vm.EmployeeName = selected.FullName;
            vm.DepartmentName = selected.DepartmentName;
            vm.DesignationTitle = selected.DesignationTitle;
            vm.Logs = (await _attendanceRepository.GetEmployeeMonthLogAsync(companyId, selected.EmployeeId, year, month)).ToList();
        }

        return vm;
    }

    public async Task<ShiftRosterPageViewModel> GetShiftRosterPageAsync(int companyId, int? employeeId = null)
    {
        return new ShiftRosterPageViewModel
        {
            FilterEmployeeId = employeeId,
            Assignments = (await _attendanceRepository.GetShiftAssignmentsAsync(companyId, employeeId)).ToList(),
            Shifts = (await _orgStructureRepository.GetShiftsAsync(companyId)).Where(s => s.IsActive).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveShiftAssignmentAsync(int companyId, ShiftAssignmentFormViewModel model, int userId, string username)
    {
        if (model.EffectiveTo.HasValue && model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
        {
            return OperationResult<int>.Failure("Effective To date cannot precede Effective From date.");
        }

        try
        {
            var id = await _attendanceRepository.SaveShiftAssignmentAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.AssignmentId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Attendance", "ShiftAssignment", id.ToString(),
                $"Assigned shift ID {model.ShiftId} to employee ID {model.EmployeeId} from {model.EffectiveFrom:yyyy-MM-dd}.");
            return OperationResult<int>.Success(id, "Shift assignment saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteShiftAssignmentAsync(int companyId, int assignmentId, int userId, string username)
    {
        await _attendanceRepository.DeleteShiftAssignmentAsync(companyId, assignmentId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Attendance", "ShiftAssignment", assignmentId.ToString(), $"Removed shift assignment ID {assignmentId}.");
        return OperationResult.Success("Shift assignment removed.");
    }

    public async Task<HolidayPageViewModel> GetHolidayPageAsync(int companyId, int year, int? branchId = null)
    {
        return new HolidayPageViewModel
        {
            Year = year,
            BranchId = branchId,
            Holidays = (await _attendanceRepository.GetHolidaysAsync(companyId, year, branchId)).ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveHolidayAsync(int companyId, HolidayFormViewModel model, int userId, string username)
    {
        if (!ValidHolidayTypes.Contains(model.HolidayType))
        {
            return OperationResult<int>.Failure("Invalid holiday type.");
        }

        try
        {
            var id = await _attendanceRepository.SaveHolidayAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.HolidayId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Attendance", "Holiday", id.ToString(),
                $"Saved holiday '{model.HolidayName}' on {model.HolidayDate:yyyy-MM-dd}.");
            return OperationResult<int>.Success(id, "Holiday saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteHolidayAsync(int companyId, int holidayId, int userId, string username)
    {
        await _attendanceRepository.DeleteHolidayAsync(companyId, holidayId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Attendance", "Holiday", holidayId.ToString(), $"Deleted holiday ID {holidayId}.");
        return OperationResult.Success("Holiday removed.");
    }

    public async Task<RegularizationsPageViewModel> GetRegularizationsPageAsync(
        int companyId, string? filterStatus = null, int? currentUserEmployeeId = null)
    {
        return new RegularizationsPageViewModel
        {
            FilterStatus = filterStatus,
            CurrentUserEmployeeId = currentUserEmployeeId,
            Regularizations = (await _attendanceRepository.GetRegularizationsAsync(companyId, approvalStatus: filterStatus)).ToList(),
            OvertimeRequests = (await _attendanceRepository.GetOvertimeRequestsAsync(companyId, approvalStatus: filterStatus)).ToList(),
            Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
        };
    }

    public async Task<OperationResult<int>> SubmitRegularizationAsync(int companyId, RegularizationSubmitViewModel model, int userId, string username)
    {
        if (!ValidAttendanceStatuses.Contains(model.RequestedStatus))
        {
            return OperationResult<int>.Failure("Invalid requested attendance status.");
        }

        if (model.RequestedCheckIn.HasValue && model.RequestedCheckOut.HasValue && model.RequestedCheckOut.Value < model.RequestedCheckIn.Value)
        {
            return OperationResult<int>.Failure("Check-out time cannot be earlier than check-in time.");
        }

        try
        {
            var id = await _attendanceRepository.SubmitRegularizationAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Attendance", "Regularization", id.ToString(),
                $"Submitted attendance regularization for employee ID {model.EmployeeId} ({model.AttendanceDate:yyyy-MM-dd}).");
            return OperationResult<int>.Success(id, "Regularization request submitted for approval.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewRegularizationAsync(int companyId, RegularizationReviewViewModel model, int reviewedBy, string username)
    {
        if (!string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model.ApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("Review decision must be Approved or Rejected.");
        }

        try
        {
            model.ApprovalStatus = string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : "Rejected";

            await _attendanceRepository.ReviewRegularizationAsync(companyId, model, reviewedBy);
            await _auditLogService.LogAsync(companyId, reviewedBy, username,
                model.ApprovalStatus == "Approved" ? AuditActionType.Approve.ToString() : AuditActionType.Reject.ToString(),
                "Attendance", "Regularization", model.RegularizationId.ToString(),
                $"Marked regularization #{model.RegularizationId} as {model.ApprovalStatus}.");
            return OperationResult.Success($"Regularization request {model.ApprovalStatus.ToLowerInvariant()}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SubmitOvertimeAsync(int companyId, OvertimeSubmitViewModel model, int userId, string username)
    {
        try
        {
            var id = await _attendanceRepository.SubmitOvertimeAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Attendance", "Overtime", id.ToString(),
                $"Submitted {model.RequestedHours:0.##}h overtime request for employee ID {model.EmployeeId} ({model.OvertimeDate:yyyy-MM-dd}).");
            return OperationResult<int>.Success(id, "Overtime request submitted for approval.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewOvertimeAsync(int companyId, OvertimeReviewViewModel model, int reviewedBy, string username)
    {
        if (!string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(model.ApprovalStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("Review decision must be Approved or Rejected.");
        }

        try
        {
            model.ApprovalStatus = string.Equals(model.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : "Rejected";

            await _attendanceRepository.ReviewOvertimeAsync(companyId, model, reviewedBy);
            await _auditLogService.LogAsync(companyId, reviewedBy, username,
                model.ApprovalStatus == "Approved" ? AuditActionType.Approve.ToString() : AuditActionType.Reject.ToString(),
                "Attendance", "Overtime", model.OvertimeRequestId.ToString(),
                $"Marked overtime #{model.OvertimeRequestId} as {model.ApprovalStatus} ({model.ApprovedHours:0.##}h).");
            return OperationResult.Success($"Overtime request {model.ApprovalStatus.ToLowerInvariant()}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<AttendanceImportResultViewModel> ImportFromCsvStreamAsync(int companyId, Stream csvStream, int userId, string username)
    {
        var result = new AttendanceImportResultViewModel();
        var employeesByCode = (await _employeeRepository.GetAllAsync(companyId))
            .ToDictionary(e => e.EmployeeCode.Trim().ToUpperInvariant(), e => e.EmployeeId);

        using var reader = new StreamReader(csvStream);
        var header = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header))
        {
            result.RowErrors.Add("The uploaded CSV file is empty.");
            return result;
        }

        int lineNo = 1;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRowsProcessed++;
            var cols = ParseCsvRow(line);
            if (cols.Count < 3)
            {
                result.RowErrors.Add($"Row {lineNo}: Expected columns EmployeeCode,AttendanceDate,Status,CheckInTime,CheckOutTime,Remarks.");
                continue;
            }

            var empCode = cols[0].Trim().ToUpperInvariant();
            var dateRaw = cols[1].Trim();
            var status = cols[2].Trim();
            var inRaw = cols.Count > 3 ? cols[3].Trim() : string.Empty;
            var outRaw = cols.Count > 4 ? cols[4].Trim() : string.Empty;
            var remarks = cols.Count > 5 ? cols[5].Trim() : null;

            if (!employeesByCode.TryGetValue(empCode, out int employeeId))
            {
                result.RowErrors.Add($"Row {lineNo}: Unknown EmployeeCode '{empCode}'.");
                continue;
            }

            if (!DateTime.TryParseExact(dateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var attDate))
            {
                result.RowErrors.Add($"Row {lineNo}: Invalid AttendanceDate '{dateRaw}' (use yyyy-MM-dd).");
                continue;
            }

            if (!ValidAttendanceStatuses.Contains(status))
            {
                result.RowErrors.Add($"Row {lineNo}: Invalid Status '{status}'. Use Present, Late, Absent, HalfDay, or OnLeave.");
                continue;
            }

            DateTime? checkIn = null;
            DateTime? checkOut = null;
            if (!string.IsNullOrEmpty(inRaw))
            {
                if (!TimeSpan.TryParse(inRaw, CultureInfo.InvariantCulture, out var tsIn))
                {
                    result.RowErrors.Add($"Row {lineNo}: Invalid CheckInTime '{inRaw}' (use HH:mm).");
                    continue;
                }
                checkIn = attDate.Date.Add(tsIn);
            }

            if (!string.IsNullOrEmpty(outRaw))
            {
                if (!TimeSpan.TryParse(outRaw, CultureInfo.InvariantCulture, out var tsOut))
                {
                    result.RowErrors.Add($"Row {lineNo}: Invalid CheckOutTime '{outRaw}' (use HH:mm).");
                    continue;
                }
                checkOut = attDate.Date.Add(tsOut);
            }

            try
            {
                await _attendanceRepository.MarkManualAsync(
                    companyId, employeeId, attDate, status, checkIn, checkOut,
                    null, null, "BulkImport", remarks, userId);
                result.ImportedCount++;
            }
            catch (SqlException ex)
            {
                result.RowErrors.Add($"Row {lineNo} ({empCode}): {ex.Message}");
            }
        }

        if (result.ImportedCount > 0)
        {
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Attendance", "BulkImport", null, $"Imported {result.ImportedCount} attendance record(s) via CSV.");
        }

        return result;
    }

    private static List<string> ParseCsvRow(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        values.Add(current.ToString());
        return values;
    }
}
