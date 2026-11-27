using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Employees;

namespace HRMS.Data.Repositories;

public sealed class EmployeeRepository : BaseRepository, IEmployeeRepository
{
    public EmployeeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<string> GenerateNextEmployeeCodeAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return await ExecuteScalarSpAsync<string>("sp_Employee_GenerateNextCode", p) ?? "EMP-1001";
    }

    public Task<IEnumerable<EmployeeListItemViewModel>> GetAllAsync(
        int companyId, int? branchId = null, int? departmentId = null, byte? status = null, string? searchTerm = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@BranchId", branchId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@EmploymentStatus", status, DbType.Byte);
        p.Add("@SearchTerm", searchTerm, DbType.String, size: 100);
        return QuerySpAsync<EmployeeListItemViewModel>("sp_Employee_GetAll", p);
    }

    public async Task<EmployeeProfileViewModel?> GetProfileByIdAsync(int companyId, int employeeId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("sp_Employee_GetById", p, commandType: CommandType.StoredProcedure);

        var employee = await multi.ReadFirstOrDefaultAsync<EmployeeDetailRecord>();
        if (employee is null) return null;

        return new EmployeeProfileViewModel
        {
            Employee = employee,
            EmergencyContacts = (await multi.ReadAsync<EmployeeEmergencyContact>()).ToList(),
            FamilyMembers = (await multi.ReadAsync<EmployeeFamilyMember>()).ToList(),
            EducationHistory = (await multi.ReadAsync<EmployeeEducation>()).ToList(),
            ExperienceHistory = (await multi.ReadAsync<EmployeeExperience>()).ToList(),
            Skills = (await multi.ReadAsync<EmployeeSkill>()).ToList(),
            Documents = (await multi.ReadAsync<EmployeeDocument>()).ToList(),
            EmploymentTimeline = (await multi.ReadAsync<EmployeeEmploymentHistory>()).ToList(),
            SalaryHistory = (await multi.ReadAsync<EmployeeSalaryHistory>()).ToList()
        };
    }

    public async Task<EmployeeFormViewModel?> GetForEditAsync(int companyId, int employeeId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("sp_Employee_GetById", p, commandType: CommandType.StoredProcedure);
        return await multi.ReadFirstOrDefaultAsync<EmployeeFormViewModel>();
    }

    public async Task<int> InsertAsync(EmployeeFormViewModel model, int userId)
    {
        var p = BuildEmployeeParameters(model);
        p.Add("@CreatedBy", userId, DbType.Int32);
        p.Add("@NewEmployeeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_Employee_Insert", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });

        return p.Get<int>("@NewEmployeeId");
    }

    public Task UpdateAsync(EmployeeFormViewModel model, int userId)
    {
        var p = BuildEmployeeParameters(model);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Employee_Update", p);
    }

    public Task DeleteAsync(int companyId, int employeeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Employee_Delete", p);
    }

    public Task RecordCareerActionAsync(int companyId, EmployeeCareerActionViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@ActionType", model.ActionType, DbType.String, size: 40);
        p.Add("@EffectiveDate", model.EffectiveDate.Date, DbType.Date);
        p.Add("@NewBranchId", model.NewBranchId, DbType.Int32);
        p.Add("@NewDepartmentId", model.NewDepartmentId, DbType.Int32);
        p.Add("@NewDesignationId", model.NewDesignationId, DbType.Int32);
        p.Add("@NewStatus", model.NewStatus, DbType.Byte);
        p.Add("@NewBaseSalary", model.NewBaseSalary, DbType.Decimal);
        p.Add("@Remarks", model.Remarks, DbType.String, size: 500);
        p.Add("@UserId", userId, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_Employee_RecordCareerAction", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task AddEmergencyContactAsync(EmployeeEmergencyContact model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@ContactName", model.ContactName, DbType.String, size: 120);
        p.Add("@Relationship", model.Relationship, DbType.String, size: 50);
        p.Add("@PrimaryPhone", model.PrimaryPhone, DbType.String, size: 30);
        p.Add("@AlternatePhone", model.AlternatePhone, DbType.String, size: 30);
        p.Add("@Address", model.Address, DbType.String, size: 250);
        p.Add("@IsPrimary", model.IsPrimary, DbType.Boolean);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeEmergencyContact_Insert", p);
    }

    public Task DeleteEmergencyContactAsync(int companyId, int emergencyContactId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmergencyContactId", emergencyContactId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeEmergencyContact_Delete", p);
    }

    public Task AddFamilyMemberAsync(EmployeeFamilyMember model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@FullName", model.FullName, DbType.String, size: 120);
        p.Add("@Relationship", model.Relationship, DbType.String, size: 50);
        p.Add("@DateOfBirth", model.DateOfBirth?.Date, DbType.Date);
        p.Add("@ContactPhone", model.ContactPhone, DbType.String, size: 30);
        p.Add("@IsDependent", model.IsDependent, DbType.Boolean);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeFamily_Insert", p);
    }

    public Task DeleteFamilyMemberAsync(int companyId, int familyMemberId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@FamilyMemberId", familyMemberId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeFamily_Delete", p);
    }

    public Task AddEducationAsync(EmployeeEducation model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@DegreeTitle", model.DegreeTitle, DbType.String, size: 120);
        p.Add("@InstitutionName", model.InstitutionName, DbType.String, size: 150);
        p.Add("@FieldOfStudy", model.FieldOfStudy, DbType.String, size: 120);
        p.Add("@StartYear", model.StartYear, DbType.Int32);
        p.Add("@CompletionYear", model.CompletionYear, DbType.Int32);
        p.Add("@GradeOrGpa", model.GradeOrGpa, DbType.String, size: 30);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeEducation_Insert", p);
    }

    public Task DeleteEducationAsync(int companyId, int educationId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EducationId", educationId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeEducation_Delete", p);
    }

    public Task AddExperienceAsync(EmployeeExperience model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@EmployerName", model.EmployerName, DbType.String, size: 150);
        p.Add("@JobTitle", model.JobTitle, DbType.String, size: 120);
        p.Add("@StartDate", model.StartDate.Date, DbType.Date);
        p.Add("@EndDate", model.EndDate?.Date, DbType.Date);
        p.Add("@Responsibilities", model.Responsibilities, DbType.String, size: 500);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeExperience_Insert", p);
    }

    public Task DeleteExperienceAsync(int companyId, int experienceId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ExperienceId", experienceId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeExperience_Delete", p);
    }

    public Task AddSkillAsync(EmployeeSkill model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@SkillName", model.SkillName, DbType.String, size: 100);
        p.Add("@ProficiencyLevel", model.ProficiencyLevel, DbType.String, size: 30);
        p.Add("@CertificationName", model.CertificationName, DbType.String, size: 150);
        p.Add("@IssuingAuthority", model.IssuingAuthority, DbType.String, size: 120);
        p.Add("@ExpiryDate", model.ExpiryDate?.Date, DbType.Date);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeSkill_Insert", p);
    }

    public Task DeleteSkillAsync(int companyId, int skillId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@SkillId", skillId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeSkill_Delete", p);
    }

    public Task AddDocumentAsync(EmployeeDocument model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", model.CompanyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@DocumentType", model.DocumentType, DbType.String, size: 60);
        p.Add("@DocumentTitle", model.DocumentTitle, DbType.String, size: 150);
        p.Add("@OriginalFileName", model.OriginalFileName, DbType.String, size: 255);
        p.Add("@StoredFileName", model.StoredFileName, DbType.String, size: 255);
        p.Add("@ContentType", model.ContentType, DbType.String, size: 120);
        p.Add("@FileSizeBytes", model.FileSizeBytes, DbType.Int64);
        p.Add("@ExpiryDate", model.ExpiryDate?.Date, DbType.Date);
        p.Add("@CreatedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeDocument_Insert", p);
    }

    public Task<EmployeeDocument?> GetDocumentByIdAsync(int companyId, int documentId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@DocumentId", documentId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<EmployeeDocument>("sp_EmployeeDocument_GetById", p);
    }

    public Task DeleteDocumentAsync(int companyId, int documentId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@DocumentId", documentId, DbType.Int32);
        return ExecuteSpAsync("sp_EmployeeDocument_Delete", p);
    }

    private static DynamicParameters BuildEmployeeParameters(EmployeeFormViewModel m)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", m.CompanyId, DbType.Int32);
        p.Add("@BranchId", m.BranchId, DbType.Int32);
        p.Add("@DepartmentId", m.DepartmentId, DbType.Int32);
        p.Add("@DesignationId", m.DesignationId, DbType.Int32);
        p.Add("@ReportingManagerId", m.ReportingManagerId, DbType.Int32);
        p.Add("@WorkLocationId", m.WorkLocationId, DbType.Int32);
        p.Add("@JobPositionId", m.JobPositionId, DbType.Int32);
        p.Add("@ShiftId", m.ShiftId, DbType.Int32);
        p.Add("@EmployeeCode", m.EmployeeCode, DbType.String, size: 30);
        p.Add("@FirstName", m.FirstName, DbType.String, size: 80);
        p.Add("@MiddleName", m.MiddleName, DbType.String, size: 80);
        p.Add("@LastName", m.LastName, DbType.String, size: 80);
        p.Add("@Gender", m.Gender, DbType.String, size: 20);
        p.Add("@DateOfBirth", m.DateOfBirth.Date, DbType.Date);
        p.Add("@MaritalStatus", m.MaritalStatus, DbType.String, size: 20);
        p.Add("@Nationality", m.Nationality, DbType.String, size: 60);
        p.Add("@NationalIdNumber", m.NationalIdNumber, DbType.String, size: 50);
        p.Add("@BloodGroup", m.BloodGroup, DbType.String, size: 10);
        p.Add("@WorkEmail", m.WorkEmail, DbType.String, size: 120);
        p.Add("@PersonalEmail", m.PersonalEmail, DbType.String, size: 120);
        p.Add("@Phone", m.Phone, DbType.String, size: 30);
        p.Add("@MobilePhone", m.MobilePhone, DbType.String, size: 30);
        p.Add("@CurrentAddress", m.CurrentAddress, DbType.String, size: 300);
        p.Add("@PermanentAddress", m.PermanentAddress, DbType.String, size: 300);
        p.Add("@City", m.City, DbType.String, size: 80);
        p.Add("@Country", m.Country, DbType.String, size: 80);
        p.Add("@HireDate", m.HireDate.Date, DbType.Date);
        p.Add("@ConfirmationDate", m.ConfirmationDate?.Date, DbType.Date);
        p.Add("@EmploymentStatus", m.EmploymentStatus, DbType.Byte);
        p.Add("@BaseSalary", m.BaseSalary, DbType.Decimal);
        p.Add("@BankName", m.BankName, DbType.String, size: 120);
        p.Add("@BankAccountTitle", m.BankAccountTitle, DbType.String, size: 120);
        p.Add("@BankAccountNumber", m.BankAccountNumber, DbType.String, size: 50);
        p.Add("@BankRoutingCode", m.BankRoutingCode, DbType.String, size: 40);
        p.Add("@TaxIdentificationNumber", m.TaxIdentificationNumber, DbType.String, size: 50);
        p.Add("@TaxFilingStatus", m.TaxFilingStatus, DbType.String, size: 40);
        p.Add("@PhotoPath", m.PhotoPath, DbType.String, size: 260);
        return p;
    }
}
