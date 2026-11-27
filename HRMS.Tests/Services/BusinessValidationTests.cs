using HRMS.Business.Interfaces;
using HRMS.Business.Services;
using HRMS.Data.Interfaces;
using HRMS.Models.ViewModels.Exit;
using HRMS.Models.ViewModels.Leave;
using HRMS.Models.ViewModels.Organization;
using HRMS.Models.ViewModels.Payroll;
using HRMS.Models.ViewModels.Reports;
using Moq;
using Xunit;

namespace HRMS.Tests.Services;

public sealed class BusinessValidationTests
{
    [Fact]
    public async Task LeaveService_RejectsInvalidDatesAndMultiDayHalfDay()
    {
        var leaveRepo = new Mock<ILeaveRepository>();
        var empRepo = new Mock<IEmployeeRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        var attRepo = new Mock<IAttendanceRepository>();
        var audit = new Mock<IAuditLogService>();

        var service = new LeaveService(leaveRepo.Object, empRepo.Object, deptRepo.Object, attRepo.Object, audit.Object);

        var invertedDates = new LeaveApplyViewModel
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = new DateTime(2026, 5, 10),
            EndDate = new DateTime(2026, 5, 8),
            Reason = "Family travel"
        };
        var res1 = await service.SubmitApplicationAsync(1, invertedDates, 10, "emp");
        Assert.False(res1.Succeeded);

        var multiDayHalf = new LeaveApplyViewModel
        {
            EmployeeId = 5,
            LeaveTypeId = 1,
            StartDate = new DateTime(2026, 5, 10),
            EndDate = new DateTime(2026, 5, 11),
            IsHalfDay = true,
            Reason = "Appointment"
        };
        var res2 = await service.SubmitApplicationAsync(1, multiDayHalf, 10, "emp");
        Assert.False(res2.Succeeded);

        var badReview = await service.ReviewApplicationAsync(1, new LeaveReviewViewModel
        {
            LeaveApplicationId = 1,
            ApprovalStatus = "Deferred"
        }, 2, "mgr");
        Assert.False(badReview.Succeeded);
    }

    [Fact]
    public async Task PayrollService_RejectsInvalidStatuses()
    {
        var payRepo = new Mock<IPayrollRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        var audit = new Mock<IAuditLogService>();

        var service = new PayrollService(payRepo.Object, deptRepo.Object, audit.Object);

        var runResult = await service.UpdatePayrollRunStatusAsync(1, 50, "Archived", 1, "payroll");
        Assert.False(runResult.Succeeded);

        var slipResult = await service.AdjustPayslipAsync(1, new PayslipAdjustViewModel
        {
            PayslipId = 100,
            BonusAmount = 250,
            OtherDeductions = 0,
            PaymentStatus = "Disputed"
        }, 1, "payroll");
        Assert.False(slipResult.Succeeded);
    }

    [Fact]
    public async Task ReportService_NormalizesInvertedDateRange_AndRejectsInvalidReportKey()
    {
        var repRepo = new Mock<IReportRepository>();
        var branchRepo = new Mock<IBranchRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        var audit = new Mock<IAuditLogService>();

        branchRepo.Setup(b => b.GetAllAsync(1)).ReturnsAsync(Array.Empty<BranchListItemViewModel>());
        deptRepo.Setup(d => d.GetAllAsync(1)).ReturnsAsync(Array.Empty<DepartmentListItemViewModel>());

        DateTime capturedFrom = default;
        DateTime capturedTo = default;
        repRepo.Setup(r => r.GetAnalyticsHubAsync(1, null, null, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1))
            .Callback<int, int?, int?, DateTime, DateTime, int>((_, _, _, f, t, _) =>
            {
                capturedFrom = f;
                capturedTo = t;
            })
            .ReturnsAsync(new ReportsHubPageViewModel());

        var service = new ReportService(repRepo.Object, branchRepo.Object, deptRepo.Object, audit.Object);

        await service.GetAnalyticsHubAsync(1, null, null, new DateTime(2026, 6, 30), new DateTime(2026, 6, 1), 1, true);
        Assert.Equal(new DateTime(2026, 6, 1), capturedFrom);
        Assert.Equal(new DateTime(2026, 6, 30), capturedTo);

        var invalidPreset = await service.SaveReportTemplateAsync(1, new SaveReportTemplateFormViewModel
        {
            TemplateName = "Bad",
            ReportKey = "UnknownModule"
        }, 1, "admin");
        Assert.False(invalidPreset.Succeeded);
    }

    [Fact]
    public async Task ExitService_RejectsInvalidSeparationType_AndInvertedLastWorkingDay()
    {
        var exitRepo = new Mock<IExitRepository>();
        var empRepo = new Mock<IEmployeeRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        var portalRepo = new Mock<IPortalRepository>();
        var audit = new Mock<IAuditLogService>();

        var service = new ExitService(exitRepo.Object, empRepo.Object, deptRepo.Object, portalRepo.Object, audit.Object);

        var badType = new SeparationSubmitFormViewModel
        {
            EmployeeId = 8,
            SeparationType = "Sabbatical",
            ReasonCategory = "Other",
            SubmissionDate = new DateTime(2026, 6, 1),
            RequestedLastWorkingDay = new DateTime(2026, 6, 30)
        };
        var res1 = await service.SubmitSeparationAsync(1, badType, 1, "hr");
        Assert.False(res1.Succeeded);

        var badLwd = new SeparationSubmitFormViewModel
        {
            EmployeeId = 8,
            SeparationType = "Resignation",
            ReasonCategory = "BetterOpportunity",
            SubmissionDate = new DateTime(2026, 6, 15),
            RequestedLastWorkingDay = new DateTime(2026, 6, 10)
        };
        var res2 = await service.SubmitSeparationAsync(1, badLwd, 1, "hr");
        Assert.False(res2.Succeeded);
    }
}
