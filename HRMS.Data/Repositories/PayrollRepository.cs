using System.Data;
using System.Text.Json;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Payroll;

namespace HRMS.Data.Repositories;

public sealed class PayrollRepository : BaseRepository, IPayrollRepository
{
    public PayrollRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<SalaryComponent>> GetSalaryComponentsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<SalaryComponent>("sp_SalaryComponent_GetAll", p);
    }

    public async Task<int> SaveSalaryComponentAsync(int companyId, SalaryComponentFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@ComponentId", model.ComponentId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ComponentCode", model.ComponentCode.Trim().ToUpperInvariant(), DbType.String, size: 20);
        p.Add("@ComponentName", model.ComponentName.Trim(), DbType.String, size: 100);
        p.Add("@ComponentType", model.ComponentType.Trim(), DbType.String, size: 30);
        p.Add("@CalculationType", model.CalculationType.Trim(), DbType.String, size: 20);
        p.Add("@DefaultValue", model.DefaultValue, DbType.Decimal);
        p.Add("@IsTaxable", model.IsTaxable, DbType.Boolean);
        p.Add("@IsStatutory", model.IsStatutory, DbType.Boolean);
        p.Add("@DisplayOrder", model.DisplayOrder, DbType.Int32);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_SalaryComponent_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteSalaryComponentAsync(int companyId, int componentId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ComponentId", componentId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_SalaryComponent_Delete", p);
    }

    public Task<IEnumerable<TaxSlab>> GetTaxSlabsAsync(int companyId, int? taxYear = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@TaxYear", taxYear, DbType.Int32);
        return QuerySpAsync<TaxSlab>("sp_TaxSlab_GetAll", p);
    }

    public async Task<int> SaveTaxSlabAsync(int companyId, TaxSlabFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@TaxSlabId", model.TaxSlabId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@TaxYear", model.TaxYear, DbType.Int32);
        p.Add("@SlabName", model.SlabName.Trim(), DbType.String, size: 80);
        p.Add("@MinAnnualIncome", model.MinAnnualIncome, DbType.Decimal);
        p.Add("@MaxAnnualIncome", model.MaxAnnualIncome, DbType.Decimal);
        p.Add("@TaxRatePercent", model.TaxRatePercent, DbType.Decimal);
        p.Add("@FixedTaxAmount", model.FixedTaxAmount, DbType.Decimal);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_TaxSlab_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteTaxSlabAsync(int companyId, int taxSlabId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@TaxSlabId", taxSlabId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_TaxSlab_Delete", p);
    }

    public Task<IEnumerable<EmployeeSalaryStructureListItemViewModel>> GetSalaryStructuresAsync(int companyId, int? departmentId = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);
        return QuerySpAsync<EmployeeSalaryStructureListItemViewModel>("sp_EmployeeSalaryStructure_GetAll", p);
    }

    public async Task<SalaryStructureEditViewModel?> GetEmployeeSalaryStructureAsync(int companyId, int employeeId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_EmployeeSalaryStructure_GetByEmployee", p, commandType: CommandType.StoredProcedure);
        var header = await multi.ReadFirstOrDefaultAsync<SalaryStructureEditViewModel>();
        if (header is null) return null;
        header.Items = (await multi.ReadAsync<SalaryStructureItemInputViewModel>()).ToList();
        return header;
    }

    public async Task<int> SaveEmployeeSalaryStructureAsync(int companyId, SalaryStructureEditViewModel model, int userId)
    {
        var itemsJson = JsonSerializer.Serialize(model.Items.Select(x => new
        {
            x.ComponentId,
            x.CalculationType,
            x.AmountOrPercent,
            x.IsIncluded
        }));

        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@EffectiveFrom", model.EffectiveFrom.Date, DbType.Date);
        p.Add("@BasicSalaryMonthly", model.BasicSalaryMonthly, DbType.Decimal);
        p.Add("@HourlyOvertimeRate", model.HourlyOvertimeRate, DbType.Decimal);
        p.Add("@TaxRegime", model.TaxRegime.Trim(), DbType.String, size: 40);
        p.Add("@PaymentMethod", model.PaymentMethod.Trim(), DbType.String, size: 30);
        p.Add("@Remarks", model.Remarks, DbType.String, size: 300);
        p.Add("@ItemsJson", itemsJson, DbType.String);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_EmployeeSalaryStructure_Save", p);
        return p.Get<int>("@SavedId");
    }

    public async Task<(IEnumerable<PayrollRun> Runs, PayrollKpiSummaryViewModel Kpi)> GetPayrollRunsAsync(int companyId, int? year = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_PayrollRun_GetAll", p, commandType: CommandType.StoredProcedure);
        var runs = (await multi.ReadAsync<PayrollRun>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<PayrollKpiSummaryViewModel>() ?? new PayrollKpiSummaryViewModel();
        return (runs, kpi);
    }

    public async Task<int> GeneratePayrollRunAsync(int companyId, PayrollGenerateViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayrollYear", model.PayrollYear, DbType.Int32);
        p.Add("@PayrollMonth", model.PayrollMonth, DbType.Int32);
        p.Add("@PayDate", model.PayDate?.Date, DbType.Date);
        p.Add("@Notes", model.Notes, DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@PayrollRunId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_PayrollRun_Generate", p);
        return p.Get<int>("@PayrollRunId");
    }

    public async Task<PayrollRunDetailsViewModel?> GetPayrollRunDetailsAsync(int companyId, int payrollRunId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayrollRunId", payrollRunId, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_PayrollRun_GetDetails", p, commandType: CommandType.StoredProcedure);
        var run = await multi.ReadFirstOrDefaultAsync<PayrollRun>();
        if (run is null) return null;

        var payslips = (await multi.ReadAsync<Payslip>()).ToList();
        var deptSummary = (await multi.ReadAsync<PayrollDepartmentSummaryViewModel>()).ToList();

        return new PayrollRunDetailsViewModel
        {
            Run = run,
            Payslips = payslips,
            DepartmentSummary = deptSummary
        };
    }

    public Task UpdatePayrollRunStatusAsync(int companyId, int payrollRunId, string status, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayrollRunId", payrollRunId, DbType.Int32);
        p.Add("@PayrollStatus", status, DbType.String, size: 20);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PayrollRun_UpdateStatus", p);
    }

    public Task DeletePayrollRunAsync(int companyId, int payrollRunId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayrollRunId", payrollRunId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_PayrollRun_Delete", p);
    }

    public Task AdjustPayslipAsync(int companyId, PayslipAdjustViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayslipId", model.PayslipId, DbType.Int32);
        p.Add("@BonusAmount", model.BonusAmount, DbType.Decimal);
        p.Add("@OtherDeductions", model.OtherDeductions, DbType.Decimal);
        p.Add("@PaymentStatus", model.PaymentStatus.Trim(), DbType.String, size: 20);
        p.Add("@Notes", model.Notes, DbType.String, size: 300);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Payslip_Adjust", p);
    }

    public async Task<PayslipDetailViewModel?> GetPayslipDetailAsync(int companyId, int payslipId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayslipId", payslipId, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_Payslip_GetById", p, commandType: CommandType.StoredProcedure);
        var header = await multi.ReadFirstOrDefaultAsync<PayslipHeaderInfo>();
        if (header is null) return null;

        var lines = (await multi.ReadAsync<PayslipLine>()).ToList();
        return new PayslipDetailViewModel
        {
            Payslip = header,
            Lines = lines
        };
    }

    public Task<IEnumerable<BankTransferRowViewModel>> GetBankTransferListAsync(int companyId, int payrollRunId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@PayrollRunId", payrollRunId, DbType.Int32);
        return QuerySpAsync<BankTransferRowViewModel>("sp_Payroll_GetBankTransferList", p);
    }
}
