using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.LoanExpense;

namespace HRMS.Business.Interfaces;

public interface ILoanExpenseService
{
    Task<List<LoanType>> GetLoanTypesAsync(int companyId, bool includeInactive = true);
    Task<OperationResult<int>> SaveLoanTypeAsync(int companyId, LoanTypeFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteLoanTypeAsync(int companyId, int loanTypeId, int userId, string username);

    Task<LoansIndexPageViewModel> GetLoansPageAsync(int companyId, int? employeeId = null, string? status = null, string? searchTerm = null, int? currentEmployeeId = null, bool canManage = false);
    Task<LoanDetailsPageViewModel?> GetLoanDetailsAsync(int companyId, int loanId);
    Task<OperationResult<int>> SubmitLoanAsync(int companyId, EmployeeLoanFormViewModel model, int userId, string username);
    Task<OperationResult> ReviewLoanAsync(int companyId, LoanReviewViewModel model, int userId, string username);
    Task<OperationResult> RecordRepaymentAsync(int companyId, RecordRepaymentViewModel model, int userId, string username);

    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync(int companyId, bool includeInactive = true);
    Task<OperationResult<int>> SaveExpenseCategoryAsync(int companyId, ExpenseCategoryFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteExpenseCategoryAsync(int companyId, int categoryId, int userId, string username);

    Task<ExpensesIndexPageViewModel> GetExpensesPageAsync(int companyId, int? employeeId = null, int? categoryId = null, string? status = null, string? searchTerm = null, int? currentEmployeeId = null, bool canManage = false);
    Task<ExpenseClaim?> GetExpenseClaimByIdAsync(int companyId, int claimId);
    Task<OperationResult<int>> SubmitExpenseClaimAsync(int companyId, ExpenseClaimFormViewModel model, int userId, string username);
    Task<OperationResult> ReviewExpenseClaimAsync(int companyId, ExpenseReviewViewModel model, int userId, string username);
}
