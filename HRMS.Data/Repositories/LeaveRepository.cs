using System.Data;
using Dapper;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Models.Entities;
using HRMS.Models.ViewModels.Leave;

namespace HRMS.Data.Repositories;

public sealed class LeaveRepository : BaseRepository, ILeaveRepository
{
    public LeaveRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(int companyId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        return QuerySpAsync<LeaveType>("sp_LeaveType_GetAll", p);
    }

    public async Task<int> SaveLeaveTypeAsync(int companyId, LeaveTypeFormViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@LeaveTypeId", model.LeaveTypeId, DbType.Int32);
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveCode", model.LeaveCode.Trim().ToUpperInvariant(), DbType.String, size: 20);
        p.Add("@LeaveName", model.LeaveName.Trim(), DbType.String, size: 80);
        p.Add("@AnnualQuota", model.AnnualQuota, DbType.Decimal);
        p.Add("@IsPaid", model.IsPaid, DbType.Boolean);
        p.Add("@AllowCarryForward", model.AllowCarryForward, DbType.Boolean);
        p.Add("@MaxCarryForwardDays", model.MaxCarryForwardDays, DbType.Decimal);
        p.Add("@AllowEncashment", model.AllowEncashment, DbType.Boolean);
        p.Add("@RequiresDocument", model.RequiresDocument, DbType.Boolean);
        p.Add("@MaxConsecutiveDays", model.MaxConsecutiveDays, DbType.Int32);
        p.Add("@IsHalfDayAllowed", model.IsHalfDayAllowed, DbType.Boolean);
        p.Add("@GenderRestriction", string.IsNullOrWhiteSpace(model.GenderRestriction) ? null : model.GenderRestriction.Trim(), DbType.String, size: 20);
        p.Add("@ColorHex", model.ColorHex.Trim(), DbType.String, size: 15);
        p.Add("@Description", model.Description, DbType.String, size: 300);
        p.Add("@IsActive", model.IsActive, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@SavedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_LeaveType_Save", p);
        return p.Get<int>("@SavedId");
    }

    public Task DeleteLeaveTypeAsync(int companyId, int leaveTypeId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveTypeId", leaveTypeId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_LeaveType_Delete", p);
    }

    public Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(int companyId, int leaveYear, int? employeeId = null, int? departmentId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveYear", leaveYear, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        return QuerySpAsync<LeaveBalance>("sp_LeaveBalance_GetAll", p);
    }

    public Task AllocateAnnualBalancesAsync(int companyId, int leaveYear, bool includeCarryForward, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveYear", leaveYear, DbType.Int32);
        p.Add("@IncludeCarryForward", includeCarryForward, DbType.Boolean);
        p.Add("@UserId", userId, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_LeaveBalance_AllocateAnnual", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task AdjustLeaveBalanceAsync(int companyId, LeaveBalanceAdjustViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveBalanceId", model.LeaveBalanceId, DbType.Int32);
        p.Add("@OpeningBalance", model.OpeningBalance, DbType.Decimal);
        p.Add("@AccruedDays", model.AccruedDays, DbType.Decimal);
        p.Add("@CarryForwardDays", model.CarryForwardDays, DbType.Decimal);
        p.Add("@UserId", userId, DbType.Int32);
        return ExecuteSpAsync("sp_LeaveBalance_Adjust", p);
    }

    public async Task<(IEnumerable<LeaveApplication> Applications, LeaveKpiSummaryViewModel Kpi)> GetLeaveApplicationsAsync(
        int companyId, int? employeeId = null, int? departmentId = null, int? leaveTypeId = null, string? approvalStatus = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", employeeId, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        p.Add("@LeaveTypeId", leaveTypeId, DbType.Int32);
        p.Add("@ApprovalStatus", string.IsNullOrWhiteSpace(approvalStatus) ? null : approvalStatus.Trim(), DbType.String, size: 20);

        using var connection = ConnectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync("sp_LeaveApplication_GetAll", p, commandType: CommandType.StoredProcedure);
        var apps = (await multi.ReadAsync<LeaveApplication>()).ToList();
        var kpi = await multi.ReadFirstOrDefaultAsync<LeaveKpiSummaryViewModel>() ?? new LeaveKpiSummaryViewModel();
        return (apps, kpi);
    }

