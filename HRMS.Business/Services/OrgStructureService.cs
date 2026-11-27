using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Organization;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class OrgStructureService : IOrgStructureService
{
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IAuditLogService _auditLogService;

    public OrgStructureService(
        IOrgStructureRepository orgStructureRepository,
        IDepartmentRepository departmentRepository,
        IDesignationRepository designationRepository,
        IBranchRepository branchRepository,
        IAuditLogService auditLogService)
    {
        _orgStructureRepository = orgStructureRepository;
        _departmentRepository = departmentRepository;
        _designationRepository = designationRepository;
        _branchRepository = branchRepository;
        _auditLogService = auditLogService;
    }

    public async Task<PositionsAndGradesPageViewModel> GetPositionsAndGradesPageAsync(int companyId)
    {
        return new PositionsAndGradesPageViewModel
        {
            JobGrades = (await _orgStructureRepository.GetJobGradesAsync(companyId)).ToList(),
            JobPositions = (await _orgStructureRepository.GetJobPositionsAsync(companyId)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList(),
            Designations = (await _designationRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public async Task<LocationsAndCostCentersPageViewModel> GetLocationsAndCostCentersPageAsync(int companyId)
    {
        return new LocationsAndCostCentersPageViewModel
        {
            CostCenters = (await _orgStructureRepository.GetCostCentersAsync(companyId)).ToList(),
            WorkLocations = (await _orgStructureRepository.GetWorkLocationsAsync(companyId)).ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList()
        };
    }

    public Task<IEnumerable<ShiftViewModel>> GetShiftsAsync(int companyId)
        => _orgStructureRepository.GetShiftsAsync(companyId);

    public async Task<OrgChartViewModel> GetOrgChartAsync(int companyId)
    {
        var (departments, employees) = await _orgStructureRepository.GetOrgChartDataAsync(companyId);

        var deptLookup = departments.ToDictionary(d => d.DepartmentId);
        var rootDepts = new List<OrgChartDepartmentNodeViewModel>();
        foreach (var dept in departments)
        {
            if (dept.ParentDepartmentId.HasValue && deptLookup.TryGetValue(dept.ParentDepartmentId.Value, out var parent))
            {
                parent.Children.Add(dept);
            }
            else
            {
                rootDepts.Add(dept);
            }
        }

        var empLookup = employees.ToDictionary(e => e.EmployeeId);
        var rootEmps = new List<OrgChartEmployeeNodeViewModel>();
        foreach (var emp in employees)
        {
            if (emp.ReportingManagerId.HasValue && empLookup.TryGetValue(emp.ReportingManagerId.Value, out var manager))
            {
                manager.DirectReports.Add(emp);
            }
            else
            {
                rootEmps.Add(emp);
            }
        }

        return new OrgChartViewModel
        {
            DepartmentTree = rootDepts,
            ReportingTree = rootEmps
        };
    }

    public async Task<OperationResult<int>> SaveJobGradeAsync(JobGradeViewModel model, int userId, string username)
    {
        if (model.MaxSalary < model.MinSalary)
        {
            return OperationResult<int>.Failure("Maximum salary cannot be lower than minimum salary.");
        }

        try
        {
            model.GradeCode = model.GradeCode.Trim().ToUpperInvariant();
            var id = await _orgStructureRepository.SaveJobGradeAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username,
                model.JobGradeId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Organization", "JobGrade", id.ToString(), $"Saved job grade {model.GradeName} ({model.GradeCode}).");
            return OperationResult<int>.Success(id, "Job grade saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteJobGradeAsync(int companyId, int jobGradeId, int userId, string username)
    {
        try
        {
            await _orgStructureRepository.DeleteJobGradeAsync(companyId, jobGradeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "JobGrade", jobGradeId.ToString(), $"Deleted job grade ID {jobGradeId}.");
            return OperationResult.Success("Job grade deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveJobPositionAsync(JobPositionViewModel model, int userId, string username)
    {
        try
        {
            model.PositionCode = model.PositionCode.Trim().ToUpperInvariant();
            var id = await _orgStructureRepository.SaveJobPositionAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username,
                model.JobPositionId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Organization", "JobPosition", id.ToString(), $"Saved job position {model.PositionTitle} ({model.PositionCode}).");
            return OperationResult<int>.Success(id, "Job position saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteJobPositionAsync(int companyId, int jobPositionId, int userId, string username)
    {
        try
        {
            await _orgStructureRepository.DeleteJobPositionAsync(companyId, jobPositionId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "JobPosition", jobPositionId.ToString(), $"Deleted job position ID {jobPositionId}.");
            return OperationResult.Success("Job position deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveCostCenterAsync(CostCenterViewModel model, int userId, string username)
    {
        try
        {
            model.CostCenterCode = model.CostCenterCode.Trim().ToUpperInvariant();
            var id = await _orgStructureRepository.SaveCostCenterAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username,
                model.CostCenterId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Organization", "CostCenter", id.ToString(), $"Saved cost center {model.CostCenterName} ({model.CostCenterCode}).");
            return OperationResult<int>.Success(id, "Cost center saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteCostCenterAsync(int companyId, int costCenterId, int userId, string username)
    {
        try
        {
            await _orgStructureRepository.DeleteCostCenterAsync(companyId, costCenterId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "CostCenter", costCenterId.ToString(), $"Deleted cost center ID {costCenterId}.");
            return OperationResult.Success("Cost center deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveWorkLocationAsync(WorkLocationViewModel model, int userId, string username)
    {
        try
        {
            model.LocationCode = model.LocationCode.Trim().ToUpperInvariant();
            var id = await _orgStructureRepository.SaveWorkLocationAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username,
                model.WorkLocationId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Organization", "WorkLocation", id.ToString(), $"Saved work location {model.LocationName} ({model.LocationCode}).");
            return OperationResult<int>.Success(id, "Work location saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteWorkLocationAsync(int companyId, int workLocationId, int userId, string username)
    {
        try
        {
            await _orgStructureRepository.DeleteWorkLocationAsync(companyId, workLocationId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "WorkLocation", workLocationId.ToString(), $"Deleted work location ID {workLocationId}.");
            return OperationResult.Success("Work location deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<int>> SaveShiftAsync(ShiftViewModel model, int userId, string username)
    {
        if (!TimeSpan.TryParse(model.StartTimeText, out _) || !TimeSpan.TryParse(model.EndTimeText, out _))
        {
            return OperationResult<int>.Failure("Please enter valid HH:mm shift start and end times.");
        }

        try
        {
            model.ShiftCode = model.ShiftCode.Trim().ToUpperInvariant();
            var id = await _orgStructureRepository.SaveShiftAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username,
                model.ShiftId == 0 ? AuditActionType.Create.ToString() : AuditActionType.Update.ToString(),
                "Organization", "Shift", id.ToString(), $"Saved shift {model.ShiftName} ({model.ShiftCode}).");
            return OperationResult<int>.Success(id, "Shift saved successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteShiftAsync(int companyId, int shiftId, int userId, string username)
    {
        try
        {
            await _orgStructureRepository.DeleteShiftAsync(companyId, shiftId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Organization", "Shift", shiftId.ToString(), $"Deleted shift ID {shiftId}.");
            return OperationResult.Success("Shift deleted successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }
}
