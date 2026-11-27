using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Data.Interfaces;

public interface IEmployeeRepository
{
    Task<string> GenerateNextEmployeeCodeAsync(int companyId);
    Task<IEnumerable<EmployeeListItemViewModel>> GetAllAsync(int companyId, int? branchId = null, int? departmentId = null, byte? status = null, string? searchTerm = null);
    Task<EmployeeProfileViewModel?> GetProfileByIdAsync(int companyId, int employeeId);
    Task<EmployeeFormViewModel?> GetForEditAsync(int companyId, int employeeId);
    Task<int> InsertAsync(EmployeeFormViewModel model, int userId);
    Task UpdateAsync(EmployeeFormViewModel model, int userId);
    Task DeleteAsync(int companyId, int employeeId, int userId);
    Task RecordCareerActionAsync(int companyId, EmployeeCareerActionViewModel model, int userId);

    Task AddEmergencyContactAsync(EmployeeEmergencyContact model, int userId);
    Task DeleteEmergencyContactAsync(int companyId, int emergencyContactId);

    Task AddFamilyMemberAsync(EmployeeFamilyMember model, int userId);
    Task DeleteFamilyMemberAsync(int companyId, int familyMemberId);

    Task AddEducationAsync(EmployeeEducation model, int userId);
    Task DeleteEducationAsync(int companyId, int educationId);

    Task AddExperienceAsync(EmployeeExperience model, int userId);
    Task DeleteExperienceAsync(int companyId, int experienceId);

    Task AddSkillAsync(EmployeeSkill model, int userId);
    Task DeleteSkillAsync(int companyId, int skillId);

    Task AddDocumentAsync(EmployeeDocument model, int userId);
    Task<EmployeeDocument?> GetDocumentByIdAsync(int companyId, int documentId);
    Task DeleteDocumentAsync(int companyId, int documentId);
}
