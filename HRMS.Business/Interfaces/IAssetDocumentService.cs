using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.AssetDocument;

namespace HRMS.Business.Interfaces;

public interface IAssetDocumentService
{
    // Assets
    Task<List<AssetCategory>> GetAssetCategoriesAsync(int companyId, bool includeInactive = true);
    Task<OperationResult<int>> SaveAssetCategoryAsync(int companyId, AssetCategoryFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAssetCategoryAsync(int companyId, int categoryId, int userId, string username);

    Task<AssetIndexPageViewModel> GetAssetIndexPageAsync(
        int companyId, int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null,
        string? search = null, int? currentEmployeeId = null, bool canManage = false);
    Task<AssetDetailsPageViewModel?> GetAssetDetailsAsync(int companyId, int assetId, bool canManage);
    Task<OperationResult<int>> SaveAssetAsync(int companyId, AssetFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAssetAsync(int companyId, int assetId, int userId, string username);

    Task<AssetAllocationsPageViewModel> GetAllocationsPageAsync(
        int companyId, int? assetId = null, int? employeeId = null, string? status = null,
        int? currentEmployeeId = null, bool canManage = false);
    Task<OperationResult<int>> AssignAssetAsync(int companyId, AssetAssignFormViewModel model, int userId, string username);
    Task<OperationResult> ReturnAssetAsync(int companyId, AssetReturnFormViewModel model, int userId, string username);
    Task<OperationResult> AcknowledgeAllocationAsync(int companyId, int allocationId, int employeeId, int userId, string username);

    Task<AssetMaintenancePageViewModel> GetMaintenancePageAsync(int companyId, int? assetId = null, string? status = null);
    Task<OperationResult<int>> SaveMaintenanceLogAsync(int companyId, AssetMaintenanceFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteMaintenanceLogAsync(int companyId, int maintenanceId, int userId, string username);

    // Documents
    Task<List<DocumentCategory>> GetDocumentCategoriesAsync(int companyId, bool includeInactive = true);
    Task<OperationResult<int>> SaveDocumentCategoryAsync(int companyId, DocumentCategoryFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteDocumentCategoryAsync(int companyId, int categoryId, int userId, string username);

    Task<DocumentIndexPageViewModel> GetDocumentIndexPageAsync(
        int companyId, int? categoryId = null, string? status = null, string? visibility = null, string? search = null,
        int? currentEmployeeId = null, bool canManage = false, bool isManager = false);
    Task<CompanyDocument?> GetAccessibleCompanyDocumentAsync(
        int companyId, int documentId, int? currentEmployeeId, bool canManage, bool isManager);
    Task<OperationResult<int>> SaveCompanyDocumentAsync(
        int companyId, CompanyDocumentFormViewModel model, string? originalFileName,
        string? storedFileName, string? contentType, long? fileSizeBytes, int userId, string username);
    Task<OperationResult> DeleteCompanyDocumentAsync(int companyId, int documentId, int userId, string username);
    Task<OperationResult> AcknowledgeCompanyDocumentAsync(
        int companyId, int documentId, int employeeId, bool canManage, bool isManager, string? ipAddress, string? remarks, int userId, string username);
    Task<DocumentAcknowledgmentsPageViewModel> GetAcknowledgmentsPageAsync(
        int companyId, int? documentId = null, int? employeeId = null, int? currentEmployeeId = null, bool canManage = false, bool isManager = false);

    // Employee Vault
    Task<EmployeeVaultPageViewModel> GetEmployeeVaultPageAsync(
        int companyId, int? employeeId = null, string? documentType = null, string? expiryFilter = null,
        int? currentEmployeeId = null, bool canManage = false);
}
