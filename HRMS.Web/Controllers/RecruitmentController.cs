using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Recruitment;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("RECRUITMENT.VIEW")]
public sealed class RecruitmentController : Controller
{
    private readonly IRecruitmentService _recruitmentService;
    private readonly IWebHostEnvironment _env;

    public RecruitmentController(IRecruitmentService recruitmentService, IWebHostEnvironment env)
    {
        _recruitmentService = recruitmentService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId = null, string? status = null, string? search = null)
    {
        var model = await _recruitmentService.GetRequisitionsPageAsync(User.GetCompanyId(), departmentId, status, search);
        return View(model);
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRequisition([FromForm] JobRequisitionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _recruitmentService.SaveRequisitionAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequisitionStatus(int requisitionId, string status)
    {
        var result = await _recruitmentService.UpdateRequisitionStatusAsync(
            User.GetCompanyId(), requisitionId, status, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequisition(int id)
    {
        var result = await _recruitmentService.DeleteRequisitionAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportRequisitions(int? departmentId = null, string? status = null, string? search = null)
    {
        var model = await _recruitmentService.GetRequisitionsPageAsync(User.GetCompanyId(), departmentId, status, search);
        var bytes = CsvExportHelper.BuildCsv(model.Requisitions, new (string, Func<JobRequisition, object?>)[]
        {
            ("Code", x => x.RequisitionCode),
            ("Job Title", x => x.JobTitle),
            ("Department", x => x.DepartmentName),
            ("Branch", x => x.BranchName),
            ("Employment Type", x => x.EmploymentType),
            ("Work Mode", x => x.WorkMode),
            ("Openings", x => x.OpeningsCount),
            ("Applicants", x => x.ApplicantsCount),
            ("Hired", x => x.HiredCount),
            ("Status", x => x.RequisitionStatus)
        });

        return File(bytes, "text/csv", $"JobRequisitions_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Candidates(string? source = null, string? search = null)
    {
        var model = await _recruitmentService.GetCandidatesPageAsync(User.GetCompanyId(), source, search);
        return View(model);
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCandidate([FromForm] CandidateFormViewModel model, IFormFile? resumeFile)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (resumeFile is not null && resumeFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveCandidateResumeAsync(resumeFile, _env.ContentRootPath);
            if (!saved.Ok)
            {
                return Json(ApiResponse<bool>.Fail(saved.Error!));
            }
            model.ResumeFileName = Path.GetFileName(resumeFile.FileName);
            model.ResumeStoredName = saved.StoredFileName;
        }

        var result = await _recruitmentService.SaveCandidateAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResume(int id)
    {
        var resume = await _recruitmentService.GetCandidateResumeAsync(User.GetCompanyId(), id);
        if (resume is null || string.IsNullOrWhiteSpace(resume.Value.ResumeStoredName))
        {
            return NotFound();
        }

        var path = FileStorageHelper.ResolveCandidateResumePath(resume.Value.ResumeStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", resume.Value.ResumeFileName ?? "resume.pdf");
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCandidate(int id)
    {
        var result = await _recruitmentService.DeleteCandidateAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCandidates(string? source = null, string? search = null)
    {
        var model = await _recruitmentService.GetCandidatesPageAsync(User.GetCompanyId(), source, search);
        var bytes = CsvExportHelper.BuildCsv(model.Candidates, new (string, Func<Candidate, object?>)[]
        {
            ("Candidate Name", x => x.FullName),
            ("Email", x => x.Email),
            ("Phone", x => x.Phone),
            ("Current Title", x => x.CurrentJobTitle),
            ("Current Company", x => x.CurrentCompany),
            ("Experience (Yrs)", x => x.TotalExperienceYears),
            ("Expected Salary", x => x.ExpectedSalary),
            ("Source", x => x.Source),
            ("Skills", x => x.Skills)
        });

        return File(bytes, "text/csv", $"Candidates_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline(int? requisitionId = null, string? stage = null)
    {
        var model = await _recruitmentService.GetPipelinePageAsync(User.GetCompanyId(), requisitionId, stage);
        return View(model);
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitApplication([FromForm] JobApplicationSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _recruitmentService.SubmitApplicationAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveStage([FromForm] JobApplicationMoveStageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid pipeline stage request."));
        }

        var result = await _recruitmentService.MoveApplicationStageAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Interviews(string? status = null)
    {
        var model = await _recruitmentService.GetInterviewsPageAsync(User.GetCompanyId(), status);
        return View(model);
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleInterview([FromForm] InterviewScheduleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _recruitmentService.ScheduleInterviewAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitInterviewFeedback([FromForm] InterviewFeedbackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _recruitmentService.SubmitInterviewFeedbackAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Offers(string? status = null)
    {
        var model = await _recruitmentService.GetOffersPageAsync(User.GetCompanyId(), status);
        return View(model);
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOffer([FromForm] JobOfferFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _recruitmentService.SaveOfferAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOfferStatus(int offerId, string status, string? responseNotes = null)
    {
        var result = await _recruitmentService.UpdateOfferStatusAsync(
            User.GetCompanyId(), offerId, status, responseNotes, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("RECRUITMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertOfferToEmployee(int offerId)
    {
        var result = await _recruitmentService.ConvertOfferToEmployeeAsync(
            User.GetCompanyId(), offerId, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }
}
