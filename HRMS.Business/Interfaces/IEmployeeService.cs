using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Business.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDirectoryFilterViewModel> GetFilteredDirectoryAsync(int companyId, int? branchId = null, int? departmentId = null, byte? status = null, string? searchTerm = null);
    Task<EmployeeProfileViewModel?> GetProfileByIdAsync(int companyId, int employeeId);
    Task<EmployeeFormViewModel> PrepareFormAsync(int companyId, int? employeeId = null, EmployeeFormViewModel? posted = null);
    Task<OperationResult<int>> CreateAsync(EmployeeFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateAsync(EmployeeFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAsync(int companyId, int employeeId, int userId, string username);
    Task<OperationResult> RecordCareerActionAsync(int companyId, EmployeeCareerActionViewModel model, int userId, string username);
    Task<EmployeeImportResultViewModel> ImportFromCsvStreamAsync(int companyId, Stream csvStream, int userId, string username);

    Task<OperationResult> AddEmergencyContactAsync(EmployeeEmergencyContact model, int userId);
    Task<OperationResult> DeleteEmergencyContactAsync(int companyId, int id);

    Task<OperationResult> AddFamilyMemberAsync(EmployeeFamilyMember model, int userId);
    Task<OperationResult> DeleteFamilyMemberAsync(int companyId, int id);

    Task<OperationResult> AddEducationAsync(EmployeeEducation model, int userId);
    Task<OperationResult> DeleteEducationAsync(int companyId, int id);

    Task<OperationResult> AddExperienceAsync(EmployeeExperience model, int userId);
    Task<OperationResult> DeleteExperienceAsync(int companyId, int id);

    Task<OperationResult> AddSkillAsync(EmployeeSkill model, int userId);
    Task<OperationResult> DeleteSkillAsync(int companyId, int id);

    Task<OperationResult> AddDocumentAsync(EmployeeDocument model, int userId);
    Task<EmployeeDocument?> GetDocumentByIdAsync(int companyId, int documentId);
    Task<OperationResult> DeleteDocumentAsync(int companyId, int documentId);
}
