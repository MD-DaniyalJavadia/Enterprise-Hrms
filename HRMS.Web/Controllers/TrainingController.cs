using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.PerformanceTraining;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("TRAINING.VIEW")]
public sealed class TrainingController : Controller
{
    private readonly IPerformanceTrainingService _service;

    public TrainingController(IPerformanceTrainingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category = null, string? status = null, string? search = null)
    {
        var model = await _service.GetTrainingProgramsPageAsync(User.GetCompanyId(), category, status, search);
        return View(model);
    }

    [HasPermission("TRAINING.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProgram([FromForm] TrainingProgramFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveTrainingProgramAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("TRAINING.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProgram(int id)
    {
        var result = await _service.DeleteTrainingProgramAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Enrollments(int? programId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("TRAINING.MANAGE");
        var model = await _service.GetEnrollmentsPageAsync(
            User.GetCompanyId(), programId, employeeId, status, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll([FromForm] TrainingEnrollFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("TRAINING.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _service.EnrollEmployeeAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("TRAINING.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOutcome([FromForm] TrainingOutcomeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.UpdateEnrollmentOutcomeAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("TRAINING.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var result = await _service.DeleteEnrollmentAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportEnrollments(int? programId = null, int? employeeId = null, string? status = null)
    {
        var canManage = User.HasPermission("TRAINING.MANAGE");
        var model = await _service.GetEnrollmentsPageAsync(
            User.GetCompanyId(), programId, employeeId, status, User.GetEmployeeId(), canManage);

        var bytes = CsvExportHelper.BuildCsv(model.Enrollments, new (string, Func<TrainingEnrollment, object?>)[]
        {
            ("Program Code", x => x.ProgramCode),
            ("Program Title", x => x.ProgramTitle),
            ("Category", x => x.Category),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Enrollment Date", x => x.EnrollmentDate.ToString("yyyy-MM-dd")),
            ("Status", x => x.AttendanceStatus),
            ("Assessment Score", x => x.AssessmentScore),
            ("Certificate #", x => x.CertificateNumber),
            ("Completion Date", x => x.CompletionDate?.ToString("yyyy-MM-dd"))
        });

        return File(bytes, "text/csv", $"TrainingEnrollments_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
