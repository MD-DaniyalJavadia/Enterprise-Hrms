using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Data.Repositories;

public sealed class OrgStructureRepository : BaseRepository, IOrgStructureRepository
{
    public OrgStructureRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<JobGradeViewModel>> GetJobGradesAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<JobGradeViewModel>("sp_JobGrade_GetAll", p);
    }

    public async Task<int> SaveJobGradeAsync(JobGradeViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@JobGradeId", model.JobGradeId, DbType.Int32);
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@GradeCode", model.GradeCode, DbType.String, size: 20);
        p.Add("@GradeName", model.GradeName, DbType.String, size: 100);
        p.Add("@LevelOrder", model.LevelOrder, DbType.Int32);
        p.Add("@MinSalary", model.MinSalary, DbType.Decimal);
        p.Add("@MaxSalary", model.MaxSalary, DbType.Decimal);
        p.Add("@Description", model.Description, DbType.String, size: 300);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobGrade_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteJobGradeAsync(int companyId, int jobGradeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@JobGradeId", jobGradeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobGrade_Delete", p);
    }

    public Task<IEnumerable<JobPositionViewModel>> GetJobPositionsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<JobPositionViewModel>("sp_JobPosition_GetAll", p);
    }

    public async Task<int> SaveJobPositionAsync(JobPositionViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@JobPositionId", model.JobPositionId, DbType.Int32);
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@DepartmentId", model.DepartmentId, DbType.Int32);
        p.Add("@DesignationId", model.DesignationId, DbType.Int32);
        p.Add("@JobGradeId", model.JobGradeId, DbType.Int32);
        p.Add("@ReportsToPositionId", model.ReportsToPositionId, DbType.Int32);
        p.Add("@PositionCode", model.PositionCode, DbType.String, size: 25);
        p.Add("@PositionTitle", model.PositionTitle, DbType.String, size: 120);
        p.Add("@HeadcountBudget", model.HeadcountBudget, DbType.Int32);
        p.Add("@Description", model.Description, DbType.String, size: 400);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_JobPosition_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteJobPositionAsync(int companyId, int jobPositionId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@JobPositionId", jobPositionId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_JobPosition_Delete", p);
    }

    public Task<IEnumerable<CostCenterViewModel>> GetCostCentersAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<CostCenterViewModel>("sp_CostCenter_GetAll", p);
    }

    public async Task<int> SaveCostCenterAsync(CostCenterViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CostCenterId", model.CostCenterId, DbType.Int32);
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@BranchId", model.BranchId, DbType.Int32);
        p.Add("@CostCenterCode", model.CostCenterCode, DbType.String, size: 30);
        p.Add("@CostCenterName", model.CostCenterName, DbType.String, size: 120);
        p.Add("@Description", model.Description, DbType.String, size: 300);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_CostCenter_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteCostCenterAsync(int companyId, int costCenterId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@CostCenterId", costCenterId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_CostCenter_Delete", p);
    }

    public Task<IEnumerable<WorkLocationViewModel>> GetWorkLocationsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<WorkLocationViewModel>("sp_WorkLocation_GetAll", p);
    }

    public async Task<int> SaveWorkLocationAsync(WorkLocationViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@WorkLocationId", model.WorkLocationId, DbType.Int32);
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@BranchId", model.BranchId, DbType.Int32);
        p.Add("@LocationCode", model.LocationCode, DbType.String, size: 20);
        p.Add("@LocationName", model.LocationName, DbType.String, size: 120);
        p.Add("@AddressLine1", model.AddressLine1, DbType.String, size: 250);
        p.Add("@City", model.City, DbType.String, size: 80);
        p.Add("@Country", model.Country, DbType.String, size: 80);
        p.Add("@TimeZoneId", model.TimeZoneId, DbType.String, size: 60);
        p.Add("@IsRemote", model.IsRemote, DbType.Boolean);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_WorkLocation_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteWorkLocationAsync(int companyId, int workLocationId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@WorkLocationId", workLocationId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_WorkLocation_Delete", p);
    }

    public Task<IEnumerable<ShiftViewModel>> GetShiftsAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<ShiftViewModel>("sp_Shift_GetAll", p);
    }

    public async Task<int> SaveShiftAsync(ShiftViewModel model, int userId)
    {
        var startTime = TimeSpan.Parse(model.StartTimeText);
        var endTime = TimeSpan.Parse(model.EndTimeText);

        var p = new DynamicParameters();
        p.Add("@ShiftId", model.ShiftId, DbType.Int32);
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@ShiftCode", model.ShiftCode, DbType.String, size: 20);
        p.Add("@ShiftName", model.ShiftName, DbType.String, size: 100);
        p.Add("@StartTime", startTime, DbType.Time);
        p.Add("@EndTime", endTime, DbType.Time);
        p.Add("@GracePeriodMinutes", model.GracePeriodMinutes, DbType.Int32);
        p.Add("@BreakMinutes", model.BreakMinutes, DbType.Int32);
        p.Add("@WorkingHours", model.WorkingHours, DbType.Decimal);
        p.Add("@IsNightShift", model.IsNightShift, DbType.Boolean);
        p.Add("@WeeklyOffDays", model.WeeklyOffDays, DbType.String, size: 60);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Shift_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteShiftAsync(int companyId, int shiftId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ShiftId", shiftId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Shift_Delete", p);
    }

    public async Task<(List<OrgChartDepartmentNodeViewModel> Departments, List<OrgChartEmployeeNodeViewModel> Employees)> GetOrgChartDataAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("sp_Organization_GetOrgChart", p, commandType: CommandType.StoredProcedure);

        var departments = (await multi.ReadAsync<OrgChartDepartmentNodeViewModel>()).ToList();
        var employees = (await multi.ReadAsync<OrgChartEmployeeNodeViewModel>()).ToList();

        return (departments, employees);
    }
}
