using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Organization;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuditLogService _auditLogService;

    public CompanyService(ICompanyRepository companyRepository, IAuditLogService auditLogService)
    {
        _companyRepository = companyRepository;
        _auditLogService = auditLogService;
    }

    public Task<IEnumerable<CompanyListItemViewModel>> GetAllAsync(string? searchTerm = null, bool? isActive = null)
        => _companyRepository.GetAllAsync(searchTerm, isActive);

    public Task<CompanyFormViewModel?> GetByIdAsync(int companyId)
        => _companyRepository.GetByIdAsync(companyId);

    public async Task<OperationResult<int>> CreateAsync(CompanyFormViewModel model, int userId, string username)
    {
        try
        {
            model.CompanyCode = model.CompanyCode.Trim().ToUpperInvariant();
            model.CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant();
            var newId = await _companyRepository.InsertAsync(model, userId);

            await _auditLogService.LogAsync(newId, userId, username, AuditActionType.Create.ToString(),
                "Organization", "Company", newId.ToString(), $"Created company {model.CompanyName} ({model.CompanyCode}).");

            return OperationResult<int>.Success(newId, "Company created successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(CompanyFormViewModel model, int userId, string username)
    {
        try
        {
            model.CompanyCode = model.CompanyCode.Trim().ToUpperInvariant();
            model.CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant();
            await _companyRepository.UpdateAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Update.ToString(),
                "Organization", "Company", model.CompanyId.ToString(), $"Updated company {model.CompanyName} ({model.CompanyCode}).");

            return OperationResult.Success("Company updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int companyId, int userId, string username)
    {
        try
        {
            await _companyRepository.DeleteAsync(companyId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "Company", companyId.ToString(), $"Soft-deleted company ID {companyId}.");

            return OperationResult.Success("Company deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}

public sealed class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IAuditLogService _auditLogService;

    public BranchService(IBranchRepository branchRepository, IAuditLogService auditLogService)
    {
        _branchRepository = branchRepository;
        _auditLogService = auditLogService;
    }

    public Task<IEnumerable<BranchListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null, bool? isActive = null)
        => _branchRepository.GetAllAsync(companyId, searchTerm, isActive);

    public Task<BranchFormViewModel?> GetByIdAsync(int companyId, int branchId)
        => _branchRepository.GetByIdAsync(companyId, branchId);

    public async Task<OperationResult<int>> CreateAsync(BranchFormViewModel model, int userId, string username)
    {
        try
        {
            model.BranchCode = model.BranchCode.Trim().ToUpperInvariant();
            var newId = await _branchRepository.InsertAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Create.ToString(),
                "Organization", "Branch", newId.ToString(), $"Created branch {model.BranchName} ({model.BranchCode}).");

            return OperationResult<int>.Success(newId, "Branch created successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(BranchFormViewModel model, int userId, string username)
    {
        try
        {
            model.BranchCode = model.BranchCode.Trim().ToUpperInvariant();
            await _branchRepository.UpdateAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Update.ToString(),
                "Organization", "Branch", model.BranchId.ToString(), $"Updated branch {model.BranchName} ({model.BranchCode}).");

            return OperationResult.Success("Branch updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int companyId, int branchId, int userId, string username)
    {
        try
        {
            await _branchRepository.DeleteAsync(companyId, branchId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "Branch", branchId.ToString(), $"Soft-deleted branch ID {branchId}.");

            return OperationResult.Success("Branch deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
