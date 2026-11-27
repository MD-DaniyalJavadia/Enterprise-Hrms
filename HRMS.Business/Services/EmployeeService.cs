using System.Globalization;
using HRMS.Business.Interfaces;
using HRMS.Data.Interfaces;
using HRMS.Models.DTOs;
using HRMS.Models.Entities;
using HRMS.Models.Enums;
using HRMS.Models.ViewModels.Employees;
using Microsoft.Data.SqlClient;

namespace HRMS.Business.Services;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IOrgStructureRepository _orgStructureRepository;
    private readonly IAuditLogService _auditLogService;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IDepartmentRepository departmentRepository,
        IDesignationRepository designationRepository,
        IOrgStructureRepository orgStructureRepository,
        IAuditLogService auditLogService)
    {
        _employeeRepository = employeeRepository;
        _branchRepository = branchRepository;
        _departmentRepository = departmentRepository;
        _designationRepository = designationRepository;
        _orgStructureRepository = orgStructureRepository;
        _auditLogService = auditLogService;
    }

    public async Task<EmployeeDirectoryFilterViewModel> GetFilteredDirectoryAsync(
        int companyId, int? branchId = null, int? departmentId = null, byte? status = null, string? searchTerm = null)
    {
        return new EmployeeDirectoryFilterViewModel
        {
            BranchId = branchId,
            DepartmentId = departmentId,
            EmploymentStatus = status,
            SearchTerm = searchTerm,
            Employees = (await _employeeRepository.GetAllAsync(companyId, branchId, departmentId, status, searchTerm)).ToList(),
            Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList(),
            Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList()
        };
    }

    public async Task<EmployeeProfileViewModel?> GetProfileByIdAsync(int companyId, int employeeId)
    {
        var profile = await _employeeRepository.GetProfileByIdAsync(companyId, employeeId);
        if (profile is null) return null;

        profile.Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList();
        profile.Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList();
        profile.Designations = (await _designationRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList();
        return profile;
    }

    public async Task<EmployeeFormViewModel> PrepareFormAsync(int companyId, int? employeeId = null, EmployeeFormViewModel? posted = null)
    {
        EmployeeFormViewModel model;
        if (posted is not null)
        {
            model = posted;
        }
        else if (employeeId.HasValue && employeeId.Value > 0)
        {
            model = await _employeeRepository.GetForEditAsync(companyId, employeeId.Value)
                    ?? new EmployeeFormViewModel { CompanyId = companyId };
        }
        else
        {
            model = new EmployeeFormViewModel
            {
                CompanyId = companyId,
                EmployeeCode = await _employeeRepository.GenerateNextEmployeeCodeAsync(companyId),
                HireDate = DateTime.Today,
                DateOfBirth = DateTime.Today.AddYears(-26),
                EmploymentStatus = (byte)EmploymentStatus.Active
            };
        }

        model.CompanyId = companyId;
        model.Branches = (await _branchRepository.GetAllAsync(companyId, isActive: true)).ToList();
        model.Departments = (await _departmentRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList();
        model.Designations = (await _designationRepository.GetAllAsync(companyId)).Where(d => d.IsActive).ToList();
        model.Managers = (await _employeeRepository.GetAllAsync(companyId)).Where(e => e.IsActive && e.EmployeeId != model.EmployeeId).ToList();
        model.WorkLocations = (await _orgStructureRepository.GetWorkLocationsAsync(companyId)).Where(w => w.IsActive).ToList();
        model.JobPositions = (await _orgStructureRepository.GetJobPositionsAsync(companyId)).Where(j => j.IsActive).ToList();
        model.Shifts = (await _orgStructureRepository.GetShiftsAsync(companyId)).Where(s => s.IsActive).ToList();
        return model;
    }

    public async Task<OperationResult<int>> CreateAsync(EmployeeFormViewModel model, int userId, string username)
    {
        try
        {
            model.EmployeeCode = model.EmployeeCode.Trim().ToUpperInvariant();
            var newId = await _employeeRepository.InsertAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Create.ToString(),
                "Employees", "Employee", newId.ToString(), $"Registered employee {model.FirstName} {model.LastName} ({model.EmployeeCode}).");
            return OperationResult<int>.Success(newId, "Employee registered successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult<int>.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(EmployeeFormViewModel model, int userId, string username)
    {
        try
        {
            model.EmployeeCode = model.EmployeeCode.Trim().ToUpperInvariant();
            await _employeeRepository.UpdateAsync(model, userId);
            await _auditLogService.LogAsync(model.CompanyId, userId, username, AuditActionType.Update.ToString(),
                "Employees", "Employee", model.EmployeeId.ToString(), $"Updated employee {model.FirstName} {model.LastName} ({model.EmployeeCode}).");
            return OperationResult.Success("Employee profile updated successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int companyId, int employeeId, int userId, string username)
    {
        try
        {
            await _employeeRepository.DeleteAsync(companyId, employeeId, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Delete.ToString(),
                "Employees", "Employee", employeeId.ToString(), $"Archived employee ID {employeeId}.");
            return OperationResult.Success("Employee archived successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> RecordCareerActionAsync(int companyId, EmployeeCareerActionViewModel model, int userId, string username)
    {
        try
        {
            await _employeeRepository.RecordCareerActionAsync(companyId, model, userId);
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Update.ToString(),
                "Employees", "CareerAction", model.EmployeeId.ToString(),
                $"Recorded {model.ActionType} effective {model.EffectiveDate:yyyy-MM-dd}: {model.Remarks}");
            return OperationResult.Success($"{model.ActionType} recorded successfully.");
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<EmployeeImportResultViewModel> ImportFromCsvStreamAsync(int companyId, Stream csvStream, int userId, string username)
    {
        var result = new EmployeeImportResultViewModel();
        var branches = (await _branchRepository.GetAllAsync(companyId, isActive: true))
            .ToDictionary(b => b.BranchCode.Trim().ToUpperInvariant(), b => b.BranchId);
        var depts = (await _departmentRepository.GetAllAsync(companyId))
            .Where(d => d.IsActive)
            .ToDictionary(d => d.DepartmentCode.Trim().ToUpperInvariant(), d => d.DepartmentId);
        var desigs = (await _designationRepository.GetAllAsync(companyId))
            .Where(d => d.IsActive)
            .ToDictionary(d => d.DesignationCode.Trim().ToUpperInvariant(), d => d.DesignationId);

        using var reader = new StreamReader(csvStream);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            result.RowErrors.Add("The uploaded CSV file is empty.");
            return result;
        }

        int lineNo = 1;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRowsProcessed++;
            var cols = ParseCsvRow(line);
            if (cols.Count < 10)
            {
                result.RowErrors.Add($"Row {lineNo}: Expected at least 10 columns (EmployeeCode,FirstName,LastName,Gender,DateOfBirth,WorkEmail,HireDate,BranchCode,DepartmentCode,DesignationCode,BaseSalary).");
                continue;
            }

            var empCode = cols[0].Trim().ToUpperInvariant();
            var firstName = cols[1].Trim();
            var lastName = cols[2].Trim();
            var gender = cols[3].Trim();
            var dobRaw = cols[4].Trim();
            var workEmail = cols[5].Trim();
            var hireRaw = cols[6].Trim();
            var branchCode = cols[7].Trim().ToUpperInvariant();
            var deptCode = cols[8].Trim().ToUpperInvariant();
            var desigCode = cols[9].Trim().ToUpperInvariant();
            var salaryRaw = cols.Count > 10 ? cols[10].Trim() : "0";

            if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(workEmail))
            {
                result.RowErrors.Add($"Row {lineNo}: EmployeeCode, FirstName, LastName, and WorkEmail are required.");
                continue;
            }

            if (!DateTime.TryParse(dobRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob) ||
                !DateTime.TryParse(hireRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hireDate))
            {
                result.RowErrors.Add($"Row {lineNo}: Invalid DateOfBirth or HireDate format (use yyyy-MM-dd).");
                continue;
            }

            if (!branches.TryGetValue(branchCode, out int branchId))
            {
                result.RowErrors.Add($"Row {lineNo}: Unknown BranchCode '{branchCode}'.");
                continue;
            }

            if (!depts.TryGetValue(deptCode, out int deptId))
            {
                result.RowErrors.Add($"Row {lineNo}: Unknown DepartmentCode '{deptCode}'.");
                continue;
            }

            if (!desigs.TryGetValue(desigCode, out int desigId))
            {
                result.RowErrors.Add($"Row {lineNo}: Unknown DesignationCode '{desigCode}'.");
                continue;
            }

            decimal.TryParse(salaryRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var baseSalary);

            var form = new EmployeeFormViewModel
            {
                CompanyId = companyId,
                EmployeeCode = empCode,
                FirstName = firstName,
                LastName = lastName,
                Gender = string.IsNullOrEmpty(gender) ? "Other" : gender,
                DateOfBirth = dob,
                WorkEmail = workEmail,
                HireDate = hireDate,
                BranchId = branchId,
                DepartmentId = deptId,
                DesignationId = desigId,
                EmploymentStatus = (byte)EmploymentStatus.Active,
                BaseSalary = baseSalary
            };

            try
            {
                await _employeeRepository.InsertAsync(form, userId);
                result.ImportedCount++;
            }
            catch (SqlException ex)
            {
                result.RowErrors.Add($"Row {lineNo} ({empCode}): {ex.Message}");
            }
        }

        if (result.ImportedCount > 0)
        {
            await _auditLogService.LogAsync(companyId, userId, username, AuditActionType.Create.ToString(),
                "Employees", "BulkImport", null, $"Imported {result.ImportedCount} employee(s) via CSV.");
        }

        return result;
    }

    public async Task<OperationResult> AddEmergencyContactAsync(EmployeeEmergencyContact model, int userId)
    {
        await _employeeRepository.AddEmergencyContactAsync(model, userId);
        return OperationResult.Success("Emergency contact added.");
    }

    public async Task<OperationResult> DeleteEmergencyContactAsync(int companyId, int id)
    {
        await _employeeRepository.DeleteEmergencyContactAsync(companyId, id);
        return OperationResult.Success("Emergency contact removed.");
    }

    public async Task<OperationResult> AddFamilyMemberAsync(EmployeeFamilyMember model, int userId)
    {
        await _employeeRepository.AddFamilyMemberAsync(model, userId);
        return OperationResult.Success("Family member added.");
    }

    public async Task<OperationResult> DeleteFamilyMemberAsync(int companyId, int id)
    {
        await _employeeRepository.DeleteFamilyMemberAsync(companyId, id);
        return OperationResult.Success("Family member removed.");
    }

    public async Task<OperationResult> AddEducationAsync(EmployeeEducation model, int userId)
    {
        await _employeeRepository.AddEducationAsync(model, userId);
        return OperationResult.Success("Education record added.");
    }

    public async Task<OperationResult> DeleteEducationAsync(int companyId, int id)
    {
        await _employeeRepository.DeleteEducationAsync(companyId, id);
        return OperationResult.Success("Education record removed.");
    }

    public async Task<OperationResult> AddExperienceAsync(EmployeeExperience model, int userId)
    {
        await _employeeRepository.AddExperienceAsync(model, userId);
        return OperationResult.Success("Experience record added.");
    }

    public async Task<OperationResult> DeleteExperienceAsync(int companyId, int id)
    {
        await _employeeRepository.DeleteExperienceAsync(companyId, id);
        return OperationResult.Success("Experience record removed.");
    }

    public async Task<OperationResult> AddSkillAsync(EmployeeSkill model, int userId)
    {
        await _employeeRepository.AddSkillAsync(model, userId);
        return OperationResult.Success("Skill/certification added.");
    }

    public async Task<OperationResult> DeleteSkillAsync(int companyId, int id)
    {
        await _employeeRepository.DeleteSkillAsync(companyId, id);
        return OperationResult.Success("Skill removed.");
    }

    public async Task<OperationResult> AddDocumentAsync(EmployeeDocument model, int userId)
    {
        await _employeeRepository.AddDocumentAsync(model, userId);
        return OperationResult.Success("Document uploaded successfully.");
    }

    public Task<EmployeeDocument?> GetDocumentByIdAsync(int companyId, int documentId)
        => _employeeRepository.GetDocumentByIdAsync(companyId, documentId);

    public async Task<OperationResult> DeleteDocumentAsync(int companyId, int documentId)
    {
        await _employeeRepository.DeleteDocumentAsync(companyId, documentId);
        return OperationResult.Success("Document deleted.");
    }

    private static List<string> ParseCsvRow(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        values.Add(current.ToString());
        return values;
    }
}
