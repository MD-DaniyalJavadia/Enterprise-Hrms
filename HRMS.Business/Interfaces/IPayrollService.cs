using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Payroll;

namespace HRMS.Business.Interfaces;

public interface IPayrollService
{
    Task<PayrollRunsPageViewModel> GetPayrollRunsPageAsync(int companyId, int? year = null);
    Task<OperationResult<int>> GeneratePayrollRunAsync(int companyId, PayrollGenerateViewModel model, int userId, string username);
    Task<PayrollRunDetailsViewModel?> GetPayrollRunDetailsAsync(int companyId, int payrollRunId);
    Task<OperationResult> UpdatePayrollRunStatusAsync(int companyId, int payrollRunId, string status, int userId, string username);
    Task<OperationResult> DeletePayrollRunAsync(int companyId, int payrollRunId, int userId, string username);

    Task<OperationResult> AdjustPayslipAsync(int companyId, PayslipAdjustViewModel model, int userId, string username);
    Task<PayslipDetailViewModel?> GetPayslipDetailAsync(int companyId, int payslipId);
    Task<List<BankTransferRowViewModel>> GetBankTransferListAsync(int companyId, int payrollRunId);

    Task<PayrollStructuresPageViewModel> GetStructuresPageAsync(int companyId, int? departmentId = null, string? searchTerm = null);
    Task<SalaryStructureEditViewModel?> GetEmployeeSalaryStructureAsync(int companyId, int employeeId);
    Task<OperationResult<int>> SaveEmployeeSalaryStructureAsync(int companyId, SalaryStructureEditViewModel model, int userId, string username);

    Task<PayrollComponentsPageViewModel> GetComponentsPageAsync(int companyId, int? taxYear = null);
    Task<OperationResult<int>> SaveSalaryComponentAsync(int companyId, SalaryComponentFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteSalaryComponentAsync(int companyId, int componentId, int userId, string username);
    Task<OperationResult<int>> SaveTaxSlabAsync(int companyId, TaxSlabFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteTaxSlabAsync(int companyId, int taxSlabId, int userId, string username);
}
