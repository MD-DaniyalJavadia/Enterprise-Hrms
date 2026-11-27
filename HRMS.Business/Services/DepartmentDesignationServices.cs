using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Organization;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IAuditLogService _auditLogService;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IBranchRepository branchRepository,
        IOrgStructureRepository orgStructureRepository,
        IAuditLogService auditLogService)
    {
        _departmentRepository = departmentRepository;
        _branchRepository = branchRepository;
        _orgStructureRepository = orgStructureRepository;
        _auditLogService = auditLogService;
    }

    public Task<IEnumerable<DepartmentListItemViewModel>> GetAllAsync(int companyId, int? branchId = null, string? searchTerm = null)
        => _departmentRepository.GetAllAsync(companyId, branchId, searchTerm);

    public Task<DepartmentFormViewModel?> GetByIdAsync(int companyId, int departmentId)
        => _departmentRepository.GetByIdAsync(companyId, departmentId);

    public async Task<DepartmentFormViewModel> PrepareFormAsync(int companyId, DepartmentFormViewModel? existing = null)
    {
        var model = existing ?? new DepartmentFormViewModel { CompanyId = companyId, IsActive = true };
        model.CompanyId = companyId;
        model.AvailableBranches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList();
        model.AvailableParentDepartments = (await _departmentRepository.GetAllAsync(companyId))
            .Where(d => d.DepartmentId != model.DepartmentId && d.IsActive)
            .ToList();
        model.AvailableCostCenters = (await _orgStructureRepository.GetCostCentersAsync(companyId))
            .Where(c => c.IsActive)
            .ToList();
        return model;
    }

    public async Task<OperationResult<int>> CreateAsync(DepartmentFormViewModel model, int userId, string username)
    {
        try
        {
            model.DepartmentCode = model.DepartmentCode.Trim().ToUpperInvariant();
            var newId = await _departmentRepository.InsertAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Create.ToString(),
                "Organization", "Department", newId.ToString(), $"Created department {model.DepartmentName} ({model.DepartmentCode}).");

            return OperationResult<int>.Success(newId, "Department created successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(DepartmentFormViewModel model, int userId, string username)
    {
        if (model.ParentDepartmentId == model.DepartmentId)
        {
            return OperationResult.Failure("A department cannot be assigned as its own parent.");
        }

        // Prevent circular hierarchy in parent departments
        if (model.ParentDepartmentId.HasValue)
        {
            var all = (await _departmentRepository.GetAllAsync(model.CompanyId)).ToDictionary(d => d.DepartmentId);
            int? cursor = model.ParentDepartmentId;
            while (cursor.HasValue && all.TryGetValue(cursor.Value, out var node))
            {
                if (node.ParentDepartmentId == model.DepartmentId)
                {
                    return OperationResult.Failure("Circular department hierarchy detected. Choose a different parent department.");
                }
                cursor = node.ParentDepartmentId;
            }
        }

        try
        {
            model.DepartmentCode = model.DepartmentCode.Trim().ToUpperInvariant();
            await _departmentRepository.UpdateAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Update.ToString(),
                "Organization", "Department", model.DepartmentId.ToString(), $"Updated department {model.DepartmentName} ({model.DepartmentCode}).");

            return OperationResult.Success("Department updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int companyId, int departmentId, int userId, string username)
    {
        try
        {
            await _departmentRepository.DeleteAsync(companyId, departmentId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "Department", departmentId.ToString(), $"Soft-deleted department ID {departmentId}.");

            return OperationResult.Success("Department deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}

public sealed class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _designationRepository;
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IAuditLogService _auditLogService;

    public DesignationService(
        IDesignationRepository designationRepository,
        IOrgStructureRepository orgStructureRepository,
        IAuditLogService auditLogService)
    {
        _designationRepository = designationRepository;
        _orgStructureRepository = orgStructureRepository;
        _auditLogService = auditLogService;
    }

    public Task<IEnumerable<DesignationListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null)
        => _designationRepository.GetAllAsync(companyId, searchTerm);

    public Task<DesignationFormViewModel?> GetByIdAsync(int companyId, int designationId)
        => _designationRepository.GetByIdAsync(companyId, designationId);

    public async Task<DesignationFormViewModel> PrepareFormAsync(int companyId, DesignationFormViewModel? existing = null)
    {
        var model = existing ?? new DesignationFormViewModel { CompanyId = companyId, IsActive = true };
        model.CompanyId = companyId;
        model.AvailableJobGrades = (await _orgStructureRepository.GetJobGradesAsync(companyId))
            .Where(g => g.IsActive)
            .ToList();
        return model;
    }

    public async Task<OperationResult<int>> CreateAsync(DesignationFormViewModel model, int userId, string username)
    {
        try
        {
            model.DesignationCode = model.DesignationCode.Trim().ToUpperInvariant();
            var newId = await _designationRepository.InsertAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Create.ToString(),
                "Organization", "Designation", newId.ToString(), $"Created designation {model.DesignationTitle} ({model.DesignationCode}).");

            return OperationResult<int>.Success(newId, "Designation created successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(DesignationFormViewModel model, int userId, string username)
    {
        try
        {
            model.DesignationCode = model.DesignationCode.Trim().ToUpperInvariant();
            await _designationRepository.UpdateAsync(model, userId);

            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Update.ToString(),
                "Organization", "Designation", model.DesignationId.ToString(), $"Updated designation {model.DesignationTitle} ({model.DesignationCode}).");

            return OperationResult.Success("Designation updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int companyId, int designationId, int userId, string username)
    {
        try
        {
            await _designationRepository.DeleteAsync(companyId, designationId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "Designation", designationId.ToString(), $"Soft-deleted designation ID {designationId}.");

            return OperationResult.Success("Designation deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
