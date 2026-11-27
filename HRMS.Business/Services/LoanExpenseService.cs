using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.LoanExpense;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class LoanExpenseService : ILoanExpenseService
{
    private static readonly HashSet<string> ValidLoanCategories = new(StringComparer.OrdinalIgnoreCase) { "Loan", "SalaryAdvance" };
    private static readonly HashSet<string> ValidLoanReviewActions = new(StringComparer.OrdinalIgnoreCase) { "Active", "Rejected", "Cancelled" };
    private static readonly HashSet<string> ValidExpenseActions = new(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected", "Reimbursed", "Cancelled" };

    private readonly ILoanExpenseRepository _loanExpenseRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;

    public LoanExpenseService(
        ILoanExpenseRepository loanExpenseRepository,
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService)
    {
        _loanExpenseRepository = loanExpenseRepository;
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<LoanType>> GetLoanTypesAsync(int companyId, bool includeInactive = true)
        => (await _loanExpenseRepository.GetLoanTypesAsync(companyId, includeInactive)).ToList();

    public async Task<OperationResult<int>> SaveLoanTypeAsync(int companyId, LoanTypeFormViewModel model, int userId, string username)
    {
        if (!ValidLoanCategories.Contains(model.Category))
        {
            return OperationResult<int>.Failure("Invalid loan category.");
        }

        model.Category = ValidLoanCategories.First(c => string.Equals(c, model.Category, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _loanExpenseRepository.SaveLoanTypeAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.LoanTypeId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Loans", "LoanType", id.ToString(), $"Saved loan policy '{model.TypeName}' ({model.TypeCode}).");
            return OperationResult<int>.Success(id, "Loan policy saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteLoanTypeAsync(int companyId, int loanTypeId, int userId, string username)
    {
        try
        {
            await _loanExpenseRepository.DeleteLoanTypeAsync(companyId, loanTypeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Loans", "LoanType", loanTypeId.ToString(), $"Deleted loan type #{loanTypeId}.");
            return OperationResult.Success("Loan type deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<LoansIndexPageViewModel> GetLoansPageAsync(
        int companyId, int? employeeId = null, string? status = null, string? searchTerm = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : currentEmployeeId;
        var (loans, kpi) = await _loanExpenseRepository.GetLoansAsync(companyId, effectiveEmpFilter, status, searchTerm);
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();

        return new LoansIndexPageViewModel
        {
            EmployeeId = effectiveEmpFilter,
            Status = status,
            SearchTerm = searchTerm,
            Kpi = kpi,
            Loans = loans.ToList(),
            LoanTypes = (await _loanExpenseRepository.GetLoanTypesAsync(companyId, includeInactive: false)).ToList(),
            Employees = canManage ? employees : employees.Where(e => e.EmployeeId == currentEmployeeId).ToList()
        };
    }

    public async Task<LoanDetailsPageViewModel?> GetLoanDetailsAsync(int companyId, int loanId)
    {
        var (loan, schedule) = await _loanExpenseRepository.GetLoanByIdAsync(companyId, loanId);
        if (loan is null) return null;
        return new LoanDetailsPageViewModel
        {
            Loan = loan,
            Schedule = schedule.ToList()
        };
    }

    public async Task<OperationResult<int>> SubmitLoanAsync(int companyId, EmployeeLoanFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _loanExpenseRepository.SubmitLoanAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Loans", "EmployeeLoan", id.ToString(),
                $"Submitted loan/advance application #{id} for Employee #{model.EmployeeId} ({model.PrincipalAmount:C2}).");
            return OperationResult<int>.Success(id, "Loan / advance application submitted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewLoanAsync(int companyId, LoanReviewViewModel model, int userId, string username)
    {
        if (!ValidLoanReviewActions.Contains(model.ActionStatus))
        {
            return OperationResult.Failure("Invalid loan review action.");
        }

        model.ActionStatus = ValidLoanReviewActions.First(s => string.Equals(s, model.ActionStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _loanExpenseRepository.ReviewLoanAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Loans", "EmployeeLoan", model.LoanId.ToString(),
                $"Updated loan #{model.LoanId} status to {model.ActionStatus}.");
            return OperationResult.Success(model.ActionStatus == "Active"
                ? "Loan approved, disbursed, and repayment schedule generated."
                : $"Loan marked as {model.ActionStatus}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> RecordRepaymentAsync(int companyId, RecordRepaymentViewModel model, int userId, string username)
    {
        try
        {
            await _loanExpenseRepository.RecordRepaymentAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Loans", "LoanRepayment", model.ScheduleId.ToString(),
                $"Recorded EMI repayment for schedule #{model.ScheduleId} (Ref: {model.PaymentReference ?? "Manual"}).");
            return OperationResult.Success("EMI repayment recorded and loan balance updated.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync(int companyId, bool includeInactive = true)
        => (await _loanExpenseRepository.GetExpenseCategoriesAsync(companyId, includeInactive)).ToList();

    public async Task<OperationResult<int>> SaveExpenseCategoryAsync(int companyId, ExpenseCategoryFormViewModel model, int userId, string username)
    {
        try
        {
            var id = await _loanExpenseRepository.SaveExpenseCategoryAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.CategoryId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Expenses", "ExpenseCategory", id.ToString(),
                $"Saved expense category '{model.CategoryName}' ({model.CategoryCode}).");
            return OperationResult<int>.Success(id, "Expense category saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteExpenseCategoryAsync(int companyId, int categoryId, int userId, string username)
    {
        try
        {
            await _loanExpenseRepository.DeleteExpenseCategoryAsync(companyId, categoryId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Expenses", "ExpenseCategory", categoryId.ToString(), $"Deleted expense category #{categoryId}.");
            return OperationResult.Success("Expense category deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<ExpensesIndexPageViewModel> GetExpensesPageAsync(
        int companyId, int? employeeId = null, int? categoryId = null, string? status = null, string? searchTerm = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : currentEmployeeId;
        var (claims, kpi) = await _loanExpenseRepository.GetExpenseClaimsAsync(companyId, effectiveEmpFilter, categoryId, status, searchTerm);
        var employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();

        return new ExpensesIndexPageViewModel
        {
            EmployeeId = effectiveEmpFilter,
            CategoryId = categoryId,
            Status = status,
            SearchTerm = searchTerm,
            Kpi = kpi,
            Claims = claims.ToList(),
            Categories = (await _loanExpenseRepository.GetExpenseCategoriesAsync(companyId, includeInactive: false)).ToList(),
            Employees = canManage ? employees : employees.Where(e => e.EmployeeId == currentEmployeeId).ToList()
        };
    }

    public Task<ExpenseClaim?> GetExpenseClaimByIdAsync(int companyId, int claimId)
        => _loanExpenseRepository.GetExpenseClaimByIdAsync(companyId, claimId);

    public async Task<OperationResult<int>> SubmitExpenseClaimAsync(int companyId, ExpenseClaimFormViewModel model, int userId, string username)
    {
        if (model.ExpenseDate.Date > DateTime.Today)
        {
            return OperationResult<int>.Failure("Expense date cannot be in the future.");
        }

        try
        {
            var id = await _loanExpenseRepository.SubmitExpenseClaimAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Expenses", "ExpenseClaim", id.ToString(),
                $"Submitted expense claim #{id} for Employee #{model.EmployeeId} ({model.ClaimAmount:C2}).");
            return OperationResult<int>.Success(id, "Expense claim submitted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReviewExpenseClaimAsync(int companyId, ExpenseReviewViewModel model, int userId, string username)
    {
        if (!ValidExpenseActions.Contains(model.ActionStatus))
        {
            return OperationResult.Failure("Invalid expense review status.");
        }

        model.ActionStatus = ValidExpenseActions.First(s => string.Equals(s, model.ActionStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _loanExpenseRepository.ReviewExpenseClaimAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Expenses", "ExpenseClaim", model.ClaimId.ToString(),
                $"Updated expense claim #{model.ClaimId} to {model.ActionStatus}.");
            return OperationResult.Success($"Expense claim marked as {model.ActionStatus}.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
