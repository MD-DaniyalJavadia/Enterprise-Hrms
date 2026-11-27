using HRMS.Models.Entities;
using HRMS.Models.ViewModels.LoanExpense;

namespace HRMS.Data.Interfaces;

public interface ILoanExpenseRepository
{
    Task<IEnumerable<LoanType>> GetLoanTypesAsync(int companyId, bool includeInactive = true);
    Task<int> SaveLoanTypeAsync(int companyId, LoanTypeFormViewModel model, int userId);
    Task DeleteLoanTypeAsync(int companyId, int loanTypeId, int userId);

    Task<(IEnumerable<EmployeeLoan> Loans, LoanKpiSummaryViewModel Kpi)> GetLoansAsync(int companyId, int? employeeId = null, string? status = null, string? searchTerm = null);
    Task<(EmployeeLoan? Loan, IEnumerable<LoanRepaymentInstallment> Schedule)> GetLoanByIdAsync(int companyId, int loanId);
    Task<int> SubmitLoanAsync(int companyId, EmployeeLoanFormViewModel model, int userId);
    Task ReviewLoanAsync(int companyId, LoanReviewViewModel model, int userId);
    Task RecordRepaymentAsync(int companyId, RecordRepaymentViewModel model, int userId);

    Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync(int companyId, bool includeInactive = true);
    Task<int> SaveExpenseCategoryAsync(int companyId, ExpenseCategoryFormViewModel model, int userId);
    Task DeleteExpenseCategoryAsync(int companyId, int categoryId, int userId);

    Task<(IEnumerable<ExpenseClaim> Claims, ExpenseKpiSummaryViewModel Kpi)> GetExpenseClaimsAsync(int companyId, int? employeeId = null, int? categoryId = null, string? status = null, string? searchTerm = null);
    Task<ExpenseClaim?> GetExpenseClaimByIdAsync(int companyId, int claimId);
    Task<int> SubmitExpenseClaimAsync(int companyId, ExpenseClaimFormViewModel model, int userId);
    Task ReviewExpenseClaimAsync(int companyId, ExpenseReviewViewModel model, int userId);
}
