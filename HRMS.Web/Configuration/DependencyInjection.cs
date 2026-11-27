using HRMS.Business.Interfaces;
using HRMS.Business.Security;
using HRMS.Business.Services;
using HRMS.Data.Database;
using HRMS.Data.Interfaces;
using HRMS.Data.Repositories;

namespace HRMS.Web.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddHrmsServices(this IServiceCollection services)
    {
        // Database Connection Factory
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        // Repositories
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<IOrgStructureRepository, OrgStructureRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
        services.AddScoped<ILoanExpenseRepository, LoanExpenseRepository>();
        services.AddScoped<IPerformanceTrainingRepository, PerformanceTrainingRepository>();
        services.AddScoped<IPortalRepository, PortalRepository>();
        services.AddScoped<IAssetDocumentRepository, AssetDocumentRepository>();
        services.AddScoped<IExitRepository, ExitRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ISystemAdminRepository, SystemAdminRepository>();

        // Security & Business Services
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDesignationService, DesignationService>();
        services.AddScoped<IOrgStructureService, OrgStructureService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IRecruitmentService, RecruitmentService>();
        services.AddScoped<ILoanExpenseService, LoanExpenseService>();
        services.AddScoped<IPerformanceTrainingService, PerformanceTrainingService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IAssetDocumentService, AssetDocumentService>();
        services.AddScoped<IExitService, ExitService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISystemAdminService, SystemAdminService>();

        return services;
    }
}
