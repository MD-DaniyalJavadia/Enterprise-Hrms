using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.AssetDocument;

namespace HRMS.Data.Repositories;

public sealed class AssetDocumentRepository : BaseRepository, IAssetDocumentRepository
{
    public AssetDocumentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<AssetCategory>> GetAssetCategoriesAsync(int companyId, bool includeInactive = true)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@IncludeInactive", includeInactive);
        return QuerySpAsync<AssetCategory>("dbo.sp_AssetCategory_GetAll", p);
    }

    public async Task<int> SaveAssetCategoryAsync(int companyId, AssetCategoryFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CategoryId", model.CategoryId);
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryCode", model.CategoryCode.Trim().ToUpperInvariant());
        p.Add("@CategoryName", model.CategoryName.Trim());
        p.Add("@AssetClass", model.AssetClass.Trim());
        p.Add("@RequiresSerialNumber", model.RequiresSerialNumber);
        p.Add("@DefaultDepreciationMonths", model.DefaultDepreciationMonths);
        p.Add("@Description", model.Description?.Trim());
        p.Add("@IsActive", model.IsActive);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_AssetCategory_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteAssetCategoryAsync(int companyId, int categoryId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_AssetCategory_Delete", p);
    }

    public async Task<(List<AssetItem> Assets, AssetKpiSummaryViewModel Kpi)> GetAssetsAsync(
        int companyId, int? categoryId = null, int? branchId = null, string? status = null, int? employeeId = null, string? search = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@BranchId", branchId);
        p.Add("@Status", status);
        p.Add("@EmployeeId", employeeId);
        p.Add("@Search", search?.Trim());

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Asset_GetAll", p, commandType: CommandType.StoredProcedure);
        var assets = (await multi.ReadAsync<AssetItem>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<AssetKpiSummaryViewModel>() ?? new AssetKpiSummaryViewModel();
        return (assets, kpi);
    }

    public async Task<AssetDetailsPageViewModel?> GetAssetByIdAsync(int companyId, int assetId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", assetId);

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_Asset_GetById", p, commandType: CommandType.StoredProcedure);
        var asset = await multi.ReadFirstOrDefaultAsync<AssetItem>();
        if (asset is null) return null;

        var allocations = (await multi.ReadAsync<AssetAllocation>()).ToList();
        var logs = (await multi.ReadAsync<AssetMaintenanceLog>()).ToList();
        return new AssetDetailsPageViewModel
        {
            Asset = asset,
            Allocations = allocations,
            MaintenanceLogs = logs
        };
    }

    public async Task<int> SaveAssetAsync(int companyId, AssetFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@AssetId", model.AssetId);
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", model.CategoryId);
        p.Add("@BranchId", model.BranchId);
        p.Add("@AssetTag", model.AssetTag?.Trim().ToUpperInvariant());
        p.Add("@AssetName", model.AssetName.Trim());
        p.Add("@Brand", model.Brand?.Trim());
        p.Add("@ModelNumber", model.ModelNumber?.Trim());
        p.Add("@SerialNumber", model.SerialNumber?.Trim());
        p.Add("@PurchaseDate", model.PurchaseDate?.Date);
        p.Add("@PurchaseCost", model.PurchaseCost);
        p.Add("@WarrantyExpiryDate", model.WarrantyExpiryDate?.Date);
        p.Add("@VendorName", model.VendorName?.Trim());
        p.Add("@ConditionStatus", model.ConditionStatus);
        p.Add("@AssetStatus", model.AssetStatus);
        p.Add("@Notes", model.Notes?.Trim());
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_Asset_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteAssetAsync(int companyId, int assetId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", assetId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_Asset_Delete", p);
    }

    public Task<IEnumerable<AssetAllocation>> GetAllocationsAsync(int companyId, int? assetId = null, int? employeeId = null, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", assetId);
        p.Add("@EmployeeId", employeeId);
        p.Add("@Status", status);
        return QuerySpAsync<AssetAllocation>("dbo.sp_AssetAllocation_GetAll", p);
    }

    public async Task<int> AssignAssetAsync(int companyId, AssetAssignFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", model.AssetId);
        p.Add("@EmployeeId", model.EmployeeId);
        p.Add("@AssignedDate", model.AssignedDate.Date);
        p.Add("@ExpectedReturnDate", model.ExpectedReturnDate?.Date);
        p.Add("@ConditionAtIssue", model.ConditionAtIssue);
        p.Add("@IssueRemarks", model.IssueRemarks?.Trim());
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_AssetAllocation_Assign", p);
        return p.Get<int>("@SavedId");
    }

    public Task ReturnAssetAsync(int companyId, AssetReturnFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AllocationId", model.AllocationId);
        p.Add("@ReturnedDate", model.ReturnedDate.Date);
        p.Add("@ConditionAtReturn", model.ConditionAtReturn);
        p.Add("@AllocationStatus", model.AllocationStatus);
        p.Add("@ReturnRemarks", model.ReturnRemarks?.Trim());
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_AssetAllocation_Return", p);
    }

    public Task AcknowledgeAllocationAsync(int companyId, int allocationId, int employeeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AllocationId", allocationId);
        p.Add("@EmployeeId", employeeId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_AssetAllocation_Acknowledge", p);
    }

    public Task<IEnumerable<AssetMaintenanceLog>> GetMaintenanceLogsAsync(int companyId, int? assetId = null, string? status = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", assetId);
        p.Add("@Status", status);
        return QuerySpAsync<AssetMaintenanceLog>("dbo.sp_AssetMaintenance_GetAll", p);
    }

    public async Task<int> SaveMaintenanceLogAsync(int companyId, AssetMaintenanceFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@MaintenanceId", model.MaintenanceId);
        p.Add("@CompanyId", companyId);
        p.Add("@AssetId", model.AssetId);
        p.Add("@MaintenanceType", model.MaintenanceType);
        p.Add("@ServiceDate", model.ServiceDate.Date);
        p.Add("@CompletionDate", model.CompletionDate?.Date);
        p.Add("@VendorOrTechnician", model.VendorOrTechnician?.Trim());
        p.Add("@Cost", model.Cost);
        p.Add("@MaintenanceStatus", model.MaintenanceStatus);
        p.Add("@Description", model.Description.Trim());
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_AssetMaintenance_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteMaintenanceLogAsync(int companyId, int maintenanceId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@MaintenanceId", maintenanceId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_AssetMaintenance_Delete", p);
    }

    public Task<IEnumerable<DocumentCategory>> GetDocumentCategoriesAsync(int companyId, bool includeInactive = true)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@IncludeInactive", includeInactive);
        return QuerySpAsync<DocumentCategory>("dbo.sp_DocumentCategory_GetAll", p);
    }

    public async Task<int> SaveDocumentCategoryAsync(int companyId, DocumentCategoryFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CategoryId", model.CategoryId);
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryCode", model.CategoryCode.Trim().ToUpperInvariant());
        p.Add("@CategoryName", model.CategoryName.Trim());
        p.Add("@ScopeType", model.ScopeType.Trim());
        p.Add("@RequiresAcknowledgment", model.RequiresAcknowledgment);
        p.Add("@DefaultVisibility", model.DefaultVisibility);
        p.Add("@RetentionMonths", model.RetentionMonths);
        p.Add("@Description", model.Description?.Trim());
        p.Add("@IsActive", model.IsActive);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_DocumentCategory_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteDocumentCategoryAsync(int companyId, int categoryId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_DocumentCategory_Delete", p);
    }

    public async Task<(List<CompanyDocument> Documents, DocumentKpiSummaryViewModel Kpi)> GetCompanyDocumentsAsync(
        int companyId, int? categoryId = null, string? status = null, string? visibility = null,
        int? employeeId = null, bool canManage = false, bool isManager = false, string? search = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", categoryId);
        p.Add("@Status", status);
        p.Add("@Visibility", visibility);
        p.Add("@EmployeeId", employeeId);
        p.Add("@CanManage", canManage);
        p.Add("@IsManager", isManager);
        p.Add("@Search", search?.Trim());

        using var conn = ConnectionFactory.CreateConnection();
        using var multi = await conn.QueryMultipleAsync("dbo.sp_CompanyDocument_GetAll", p, commandType: CommandType.StoredProcedure);
        var docs = (await multi.ReadAsync<CompanyDocument>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<DocumentKpiSummaryViewModel>() ?? new DocumentKpiSummaryViewModel();
        return (docs, kpi);
    }

    public Task<CompanyDocument?> GetCompanyDocumentByIdAsync(int companyId, int documentId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DocumentId", documentId);
        return QueryFirstOrDefaultSpAsync<CompanyDocument>("dbo.sp_CompanyDocument_GetById", p);
    }

    public async Task<int> SaveCompanyDocumentAsync(
        int companyId, CompanyDocumentFormViewModel model, string? originalFileName,
        string? storedFileName, string? contentType, long? fileSizeBytes, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@DocumentId", model.DocumentId);
        p.Add("@CompanyId", companyId);
        p.Add("@CategoryId", model.CategoryId);
        p.Add("@DepartmentId", model.Visibility == "DepartmentSpecific" ? model.DepartmentId : null);
        p.Add("@DocumentCode", model.DocumentCode?.Trim().ToUpperInvariant());
        p.Add("@Title", model.Title.Trim());
        p.Add("@VersionNumber", model.VersionNumber.Trim());
        p.Add("@Visibility", model.Visibility);
        p.Add("@RequiresAcknowledgment", model.RequiresAcknowledgment);
        p.Add("@EffectiveDate", model.EffectiveDate.Date);
        p.Add("@ExpiryDate", model.ExpiryDate?.Date);
        p.Add("@OriginalFileName", originalFileName);
        p.Add("@StoredFileName", storedFileName);
        p.Add("@ContentType", contentType);
        p.Add("@FileSizeBytes", fileSizeBytes);
        p.Add("@Description", model.Description?.Trim());
        p.Add("@DocumentStatus", model.DocumentStatus);
        p.Add("@UserId", userId);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await ExecuteSpAsync("dbo.sp_CompanyDocument_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteCompanyDocumentAsync(int companyId, int documentId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DocumentId", documentId);
        p.Add("@UserId", userId);
        return ExecuteSpAsync("dbo.sp_CompanyDocument_Delete", p);
    }

    public Task AcknowledgeCompanyDocumentAsync(int companyId, int documentId, int employeeId, string? ipAddress, string? remarks)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DocumentId", documentId);
        p.Add("@EmployeeId", employeeId);
        p.Add("@IpAddress", ipAddress);
        p.Add("@Remarks", remarks?.Trim());
        return ExecuteSpAsync("dbo.sp_CompanyDocument_Acknowledge", p);
    }

    public Task<IEnumerable<DocumentAcknowledgment>> GetDocumentAcknowledgmentsAsync(int companyId, int? documentId = null, int? employeeId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@DocumentId", documentId);
        p.Add("@EmployeeId", employeeId);
        return QuerySpAsync<DocumentAcknowledgment>("dbo.sp_CompanyDocument_GetAcknowledgments", p);
    }

    public Task<IEnumerable<EmployeeVaultDocument>> GetEmployeeVaultDocumentsAsync(
        int companyId, int? employeeId = null, string? documentType = null, string? expiryFilter = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId);
        p.Add("@EmployeeId", employeeId);
        p.Add("@DocumentType", documentType);
        p.Add("@ExpiryFilter", expiryFilter);
        return QuerySpAsync<EmployeeVaultDocument>("dbo.sp_EmployeeVault_GetAll", p);
    }
}
