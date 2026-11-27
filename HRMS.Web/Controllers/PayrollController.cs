using HRMS.Business.Interfaces;
using HRMS.Models.ResponseModels;
using HRMS.Models.ViewModels.Payroll;
using HRMS.Web.Authorization;
using HRMS.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[Authorize]
[HasPermission("PAYROLL.VIEW")]
public sealed class PayrollController : Controller
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year = null)
    {
        var model = await _payrollService.GetPayrollRunsPageAsync(User.GetCompanyId(), year);
        return View(model);
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([FromForm] PayrollGenerateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _payrollService.GeneratePayrollRunAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _payrollService.GetPayrollRunDetailsAsync(User.GetCompanyId(), id);
        if (model is null)
        {
            return NotFound();
        }

        if (!User.HasPermission("PAYROLL.MANAGE"))
        {
            var myEmpId = User.GetEmployeeId();
            model.Payslips = myEmpId.HasValue
                ? model.Payslips.Where(p => p.EmployeeId == myEmpId.Value).ToList()
                : new List<HRMS.Models.Entities.Payslip>();
            model.DepartmentSummary.Clear();
        }

        return View(model);
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int payrollRunId, string status)
    {
        var result = await _payrollService.UpdatePayrollRunStatusAsync(
            User.GetCompanyId(), payrollRunId, status, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRun(int id)
    {
        var result = await _payrollService.DeletePayrollRunAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustPayslip([FromForm] PayslipAdjustViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _payrollService.AdjustPayslipAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HttpGet]
    public async Task<IActionResult> Payslip(int id)
    {
        var detail = await _payrollService.GetPayslipDetailAsync(User.GetCompanyId(), id);
        if (detail is null)
        {
            return NotFound();
        }

        if (!User.HasPermission("PAYROLL.MANAGE") && User.GetEmployeeId() != detail.Payslip.EmployeeId)
        {
            return Forbid();
        }

        return View(detail);
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> ExportRunCsv(int id)
    {
        var detail = await _payrollService.GetPayrollRunDetailsAsync(User.GetCompanyId(), id);
        if (detail is null)
        {
            return NotFound();
        }

        var bytes = CsvExportHelper.BuildCsv(detail.Payslips, new (string, Func<HRMS.Models.Entities.Payslip, object?>)[]
        {
            ("Period", _ => detail.Run.PeriodLabel),
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Department", x => x.DepartmentName),
            ("Paid Days", x => x.PaidDays),
            ("Basic Pay", x => x.BasicPay),
            ("Allowances", x => x.TotalAllowances),
            ("Overtime Pay", x => x.OvertimePay),
            ("Leave Encashment", x => x.LeaveEncashmentPay),
            ("Bonus", x => x.BonusAmount),
            ("Gross Pay", x => x.GrossPay),
            ("Income Tax", x => x.IncomeTax),
            ("Statutory Deductions", x => x.StatutoryDeductions),
            ("Unpaid Absence Deduction", x => x.UnpaidAbsenceDeduction),
            ("Other Deductions", x => x.OtherDeductions),
            ("Total Deductions", x => x.TotalDeductions),
            ("Net Pay", x => x.NetPay),
            ("Employer Contributions", x => x.EmployerContributions),
            ("Payment Status", x => x.PaymentStatus)
        });

        return File(bytes, "text/csv", $"Payroll_{detail.Run.PayrollYear}_{detail.Run.PayrollMonth:D2}.csv");
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> ExportBankAdvice(int id)
    {
        var detail = await _payrollService.GetPayrollRunDetailsAsync(User.GetCompanyId(), id);
        if (detail is null)
        {
            return NotFound();
        }

        var rows = await _payrollService.GetBankTransferListAsync(User.GetCompanyId(), id);
        var bytes = CsvExportHelper.BuildCsv(rows, new (string, Func<BankTransferRowViewModel, object?>)[]
        {
            ("Employee Code", x => x.EmployeeCode),
            ("Employee Name", x => x.FullName),
            ("Account Title", x => x.BankAccountTitle),
            ("Bank Name", x => x.BankName),
            ("Account Number", x => x.BankAccountNumber),
            ("Routing / IBAN", x => x.BankRoutingCode),
            ("Method", x => x.PaymentMethod),
            ("Status", x => x.PaymentStatus),
            ("Net Transfer Amount", x => x.NetPay)
        });

        return File(bytes, "text/csv", $"BankAdvice_{detail.Run.PayrollYear}_{detail.Run.PayrollMonth:D2}.csv");
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Structures(int? departmentId = null, string? search = null)
    {
        var model = await _payrollService.GetStructuresPageAsync(User.GetCompanyId(), departmentId, search);
        return View(model);
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> GetEmployeeStructure(int employeeId)
    {
        var structure = await _payrollService.GetEmployeeSalaryStructureAsync(User.GetCompanyId(), employeeId);
        if (structure is null)
        {
            return Json(ApiResponse<object>.Fail("Employee not found."));
        }

        return Json(ApiResponse<SalaryStructureEditViewModel>.Ok(structure));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStructure([FromBody] SalaryStructureEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _payrollService.SaveEmployeeSalaryStructureAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpGet]
    public async Task<IActionResult> Components(int? taxYear = null)
    {
        var model = await _payrollService.GetComponentsPageAsync(User.GetCompanyId(), taxYear);
        return View(model);
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveComponent([FromForm] SalaryComponentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _payrollService.SaveSalaryComponentAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComponent(int id)
    {
        var result = await _payrollService.DeleteSalaryComponentAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTaxSlab([FromForm] TaxSlabFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(ApiResponse<object>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        }

        var result = await _payrollService.SaveTaxSlabAsync(
            User.GetCompanyId(), model, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<int>.Ok(result.Data, result.Message)
            : ApiResponse<int>.Fail(result.Message));
    }

    [HasPermission("PAYROLL.MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTaxSlab(int id)
    {
        var result = await _payrollService.DeleteTaxSlabAsync(
            User.GetCompanyId(), id, User.GetUserId(), User.Identity?.Name ?? "system");

        return Json(result.Succeeded
            ? ApiResponse<bool>.Ok(true, result.Message)
            : ApiResponse<bool>.Fail(result.Message));
    }
}
