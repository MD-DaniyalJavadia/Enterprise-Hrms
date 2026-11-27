using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.ViewModels.Organization;

namespace HRMS.Data.Repositories;

public sealed class CompanyRepository : BaseRepository, ICompanyRepository
{
    public CompanyRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<CompanyListItemViewModel>> GetAllAsync(string? searchTerm = null, bool? isActive = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 100);
        parameters.Add("@IsActive", isActive, DbType.Boolean);
        return QuerySpAsync<CompanyListItemViewModel>("sp_Company_GetAll", parameters);
    }

    public Task<CompanyFormViewModel?> GetByIdAsync(int companyId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<CompanyFormViewModel>("sp_Company_GetById", parameters);
    }

    public async Task<int> InsertAsync(CompanyFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyCode", model.CompanyCode, DbType.String, size: 20);
        parameters.Add("@CompanyName", model.CompanyName, DbType.String, size: 150);
        parameters.Add("@LegalName", model.LegalName, DbType.String, size: 200);
        parameters.Add("@TaxId", model.TaxId, DbType.String, size: 50);
        parameters.Add("@Email", model.Email, DbType.String, size: 120);
        parameters.Add("@Phone", model.Phone, DbType.String, size: 30);
        parameters.Add("@Website", model.Website, DbType.String, size: 150);
        parameters.Add("@AddressLine1", model.AddressLine1, DbType.String, size: 250);
        parameters.Add("@City", model.City, DbType.String, size: 80);
        parameters.Add("@Country", model.Country, DbType.String, size: 80);
        parameters.Add("@CurrencyCode", model.CurrencyCode, DbType.StringFixedLength, size: 3);
        parameters.Add("@TimeZoneId", model.TimeZoneId, DbType.String, size: 60);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@CreatedBy", userId, DbType.Int32);
        parameters.Add("@NewCompanyId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_Company_Insert", parameters);
        return parameters.Get<int>("@NewCompanyId");
    }

    public Task UpdateAsync(CompanyFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@CompanyCode", model.CompanyCode, DbType.String, size: 20);
        parameters.Add("@CompanyName", model.CompanyName, DbType.String, size: 150);
        parameters.Add("@LegalName", model.LegalName, DbType.String, size: 200);
        parameters.Add("@TaxId", model.TaxId, DbType.String, size: 50);
        parameters.Add("@Email", model.Email, DbType.String, size: 120);
        parameters.Add("@Phone", model.Phone, DbType.String, size: 30);
        parameters.Add("@Website", model.Website, DbType.String, size: 150);
        parameters.Add("@AddressLine1", model.AddressLine1, DbType.String, size: 250);
        parameters.Add("@City", model.City, DbType.String, size: 80);
        parameters.Add("@Country", model.Country, DbType.String, size: 80);
        parameters.Add("@CurrencyCode", model.CurrencyCode, DbType.StringFixedLength, size: 3);
        parameters.Add("@TimeZoneId", model.TimeZoneId, DbType.String, size: 60);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);

        return ExecuteSpAsync("sp_Company_Update", parameters);
    }

    public Task DeleteAsync(int companyId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Company_Delete", parameters);
    }
}

public sealed class BranchRepository : BaseRepository, IBranchRepository
{
    public BranchRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<BranchListItemViewModel>> GetAllAsync(int companyId, string? searchTerm = null, bool? isActive = null)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@SearchTerm", searchTerm, DbType.String, size: 100);
        parameters.Add("@IsActive", isActive, DbType.Boolean);
        return QuerySpAsync<BranchListItemViewModel>("sp_Branch_GetAll", parameters);
    }

    public Task<BranchFormViewModel?> GetByIdAsync(int companyId, int branchId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@BranchId", branchId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<BranchFormViewModel>("sp_Branch_GetById", parameters);
    }

    public async Task<int> InsertAsync(BranchFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@BranchCode", model.BranchCode, DbType.String, size: 20);
        parameters.Add("@BranchName", model.BranchName, DbType.String, size: 120);
        parameters.Add("@Email", model.Email, DbType.String, size: 120);
        parameters.Add("@Phone", model.Phone, DbType.String, size: 30);
        parameters.Add("@AddressLine1", model.AddressLine1, DbType.String, size: 250);
        parameters.Add("@City", model.City, DbType.String, size: 80);
        parameters.Add("@Country", model.Country, DbType.String, size: 80);
        parameters.Add("@IsHeadOffice", model.IsHeadOffice, DbType.Boolean);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@CreatedBy", userId, DbType.Int32);
        parameters.Add("@NewBranchId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_Branch_Insert", parameters, transaction: tx, commandType: CommandType.StoredProcedure);
        });
        return parameters.Get<int>("@NewBranchId");
    }

    public Task UpdateAsync(BranchFormViewModel model, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@BranchId", model.BranchId, DbType.Int32);
        parameters.Add("@CompanyId", model.CompanyId, DbType.Int32);
        parameters.Add("@BranchCode", model.BranchCode, DbType.String, size: 20);
        parameters.Add("@BranchName", model.BranchName, DbType.String, size: 120);
        parameters.Add("@Email", model.Email, DbType.String, size: 120);
        parameters.Add("@Phone", model.Phone, DbType.String, size: 30);
        parameters.Add("@AddressLine1", model.AddressLine1, DbType.String, size: 250);
        parameters.Add("@City", model.City, DbType.String, size: 80);
        parameters.Add("@Country", model.Country, DbType.String, size: 80);
        parameters.Add("@IsHeadOffice", model.IsHeadOffice, DbType.Boolean);
        parameters.Add("@IsActive", model.IsActive, DbType.Boolean);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_Branch_Update", parameters, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task DeleteAsync(int companyId, int branchId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompanyId", companyId, DbType.Int32);
        parameters.Add("@BranchId", branchId, DbType.Int32);
        parameters.Add("@ModifiedBy", userId, DbType.Int32);
        return ExecuteSpAsync("sp_Branch_Delete", parameters);
    }
}
