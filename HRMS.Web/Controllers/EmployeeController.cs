using System.Text;
using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Employees;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("EMPLOYEE.VIEW")]
public sealed class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public EmployeeController(IEmployeeService employeeService, IWebHostEnvironment env)
    {
        _employeeService = employeeService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? branchId = null, int? departmentId = null, byte? status = null, string? search = null)
    {
        var model = await _employeeService.GetFilteredDirectoryAsync(User.GetCompanyId(), branchId, departmentId, status, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Directory(int? branchId = null, int? departmentId = null, byte? status = null, string? search = null)
    {
        var model = await _employeeService.GetFilteredDirectoryAsync(User.GetCompanyId(), branchId, departmentId, status, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var profile = await _employeeService.GetProfileByIdAsync(User.GetCompanyId(), id);
        if (profile is null) return NotFound();
        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> IdCard(int id)
    {
        var profile = await _employeeService.GetProfileByIdAsync(User.GetCompanyId(), id);
        if (profile is null) return NotFound();
        return View(profile);
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await _employeeService.PrepareFormAsync(User.GetCompanyId());
        return View(model);
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model, IFormFile? photoFile)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;

        if (photoFile is not null && photoFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveEmployeePhotoAsync(photoFile, _env.WebRootPath);
            if (!saved.Ok)
            {
                ModelState.AddModelError(string.Empty, saved.Error!);
                return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
            }
            model.PhotoPath = saved.RelativePath;
        }

        if (!ModelState.IsValid)
        {
            return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
        }

        var result = await _employeeService.CreateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _employeeService.PrepareFormAsync(User.GetCompanyId(), employeeId: id);
        if (model.EmployeeId == 0) return NotFound();
        return View(model);
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel model, IFormFile? photoFile)
    {
        var companyId = User.GetCompanyId();
        model.CompanyId = companyId;

        if (photoFile is not null && photoFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveEmployeePhotoAsync(photoFile, _env.WebRootPath);
            if (!saved.Ok)
            {
                ModelState.AddModelError(string.Empty, saved.Error!);
                return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
            }
            model.PhotoPath = saved.RelativePath;
        }

        if (!ModelState.IsValid)
        {
            return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
        }

        var result = await _employeeService.UpdateAsync(model, User.GetUserId(), User.Identity?.Name ?? "system");
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await _employeeService.PrepareFormAsync(companyId, posted: model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _employeeService.DeleteAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordCareerAction([FromForm] EmployeeCareerActionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _employeeService.RecordCareerActionAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    // --- Sub-Modules AJAX Actions ---
    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEmergencyContact([FromForm] EmployeeEmergencyContact model)
    {
        if (model.EmployeeId <= 0 || string.IsNullOrWhiteSpace(model.ContactName) ||
            string.IsNullOrWhiteSpace(model.Relationship) || string.IsNullOrWhiteSpace(model.PrimaryPhone))
        {
            return Json(ApiResponse<bool>.Fail("Contact name, relationship, and primary phone are required."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        model.CompanyId = companyId;
        var result = await _employeeService.AddEmergencyContactAsync(model, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmergencyContact(int id)
    {
        var result = await _employeeService.DeleteEmergencyContactAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFamilyMember([FromForm] EmployeeFamilyMember model)
    {
        if (model.EmployeeId <= 0 || string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Relationship))
        {
            return Json(ApiResponse<bool>.Fail("Full name and relationship are required."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        model.CompanyId = companyId;
        var result = await _employeeService.AddFamilyMemberAsync(model, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFamilyMember(int id)
    {
        var result = await _employeeService.DeleteFamilyMemberAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEducation([FromForm] EmployeeEducation model)
    {
        if (model.EmployeeId <= 0 || string.IsNullOrWhiteSpace(model.DegreeTitle) || string.IsNullOrWhiteSpace(model.InstitutionName))
        {
            return Json(ApiResponse<bool>.Fail("Degree title and institution name are required."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        model.CompanyId = companyId;
        var result = await _employeeService.AddEducationAsync(model, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEducation(int id)
    {
        var result = await _employeeService.DeleteEducationAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExperience([FromForm] EmployeeExperience model)
    {
        if (model.EmployeeId <= 0 || string.IsNullOrWhiteSpace(model.EmployerName) ||
            string.IsNullOrWhiteSpace(model.JobTitle) || model.StartDate == default)
        {
            return Json(ApiResponse<bool>.Fail("Employer name, job title, and start date are required."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        model.CompanyId = companyId;
        var result = await _employeeService.AddExperienceAsync(model, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExperience(int id)
    {
        var result = await _employeeService.DeleteExperienceAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSkill([FromForm] EmployeeSkill model)
    {
        if (model.EmployeeId <= 0 || string.IsNullOrWhiteSpace(model.SkillName))
        {
            return Json(ApiResponse<bool>.Fail("Skill name is required."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        model.CompanyId = companyId;
        var result = await _employeeService.AddSkillAsync(model, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var result = await _employeeService.DeleteSkillAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int employeeId, string documentType, string documentTitle, DateTime? expiryDate, IFormFile? documentFile)
    {
        if (employeeId <= 0 || string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(documentTitle))
        {
            return Json(ApiResponse<bool>.Fail("Document type and title are required."));
        }

        if (documentFile is null || documentFile.Length == 0)
        {
            return Json(ApiResponse<bool>.Fail("Please select a valid document file."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, employeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Employee not found."));
        }

        var saved = await FileStorageHelper.SaveEmployeeDocumentAsync(documentFile, _env.ContentRootPath);
        if (!saved.Ok)
        {
            return Json(ApiResponse<bool>.Fail(saved.Error!));
        }

        var doc = new EmployeeDocument
        {
            CompanyId = companyId,
            EmployeeId = employeeId,
            DocumentType = documentType.Trim(),
            DocumentTitle = documentTitle.Trim(),
            OriginalFileName = Path.GetFileName(documentFile.FileName),
            StoredFileName = saved.StoredFileName!,
            ContentType = documentFile.ContentType,
            FileSizeBytes = documentFile.Length,
            ExpiryDate = expiryDate
        };

        var result = await _employeeService.AddDocumentAsync(doc, User.GetUserId());
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var doc = await _employeeService.GetDocumentByIdAsync(User.GetCompanyId(), id);
        if (doc is null) return NotFound();

        var path = FileStorageHelper.ResolveEmployeeDocumentPath(doc.StoredFileName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, doc.ContentType, doc.OriginalFileName);
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var result = await _employeeService.DeleteDocumentAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded ? ApiResponse<bool>.Ok(true, result.Message) : ApiResponse<bool>.Fail(result.Message));
    }

    // --- Import / Export ---
    [HttpGet]
    public async Task<IActionResult> Export(int? branchId = null, int? departmentId = null, byte? status = null, string? search = null)
    {
        var directory = await _employeeService.GetFilteredDirectoryAsync(User.GetCompanyId(), branchId, departmentId, status, search);
        var bytes = CsvExportHelper.BuildCsv(directory.Employees, new (string, Func<EmployeeListItemViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Full Name", x => x.FullName),
            ("Gender", x => x.Gender),
            ("Work Email", x => x.WorkEmail),
            ("Phone", x => x.MobilePhone ?? x.Phone),
            ("Branch", x => x.BranchName),
            ("Department", x => x.DepartmentName),
            ("Designation", x => x.DesignationTitle),
            ("Manager", x => x.ReportingManagerName),
            ("Hire Date", x => x.HireDate.ToString("yyyy-MM-dd")),
            ("Base Salary", x => x.BaseSalary),
            ("Status", x => x.StatusLabel)
        });

        return File(bytes, "text/csv", $"Employees_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpGet]
    public IActionResult Import()
    {
        return View(new EmployeeImportResultViewModel());
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? csvFile)
    {
        if (csvFile is null || csvFile.Length == 0 || !Path.GetExtension(csvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Please upload a valid .csv file.");
            return View(new EmployeeImportResultViewModel());
        }

        await using var stream = csvFile.OpenReadStream();
        var result = await _employeeService.ImportFromCsvStreamAsync(User.GetCompanyId(), stream, User.GetUserId(), User.Identity?.Name ?? "system");
        if (result.ImportedCount > 0)
        {
            TempData["SuccessMessage"] = $"Successfully imported {result.ImportedCount} employee(s).";
        }
        return View(result);
    }

    [HasPermission("EMPLOYEE.MANAGE")]
    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        var csv = "EmployeeCode,FirstName,LastName,Gender,DateOfBirth,WorkEmail,HireDate,BranchCode,DepartmentCode,DesignationCode,BaseSalary\r\n" +
                  "EMP-1099,Jordan,Taylor,Female,1994-06-15,jordan.taylor@acme-enterprise.example,2026-11-01,HQ-NYC,ENG,SWE-SR,120000\r\n";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", "Employee_Import_Template.csv");
    }
}
