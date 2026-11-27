using System.Text;
using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Attendance;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("ATTENDANCE.VIEW")]
public sealed class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date = null, int? branchId = null, int? departmentId = null, string? status = null, string? search = null)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        var model = await _attendanceService.GetDailyRegisterPageAsync(
            User.GetCompanyId(), targetDate, branchId, departmentId, status, search, User.GetEmployeeId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn([FromForm] QuickPunchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Please select an employee."));
        }

        if (!User.HasPermission("ATTENDANCE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _attendanceService.CheckInAsync(User.GetCompanyId(), model, ip, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut([FromForm] QuickPunchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Please select an employee."));
        }

        if (!User.HasPermission("ATTENDANCE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _attendanceService.CheckOutAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkManual([FromForm] ManualAttendanceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.MarkManualAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportDaily(DateTime? date = null, int? branchId = null, int? departmentId = null, string? status = null, string? search = null)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        var model = await _attendanceService.GetDailyRegisterPageAsync(
            User.GetCompanyId(), targetDate, branchId, departmentId, status, search);

        var bytes = CsvExportHelper.BuildCsv(model.Records, new (string, Func<HRMS.Models.Entities.AttendanceRecord, object?>)[]
        {
            ("Date", x => x.AttendanceDate.ToString("yyyy-MM-dd")),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Department", x => x.DepartmentName),
            ("Branch", x => x.BranchName),
            ("Shift", x => x.ShiftName),
            ("Check In", x => x.CheckInTime?.ToString("HH:mm")),
            ("Check Out", x => x.CheckOutTime?.ToString("HH:mm")),
            ("Status", x => x.Status),
            ("Late (Min)", x => x.LateMinutes),
            ("Work Hours", x => x.WorkHoursFormatted),
            ("Overtime (Min)", x => x.OvertimeMinutes),
            ("Source", x => x.Source)
        });

        return File(bytes, "text/csv", $"Attendance_{targetDate:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyReport(int? year = null, int? month = null, int? branchId = null, int? departmentId = null)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        if (m < 1 || m > 12) m = DateTime.Today.Month;

        var model = await _attendanceService.GetMonthlyReportPageAsync(User.GetCompanyId(), y, m, branchId, departmentId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportMonthly(int? year = null, int? month = null, int? branchId = null, int? departmentId = null)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        var model = await _attendanceService.GetMonthlyReportPageAsync(User.GetCompanyId(), y, m, branchId, departmentId);

        var bytes = CsvExportHelper.BuildCsv(model.Items, new (string, Func<MonthlyAttendanceReportItemViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Branch", x => x.BranchName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Present Days", x => x.PresentDays),
            ("Late Days", x => x.LateDays),
            ("Absent Days", x => x.AbsentDays),
            ("Half Days", x => x.HalfDays),
            ("Leave Days", x => x.LeaveDays),
            ("Total Work Hours", x => x.TotalWorkHours),
            ("Total Late (Min)", x => x.TotalLateMinutes),
            ("Total OT Hours", x => x.TotalOvertimeHours)
        });

        return File(bytes, "text/csv", $"MonthlyAttendance_{y}_{m:D2}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> EmployeeCalendar(int? employeeId = null, int? year = null, int? month = null)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        if (m < 1 || m > 12) m = DateTime.Today.Month;

        var model = await _attendanceService.GetEmployeeCalendarAsync(User.GetCompanyId(), employeeId ?? User.GetEmployeeId(), y, m);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Roster(int? employeeId = null)
    {
        var model = await _attendanceService.GetShiftRosterPageAsync(User.GetCompanyId(), employeeId);
        return View(model);
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShiftAssignment([FromForm] ShiftAssignmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.SaveShiftAssignmentAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShiftAssignment(int id)
    {
        var result = await _attendanceService.DeleteShiftAssignmentAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Holidays(int? year = null, int? branchId = null)
    {
        var model = await _attendanceService.GetHolidayPageAsync(User.GetCompanyId(), year ?? DateTime.Today.Year, branchId);
        return View(model);
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHoliday([FromForm] HolidayFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.SaveHolidayAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var result = await _attendanceService.DeleteHolidayAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Regularizations(string? status = null)
    {
        var model = await _attendanceService.GetRegularizationsPageAsync(User.GetCompanyId(), status, User.GetEmployeeId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRegularization([FromForm] RegularizationSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("ATTENDANCE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _attendanceService.SubmitRegularizationAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewRegularization([FromForm] RegularizationReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid review submission."));
        }

        var result = await _attendanceService.ReviewRegularizationAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOvertime([FromForm] OvertimeSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("ATTENDANCE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _attendanceService.SubmitOvertimeAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<int>.Ok(result.Data, result.Message) : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewOvertime([FromForm] OvertimeReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid overtime review submission."));
        }

        var result = await _attendanceService.ReviewOvertimeAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpGet]
    public IActionResult Import()
    {
        return View(new AttendanceImportResultViewModel());
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? csvFile)
    {
        if (csvFile is null || csvFile.Length == 0 || !Path.GetExtension(csvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Please upload a valid .csv file.");
            return View(new AttendanceImportResultViewModel());
        }

        await using var stream = csvFile.OpenReadStream();
        var result = await _attendanceService.ImportFromCsvStreamAsync(User.GetCompanyId(), stream, User.GetUserId(), User.Identity?.Name ?? "system");
        if (result.ImportedCount > 0)
        {
            TempData["SuccessMessage"] = $"Successfully imported {result.ImportedCount} attendance record(s).";
        }
        return View(result);
    }

    [HasPermission("ATTENDANCE.MANAGE")]
    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        var csv = "EmployeeCode,AttendanceDate,Status,CheckInTime,CheckOutTime,Remarks\r\n" +
                  $"EMP-1001,{DateTime.Today:yyyy-MM-dd},Present,08:55,18:05,On time\r\n" +
                  $"EMP-1002,{DateTime.Today:yyyy-MM-dd},Late,09:25,18:10,Traffic delay\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", "Attendance_Import_Template.csv");
    }
}