    public Task<LeaveApplication?> GetLeaveApplicationByIdAsync(int companyId, int leaveApplicationId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveApplicationId", leaveApplicationId, DbType.Int32);
        return QueryFirstOrDefaultSpAsync<LeaveApplication>("sp_LeaveApplication_GetById", p);
    }

    public async Task<int> SubmitLeaveApplicationAsync(int companyId, LeaveApplyViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@LeaveTypeId", model.LeaveTypeId, DbType.Int32);
        p.Add("@StartDate", model.StartDate.Date, DbType.Date);
        p.Add("@EndDate", model.EndDate.Date, DbType.Date);
        p.Add("@IsHalfDay", model.IsHalfDay, DbType.Boolean);
        p.Add("@HalfDaySession", model.IsHalfDay ? model.HalfDaySession : null, DbType.String, size: 20);
        p.Add("@Reason", model.Reason.Trim(), DbType.String, size: 500);
        p.Add("@ContactDuringLeave", model.ContactDuringLeave, DbType.String, size: 60);
        p.Add("@AttachmentFileName", model.AttachmentFileName, DbType.String, size: 255);
        p.Add("@AttachmentStoredName", model.AttachmentStoredName, DbType.String, size: 255);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_LeaveApplication_Submit", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
        return p.Get<int>("@NewId");
    }

    public Task ReviewLeaveApplicationAsync(int companyId, LeaveReviewViewModel model, int reviewedBy)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveApplicationId", model.LeaveApplicationId, DbType.Int32);
        p.Add("@ApprovalStatus", model.ApprovalStatus, DbType.String, size: 20);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@ReviewedBy", reviewedBy, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_LeaveApplication_Review", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task CancelLeaveApplicationAsync(int companyId, int leaveApplicationId, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@LeaveApplicationId", leaveApplicationId, DbType.Int32);
        p.Add("@UserId", userId, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_LeaveApplication_Cancel", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }

    public Task<IEnumerable<LeaveCalendarEventViewModel>> GetCalendarEventsAsync(int companyId, int year, int month, int? departmentId = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@Year", year, DbType.Int32);
        p.Add("@Month", month, DbType.Int32);
        p.Add("@DepartmentId", departmentId, DbType.Int32);
        return QuerySpAsync<LeaveCalendarEventViewModel>("sp_Leave_GetCalendarEvents", p);
    }

    public Task<IEnumerable<LeaveEncashment>> GetLeaveEncashmentsAsync(int companyId, string? approvalStatus = null)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@ApprovalStatus", string.IsNullOrWhiteSpace(approvalStatus) ? null : approvalStatus.Trim(), DbType.String, size: 20);
        return QuerySpAsync<LeaveEncashment>("sp_LeaveEncashment_GetAll", p);
    }

    public async Task<int> SubmitLeaveEncashmentAsync(int companyId, LeaveEncashmentSubmitViewModel model, int userId)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EmployeeId", model.EmployeeId, DbType.Int32);
        p.Add("@LeaveTypeId", model.LeaveTypeId, DbType.Int32);
        p.Add("@LeaveYear", model.LeaveYear, DbType.Int32);
        p.Add("@DaysEncashed", model.DaysEncashed, DbType.Decimal);
        p.Add("@Remarks", model.Remarks, DbType.String, size: 300);
        p.Add("@UserId", userId, DbType.Int32);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await ExecuteSpAsync("sp_LeaveEncashment_Submit", p);
        return p.Get<int>("@NewId");
    }

    public Task ReviewLeaveEncashmentAsync(int companyId, LeaveEncashmentReviewViewModel model, int reviewedBy)
    {
        var p = new DynamicParameters();
        p.Add("@CompanyId", companyId, DbType.Int32);
        p.Add("@EncashmentId", model.EncashmentId, DbType.Int32);
        p.Add("@ApprovalStatus", model.ApprovalStatus, DbType.String, size: 20);
        p.Add("@ReviewerComments", model.ReviewerComments, DbType.String, size: 400);
        p.Add("@ReviewedBy", reviewedBy, DbType.Int32);

        return ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync("sp_LeaveEncashment_Review", p, transaction: tx, commandType: CommandType.StoredProcedure);
        });
    }
}
