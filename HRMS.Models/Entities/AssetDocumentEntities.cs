namespace HRMS.Models.Entities;

public class AssetCategory
{
    public int CategoryId { get; set; }
    public int CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AssetClass { get; set; } = "IT";
    public bool RequiresSerialNumber { get; set; } = true;
    public int? DefaultDepreciationMonths { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int AssetCount { get; set; }
}

public class AssetItem
{
    public int AssetId { get; set; }
    public int CompanyId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string? VendorName { get; set; }
    public string ConditionStatus { get; set; } = "Good";
    public string AssetStatus { get; set; } = "Available";
    public int? CurrentEmployeeId { get; set; }
    public string? CurrentEmployeeCode { get; set; }
    public string? CurrentEmployeeName { get; set; }
    public string? CurrentDepartmentName { get; set; }
    public int? ActiveAllocationId { get; set; }
    public DateTime? ActiveAssignedDate { get; set; }
    public bool ActiveAllocationAcknowledged { get; set; }
    public string? Notes { get; set; }
}

public class AssetAllocation
{
    public int AllocationId { get; set; }
    public int CompanyId { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public string ConditionAtIssue { get; set; } = "Good";
    public string? ConditionAtReturn { get; set; }
    public string AllocationStatus { get; set; } = "Active";
    public string? IssueRemarks { get; set; }
    public string? ReturnRemarks { get; set; }
    public bool EmployeeAcknowledged { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
}

public class AssetMaintenanceLog
{
    public int MaintenanceId { get; set; }
    public int CompanyId { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = "Preventive";
    public DateTime ServiceDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? VendorOrTechnician { get; set; }
    public decimal Cost { get; set; }
    public string MaintenanceStatus { get; set; } = "Scheduled";
    public string Description { get; set; } = string.Empty;
}

public class DocumentCategory
{
    public int CategoryId { get; set; }
    public int CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "CompanyPolicy";
    public bool RequiresAcknowledgment { get; set; }
    public string DefaultVisibility { get; set; } = "AllEmployees";
    public int? RetentionMonths { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DocumentCount { get; set; }
}

public class CompanyDocument
{
    public int DocumentId { get; set; }
    public int CompanyId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = "1.0";
    public string Visibility { get; set; } = "AllEmployees";
    public bool RequiresAcknowledgment { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public string DocumentStatus { get; set; } = "Published";
    public DateTime CreatedDate { get; set; }
    public bool IsAcknowledgedByMe { get; set; }
    public DateTime? MyAcknowledgedDate { get; set; }
    public int AcknowledgedCount { get; set; }
    public int TargetAudienceCount { get; set; }
}

public class DocumentAcknowledgment
{
    public int AcknowledgmentId { get; set; }
    public int CompanyId { get; set; }
    public int DocumentId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string AcknowledgedVersion { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AcknowledgedDate { get; set; }
    public string? IpAddress { get; set; }
    public string? Remarks { get; set; }
}

public class EmployeeVaultDocument
{
    public int DocumentId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ExpiryStatus { get; set; } = "NoExpiry";
    public int? DaysUntilExpiry { get; set; }
}
