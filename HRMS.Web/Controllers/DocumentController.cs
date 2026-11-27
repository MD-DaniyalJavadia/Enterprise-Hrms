using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.AssetDocument;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("DOCUMENT.VIEW")]
public sealed class DocumentController : Controller
{
    private readonly IAssetDocumentService _service;
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public DocumentController(
        IAssetDocumentService service,
        IEmployeeService employeeService,
        IWebHostEnvironment env)
    {
        _service = service;
        _employeeService = employeeService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId = null, string? status = null, string? visibility = null, string? search = null)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var model = await _service.GetDocumentIndexPageAsync(
            User.GetCompanyId(), categoryId, status, visibility, search, User.GetEmployeeId(), canManage, isManager);
        return View(model);
    }

    [HasPermission("DOCUMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocument([FromForm] CompanyDocumentFormViewModel model, IFormFile? documentFile)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        string? originalFileName = null;
        string? storedFileName = null;
        string? contentType = null;
        long? fileSizeBytes = null;

        if (model.DocumentId == 0 && (documentFile is null || documentFile.Length == 0))
        {
            return Json(ApiResponse<int>.Fail("Please attach a document file before publishing."));
        }

        if (documentFile is not null && documentFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveCompanyDocumentAsync(documentFile, _env.ContentRootPath);
            if (!saved.Ok)
            {
                return Json(ApiResponse<int>.Fail(saved.Error!));
            }

            originalFileName = Path.GetFileName(documentFile.FileName);
            storedFileName = saved.StoredFileName!;
            contentType = string.IsNullOrWhiteSpace(documentFile.ContentType) ? "application/octet-stream" : documentFile.ContentType;
            fileSizeBytes = documentFile.Length;
        }

        var result = await _service.SaveCompanyDocumentAsync(
            User.GetCompanyId(), model, originalFileName, storedFileName, contentType, fileSizeBytes,
            User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("DOCUMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var result = await _service.DeleteCompanyDocumentAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCompanyDocument(int id)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var doc = await _service.GetAccessibleCompanyDocumentAsync(
            User.GetCompanyId(), id, User.GetEmployeeId(), canManage, isManager);

        if (doc is null) return NotFound();

        var path = FileStorageHelper.ResolveCompanyDocumentPath(doc.StoredFileName, _env.ContentRootPath);
        if (path is null)
        {
            TempData["WarningMessage"] = $"No physical file is stored on disk yet for seeded document '{doc.Title}'. Edit the document to upload a file.";
            return RedirectToAction(nameof(Index));
        }

        return PhysicalFile(path, doc.ContentType, doc.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeDocument([FromForm] DocumentAcknowledgeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<bool>.Fail("Invalid acknowledgment request."));
        }

        var empId = User.GetEmployeeId();
        if (!empId.HasValue)
        {
            return Json(ApiResponse<bool>.Fail("An employee profile must be linked to your account to sign off policies."));
        }

        var canManage = User.HasPermission("DOCUMENT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _service.AcknowledgeCompanyDocumentAsync(
            User.GetCompanyId(), model.DocumentId, empId.Value, canManage, isManager,
            ipAddress, model.Remarks, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Acknowledgments(int? documentId = null, int? employeeId = null)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var model = await _service.GetAcknowledgmentsPageAsync(
            User.GetCompanyId(), documentId, employeeId, User.GetEmployeeId(), canManage, isManager);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EmployeeVault(int? employeeId = null, string? documentType = null, string? expiryFilter = null)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE") || User.HasPermission("EMPLOYEE.MANAGE");
        var model = await _service.GetEmployeeVaultPageAsync(
            User.GetCompanyId(), employeeId, documentType, expiryFilter, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEmployeeDocument([FromForm] EmployeeVaultUploadViewModel model, IFormFile? documentFile)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE") || User.HasPermission("EMPLOYEE.MANAGE");
        if (!canManage)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (documentFile is null || documentFile.Length == 0)
        {
            return Json(ApiResponse<bool>.Fail("Please select a document file to upload."));
        }

        var companyId = User.GetCompanyId();
        if (await _employeeService.GetProfileByIdAsync(companyId, model.EmployeeId) is null)
        {
            return Json(ApiResponse<bool>.Fail("Selected employee not found."));
        }

        var saved = await FileStorageHelper.SaveEmployeeDocumentAsync(documentFile, _env.ContentRootPath);
        if (!saved.Ok)
        {
            return Json(ApiResponse<bool>.Fail(saved.Error!));
        }

        var doc = new EmployeeDocument
        {
            CompanyId = companyId,
            EmployeeId = model.EmployeeId,
            DocumentType = model.DocumentType.Trim(),
            DocumentTitle = model.DocumentTitle.Trim(),
            OriginalFileName = Path.GetFileName(documentFile.FileName),
            StoredFileName = saved.StoredFileName!,
            ContentType = string.IsNullOrWhiteSpace(documentFile.ContentType) ? "application/octet-stream" : documentFile.ContentType,
            FileSizeBytes = documentFile.Length,
            ExpiryDate = model.ExpiryDate
        };

        var result = await _employeeService.AddDocumentAsync(doc, User.GetUserId());
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEmployeeDocument(int id)
    {
        var doc = await _employeeService.GetDocumentByIdAsync(User.GetCompanyId(), id);
        if (doc is null) return NotFound();

        var canManage = User.HasPermission("DOCUMENT.MANAGE") || User.HasPermission("EMPLOYEE.MANAGE");
        if (!canManage && User.GetEmployeeId() != doc.EmployeeId)
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveEmployeeDocumentPath(doc.StoredFileName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, doc.ContentType, doc.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployeeDocument(int id)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE") || User.HasPermission("EMPLOYEE.MANAGE");
        if (!canManage)
        {
            return Forbid();
        }

        var result = await _employeeService.DeleteDocumentAsync(User.GetCompanyId(), id);
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("DOCUMENT.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _service.GetDocumentCategoriesAsync(User.GetCompanyId());
        return View(categories);
    }

    [HasPermission("DOCUMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory([FromForm] DocumentCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _service.SaveDocumentCategoryAsync(User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("DOCUMENT.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _service.DeleteDocumentCategoryAsync(User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");
        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportDocuments(int? categoryId = null, string? status = null, string? visibility = null, string? search = null)
    {
        var canManage = User.HasPermission("DOCUMENT.MANAGE");
        var isManager = User.HasPermission("MANAGER.VIEW");
        var model = await _service.GetDocumentIndexPageAsync(
            User.GetCompanyId(), categoryId, status, visibility, search, User.GetEmployeeId(), canManage, isManager);

        var bytes = CsvExportHelper.BuildCsv(model.Documents, new (string, Func<CompanyDocument, object?>)[]
        {
            ("Document Code", x => x.DocumentCode),
            ("Title", x => x.Title),
            ("Category", x => x.CategoryName),
            ("Version", x => x.VersionNumber),
            ("Visibility", x => x.Visibility),
            ("Department", x => x.DepartmentName),
            ("Requires Sign-Off", x => x.RequiresAcknowledgment ? "Yes" : "No"),
            ("Acknowledged", x => x.AcknowledgedCount),
            ("Target Audience", x => x.TargetAudienceCount),
            ("Effective Date", x => x.EffectiveDate.ToString("yyyy-MM-dd")),
            ("Expiry Date", x => x.ExpiryDate?.ToString("yyyy-MM-dd")),
            ("Status", x => x.DocumentStatus)
        });

        return File(bytes, "text/csv", $"CompanyDocuments_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
