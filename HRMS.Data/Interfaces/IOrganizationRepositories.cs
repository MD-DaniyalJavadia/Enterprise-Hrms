using HRMS.Models.ViewModels.Organization;

namespace HRMS.Data.Interfaces;

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyListItemViewModel>> GetAllAsync(string? searchTerm = null, bool? isActive = null);
    Task<CompanyFormViewModel?> GetByIdAsync(int companyId);
    Task<int> InsertAsync(CompanyFormViewModel model, int userId);
    Task UpdateAsync(CompanyFormViewModel model, int userId);
    Task DeleteAsync(int companyId, int userId);
}

public interface IBranchRepository
{
    Task<IEnumerable<BranchListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null, bool? isActive = null);
    Task<BranchFormViewModel?> GetByIdAsync(int companyId, int branchId);
    Task<int> InsertAsync(BranchFormViewModel model, int userId);
    Task UpdateAsync(BranchFormViewModel model, int userId);
    Task DeleteAsync(int companyId, int branchId, int userId);
}

public interface IDepartmentRepository
{
    Task<IEnumerable<DepartmentListItemViewModel>> GetAllAsync(int companyId, int? branchId = null, string? searchTerm = null);
    Task<DepartmentFormViewModel?> GetByIdAsync(int companyId, int departmentId);
    Task<int> InsertAsync(DepartmentFormViewModel model, int userId);
    Task UpdateAsync(DepartmentFormViewModel model, int userId);
    Task DeleteAsync(int companyId, int departmentId, int userId);
}

public interface IDesignationRepository
{
    Task<IEnumerable<DesignationListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null);
    Task<DesignationFormViewModel?> GetByIdAsync(int companyId, int designationId);
    Task<int> InsertAsync(DesignationFormViewModel model, int userId);
    Task UpdateAsync(DesignationFormViewModel model, int userId);
    Task DeleteAsync(int companyId, int designationId, int userId);
}

public interface IOrgStructureRepository
{
    Task<IEnumerable<JobGradeViewModel>> GetJobGradesAsync(int companyId);
    Task<int> SaveJobGradeAsync(JobGradeViewModel model, int userId);
    Task DeleteJobGradeAsync(int companyId, int jobGradeId, int userId);

    Task<IEnumerable<JobPositionViewModel>> GetJobPositionsAsync(int companyId);
    Task<int> SaveJobPositionAsync(JobPositionViewModel model, int userId);
    Task DeleteJobPositionAsync(int companyId, int jobPositionId, int userId);

    Task<IEnumerable<CostCenterViewModel>> GetCostCentersAsync(int companyId);
    Task<int> SaveCostCenterAsync(CostCenterViewModel model, int userId);
    Task DeleteCostCenterAsync(int companyId, int costCenterId, int userId);

    Task<IEnumerable<WorkLocationViewModel>> GetWorkLocationsAsync(int companyId);
    Task<int> SaveWorkLocationAsync(WorkLocationViewModel model, int userId);
    Task DeleteWorkLocationAsync(int companyId, int workLocationId, int userId);

    Task<IEnumerable<ShiftViewModel>> GetShiftsAsync(int companyId);
    Task<int> SaveShiftAsync(ShiftViewModel model, int userId);
    Task DeleteShiftAsync(int companyId, int shiftId, int userId);

    Task<(List<OrgChartDepartmentNodeViewModel> Departments, List<OrgChartEmployeeNodeViewModel> Employees)> GetOrgChartDataAsync(int companyId);
}
