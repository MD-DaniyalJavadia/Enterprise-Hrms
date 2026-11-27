namespace HRMS.Models.RequestModels;

public class DashboardFilterRequest
{
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public DateTime? AsOfDate { get; set; }
}
