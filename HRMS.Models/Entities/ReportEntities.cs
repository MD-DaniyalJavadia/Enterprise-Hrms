namespace HRMS.Models.Entities;

public class SavedReportTemplate
{
    public int TemplateId { get; set; }
    public int CompanyId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string ReportKey { get; set; } = "Workforce";
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int DateRangeDays { get; set; } = 30;
    public string? StatusFilter { get; set; }
    public string? Notes { get; set; }
    public bool IsShared { get; set; } = true;
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ReportExportLog
{
    public long ExportLogId { get; set; }
    public int CompanyId { get; set; }
    public string ReportKey { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ExportFormat { get; set; } = "CSV";
    public string? FilterSummary { get; set; }
    public int RecordCount { get; set; }
    public int ExportedBy { get; set; }
    public string ExportedByName { get; set; } = string.Empty;
    public DateTime ExportedDate { get; set; }
}
