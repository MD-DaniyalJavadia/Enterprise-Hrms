using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Payroll;

namespace HRMS.Data.Interfaces;

public interface IPayrollRepository
{
    Task<IEnumerable<SalaryComponent>> GetSalaryComponentsAsync(int companyId);
    Task<int> SaveSalaryComponentAsync(int companyId, SalaryComponentFormViewModel model, int userId);
    Task DeleteSalaryComponentAsync(int companyId, int componentId, int userId);

    Task<IEnumerable<TaxSlab>> GetTaxSlabsAsync(int companyId, int? taxYear = null);
    Task<int> SaveTaxSlabAsync(int companyId, TaxSlabFormViewModel model, int userId);
    Task DeleteTaxSlabAsync(int companyId, int taxSlabId, int userId);

    Task<IEnumerable<EmployeeSalaryStructureListItemViewModel>> GetSalaryStructuresAsync(int companyId, int? departmentId = null, string? searchTerm = null);
    Task<SalaryStructureEditViewModel?> GetEmployeeSalaryStructureAsync(int companyId, int employeeId);
    Task<int> SaveEmployeeSalaryStructureAsync(int companyId, SalaryStructureEditViewModel model, int userId);

    Task<(IEnumerable<PayrollRun> Runs, PayrollKpiSummaryViewModel Kpi)> GetPayrollRunsAsync(int companyId, int? year = null);
    Task<int> GeneratePayrollRunAsync(int companyId, PayrollGenerateViewModel model, int userId);
    Task<PayrollRunDetailsViewModel?> GetPayrollRunDetailsAsync(int companyId, int payrollRunId);
    Task UpdatePayrollRunStatusAsync(int companyId, int payrollRunId, string status, int userId);
    Task DeletePayrollRunAsync(int companyId, int payrollRunId, int userId);

    Task AdjustPayslipAsync(int companyId, PayslipAdjustViewModel model, int userId);
    Task<PayslipDetailViewModel?> GetPayslipDetailAsync(int companyId, int payslipId);
    Task<IEnumerable<BankTransferRowViewModel>> GetBankTransferListAsync(int companyId, int payrollRunId);
}
