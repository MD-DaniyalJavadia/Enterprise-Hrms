using HRMS.Models.Entities;
using HRMS.Models.ViewModels.AssetDocument;

namespace HRMS.Data.Interfaces;

public interface IAssetDocumentRepository
{
    // Asset Categories
    Task<IEnumerable<AssetCategory>> GetAssetCategoriesAsync(int companyId, bool includeInactive = true);
    Task<int> SaveAssetCategoryAsync(int companyId, AssetCategoryFormViewModel model, int userId);
    Task DeleteAssetCategoryAsync(int companyId, int categoryId, int userId);

    // Assets
    Task<(List<AssetItem> Assets, AssetKpiSummaryViewModel Kpi)> GetAssetsAsync(
        int companyId, int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null, string? search = null);
    Task<AssetDetailsPageViewModel?> GetAssetByIdAsync(int companyId, int assetId);
    Task<int> SaveAssetAsync(int companyId, AssetFormViewModel model, int userId);
    Task DeleteAssetAsync(int companyId, int assetId, int userId);

    // Allocations
    Task<IEnumerable<AssetAllocation>> GetAllocationsAsync(int companyId, int? assetId = null, int? employeeId = null, string? status = null);
    Task<int> AssignAssetAsync(int companyId, AssetAssignFormViewModel model, int userId);
    Task ReturnAssetAsync(int companyId, AssetReturnFormViewModel model, int userId);
    Task AcknowledgeAllocationAsync(int companyId, int allocationId, int employeeId, int userId);

    // Maintenance
    Task<IEnumerable<AssetMaintenanceLog>> GetMaintenanceLogsAsync(int companyId, int? assetId = null, string? status = null);
    Task<int> SaveMaintenanceLogAsync(int companyId, AssetMaintenanceFormViewModel model, int userId);
    Task DeleteMaintenanceLogAsync(int companyId, int maintenanceId, int userId);

    // Document Categories
    Task<IEnumerable<DocumentCategory>> GetDocumentCategoriesAsync(int companyId, bool includeInactive = true);
    Task<int> SaveDocumentCategoryAsync(int companyId, DocumentCategoryFormViewModel model, int userId);
    Task DeleteDocumentCategoryAsync(int companyId, int categoryId, int userId);

    // Company Documents & Acknowledgments
    Task<(List<CompanyDocument> Documents, DocumentKpiSummaryViewModel Kpi)> GetCompanyDocumentsAsync(
        int companyId, int? categoryId = null, string? status = null, string? visibility = null,
        int? employeeId = null, bool canManage = false, bool isManager = false, string? search = null);
    Task<CompanyDocument?> GetCompanyDocumentByIdAsync(int companyId, int documentId);
    Task<int> SaveCompanyDocumentAsync(
        int companyId, CompanyDocumentFormViewModel model, string? originalFileName,
        string? storedFileName, string? contentType, long? fileSizeBytes, int userId);
    Task DeleteCompanyDocumentAsync(int companyId, int documentId, int userId);
    Task AcknowledgeCompanyDocumentAsync(int companyId, int documentId, int employeeId, string? ipAddress, string? remarks);
    Task<IEnumerable<DocumentAcknowledgment>> GetDocumentAcknowledgmentsAsync(int companyId, int? documentId = null, int? employeeId = null);

    // Employee Document Vault (integrates Phase 3 tbl_EmployeeDocuments)
    Task<IEnumerable<EmployeeVaultDocument>> GetEmployeeVaultDocumentsAsync(
        int companyId, int? employeeId = null, string? documentType = null, string? expiryFilter = null);
}
