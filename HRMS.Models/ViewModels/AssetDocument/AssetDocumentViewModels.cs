using System.ComponentModel.DataAnnotations;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Models.ViewModels.AssetDocument;

// --- Asset ViewModels ---
public class AssetKpiSummaryViewModel
{
    public int TotalAssets { get; set; }
    public int AvailableAssets { get; set; }
    public int AssignedAssets { get; set; }
    public int MaintenanceAssets { get; set; }
    public int RetiredOrLostAssets { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int ExpiringWarrantyCount { get; set; }
}

public class AssetIndexPageViewModel
{
    public int? CategoryId { get; set; }
    public int? BranchId { get; set; }
    public string? Status { get; set; }
    public int? EmployeeId { get; set; }
    public string? Search { get; set; }
    public bool CanManage { get; set; }
    public AssetKpiSummaryViewModel Kpi { get; set; } = new();
    public List<AssetItem> Assets { get; set; } = new();
    public List<AssetCategory> Categories { get; set; } = new();
    public List<BranchListItemViewModel> Branches { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class AssetDetailsPageViewModel
{
    public AssetItem Asset { get; set; } = new();
    public List<AssetAllocation> Allocations { get; set; } = new();
    public List<AssetMaintenanceLog> MaintenanceLogs { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    public bool CanManage { get; set; }
}

public class AssetAllocationsPageViewModel
{
    public int? AssetId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Status { get; set; }
    public bool CanManage { get; set; }
    public List<AssetAllocation> Allocations { get; set; } = new();
    public List<AssetItem> AvailableAssets { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class AssetMaintenancePageViewModel
{
    public int? AssetId { get; set; }
    public string? Status { get; set; }
    public List<AssetMaintenanceLog> Logs { get; set; } = new();
    public List<AssetItem> Assets { get; set; } = new();
}

public class AssetCategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required, StringLength(20), Display(Name = "Category Code")]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, StringLength(100), Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required, StringLength(30), Display(Name = "Asset Class")]
    public string AssetClass { get; set; } = "IT";

    public bool RequiresSerialNumber { get; set; } = true;

    [Range(1, 360)]
    public int? DefaultDepreciationMonths { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AssetFormViewModel
{
    public int AssetId { get; set; }

    [Required, Range(1, int.MaxValue), Display(Name = "Category")]
    public int CategoryId { get; set; }

    public int? BranchId { get; set; }

    [StringLength(40), Display(Name = "Asset Tag")]
    public string? AssetTag { get; set; }

    [Required, StringLength(150), Display(Name = "Asset Name")]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Brand { get; set; }

    [StringLength(80)]
    public string? ModelNumber { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [Range(0, 10000000)]
    public decimal PurchaseCost { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    [StringLength(120)]
    public string? VendorName { get; set; }

    [Required, StringLength(20)]
    public string ConditionStatus { get; set; } = "Good";

    [Required, StringLength(20)]
    public string AssetStatus { get; set; } = "Available";

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class AssetAssignFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int AssetId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime AssignedDate { get; set; } = DateTime.Today;

    public DateTime? ExpectedReturnDate { get; set; }

    [Required, StringLength(20)]
    public string ConditionAtIssue { get; set; } = "Good";

    [StringLength(400)]
    public string? IssueRemarks { get; set; }
}

public class AssetReturnFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int AllocationId { get; set; }

    [Required]
    public DateTime ReturnedDate { get; set; } = DateTime.Today;

    [Required, StringLength(20)]
    public string ConditionAtReturn { get; set; } = "Good";

    [Required, StringLength(20)]
    public string AllocationStatus { get; set; } = "Returned";

    [StringLength(400)]
    public string? ReturnRemarks { get; set; }
}

public class AssetMaintenanceFormViewModel
{
    public int MaintenanceId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int AssetId { get; set; }

    [Required, StringLength(30)]
    public string MaintenanceType { get; set; } = "Repair";

    [Required]
    public DateTime ServiceDate { get; set; } = DateTime.Today;

    public DateTime? CompletionDate { get; set; }

    [StringLength(120)]
    public string? VendorOrTechnician { get; set; }

    [Range(0, 10000000)]
    public decimal Cost { get; set; }

    [Required, StringLength(20)]
    public string MaintenanceStatus { get; set; } = "Scheduled";

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

// --- Document ViewModels ---
public class DocumentKpiSummaryViewModel
{
    public int TotalDocuments { get; set; }
    public int PublishedCount { get; set; }
    public int MandatoryCount { get; set; }
    public int PendingMyAckCount { get; set; }
    public int ExpiringSoonCount { get; set; }
}

public class DocumentIndexPageViewModel
{
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public string? Search { get; set; }
    public bool CanManage { get; set; }
    public bool HasLinkedEmployee { get; set; }
    public DocumentKpiSummaryViewModel Kpi { get; set; } = new();
    public List<CompanyDocument> Documents { get; set; } = new();
    public List<DocumentCategory> Categories { get; set; } = new();
    public List<DepartmentListItemViewModel> Departments { get; set; } = new();
}

public class DocumentAcknowledgmentsPageViewModel
{
    public int? DocumentId { get; set; }
    public int? EmployeeId { get; set; }
    public bool CanManage { get; set; }
    public List<DocumentAcknowledgment> Acknowledgments { get; set; } = new();
    public List<CompanyDocument> Documents { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class EmployeeVaultPageViewModel
{
    public int? EmployeeId { get; set; }
    public string? DocumentType { get; set; }
    public string? ExpiryFilter { get; set; }
    public bool CanManage { get; set; }
    public int ExpiredCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ValidCount { get; set; }
    public List<EmployeeVaultDocument> Documents { get; set; } = new();
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
}

public class DocumentCategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required, StringLength(20)]
    public string CategoryCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string ScopeType { get; set; } = "CompanyPolicy";

    public bool RequiresAcknowledgment { get; set; }

    [Required, StringLength(30)]
    public string DefaultVisibility { get; set; } = "AllEmployees";

    [Range(1, 600)]
    public int? RetentionMonths { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CompanyDocumentFormViewModel
{
    public int DocumentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public int? DepartmentId { get; set; }

    [StringLength(30)]
    public string? DocumentCode { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string VersionNumber { get; set; } = "1.0";

    [Required, StringLength(30)]
    public string Visibility { get; set; } = "AllEmployees";

    public bool RequiresAcknowledgment { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; } = DateTime.Today;

    public DateTime? ExpiryDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(20)]
    public string DocumentStatus { get; set; } = "Published";
}

public class DocumentAcknowledgeFormViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int DocumentId { get; set; }

    [StringLength(250)]
    public string? Remarks { get; set; }
}

public class EmployeeVaultUploadViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required, StringLength(60)]
    public string DocumentType { get; set; } = "Contract";

    [Required, StringLength(150)]
    public string DocumentTitle { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }
}
