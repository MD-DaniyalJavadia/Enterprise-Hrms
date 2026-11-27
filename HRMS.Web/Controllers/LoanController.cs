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
[HasPermission("LOAN.VIEW")]
public sealed class LoanController : Controller
{
    private readonly ILoanExpenseService _loanExpenseService;

    public LoanController(ILoanExpenseService loanExpenseService)
    {
        _loanExpenseService = loanExpenseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? employeeId = null, string? status = null, string? search = null)
    {
        var canManage = User.HasPermission("LOAN.MANAGE");
        var model = await _loanExpenseService.GetLoansPageAsync(
            User.GetCompanyId(), employeeId, status, search, User.GetEmployeeId(), canManage);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _loanExpenseService.GetLoanDetailsAsync(User.GetCompanyId(), id);
        if (model is null) return NotFound();

        if (!User.HasPermission("LOAN.MANAGE") && User.GetEmployeeId() != model.Loan.EmployeeId)
        {
            return Forbid();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLoan([FromForm] EmployeeLoanFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        if (!User.HasPermission("LOAN.MANAGE") && User.GetEmployeeId() != model.EmployeeId)
        {
            return Forbid();
        }

        var result = await _loanExpenseService.SubmitLoanAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("LOAN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewLoan([FromForm] LoanReviewViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.ReviewLoanAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("LOAN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordRepayment([FromForm] RecordRepaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.RecordRepaymentAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("LOAN.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Types()
    {
        var types = await _loanExpenseService.GetLoanTypesAsync(User.GetCompanyId(), includeInactive: true);
        return View(types);
    }

    [HasPermission("LOAN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLoanType([FromForm] LoanTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _loanExpenseService.SaveLoanTypeAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("LOAN.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLoanType(int id)
    {
        var result = await _loanExpenseService.DeleteLoanTypeAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> ExportLoans(int? employeeId = null, string? status = null, string? search = null)
    {
        var canManage = User.HasPermission("LOAN.MANAGE");
        var model = await _loanExpenseService.GetLoansPageAsync(
            User.GetCompanyId(), employeeId, status, search, User.GetEmployeeId(), canManage);

        var bytes = CsvExportHelper.BuildCsv(model.Loans, new (string, Func<EmployeeLoan, object?>)[]
        {
            ("Loan Number", x => x.LoanNumber),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.EmployeeName),
            ("Department", x => x.DepartmentName),
            ("Policy Type", x => x.LoanTypeName),
            ("Category", x => x.LoanCategory),
            ("Principal", x => x.PrincipalAmount),
            ("Interest %", x => x.InterestRatePercent),
            ("Tenure (Months)", x => x.TenureMonths),
            ("Monthly EMI", x => x.MonthlyInstallment),
            ("Total Payable", x => x.TotalPayableAmount),
            ("Paid", x => x.PaidAmount),
            ("Remaining Balance", x => x.RemainingBalance),
            ("Status", x => x.LoanStatus)
        });

        return File(bytes, "text/csv", $"EmployeeLoans_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
