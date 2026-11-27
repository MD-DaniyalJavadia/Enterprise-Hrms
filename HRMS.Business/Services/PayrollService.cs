using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Payroll;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class PayrollService : IPayrollService
{
    private static readonly HashSet<string> ValidRunStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approved", "Locked", "Paid"
    };

    private static readonly HashSet<string> ValidComponentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Earning", "Deduction", "EmployerContribution"
    };

    private static readonly HashSet<string> ValidCalcTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fixed", "PercentOfBasic"
    };

    private static readonly HashSet<string> ValidPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Paid", "Hold"
    };

    private readonly IPayrollRepository _payrollRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditLogService _auditLogService;

    public PayrollService(
        IPayrollRepository payrollRepository,
        IDepartmentRepository departmentRepository,
        IAuditLogService auditLogService)
    {
        _payrollRepository = payrollRepository;
        _departmentRepository = departmentRepository;
        _auditLogService = auditLogService;
    }

    public async Task<PayrollRunsPageViewModel> GetPayrollRunsPageAsync(int companyId, int? year = null)
    {
        var (runs, kpi) = await _payrollRepository.GetPayrollRunsAsync(companyId, year);
        return new PayrollRunsPageViewModel
        {
            FilterYear = year,
            Kpi = kpi,
            Runs = runs.ToList()
        };
    }

    public async Task<OperationResult<int>> GeneratePayrollRunAsync(int companyId, PayrollGenerateViewModel model, int userId, string username)
    {
        try
        {
            var runId = await _payrollRepository.GeneratePayrollRunAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Payroll", "PayrollRun", runId.ToString(),
                $"Generated payroll batch #{runId} for {model.PayrollYear}-{model.PayrollMonth:D2}.");
            return OperationResult<int>.Success(runId, "Payroll batch calculated and generated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public Task<PayrollRunDetailsViewModel?> GetPayrollRunDetailsAsync(int companyId, int payrollRunId)
        => _payrollRepository.GetPayrollRunDetailsAsync(companyId, payrollRunId);

    public async Task<OperationResult> UpdatePayrollRunStatusAsync(int companyId, int payrollRunId, string status, int userId, string username)
    {
        if (!ValidRunStatuses.Contains(status))
        {
            return OperationResult.Failure("Invalid payroll run status transition.");
        }

        var normalized = ValidRunStatuses.First(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _payrollRepository.UpdatePayrollRunStatusAsync(companyId, payrollRunId, normalized, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                normalized == "Approved" ? AuditActionType.Approve.ToString() : AuditActionType.Update.ToString(),
                "Payroll", "PayrollRun", payrollRunId.ToString(),
                $"Updated payroll batch #{payrollRunId} status to {normalized}.");
            return OperationResult.Success($"Payroll batch marked as {normalized}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeletePayrollRunAsync(int companyId, int payrollRunId, int userId, string username)
    {
        try
        {
            await _payrollRepository.DeletePayrollRunAsync(companyId, payrollRunId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Payroll", "PayrollRun", payrollRunId.ToString(),
                $"Deleted draft payroll batch #{payrollRunId}.");
            return OperationResult.Success("Draft payroll run deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> AdjustPayslipAsync(int companyId, PayslipAdjustViewModel model, int userId, string username)
    {
        if (!ValidPaymentStatuses.Contains(model.PaymentStatus))
        {
            return OperationResult.Failure("Invalid payment status.");
        }

        model.PaymentStatus = ValidPaymentStatuses.First(s => string.Equals(s, model.PaymentStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _payrollRepository.AdjustPayslipAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Payroll", "Payslip", model.PayslipId.ToString(),
                $"Adjusted payslip #{model.PayslipId} (Bonus={model.BonusAmount}, OtherDed={model.OtherDeductions}, Status={model.PaymentStatus}).");
            return OperationResult.Success("Payslip updated and batch totals recalculated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public Task<PayslipDetailViewModel?> GetPayslipDetailAsync(int companyId, int payslipId)
        => _payrollRepository.GetPayslipDetailAsync(companyId, payslipId);

    public async Task<List<BankTransferRowViewModel>> GetBankTransferListAsync(int companyId, int payrollRunId)
        => (await _payrollRepository.GetBankTransferListAsync(companyId, payrollRunId)).ToList();

    public async Task<PayrollStructuresPageViewModel> GetStructuresPageAsync(int companyId, int? departmentId = null, string? searchTerm = null)
    {
        return new PayrollStructuresPageViewModel
        {
            DepartmentId = departmentId,
            SearchTerm = searchTerm,
            Structures = (await _payrollRepository.GetSalaryStructuresAsync(companyId, departmentId, searchTerm)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public Task<SalaryStructureEditViewModel?> GetEmployeeSalaryStructureAsync(int companyId, int employeeId)
        => _payrollRepository.GetEmployeeSalaryStructureAsync(companyId, employeeId);

    public async Task<OperationResult<int>> SaveEmployeeSalaryStructureAsync(int companyId, SalaryStructureEditViewModel model, int userId, string username)
    {
        try
        {
            var id = await _payrollRepository.SaveEmployeeSalaryStructureAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Payroll", "SalaryStructure", id.ToString(),
                $"Saved salary structure #{id} for employee ID {model.EmployeeId} (Basic={model.BasicSalaryMonthly}).");
            return OperationResult<int>.Success(id, "Employee salary structure saved.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<PayrollComponentsPageViewModel> GetComponentsPageAsync(int companyId, int? taxYear = null)
    {
        return new PayrollComponentsPageViewModel
        {
            FilterTaxYear = taxYear,
            Components = (await _payrollRepository.GetSalaryComponentsAsync(companyId)).ToList(),
            TaxSlabs = (await _payrollRepository.GetTaxSlabsAsync(companyId, taxYear)).ToList()
        };
    }

    public async Task<OperationResult<int>> SaveSalaryComponentAsync(int companyId, SalaryComponentFormViewModel model, int userId, string username)
    {
        if (!ValidComponentTypes.Contains(model.ComponentType))
        {
            return OperationResult<int>.Failure("Invalid component type.");
        }

        if (!ValidCalcTypes.Contains(model.CalculationType))
        {
            return OperationResult<int>.Failure("Invalid calculation type.");
        }

        try
        {
            var id = await _payrollRepository.SaveSalaryComponentAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.ComponentId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Payroll", "SalaryComponent", id.ToString(),
                $"Saved salary component '{model.ComponentName}' ({model.ComponentCode}).");
            return OperationResult<int>.Success(id, "Salary component saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteSalaryComponentAsync(int companyId, int componentId, int userId, string username)
    {
        try
        {
            await _payrollRepository.DeleteSalaryComponentAsync(companyId, componentId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Payroll", "SalaryComponent", componentId.ToString(),
                $"Deleted salary component ID {componentId}.");
            return OperationResult.Success("Salary component removed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveTaxSlabAsync(int companyId, TaxSlabFormViewModel model, int userId, string username)
    {
        if (model.MaxAnnualIncome.HasValue && model.MaxAnnualIncome.Value <= model.MinAnnualIncome)
        {
            return OperationResult<int>.Failure("Maximum annual income must be greater than minimum annual income.");
        }

        try
        {
            var id = await _payrollRepository.SaveTaxSlabAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.TaxSlabId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Payroll", "TaxSlab", id.ToString(),
                $"Saved tax slab '{model.SlabName}' for {model.TaxYear}.");
            return OperationResult<int>.Success(id, "Tax slab saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteTaxSlabAsync(int companyId, int taxSlabId, int userId, string username)
    {
        try
        {
            await _payrollRepository.DeleteTaxSlabAsync(companyId, taxSlabId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Payroll", "TaxSlab", taxSlabId.ToString(),
                $"Deleted tax slab ID {taxSlabId}.");
            return OperationResult.Success("Tax slab removed.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
