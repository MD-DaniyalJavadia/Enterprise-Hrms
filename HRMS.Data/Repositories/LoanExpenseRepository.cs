using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.LoanExpense;

namespace HRMS.Data.Repositories;

public sealed class LoanExpenseRepository : BaseRepository, ILoanExpenseRepository
{
    public LoanExpenseRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<LoanType>> GetLoanTypesAsync(int companyId, bool includeInactive = true)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@IncludeInactive", includeInactive, DbType.Boolean);
        return QuerySpAsync<LoanType>("sp_LoanType_GetAll", p);
    }

    public async Task<int> SaveLoanTypeAsync(int companyId, LoanTypeFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@LoanTypeId", model.LoanTypeId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@TypeCode", model.TypeCode.Trim().ToUpperInvariant(), DbType.String, size: 20);
        p.Add("@TypeName", model.TypeName.Trim(), DbType.String, size: 100);
        p.Add("@Category", model.Category.Trim(), DbType.String, size: 20);
        p.Add("@MaxAmount", model.MaxAmount, DbType.Decimal);
        p.Add("@DefaultInterestRate", model.DefaultInterestRate, DbType.Decimal);
        p.Add("@MaxTenureMonths", model.MaxTenureMonths, DbType.Int32);
        p.Add("@RequiresGuarantor", model.RequiresGuarantor, DbType.Boolean);
        p.Add("@Description", model.Description, DbType.String, size: 400);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_LoanType_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteLoanTypeAsync(int companyId, int loanTypeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LoanTypeId", loanTypeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_LoanType_Delete", p);
    }

    public async Task<(IEnumerable<EmployeeLoan> Loans, LoanKpiSummaryViewModel Kpi)> GetLoansAsync(
        int companyId, int? employeeId = null, string? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_EmployeeLoan_GetAll", p, commandType: CommandType.StoredProcedure);
        var loans = (await multi.ReadAsync<EmployeeLoan>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<LoanKpiSummaryViewModel>() ?? new LoanKpiSummaryViewModel();
        return (loans, kpi);
    }

    public async Task<(EmployeeLoan? Loan, IEnumerable<LoanRepaymentInstallment> Schedule)> GetLoanByIdAsync(int companyId, int loanId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LoanId", loanId, DbType.Int32);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_EmployeeLoan_GetById", p, commandType: CommandType.StoredProcedure);
        var loan = await multi.ReadFirstOrDefaultAsync<EmployeeLoan>();
        var schedule = loan is null ? Enumerable.Empty<LoanRepaymentInstallment>() : (await multi.ReadAsync<LoanRepaymentInstallment>()).ToList();
        return (loan, schedule);
    }

    public async Task<int> SubmitLoanAsync(int companyId, EmployeeLoanFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@LoanTypeId", model.LoanTypeId, DbType.Int32);
        p.Add("@PrincipalAmount", model.PrincipalAmount, DbType.Decimal);
        p.Add("@TenureMonths", model.TenureMonths, DbType.Int32);
        p.Add("@RepaymentStartDate", model.RepaymentStartDate.Date, DbType.Date);
        p.Add("@Purpose", model.Purpose, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_EmployeeLoan_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task ReviewLoanAsync(int companyId, LoanReviewViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LoanId", model.LoanId, DbType.Int32);
        p.Add("@ActionStatus", model.ActionStatus.Trim(), DbType.String, size: 20);
        p.Add("@RepaymentStartDate", model.RepaymentStartDate?.Date, DbType.Date);
        p.Add("@DisbursedDate", model.DisbursedDate?.Date, DbType.Date);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeLoan_Review", p);
    }

    public Task RecordRepaymentAsync(int companyId, RecordRepaymentViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ScheduleId", model.ScheduleId, DbType.Int32);
        p.Add("@PaidDate", model.PaidDate.Date, DbType.Date);
        p.Add("@PaymentReference", model.PaymentReference, DbType.String, size: 80);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeLoan_RecordRepayment", p);
    }

    public Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync(int companyId, bool includeInactive = true)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@IncludeInactive", includeInactive, DbType.Boolean);
        return QuerySpAsync<ExpenseCategory>("sp_ExpenseCategory_GetAll", p);
    }

    public async Task<int> SaveExpenseCategoryAsync(int companyId, ExpenseCategoryFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CategoryId", model.CategoryId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CategoryCode", model.CategoryCode.Trim().ToUpperInvariant(), DbType.String, size: 20);
        p.Add("@CategoryName", model.CategoryName.Trim(), DbType.String, size: 100);
        p.Add("@MaxLimitPerClaim", model.MaxLimitPerClaim, DbType.Decimal);
        p.Add("@RequiresReceipt", model.RequiresReceipt, DbType.Boolean);
        p.Add("@Description", model.Description, DbType.String, size: 400);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_ExpenseCategory_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteExpenseCategoryAsync(int companyId, int categoryId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CategoryId", categoryId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_ExpenseCategory_Delete", p);
    }

    public async Task<(IEnumerable<ExpenseClaim> Claims, ExpenseKpiSummaryViewModel Kpi)> GetExpenseClaimsAsync(
        int companyId, int? employeeId = null, int? categoryId = null, string? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@CategoryId", categoryId, DbType.Int32);
        p.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status.Trim(), DbType.String, size: 20);
        p.Add("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(), DbType.String, size: 100);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("sp_ExpenseClaim_GetAll", p, commandType: CommandType.StoredProcedure);
        var claims = (await multi.ReadAsync<ExpenseClaim>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<ExpenseKpiSummaryViewModel>() ?? new ExpenseKpiSummaryViewModel();
        return (claims, kpi);
    }

    public Task<ExpenseClaim?> GetExpenseClaimByIdAsync(int companyId, int claimId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ClaimId", claimId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<ExpenseClaim>("sp_ExpenseClaim_GetById", p);
    }

    public async Task<int> SubmitExpenseClaimAsync(int companyId, ExpenseClaimFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@CategoryId", model.CategoryId, DbType.Int32);
        p.Add("@ExpenseDate", model.ExpenseDate.Date, DbType.Date);
        p.Add("@MerchantOrVendor", model.MerchantOrVendor.Trim(), DbType.String, size: 150);
        p.Add("@ClaimAmount", model.ClaimAmount, DbType.Decimal);
        p.Add("@Description", model.Description, DbType.String, size: 500);
        p.Add("@ReceiptFileName", model.ReceiptFileName, DbType.String, size: 255);
        p.Add("@ReceiptStoredName", model.ReceiptStoredName, DbType.String, size: 255);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_ExpenseClaim_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task ReviewExpenseClaimAsync(int companyId, ExpenseReviewViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ClaimId", model.ClaimId, DbType.Int32);
        p.Add("@ActionStatus", model.ActionStatus.Trim(), DbType.String, size: 20);
        p.Add("@ApprovedAmount", model.ApprovedAmount, DbType.Decimal);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@PaymentReference", model.PaymentReference, DbType.String, size: 80);
        p.Add("@ReimbursedDate", model.ReimbursedDate?.Date, DbType.Date);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_ExpenseClaim_Review", p);
    }
}
