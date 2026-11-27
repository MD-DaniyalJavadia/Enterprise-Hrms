using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.AssetDocument;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class AssetDocumentService : IAssetDocumentService
{
    private static readonly HashSet<string> ValidAssetClasses = new(StringComparer.OrdinalIgnoreCase)
        { "IT", "Furniture", "Vehicle", "AccessCard", "Mobile", "Other" };
    private static readonly HashSet<string> ValidConditions = new(StringComparer.OrdinalIgnoreCase)
        { "New", "Good", "Fair", "Damaged" };
    private static readonly HashSet<string> ValidAssetStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Available", "Assigned", "Maintenance", "Retired", "Lost" };
    private static readonly HashSet<string> ValidReturnStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Returned", "Damaged", "Lost" };
    private static readonly HashSet<string> ValidReturnConditions = new(StringComparer.OrdinalIgnoreCase)
        { "Good", "Fair", "Damaged", "Lost" };
    private static readonly HashSet<string> ValidMaintTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Repair", "Preventive", "Upgrade", "Calibration", "Inspection" };
    private static readonly HashSet<string> ValidMaintStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Scheduled", "InProgress", "Completed", "Cancelled" };

    private static readonly HashSet<string> ValidDocScopeTypes = new(StringComparer.OrdinalIgnoreCase)
        { "CompanyPolicy", "Template", "Compliance", "Guide" };
    private static readonly HashSet<string> ValidDocVisibilities = new(StringComparer.OrdinalIgnoreCase)
        { "AllEmployees", "ManagersOnly", "HrOnly", "DepartmentSpecific" };
    private static readonly HashSet<string> ValidDocStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Draft", "Published", "Archived" };

    private readonly IAssetDocumentRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditLogService _auditLogService;

    public AssetDocumentService(
        IAssetDocumentRepository repository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IDepartmentRepository departmentRepository,
        IAuditLogService auditLogService)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _departmentRepository = departmentRepository;
        _auditLogService = auditLogService;
    }

    public async Task<List<AssetCategory>> GetAssetCategoriesAsync(int companyId, bool includeInactive = true)
        => (await _repository.GetAssetCategoriesAsync(companyId, includeInactive)).ToList();

    public async Task<OperationResult<int>> SaveAssetCategoryAsync(int companyId, AssetCategoryFormViewModel model, int userId, string username)
    {
        if (!ValidAssetClasses.Contains(model.AssetClass))
            return OperationResult<int>.Failure("Invalid asset class selected.");

        model.AssetClass = ValidAssetClasses.First(x => x.Equals(model.AssetClass, StringComparison.OrdinalIgnoreCase));
        try
        {
            var id = await _repository.SaveAssetCategoryAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.CategoryId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Assets", "AssetCategory", id.ToString(), $"Saved asset category '{model.CategoryName}'.");
            return OperationResult<int>.Success(id, "Asset category saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAssetCategoryAsync(int companyId, int categoryId, int userId, string username)
    {
        try
        {
            await _repository.DeleteAssetCategoryAsync(companyId, categoryId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Assets", "AssetCategory", categoryId.ToString(), $"Deleted asset category #{categoryId}.");
            return OperationResult.Success("Asset category deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<AssetIndexPageViewModel> GetAssetIndexPageAsync(
        int companyId, int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null,
        string? search = null, int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : (currentEmployeeId ?? -1);
        var (assets, kpi) = await _repository.GetAssetsAsync(companyId, categoryId, branchId, status, effectiveEmpFilter, search);
        var categories = (await _repository.GetAssetCategoriesAsync(companyId, includeInactive: false)).ToList();
        var branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList();
        var employees = canManage
            ? (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
            : new();

        return new AssetIndexPageViewModel
        {
            CategoryId = categoryId,
            BranchId = branchId,
            Status = status,
            EmployeeId = effectiveEmpFilter > 0 ? effectiveEmpFilter : null,
            Search = search,
            CanManage = canManage,
            Kpi = kpi,
            Assets = assets,
            Categories = categories,
            Branches = branches,
            Employees = employees
        };
    }

    public async Task<AssetDetailsPageViewModel?> GetAssetDetailsAsync(int companyId, int assetId, bool canManage)
    {
        var detail = await _repository.GetAssetByIdAsync(companyId, assetId);
        if (detail is null) return null;

        detail.CanManage = canManage;
        if (canManage)
        {
            detail.Employees = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList();
        }
        return detail;
    }

    public async Task<OperationResult<int>> SaveAssetAsync(int companyId, AssetFormViewModel model, int userId, string username)
    {
        if (!ValidConditions.Contains(model.ConditionStatus))
            return OperationResult<int>.Failure("Invalid condition status.");
        if (!ValidAssetStatuses.Contains(model.AssetStatus))
            return OperationResult<int>.Failure("Invalid asset status.");

        model.ConditionStatus = ValidConditions.First(x => x.Equals(model.ConditionStatus, StringComparison.OrdinalIgnoreCase));
        model.AssetStatus = ValidAssetStatuses.First(x => x.Equals(model.AssetStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveAssetAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.AssetId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Assets", "Asset", id.ToString(), $"Saved asset '{model.AssetName}' ({model.AssetStatus}).");
            return OperationResult<int>.Success(id, "Asset saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAssetAsync(int companyId, int assetId, int userId, string username)
    {
        try
        {
            await _repository.DeleteAssetAsync(companyId, assetId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Assets", "Asset", assetId.ToString(), $"Deleted asset #{assetId}.");
            return OperationResult.Success("Asset removed from register.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<AssetAllocationsPageViewModel> GetAllocationsPageAsync(
        int companyId, int? assetId = null, int? employeeId = null, string? status = null,
        int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmpFilter = canManage ? employeeId : (currentEmployeeId ?? -1);
        var allocations = (await _repository.GetAllocationsAsync(companyId, assetId, effectiveEmpFilter, status)).ToList();
        var availableAssets = canManage
            ? (await _repository.GetAssetsAsync(companyId, status: "Available")).Assets
            : new();
        var employees = canManage
            ? (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList()
            : new();

        return new AssetAllocationsPageViewModel
        {
            AssetId = assetId,
            EmployeeId = effectiveEmpFilter > 0 ? effectiveEmpFilter : null,
            Status = status,
            CanManage = canManage,
            Allocations = allocations,
            AvailableAssets = availableAssets,
            Employees = employees
        };
    }

    public async Task<OperationResult<int>> AssignAssetAsync(int companyId, AssetAssignFormViewModel model, int userId, string username)
    {
        if (!ValidConditions.Contains(model.ConditionAtIssue))
            return OperationResult<int>.Failure("Invalid issue condition.");

        if (model.ExpectedReturnDate.HasValue && model.ExpectedReturnDate.Value.Date < model.AssignedDate.Date)
            return OperationResult<int>.Failure("Expected return date cannot precede assigned date.");

        model.ConditionAtIssue = ValidConditions.First(x => x.Equals(model.ConditionAtIssue, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.AssignAssetAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Assets", "AssetAllocation", id.ToString(), $"Assigned asset #{model.AssetId} to employee #{model.EmployeeId}.");
            return OperationResult<int>.Success(id, "Asset assigned successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> ReturnAssetAsync(int companyId, AssetReturnFormViewModel model, int userId, string username)
    {
        if (!ValidReturnConditions.Contains(model.ConditionAtReturn))
            return OperationResult.Failure("Invalid return condition.");
        if (!ValidReturnStatuses.Contains(model.AllocationStatus))
            return OperationResult.Failure("Invalid return status.");

        model.ConditionAtReturn = ValidReturnConditions.First(x => x.Equals(model.ConditionAtReturn, StringComparison.OrdinalIgnoreCase));
        model.AllocationStatus = ValidReturnStatuses.First(x => x.Equals(model.AllocationStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            await _repository.ReturnAssetAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Assets", "AssetAllocation", model.AllocationId.ToString(),
                $"Processed asset return for allocation #{model.AllocationId} ({model.AllocationStatus}).");
            return OperationResult.Success("Asset return recorded.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> AcknowledgeAllocationAsync(int companyId, int allocationId, int employeeId, int userId, string username)
    {
        try
        {
            await _repository.AcknowledgeAllocationAsync(companyId, allocationId, employeeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Assets", "AssetAllocation", allocationId.ToString(), $"Employee #{employeeId} acknowledged asset custody.");
            return OperationResult.Success("Asset custody acknowledged.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<AssetMaintenancePageViewModel> GetMaintenancePageAsync(int companyId, int? assetId = null, string? status = null)
    {
        var logs = (await _repository.GetMaintenanceLogsAsync(companyId, assetId, status)).ToList();
        var (assets, _) = await _repository.GetAssetsAsync(companyId);
        return new AssetMaintenancePageViewModel
        {
            AssetId = assetId,
            Status = status,
            Logs = logs,
            Assets = assets
        };
    }

    public async Task<OperationResult<int>> SaveMaintenanceLogAsync(int companyId, AssetMaintenanceFormViewModel model, int userId, string username)
    {
        if (!ValidMaintTypes.Contains(model.MaintenanceType))
            return OperationResult<int>.Failure("Invalid maintenance type.");
        if (!ValidMaintStatuses.Contains(model.MaintenanceStatus))
            return OperationResult<int>.Failure("Invalid maintenance status.");

        if (model.CompletionDate.HasValue && model.CompletionDate.Value.Date < model.ServiceDate.Date)
            return OperationResult<int>.Failure("Completion date cannot precede service date.");

        model.MaintenanceType = ValidMaintTypes.First(x => x.Equals(model.MaintenanceType, StringComparison.OrdinalIgnoreCase));
        model.MaintenanceStatus = ValidMaintStatuses.First(x => x.Equals(model.MaintenanceStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveMaintenanceLogAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.MaintenanceId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Assets", "AssetMaintenance", id.ToString(), $"Saved maintenance log #{id} ({model.MaintenanceStatus}).");
            return OperationResult<int>.Success(id, "Maintenance log saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteMaintenanceLogAsync(int companyId, int maintenanceId, int userId, string username)
    {
        await _repository.DeleteMaintenanceLogAsync(companyId, maintenanceId, userId);
        await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
            "Assets", "AssetMaintenance", maintenanceId.ToString(), $"Deleted maintenance log #{maintenanceId}.");
        return OperationResult.Success("Maintenance log deleted.");
    }

    public async Task<List<DocumentCategory>> GetDocumentCategoriesAsync(int companyId, bool includeInactive = true)
        => (await _repository.GetDocumentCategoriesAsync(companyId, includeInactive)).ToList();

    public async Task<OperationResult<int>> SaveDocumentCategoryAsync(int companyId, DocumentCategoryFormViewModel model, int userId, string username)
    {
        if (!ValidDocScopeTypes.Contains(model.ScopeType))
            return OperationResult<int>.Failure("Invalid document scope type.");
        if (!ValidDocVisibilities.Contains(model.DefaultVisibility))
            return OperationResult<int>.Failure("Invalid default visibility.");

        model.ScopeType = ValidDocScopeTypes.First(x => x.Equals(model.ScopeType, StringComparison.OrdinalIgnoreCase));
        model.DefaultVisibility = ValidDocVisibilities.First(x => x.Equals(model.DefaultVisibility, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveDocumentCategoryAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.CategoryId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Documents", "DocumentCategory", id.ToString(), $"Saved document category '{model.CategoryName}'.");
            return OperationResult<int>.Success(id, "Document category saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteDocumentCategoryAsync(int companyId, int categoryId, int userId, string username)
    {
        try
        {
            await _repository.DeleteDocumentCategoryAsync(companyId, categoryId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Documents", "DocumentCategory", categoryId.ToString(), $"Deleted document category #{categoryId}.");
            return OperationResult.Success("Document category deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<DocumentIndexPageViewModel> GetDocumentIndexPageAsync(
        int companyId, int? categoryId = null, string? status = null, string? visibility = null, string? search = null,
        int? currentEmployeeId = null, bool canManage = false, bool isManager = false)
    {
        var (docs, kpi) = await _repository.GetCompanyDocumentsAsync(
            companyId, categoryId, status, visibility, currentEmployeeId, canManage, isManager, search);
        var categories = (await _repository.GetDocumentCategoriesAsync(companyId, includeInactive: false)).ToList();
        var departments = canManage ? (await _departmentRepository.GetAllAsync(companyId)).ToList() : new();

        return new DocumentIndexPageViewModel
        {
            CategoryId = categoryId,
            Status = status,
            Visibility = visibility,
            Search = search,
            CanManage = canManage,
            HasLinkedEmployee = currentEmployeeId.HasValue,
            Kpi = kpi,
            Documents = docs,
            Categories = categories,
            Departments = departments
        };
    }

    public async Task<CompanyDocument?> GetAccessibleCompanyDocumentAsync(
        int companyId, int documentId, int? currentEmployeeId, bool canManage, bool isManager)
    {
        var doc = await _repository.GetCompanyDocumentByIdAsync(companyId, documentId);
        if (doc is null) return null;
        if (canManage) return doc;

        if (!string.Equals(doc.DocumentStatus, "Published", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(doc.Visibility, "AllEmployees", StringComparison.OrdinalIgnoreCase))
        {
            return doc;
        }

        if (string.Equals(doc.Visibility, "ManagersOnly", StringComparison.OrdinalIgnoreCase) && isManager)
        {
            return doc;
        }

        if (string.Equals(doc.Visibility, "DepartmentSpecific", StringComparison.OrdinalIgnoreCase) &&
            currentEmployeeId.HasValue && doc.DepartmentId.HasValue)
        {
            var emp = await _employeeRepository.GetProfileByIdAsync(companyId, currentEmployeeId.Value);
            if (emp is not null && emp.DepartmentId == doc.DepartmentId.Value)
            {
                return doc;
            }
        }

        return null;
    }

    public async Task<OperationResult<int>> SaveCompanyDocumentAsync(
        int companyId, CompanyDocumentFormViewModel model, string? originalFileName,
        string? storedFileName, string? contentType, long? fileSizeBytes, int userId, string username)
    {
        if (!ValidDocVisibilities.Contains(model.Visibility))
            return OperationResult<int>.Failure("Invalid document visibility.");
        if (!ValidDocStatuses.Contains(model.DocumentStatus))
            return OperationResult<int>.Failure("Invalid document status.");

        if (model.Visibility.Equals("DepartmentSpecific", StringComparison.OrdinalIgnoreCase) && !model.DepartmentId.HasValue)
            return OperationResult<int>.Failure("Please select a target department for department-specific documents.");

        if (model.ExpiryDate.HasValue && model.ExpiryDate.Value.Date < model.EffectiveDate.Date)
            return OperationResult<int>.Failure("Expiry date cannot precede effective date.");

        model.Visibility = ValidDocVisibilities.First(x => x.Equals(model.Visibility, StringComparison.OrdinalIgnoreCase));
        model.DocumentStatus = ValidDocStatuses.First(x => x.Equals(model.DocumentStatus, StringComparison.OrdinalIgnoreCase));

        try
        {
            var id = await _repository.SaveCompanyDocumentAsync(
                companyId, model, originalFileName, storedFileName, contentType, fileSizeBytes, userId);
            await _auditLogService.LogAsync(companyId, userId, username,
                model.DocumentId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Documents", "CompanyDocument", id.ToString(), $"Saved document '{model.Title}' v{model.VersionNumber}.");
            return OperationResult<int>.Success(id, "Document saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteCompanyDocumentAsync(int companyId, int documentId, int userId, string username)
    {
        try
        {
            await _repository.DeleteCompanyDocumentAsync(companyId, documentId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Documents", "CompanyDocument", documentId.ToString(), $"Deleted document #{documentId}.");
            return OperationResult.Success("Document deleted.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> AcknowledgeCompanyDocumentAsync(
        int companyId, int documentId, int employeeId, bool canManage, bool isManager, string? ipAddress, string? remarks, int userId, string username)
    {
        var accessible = await GetAccessibleCompanyDocumentAsync(companyId, documentId, employeeId, canManage, isManager);
        if (accessible is null)
            return OperationResult.Failure("Document not found or you do not have access to acknowledge it.");

        try
        {
            await _repository.AcknowledgeCompanyDocumentAsync(companyId, documentId, employeeId, ipAddress, remarks);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Documents", "DocumentAcknowledgment", documentId.ToString(),
                $"Employee #{employeeId} acknowledged document '{accessible.Title}' v{accessible.VersionNumber}.");
            return OperationResult.Success($"Acknowledged '{accessible.Title}' (v{accessible.VersionNumber}).");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<DocumentAcknowledgmentsPageViewModel> GetAcknowledgmentsPageAsync(
        int companyId, int? documentId = null, int? employeeId = null, int? currentEmployeeId = null, bool canManage = false, bool isManager = false)
    {
        var effectiveEmp = canManage ? employeeId : (currentEmployeeId ?? -1);
        var acks = (await _repository.GetDocumentAcknowledgmentsAsync(companyId, documentId, effectiveEmp)).ToList();
        var (docs, _) = await _repository.GetCompanyDocumentsAsync(companyId, employeeId: currentEmployeeId, canManage: canManage, isManager: isManager);
        var employees = canManage ? (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList() : new();

        return new DocumentAcknowledgmentsPageViewModel
        {
            DocumentId = documentId,
            EmployeeId = effectiveEmp > 0 ? effectiveEmp : null,
            CanManage = canManage,
            Acknowledgments = acks,
            Documents = docs,
            Employees = employees
        };
    }

    public async Task<EmployeeVaultPageViewModel> GetEmployeeVaultPageAsync(
        int companyId, int? employeeId = null, string? documentType = null, string? expiryFilter = null,
        int? currentEmployeeId = null, bool canManage = false)
    {
        var effectiveEmp = canManage ? employeeId : (currentEmployeeId ?? -1);
        var docs = (await _repository.GetEmployeeVaultDocumentsAsync(companyId, effectiveEmp, documentType, expiryFilter)).ToList();
        var employees = canManage ? (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive).ToList() : new();

        return new EmployeeVaultPageViewModel
        {
            EmployeeId = effectiveEmp > 0 ? effectiveEmp : null,
            DocumentType = documentType,
            ExpiryFilter = expiryFilter,
            CanManage = canManage,
            ExpiredCount = docs.Count(d => d.ExpiryStatus == "Expired"),
            ExpiringSoonCount = docs.Count(d => d.ExpiryStatus == "ExpiringSoon"),
            ValidCount = docs.Count(d => d.ExpiryStatus == "Valid" || d.ExpiryStatus == "NoExpiry"),
            Documents = docs,
            Employees = employees
        };
    }
}
