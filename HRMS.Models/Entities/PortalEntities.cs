namespace HRMS.Models.Entities;

public class ProfileUpdateRequest
{
    public int RequestId { get; set; }
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RequestType { get; set; } = "ContactInfo"; // ContactInfo, Address, BankDetails, EmergencyContact
    public string FieldSummary { get; set; } = string.Empty;
    public string? RequestedPhone { get; set; }
    public string? RequestedMobilePhone { get; set; }
    public string? RequestedPersonalEmail { get; set; }
    public string? RequestedAddress { get; set; }
    public string? RequestedCity { get; set; }
    public string? RequestedCountry { get; set; }
    public string? RequestedBankName { get; set; }
    public string? RequestedBankAccountTitle { get; set; }
    public string? RequestedBankAccountNumber { get; set; }
    public string? RequestedBankRoutingCode { get; set; }
    public string? RequestedEmergencyContactName { get; set; }
    public string? RequestedEmergencyRelationship { get; set; }
    public string? RequestedEmergencyPhone { get; set; }
    public string? EmployeeNotes { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedDate { get; set; }
}
