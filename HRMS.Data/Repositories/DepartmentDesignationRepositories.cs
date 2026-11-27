using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Data.Repositories;

public sealed class DepartmentRepository : BaseRepository, IDepartmentRepository
{
    public DepartmentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<DepartmentListItemViewModel>> GetAllAsync(int companyId, int? branchId = null, string? searchTerm = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@BranchId", branchId, DbType.Int32);
        parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 100);
        return QuerySpAsync<DepartmentListItemViewModel>("sp_Department_GetAll", parameters);
    }

    public Task<DepartmentFormViewModel?> GetByIdAsync(int companyId, int departmentId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@DepartmentId", departmentId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<DepartmentFormViewModel>("sp_Department_GetById", parameters);
    }

    public async Task<int> InsertAsync(DepartmentFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@BranchId", model.BranchId, DbType.Int32);
        parameters.Add("@ParentDepartmentId", model.ParentDepartmentId, DbType.Int32);
        parameters.Add("@DepartmentCode", model.DepartmentCode, DbType.String, size: 20);
        parameters.Add("@DepartmentName", model.DepartmentName, DbType.String, size: 120);
        parameters.Add("@CostCenterCode", model.CostCenterCode, DbType.String, size: 30);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@CreatedBy", userId, DbType.Int32);
        parameters.Add("@NewDepartmentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Department_Insert", parameters);
        return parameters.Get<int>("@NewDepartmentId");
    }

    public Task UpdateAsync(DepartmentFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@DepartmentId", model.DepartmentId, DbType.Int32);
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@BranchId", model.BranchId, DbType.Int32);
        parameters.Add("@ParentDepartmentId", model.ParentDepartmentId, DbType.Int32);
        parameters.Add("@DepartmentCode", model.DepartmentCode, DbType.String, size: 20);
        parameters.Add("@DepartmentName", model.DepartmentName, DbType.String, size: 120);
        parameters.Add("@CostCenterCode", model.CostCenterCode, DbType.String, size: 30);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Department_Update", parameters);
    }

    public Task DeleteAsync(int companyId, int departmentId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@DepartmentId", departmentId, DbType.Int32);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Department_Delete", parameters);
    }
}

public sealed class DesignationRepository : BaseRepository, IDesignationRepository
{
    public DesignationRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<DesignationListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 100);
        return QuerySpAsync<DesignationListItemViewModel>("sp_Designation_GetAll", parameters);
    }

    public Task<DesignationFormViewModel?> GetByIdAsync(int companyId, int designationId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@DesignationId", designationId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<DesignationFormViewModel>("sp_Designation_GetById", parameters);
    }

    public async Task<int> InsertAsync(DesignationFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@DesignationCode", model.DesignationCode, DbType.String, size: 20);
        parameters.Add("@DesignationTitle", model.DesignationTitle, DbType.String, size: 120);
        parameters.Add("@JobGrade", model.JobGrade, DbType.String, size: 20);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@CreatedBy", userId, DbType.Int32);
        parameters.Add("@NewDesignationId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Designation_Insert", parameters);
        return parameters.Get<int>("@NewDesignationId");
    }

    public Task UpdateAsync(DesignationFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@DesignationId", model.DesignationId, DbType.Int32);
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@DesignationCode", model.DesignationCode, DbType.String, size: 20);
        parameters.Add("@DesignationTitle", model.DesignationTitle, DbType.String, size: 120);
        parameters.Add("@JobGrade", model.JobGrade, DbType.String, size: 20);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Designation_Update", parameters);
    }

    public Task DeleteAsync(int companyId, int designationId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@DesignationId", designationId, DbType.Int32);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Designation_Delete", parameters);
    }
}
