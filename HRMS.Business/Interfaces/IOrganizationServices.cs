using HRMS.Models.DTOs;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Business.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<CompanyListItemViewModel>> GetAllAsync(string? searchTerm = null, bool? isActive = null);
    Task<CompanyFormViewModel?> GetByIdAsync(int companyId);
    Task<OperationResult<int>> CreateAsync(CompanyFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateAsync(CompanyFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAsync(int companyId, int userId, string username);
}

public interface IBranchService
{
    Task<IEnumerable<BranchListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null, bool? isActive = null);
    Task<BranchFormViewModel?> GetByIdAsync(int companyId, int branchId);
    Task<OperationResult<int>> CreateAsync(BranchFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateAsync(BranchFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAsync(int companyId, int branchId, int userId, string username);
}

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentListItemViewModel>> GetAllAsync(int companyId, int? branchId = null, string? searchTerm = null);
    Task<DepartmentFormViewModel?> GetByIdAsync(int companyId, int departmentId);
    Task<DepartmentFormViewModel> PrepareFormAsync(int companyId, DepartmentFormViewModel? existing = null);
    Task<OperationResult<int>> CreateAsync(DepartmentFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateAsync(DepartmentFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAsync(int companyId, int departmentId, int userId, string username);
}

public interface IDesignationService
{
    Task<IEnumerable<DesignationListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null);
    Task<DesignationFormViewModel?> GetByIdAsync(int companyId, int designationId);
    Task<DesignationFormViewModel> PrepareFormAsync(int companyId, DesignationFormViewModel? existing = null);
    Task<OperationResult<int>> CreateAsync(DesignationFormViewModel model, int userId, string username);
    Task<OperationResult> UpdateAsync(DesignationFormViewModel model, int userId, string username);
    Task<OperationResult> DeleteAsync(int companyId, int designationId, int userId, string username);
}

public interface IOrgStructureService
{
    Task<PositionsAndGradesPageViewModel> GetPositionsAndGradesPageAsync(int companyId);
    Task<LocationsAndCostCentersPageViewModel> GetLocationsAndCostCentersPageAsync(int companyId);
    Task<IEnumerable<ShiftViewModel>> GetShiftsAsync(int companyId);
    Task<OrgChartViewModel> GetOrgChartAsync(int companyId);

    Task<OperationResult<int>> SaveJobGradeAsync(JobGradeViewModel model, int userId, string username);
    Task<OperationResult> DeleteJobGradeAsync(int companyId, int jobGradeId, int userId, string username);

    Task<OperationResult<int>> SaveJobPositionAsync(JobPositionViewModel model, int userId, string username);
    Task<OperationResult> DeleteJobPositionAsync(int companyId, int jobPositionId, int userId, string username);

    Task<OperationResult<int>> SaveCostCenterAsync(CostCenterViewModel model, int userId, string username);
    Task<OperationResult> DeleteCostCenterAsync(int companyId, int costCenterId, int userId, string username);

    Task<OperationResult<int>> SaveWorkLocationAsync(WorkLocationViewModel model, int userId, string username);
    Task<OperationResult> DeleteWorkLocationAsync(int companyId, int workLocationId, int userId, string username);

    Task<OperationResult<int>> SaveShiftAsync(ShiftViewModel model, int userId, string username);
    Task<OperationResult> DeleteShiftAsync(int companyId, int shiftId, int userId, string username);
}
