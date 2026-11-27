using HRMS.Business.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.LoanExpense;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("EXPENSE.VIEW")]
public sealed class ExpenseController : Controller
{
    private readonly ILoanExpenseService _loanExpenseService;
    private readonly IWebHostEnvironment _env;

    public ExpenseController(ILoanExpenseService loanExpenseService, IWebHostEnvironment env)
    {
        _loanExpenseService = loanExpenseService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId = null, int? categoryId = null, string? status = null, string? search = null)
    {
        var canManage = User.HasPermission("EXPENSE.MANAGE");
        var model = await _loanExpenseService.GetExpensesPageAsync(
            User.GetCompanyId(), employeeId, categoryId, status, search, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitClaim([FromForm] ExpenseClaimFormViewModel model, IFormFile? receiptFile)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("EXPENSE.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        if (receiptFile is not null && receiptFile.Length > 0)
        {
            var saved = await FileStorageHelper.SaveExpenseReceiptAsync(receiptFile, _env.ContentRootPath);
            if (!saved.Ok)
            {
                return Json(ApiResponse<bool>.Fail(saved.Error!));
            }
            model.ReceiptFileName = Path.GetFileName(receiptFile.FileName);
            model.ReceiptStoredName = saved.StoredFileName;
        }

        var result = await _loanExpenseService.SubmitExpenseClaimAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadReceipt(int id)
    {
        var claim = await _loanExpenseService.GetExpenseClaimByIdAsync(User.GetCompanyId(), id);
        if (claim is null || string.IsNullOrWhiteSpace(claim.ReceiptStoredName))
        {
            return NotFound();
        }

        if (!User.HasPermission("EXPENSE.MANAGE") && User.GetEmployeeId() != claim.EmployeeId)
        {
            return Forbid();
        }

        var path = FileStorageHelper.ResolveExpenseReceiptPath(claim.ReceiptStoredName, _env.ContentRootPath);
        if (path is null) return NotFound();

        return PhysicalFile(path, "application/octet-stream", claim.ReceiptFileName ?? "receipt");
    }

    [HasPermission("EXPENSE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewClaim([FromForm] ExpenseReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.ReviewExpenseClaimAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("EXPENSE.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var cats = await _loanExpenseService.GetExpenseCategoriesAsync(User.GetCompanyId(), includeInactive: true);
        return View(cats);
    }

    [HasPermission("EXPENSE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory([FromForm] ExpenseCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.SaveExpenseCategoryAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("EXPENSE.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _loanExpenseService.DeleteExpenseCategoryAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportClaims(int? employeeId = null, int? categoryId = null, string? status = null, string? search = null)
    {
        var canManage = User.HasPermission("EXPENSE.MANAGE");
        var model = await _loanExpenseService.GetExpensesPageAsync(
            User.GetCompanyId(), employeeId, categoryId, status, search, User.GetEmployeeId(), canManage);

        var bytes = CsvExportHelper.BuildCsv(model.Claims, new (string, Func<ExpenseClaim, object?>)[]
        {
            ("Claim Number", x => x.ClaimNumber),
            ("Expense Date", x => x.ExpenseDate.ToString("yyyy-MM-dd")),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Category", x => x.CategoryName),
            ("Merchant", x => x.MerchantOrVendor),
            ("Claim Amount", x => x.ClaimAmount),
            ("Approved Amount", x => x.ApprovedAmount),
            ("Status", x => x.ClaimStatus),
            ("Payment Reference", x => x.PaymentReference)
        });

        return File(bytes, "text/csv", $"ExpenseClaims_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
