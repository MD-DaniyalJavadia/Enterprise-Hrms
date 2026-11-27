USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 12: Separation & Exit Management Stored Procedures
-- ============================================================================

-- 1. List Separation Requests + KPI Summary
CREATE OR ALTER PROCEDURE dbo.sp_SeparationRequest_GetAll
    @CompanyId         INT,
    @EmployeeId        INT = NULL,
    @ManagerEmployeeId INT = NULL,
    @DepartmentId      INT = NULL,
    @Status            NVARCHAR(25) = NULL,
    @Type              NVARCHAR(30) = NULL,
    @Search            NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH TeamTree AS (
        SELECT e.EmployeeId
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId
          AND e.IsDeleted = 0
          AND @ManagerEmployeeId IS NOT NULL
          AND e.ReportingManagerId = @ManagerEmployeeId
        UNION ALL
        SELECT c.EmployeeId
        FROM dbo.tbl_Employees c
        INNER JOIN TeamTree t ON c.ReportingManagerId = t.EmployeeId
        WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0
    )
    SELECT
        sr.SeparationId,
        sr.CompanyId,
        sr.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        desig.DesignationTitle,
        mgr.FullName AS ManagerName,
        sr.SeparationNumber,
        sr.SeparationType,
        sr.ReasonCategory,
        sr.SubmissionDate,
        sr.NoticePeriodDays,
        sr.RequestedLastWorkingDay,
        sr.ApprovedLastWorkingDay,
        sr.IsNoticeWaived,
        sr.NoticeShortfallDays,
        sr.SeparationStatus,
        sr.IsRehireEligible,
        (SELECT COUNT(1) FROM dbo.tbl_ExitClearanceItems ci WHERE ci.SeparationId = sr.SeparationId AND ci.IsDeleted = 0 AND ci.ClearanceStatus IN (N'Cleared', N'Waived')) AS ClearedItemsCount,
        (SELECT COUNT(1) FROM dbo.tbl_ExitClearanceItems ci WHERE ci.SeparationId = sr.SeparationId AND ci.IsDeleted = 0) AS TotalClearanceItemsCount,
        (SELECT COUNT(1) FROM dbo.tbl_AssetAllocations aa WHERE aa.CompanyId = sr.CompanyId AND aa.EmployeeId = sr.EmployeeId AND aa.AllocationStatus = N'Active' AND aa.IsDeleted = 0) AS ActiveAssetsCount,
        (SELECT COUNT(1) FROM dbo.tbl_EmployeeLoans el WHERE el.CompanyId = sr.CompanyId AND el.EmployeeId = sr.EmployeeId AND el.LoanStatus = N'Active' AND el.IsDeleted = 0) AS ActiveLoansCount,
        CASE WHEN ei.ExitInterviewId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasExitInterview,
        fs.SettlementStatus,
        fs.NetPayableAmount AS SettlementNetAmount
    FROM dbo.tbl_SeparationRequests sr
    INNER JOIN dbo.tbl_Employees e ON sr.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    LEFT JOIN dbo.tbl_ExitInterviews ei ON sr.SeparationId = ei.SeparationId
    LEFT JOIN dbo.tbl_FinalSettlements fs ON sr.SeparationId = fs.SeparationId AND fs.IsDeleted = 0
    WHERE sr.CompanyId = @CompanyId
      AND sr.IsDeleted = 0
      AND (@EmployeeId IS NULL OR sr.EmployeeId = @EmployeeId)
      AND (@ManagerEmployeeId IS NULL OR sr.EmployeeId = @ManagerEmployeeId OR EXISTS (SELECT 1 FROM TeamTree tt WHERE tt.EmployeeId = sr.EmployeeId))
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@Status IS NULL OR sr.SeparationStatus = @Status)
      AND (@Type IS NULL OR sr.SeparationType = @Type)
      AND (@Search IS NULL OR @Search = N'' OR
           sr.SeparationNumber LIKE N'%' + @Search + N'%' OR
           e.EmployeeCode LIKE N'%' + @Search + N'%' OR
           e.FullName LIKE N'%' + @Search + N'%')
    ORDER BY
        CASE WHEN sr.SeparationStatus IN (N'Completed', N'Rejected', N'Withdrawn') THEN 1 ELSE 0 END,
        sr.SubmissionDate DESC,
        sr.SeparationId DESC;

    -- KPI Summary (scoped cleanly when @EmployeeId or @ManagerEmployeeId is passed)
    ;WITH TeamTreeKpi AS (
        SELECT e.EmployeeId
        FROM dbo.tbl_Employees e
        WHERE e.CompanyId = @CompanyId
          AND e.IsDeleted = 0
          AND @ManagerEmployeeId IS NOT NULL
          AND e.ReportingManagerId = @ManagerEmployeeId
        UNION ALL
        SELECT c.EmployeeId
        FROM dbo.tbl_Employees c
        INNER JOIN TeamTreeKpi t ON c.ReportingManagerId = t.EmployeeId
        WHERE c.CompanyId = @CompanyId AND c.IsDeleted = 0
    )
    SELECT
        COUNT(1) AS TotalRequests,
        SUM(CASE WHEN sr.SeparationStatus IN (N'Submitted', N'ManagerApproved') THEN 1 ELSE 0 END) AS PendingApprovalCount,
        SUM(CASE WHEN sr.SeparationStatus IN (N'HrApproved', N'ClearanceInProgress') THEN 1 ELSE 0 END) AS ClearanceInProgressCount,
        SUM(CASE WHEN sr.SeparationStatus IN (N'Settled', N'Completed') THEN 1 ELSE 0 END) AS CompletedCount,
        ISNULL(SUM(CASE WHEN fs.SettlementStatus IN (N'Draft', N'Approved') THEN fs.NetPayableAmount ELSE 0 END), 0) AS TotalPendingSettlementAmount,
        ISNULL((
            SELECT AVG(CAST(ei2.OverallSatisfactionRating AS DECIMAL(4,2)))
            FROM dbo.tbl_ExitInterviews ei2
            INNER JOIN dbo.tbl_SeparationRequests sr2 ON ei2.SeparationId = sr2.SeparationId
            WHERE ei2.CompanyId = @CompanyId
              AND sr2.IsDeleted = 0
              AND (@EmployeeId IS NULL OR sr2.EmployeeId = @EmployeeId)
              AND (@ManagerEmployeeId IS NULL OR sr2.EmployeeId = @ManagerEmployeeId OR EXISTS (SELECT 1 FROM TeamTreeKpi tt2 WHERE tt2.EmployeeId = sr2.EmployeeId))
        ), 0) AS AvgExitSatisfaction
    FROM dbo.tbl_SeparationRequests sr
    LEFT JOIN dbo.tbl_FinalSettlements fs ON sr.SeparationId = fs.SeparationId AND fs.IsDeleted = 0
    WHERE sr.CompanyId = @CompanyId
      AND sr.IsDeleted = 0
      AND (@EmployeeId IS NULL OR sr.EmployeeId = @EmployeeId)
      AND (@ManagerEmployeeId IS NULL OR sr.EmployeeId = @ManagerEmployeeId OR EXISTS (SELECT 1 FROM TeamTreeKpi tt WHERE tt.EmployeeId = sr.EmployeeId));
END
GO

-- 2. Get 360 Separation Details
CREATE OR ALTER PROCEDURE dbo.sp_SeparationRequest_GetById
    @CompanyId    INT,
    @SeparationId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EmployeeId INT;
    SELECT TOP 1 @EmployeeId = EmployeeId
    FROM dbo.tbl_SeparationRequests
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    SELECT TOP 1
        sr.SeparationId,
        sr.CompanyId,
        sr.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        e.WorkEmail,
        e.HireDate,
        e.BaseSalary,
        b.BranchName,
        d.DepartmentName,
        desig.DesignationTitle,
        mgr.FullName AS ManagerName,
        sr.SeparationNumber,
        sr.SeparationType,
        sr.ReasonCategory,
        sr.SubmissionDate,
        sr.NoticePeriodDays,
        sr.RequestedLastWorkingDay,
        sr.ApprovedLastWorkingDay,
        sr.IsNoticeWaived,
        sr.NoticeShortfallDays,
        sr.EmployeeComments,
        sr.ManagerComments,
        sr.HrComments,
        sr.SeparationStatus,
        sr.IsRehireEligible,
        sr.ManagerApprovedDate,
        sr.HrApprovedDate,
        sr.CompletedDate
    FROM dbo.tbl_SeparationRequests sr
    INNER JOIN dbo.tbl_Employees e ON sr.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    WHERE sr.CompanyId = @CompanyId AND sr.SeparationId = @SeparationId AND sr.IsDeleted = 0;

    -- Result Set 2: Live Offboarding Readiness Snapshot
    DECLARE @CurrentYear INT = YEAR(SYSUTCDATETIME());
    SELECT
        (SELECT COUNT(1) FROM dbo.tbl_AssetAllocations aa WHERE aa.CompanyId = @CompanyId AND aa.EmployeeId = @EmployeeId AND aa.AllocationStatus = N'Active' AND aa.IsDeleted = 0) AS ActiveAssetsCount,
        (SELECT COUNT(1) FROM dbo.tbl_EmployeeLoans el WHERE el.CompanyId = @CompanyId AND el.EmployeeId = @EmployeeId AND el.LoanStatus = N'Active' AND el.IsDeleted = 0) AS ActiveLoansCount,
        ISNULL((SELECT SUM(el.RemainingBalance) FROM dbo.tbl_EmployeeLoans el WHERE el.CompanyId = @CompanyId AND el.EmployeeId = @EmployeeId AND el.LoanStatus = N'Active' AND el.IsDeleted = 0), 0) AS ActiveLoanBalance,
        ISNULL((
            SELECT SUM(CASE WHEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays) > 0
                            THEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays)
                            ELSE 0 END)
            FROM dbo.tbl_LeaveBalances lb
            INNER JOIN dbo.tbl_LeaveTypes lt ON lb.LeaveTypeId = lt.LeaveTypeId
            WHERE lb.CompanyId = @CompanyId
              AND lb.EmployeeId = @EmployeeId
              AND lb.LeaveYear = @CurrentYear
              AND lt.AllowEncashment = 1
              AND lt.IsDeleted = 0
        ), 0) AS EncashableLeaveDays,
        ISNULL((SELECT TOP 1 BaseSalary FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId), 0) AS MonthlyBaseSalary;

    -- Result Set 3: Clearance Items
    SELECT
        ClearanceItemId,
        CompanyId,
        SeparationId,
        DepartmentScope,
        ItemName,
        IsMandatory,
        ClearanceStatus,
        ClearedBy,
        ClearedDate,
        RecoveryAmount,
        Remarks
    FROM dbo.tbl_ExitClearanceItems
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0
    ORDER BY DepartmentScope, ClearanceItemId;

    -- Result Set 4: Exit Interview
    SELECT TOP 1
        ei.ExitInterviewId,
        ei.CompanyId,
        ei.SeparationId,
        ei.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        sr.SeparationNumber,
        sr.SeparationType,
        ei.InterviewDate,
        ei.OverallSatisfactionRating,
        ei.ManagerSupportRating,
        ei.WorkLifeBalanceRating,
        ei.CompensationRating,
        ei.WouldRecommendCompany,
        ei.WouldConsiderRejoining,
        ei.PrimaryLeavingReason,
        ei.LikedMost,
        ei.ImprovementSuggestions,
        ei.InterviewerNotes,
        ei.IsConfidential
    FROM dbo.tbl_ExitInterviews ei
    INNER JOIN dbo.tbl_Employees e ON ei.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_SeparationRequests sr ON ei.SeparationId = sr.SeparationId
    WHERE ei.CompanyId = @CompanyId AND ei.SeparationId = @SeparationId;

    -- Result Set 5: Final Settlement
    SELECT TOP 1
        SettlementId,
        CompanyId,
        SeparationId,
        EmployeeId,
        SettlementNumber,
        MonthlyBaseSalary,
        WorkedDaysInFinalMonth,
        ProratedSalaryPay,
        EncashableLeaveDays,
        LeaveEncashmentAmount,
        GratuityOrSeverance,
        BonusOrOtherEarnings,
        OutstandingLoanRecovery,
        AssetOrNoticeRecovery,
        TaxAndOtherDeductions,
        GrossEarnings,
        TotalDeductions,
        NetPayableAmount,
        SettlementStatus,
        PaymentReference,
        PaidDate,
        Notes,
        ApprovedDate
    FROM dbo.tbl_FinalSettlements
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;
END
GO

-- 3. Submit Separation / Resignation Request
CREATE OR ALTER PROCEDURE dbo.sp_SeparationRequest_Submit
    @CompanyId               INT,
    @EmployeeId              INT,
    @SeparationType          NVARCHAR(30),
    @ReasonCategory          NVARCHAR(60),
    @SubmissionDate          DATE,
    @NoticePeriodDays        INT,
    @RequestedLastWorkingDay DATE,
    @IsNoticeWaived          BIT = 0,
    @EmployeeComments        NVARCHAR(600) = NULL,
    @HrComments              NVARCHAR(600) = NULL,
    @InitiateAsHrApproved    BIT = 0,
    @UserId                  INT,
    @SavedId                 INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        ROLLBACK TRAN;
        THROW 50161, N'Employee is not active or not found.', 1;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_SeparationRequests
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
          AND SeparationStatus NOT IN (N'Rejected', N'Withdrawn', N'Completed')
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50162, N'An active separation request already exists for this employee.', 1;
    END

    IF @RequestedLastWorkingDay < @SubmissionDate
    BEGIN
        ROLLBACK TRAN;
        THROW 50163, N'Requested last working day cannot be earlier than submission date.', 1;
    END

    DECLARE @NextSeq INT = ISNULL((SELECT MAX(SeparationId) FROM dbo.tbl_SeparationRequests WHERE CompanyId = @CompanyId), 0) + 1;
    DECLARE @SepNumber NVARCHAR(30) = CONCAT(N'EXT-', YEAR(SYSUTCDATETIME()), N'-', RIGHT(CONCAT(N'0000', @NextSeq), 4));

    DECLARE @ActualNotice INT = DATEDIFF(DAY, @SubmissionDate, @RequestedLastWorkingDay);
    DECLARE @Shortfall INT = CASE WHEN @IsNoticeWaived = 1 OR @ActualNotice >= @NoticePeriodDays THEN 0 ELSE (@NoticePeriodDays - @ActualNotice) END;
    DECLARE @InitStatus NVARCHAR(25) = CASE WHEN @InitiateAsHrApproved = 1 THEN N'ClearanceInProgress' ELSE N'Submitted' END;

    INSERT INTO dbo.tbl_SeparationRequests (
        CompanyId, EmployeeId, SeparationNumber, SeparationType, ReasonCategory, SubmissionDate,
        NoticePeriodDays, RequestedLastWorkingDay, ApprovedLastWorkingDay, IsNoticeWaived, NoticeShortfallDays,
        EmployeeComments, HrComments, SeparationStatus, HrApprovedBy, HrApprovedDate, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, @SepNumber, @SeparationType, @ReasonCategory, @SubmissionDate,
        @NoticePeriodDays, @RequestedLastWorkingDay,
        CASE WHEN @InitiateAsHrApproved = 1 THEN @RequestedLastWorkingDay ELSE NULL END,
        @IsNoticeWaived, @Shortfall,
        @EmployeeComments, @HrComments, @InitStatus,
        CASE WHEN @InitiateAsHrApproved = 1 THEN @UserId ELSE NULL END,
        CASE WHEN @InitiateAsHrApproved = 1 THEN SYSUTCDATETIME() ELSE NULL END,
        @UserId
    );
    SET @SavedId = SCOPE_IDENTITY();

    -- Seed Exit Clearance Checklist from active company templates
    INSERT INTO dbo.tbl_ExitClearanceItems (CompanyId, SeparationId, DepartmentScope, ItemName, IsMandatory, ClearanceStatus, CreatedBy)
    SELECT @CompanyId, @SavedId, DepartmentScope, ItemName, IsMandatory, N'Pending', @UserId
    FROM dbo.tbl_ExitClearanceTemplates
    WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1
    ORDER BY DisplayOrder, TemplateId;

    COMMIT TRAN;
END
GO

-- 4. Review / Approve / Reject / Withdraw Separation Request
CREATE OR ALTER PROCEDURE dbo.sp_SeparationRequest_Review
    @CompanyId              INT,
    @SeparationId           INT,
    @Action                 NVARCHAR(25), -- ManagerApprove, HrApprove, Reject, Withdraw
    @ApprovedLastWorkingDay DATE = NULL,
    @IsNoticeWaived         BIT = 0,
    @IsRehireEligible       BIT = 1,
    @Comments               NVARCHAR(600) = NULL,
    @UserId                 INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @CurrStatus NVARCHAR(25), @SubDate DATE, @NoticeDays INT, @ReqLwd DATE;
    SELECT @CurrStatus = SeparationStatus, @SubDate = SubmissionDate, @NoticeDays = NoticePeriodDays, @ReqLwd = RequestedLastWorkingDay
    FROM dbo.tbl_SeparationRequests WITH (UPDLOCK, HOLDLOCK)
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    IF @CurrStatus IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50164, N'Separation request not found.', 1;
    END

    IF @CurrStatus IN (N'Completed', N'Rejected', N'Withdrawn')
    BEGIN
        ROLLBACK TRAN;
        THROW 50165, N'This separation request is already finalized or closed.', 1;
    END

    DECLARE @EffLwd DATE = ISNULL(@ApprovedLastWorkingDay, @ReqLwd);
    IF @EffLwd < @SubDate
    BEGIN
        ROLLBACK TRAN;
        THROW 50163, N'Approved last working day cannot be earlier than submission date.', 1;
    END

    DECLARE @ActualNotice INT = DATEDIFF(DAY, @SubDate, @EffLwd);
    DECLARE @Shortfall INT = CASE WHEN @IsNoticeWaived = 1 OR @ActualNotice >= @NoticeDays THEN 0 ELSE (@NoticeDays - @ActualNotice) END;

    IF @Action = N'ManagerApprove'
    BEGIN
        IF @CurrStatus <> N'Submitted'
        BEGIN
            ROLLBACK TRAN;
            THROW 50166, N'Only Submitted requests can be manager-approved.', 1;
        END

        UPDATE dbo.tbl_SeparationRequests
        SET SeparationStatus = N'ManagerApproved',
            ApprovedLastWorkingDay = @EffLwd,
            ManagerComments = @Comments,
            ManagerApprovedBy = @UserId,
            ManagerApprovedDate = SYSUTCDATETIME(),
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;
    END
    ELSE IF @Action = N'HrApprove'
    BEGIN
        IF @CurrStatus NOT IN (N'Submitted', N'ManagerApproved', N'HrApproved')
        BEGIN
            ROLLBACK TRAN;
            THROW 50167, N'Request is not in a valid state for HR approval.', 1;
        END

        UPDATE dbo.tbl_SeparationRequests
        SET SeparationStatus = N'ClearanceInProgress',
            ApprovedLastWorkingDay = @EffLwd,
            IsNoticeWaived = @IsNoticeWaived,
            NoticeShortfallDays = @Shortfall,
            IsRehireEligible = @IsRehireEligible,
            HrComments = @Comments,
            HrApprovedBy = @UserId,
            HrApprovedDate = SYSUTCDATETIME(),
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;
    END
    ELSE IF @Action = N'Reject'
    BEGIN
        UPDATE dbo.tbl_SeparationRequests
        SET SeparationStatus = N'Rejected',
            HrComments = ISNULL(@Comments, HrComments),
            ManagerComments = CASE WHEN @CurrStatus = N'Submitted' THEN ISNULL(@Comments, ManagerComments) ELSE ManagerComments END,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;
    END
    ELSE IF @Action = N'Withdraw'
    BEGIN
        IF @CurrStatus NOT IN (N'Submitted', N'ManagerApproved')
        BEGIN
            ROLLBACK TRAN;
            THROW 50168, N'Cannot withdraw separation once clearance has started.', 1;
        END

        UPDATE dbo.tbl_SeparationRequests
        SET SeparationStatus = N'Withdrawn',
            EmployeeComments = COALESCE(@Comments, EmployeeComments),
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        THROW 50169, N'Unsupported review action.', 1;
    END

    COMMIT TRAN;
END
GO

-- 5. Complete / Finalize Exit (Deactivates Employee & Login Account + Records Employment History)
CREATE OR ALTER PROCEDURE dbo.sp_SeparationRequest_FinalizeExit
    @CompanyId    INT,
    @SeparationId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @EmployeeId INT, @SepType NVARCHAR(30), @SepStatus NVARCHAR(25), @SepNumber NVARCHAR(30), @EffLwd DATE;
    SELECT
        @EmployeeId = EmployeeId,
        @SepType = SeparationType,
        @SepStatus = SeparationStatus,
        @SepNumber = SeparationNumber,
        @EffLwd = ISNULL(ApprovedLastWorkingDay, RequestedLastWorkingDay)
    FROM dbo.tbl_SeparationRequests WITH (UPDLOCK, HOLDLOCK)
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    IF @EmployeeId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50164, N'Separation request not found.', 1;
    END

    IF @SepStatus IN (N'Completed', N'Rejected', N'Withdrawn', N'Submitted', N'ManagerApproved')
    BEGIN
        ROLLBACK TRAN;
        THROW 50170, N'Separation must pass HR approval and clearance before finalizing exit.', 1;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_ExitClearanceItems
        WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0
          AND IsMandatory = 1 AND ClearanceStatus NOT IN (N'Cleared', N'Waived')
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50171, N'All mandatory exit clearance items must be Cleared or Waived before completing exit.', 1;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_AssetAllocations
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND AllocationStatus = N'Active' AND IsDeleted = 0
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50172, N'Employee still holds active company assets. Process asset returns before completing exit.', 1;
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.tbl_FinalSettlements
        WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0 AND SettlementStatus IN (N'Approved', N'Paid')
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50173, N'Final Settlement must be Approved or Paid before finalizing exit.', 1;
    END

    DECLARE @NewEmpStatus TINYINT = CASE WHEN @SepType = N'Termination' THEN 5 ELSE 4 END; -- 4=Resigned, 5=Terminated
    DECLARE @BranchName NVARCHAR(120), @DeptName NVARCHAR(120), @DesigTitle NVARCHAR(120);
    SELECT TOP 1
        @BranchName = b.BranchName,
        @DeptName = d.DepartmentName,
        @DesigTitle = desig.DesignationTitle
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    WHERE e.EmployeeId = @EmployeeId AND e.CompanyId = @CompanyId;

    UPDATE dbo.tbl_SeparationRequests
    SET SeparationStatus = N'Completed',
        ApprovedLastWorkingDay = @EffLwd,
        CompletedBy = @UserId,
        CompletedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;

    UPDATE dbo.tbl_Employees
    SET ExitDate = @EffLwd,
        EmploymentStatus = @NewEmpStatus,
        IsActive = 0,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId;

    -- Deactivate linked portal login account
    UPDATE dbo.tbl_Users
    SET IsActive = 0,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0;

    INSERT INTO dbo.tbl_EmployeeEmploymentHistory (
        CompanyId, EmployeeId, EventType, EffectiveDate,
        FromBranchName, ToBranchName, FromDepartmentName, ToDepartmentName, FromDesignation, ToDesignation,
        Remarks, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, N'StatusChange', @EffLwd,
        @BranchName, @BranchName, @DeptName, @DeptName, @DesigTitle, @DesigTitle,
        CONCAT(N'Separated via ', @SepType, N' (', @SepNumber, N').'), @UserId
    );

    COMMIT TRAN;
END
GO

-- 6. Clearance Templates & Item Updates
CREATE OR ALTER PROCEDURE dbo.sp_ExitClearance_GetTemplates
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TemplateId, CompanyId, DepartmentScope, ItemName, Description, IsMandatory, DisplayOrder, IsActive
    FROM dbo.tbl_ExitClearanceTemplates
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY DisplayOrder, DepartmentScope, ItemName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExitClearance_SaveTemplate
    @TemplateId      INT,
    @CompanyId       INT,
    @DepartmentScope NVARCHAR(40),
    @ItemName        NVARCHAR(150),
    @Description     NVARCHAR(300) = NULL,
    @IsMandatory     BIT,
    @DisplayOrder    INT,
    @IsActive        BIT,
    @UserId          INT,
    @SavedId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @TemplateId = 0
    BEGIN
        INSERT INTO dbo.tbl_ExitClearanceTemplates (CompanyId, DepartmentScope, ItemName, Description, IsMandatory, DisplayOrder, IsActive, CreatedBy)
        VALUES (@CompanyId, @DepartmentScope, @ItemName, @Description, @IsMandatory, @DisplayOrder, @IsActive, @UserId);
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_ExitClearanceTemplates
        SET DepartmentScope = @DepartmentScope,
            ItemName = @ItemName,
            Description = @Description,
            IsMandatory = @IsMandatory,
            DisplayOrder = @DisplayOrder,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50174, N'Clearance template not found.', 1;
        SET @SavedId = @TemplateId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExitClearance_DeleteTemplate
    @CompanyId  INT,
    @TemplateId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_ExitClearanceTemplates
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE TemplateId = @TemplateId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExitClearance_UpdateItem
    @CompanyId       INT,
    @ClearanceItemId INT,
    @ClearanceStatus NVARCHAR(20),
    @RecoveryAmount  DECIMAL(18,2),
    @Remarks         NVARCHAR(400) = NULL,
    @UserId          INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @SeparationId INT;
    SELECT @SeparationId = SeparationId
    FROM dbo.tbl_ExitClearanceItems
    WHERE CompanyId = @CompanyId AND ClearanceItemId = @ClearanceItemId AND IsDeleted = 0;

    IF @SeparationId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50175, N'Clearance checklist item not found.', 1;
    END

    UPDATE dbo.tbl_ExitClearanceItems
    SET ClearanceStatus = @ClearanceStatus,
        RecoveryAmount = @RecoveryAmount,
        Remarks = @Remarks,
        ClearedBy = CASE WHEN @ClearanceStatus IN (N'Cleared', N'Waived') THEN @UserId ELSE NULL END,
        ClearedDate = CASE WHEN @ClearanceStatus IN (N'Cleared', N'Waived') THEN SYSUTCDATETIME() ELSE NULL END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ClearanceItemId = @ClearanceItemId AND CompanyId = @CompanyId;

    UPDATE dbo.tbl_SeparationRequests
    SET SeparationStatus = CASE WHEN SeparationStatus IN (N'Submitted', N'ManagerApproved', N'HrApproved') THEN N'ClearanceInProgress' ELSE SeparationStatus END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO

-- 7. Exit Interviews
CREATE OR ALTER PROCEDURE dbo.sp_ExitInterview_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ei.ExitInterviewId,
        ei.CompanyId,
        ei.SeparationId,
        ei.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        sr.SeparationNumber,
        sr.SeparationType,
        ei.InterviewDate,
        ei.OverallSatisfactionRating,
        ei.ManagerSupportRating,
        ei.WorkLifeBalanceRating,
        ei.CompensationRating,
        ei.WouldRecommendCompany,
        ei.WouldConsiderRejoining,
        ei.PrimaryLeavingReason,
        ei.LikedMost,
        ei.ImprovementSuggestions,
        ei.InterviewerNotes,
        ei.IsConfidential
    FROM dbo.tbl_ExitInterviews ei
    INNER JOIN dbo.tbl_Employees e ON ei.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_SeparationRequests sr ON ei.SeparationId = sr.SeparationId
    WHERE ei.CompanyId = @CompanyId AND sr.IsDeleted = 0
    ORDER BY ei.InterviewDate DESC, ei.ExitInterviewId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ExitInterview_Save
    @CompanyId                 INT,
    @SeparationId              INT,
    @InterviewDate             DATE,
    @OverallSatisfactionRating INT,
    @ManagerSupportRating      INT,
    @WorkLifeBalanceRating     INT,
    @CompensationRating        INT,
    @WouldRecommendCompany     BIT,
    @WouldConsiderRejoining    BIT,
    @PrimaryLeavingReason      NVARCHAR(200),
    @LikedMost                 NVARCHAR(500) = NULL,
    @ImprovementSuggestions    NVARCHAR(500) = NULL,
    @InterviewerNotes          NVARCHAR(500) = NULL,
    @IsConfidential            BIT,
    @UserId                    INT,
    @SavedId                   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EmployeeId INT;
    SELECT TOP 1 @EmployeeId = EmployeeId
    FROM dbo.tbl_SeparationRequests
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    IF @EmployeeId IS NULL THROW 50164, N'Separation request not found.', 1;

    DECLARE @ExistingId INT = (SELECT TOP 1 ExitInterviewId FROM dbo.tbl_ExitInterviews WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId);

    IF @ExistingId IS NULL
    BEGIN
        INSERT INTO dbo.tbl_ExitInterviews (
            CompanyId, SeparationId, EmployeeId, InterviewDate, ConductedByUserId,
            OverallSatisfactionRating, ManagerSupportRating, WorkLifeBalanceRating, CompensationRating,
            WouldRecommendCompany, WouldConsiderRejoining, PrimaryLeavingReason, LikedMost,
            ImprovementSuggestions, InterviewerNotes, IsConfidential, CreatedBy
        )
        VALUES (
            @CompanyId, @SeparationId, @EmployeeId, @InterviewDate, @UserId,
            @OverallSatisfactionRating, @ManagerSupportRating, @WorkLifeBalanceRating, @CompensationRating,
            @WouldRecommendCompany, @WouldConsiderRejoining, @PrimaryLeavingReason, @LikedMost,
            @ImprovementSuggestions, @InterviewerNotes, @IsConfidential, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_ExitInterviews
        SET InterviewDate = @InterviewDate,
            ConductedByUserId = @UserId,
            OverallSatisfactionRating = @OverallSatisfactionRating,
            ManagerSupportRating = @ManagerSupportRating,
            WorkLifeBalanceRating = @WorkLifeBalanceRating,
            CompensationRating = @CompensationRating,
            WouldRecommendCompany = @WouldRecommendCompany,
            WouldConsiderRejoining = @WouldConsiderRejoining,
            PrimaryLeavingReason = @PrimaryLeavingReason,
            LikedMost = @LikedMost,
            ImprovementSuggestions = @ImprovementSuggestions,
            InterviewerNotes = @InterviewerNotes,
            IsConfidential = @IsConfidential,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ExitInterviewId = @ExistingId AND CompanyId = @CompanyId;
        SET @SavedId = @ExistingId;
    END
END
GO

-- 8. Full & Final Settlement Calculation & Payout Status
CREATE OR ALTER PROCEDURE dbo.sp_FinalSettlement_CalculateAndSave
    @CompanyId               INT,
    @SeparationId            INT,
    @WorkedDaysInFinalMonth  INT = NULL,
    @EncashableLeaveDays     DECIMAL(5,2) = NULL,
    @GratuityOrSeverance     DECIMAL(18,2) = 0,
    @BonusOrOtherEarnings    DECIMAL(18,2) = 0,
    @OutstandingLoanRecovery DECIMAL(18,2) = NULL,
    @AssetOrNoticeRecovery   DECIMAL(18,2) = NULL,
    @TaxAndOtherDeductions   DECIMAL(18,2) = 0,
    @Notes                   NVARCHAR(500) = NULL,
    @UserId                  INT,
    @SavedId                 INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @EmployeeId INT, @SepStatus NVARCHAR(25), @Lwd DATE, @Shortfall INT;
    SELECT
        @EmployeeId = EmployeeId,
        @SepStatus = SeparationStatus,
        @Lwd = ISNULL(ApprovedLastWorkingDay, RequestedLastWorkingDay),
        @Shortfall = NoticeShortfallDays
    FROM dbo.tbl_SeparationRequests
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    IF @EmployeeId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50164, N'Separation request not found.', 1;
    END

    IF @SepStatus IN (N'Rejected', N'Withdrawn')
    BEGIN
        ROLLBACK TRAN;
        THROW 50176, N'Cannot calculate settlement for a rejected or withdrawn separation.', 1;
    END

    DECLARE @ExistingId INT, @ExistingStatus NVARCHAR(20);
    SELECT TOP 1 @ExistingId = SettlementId, @ExistingStatus = SettlementStatus
    FROM dbo.tbl_FinalSettlements
    WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0;

    IF @ExistingStatus = N'Paid'
    BEGIN
        ROLLBACK TRAN;
        THROW 50177, N'Settlement is already marked Paid and locked.', 1;
    END

    DECLARE @AnnualBase DECIMAL(18,2) = ISNULL((SELECT TOP 1 BaseSalary FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId), 0);
    DECLARE @MonthlyBase DECIMAL(18,2) = ROUND(@AnnualBase / 12.0, 2);
    DECLARE @DaysInMonth INT = DAY(EOMONTH(@Lwd));
    DECLARE @WorkedDays INT = COALESCE(@WorkedDaysInFinalMonth, DAY(@Lwd));
    IF @WorkedDays < 0 SET @WorkedDays = 0;
    IF @WorkedDays > @DaysInMonth SET @WorkedDays = @DaysInMonth;

    DECLARE @ProratedPay DECIMAL(18,2) = ROUND((@MonthlyBase / NULLIF(@DaysInMonth, 0)) * @WorkedDays, 2);

    DECLARE @AutoLeaveDays DECIMAL(5,2) = ISNULL((
        SELECT SUM(CASE WHEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays) > 0
                        THEN (lb.OpeningBalance + lb.AccruedDays + lb.CarryForwardDays - lb.UsedDays - lb.PendingDays - lb.EncashedDays)
                        ELSE 0 END)
        FROM dbo.tbl_LeaveBalances lb
        INNER JOIN dbo.tbl_LeaveTypes lt ON lb.LeaveTypeId = lt.LeaveTypeId
        WHERE lb.CompanyId = @CompanyId AND lb.EmployeeId = @EmployeeId AND lb.LeaveYear = YEAR(@Lwd)
          AND lt.AllowEncashment = 1 AND lt.IsDeleted = 0
    ), 0);

    DECLARE @EffLeaveDays DECIMAL(5,2) = COALESCE(@EncashableLeaveDays, @AutoLeaveDays);
    DECLARE @LeaveAmt DECIMAL(18,2) = ROUND((@MonthlyBase / 22.0) * @EffLeaveDays, 2);

    DECLARE @AutoLoanBal DECIMAL(18,2) = ISNULL((
        SELECT SUM(RemainingBalance) FROM dbo.tbl_EmployeeLoans
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND LoanStatus = N'Active' AND IsDeleted = 0
    ), 0);
    DECLARE @EffLoanRec DECIMAL(18,2) = COALESCE(@OutstandingLoanRecovery, @AutoLoanBal);

    DECLARE @ClearanceRec DECIMAL(18,2) = ISNULL((
        SELECT SUM(RecoveryAmount) FROM dbo.tbl_ExitClearanceItems
        WHERE CompanyId = @CompanyId AND SeparationId = @SeparationId AND IsDeleted = 0
    ), 0);
    DECLARE @NoticeRec DECIMAL(18,2) = ROUND((@MonthlyBase / 30.0) * @Shortfall, 2);
    DECLARE @EffAssetNoticeRec DECIMAL(18,2) = COALESCE(@AssetOrNoticeRecovery, @ClearanceRec + @NoticeRec);

    DECLARE @Gross DECIMAL(18,2) = @ProratedPay + @LeaveAmt + @GratuityOrSeverance + @BonusOrOtherEarnings;
    DECLARE @TotDed DECIMAL(18,2) = @EffLoanRec + @EffAssetNoticeRec + @TaxAndOtherDeductions;
    DECLARE @Net DECIMAL(18,2) = @Gross - @TotDed;

    IF @ExistingId IS NULL
    BEGIN
        DECLARE @NextSeq INT = ISNULL((SELECT MAX(SettlementId) FROM dbo.tbl_FinalSettlements WHERE CompanyId = @CompanyId), 0) + 1;
        DECLARE @SetNum NVARCHAR(30) = CONCAT(N'FFS-', YEAR(SYSUTCDATETIME()), N'-', RIGHT(CONCAT(N'0000', @NextSeq), 4));

        INSERT INTO dbo.tbl_FinalSettlements (
            CompanyId, SeparationId, EmployeeId, SettlementNumber, MonthlyBaseSalary,
            WorkedDaysInFinalMonth, ProratedSalaryPay, EncashableLeaveDays, LeaveEncashmentAmount,
            GratuityOrSeverance, BonusOrOtherEarnings, OutstandingLoanRecovery, AssetOrNoticeRecovery,
            TaxAndOtherDeductions, GrossEarnings, TotalDeductions, NetPayableAmount,
            SettlementStatus, Notes, CreatedBy
        )
        VALUES (
            @CompanyId, @SeparationId, @EmployeeId, @SetNum, @MonthlyBase,
            @WorkedDays, @ProratedPay, @EffLeaveDays, @LeaveAmt,
            @GratuityOrSeverance, @BonusOrOtherEarnings, @EffLoanRec, @EffAssetNoticeRec,
            @TaxAndOtherDeductions, @Gross, @TotDed, @Net,
            N'Draft', @Notes, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_FinalSettlements
        SET MonthlyBaseSalary = @MonthlyBase,
            WorkedDaysInFinalMonth = @WorkedDays,
            ProratedSalaryPay = @ProratedPay,
            EncashableLeaveDays = @EffLeaveDays,
            LeaveEncashmentAmount = @LeaveAmt,
            GratuityOrSeverance = @GratuityOrSeverance,
            BonusOrOtherEarnings = @BonusOrOtherEarnings,
            OutstandingLoanRecovery = @EffLoanRec,
            AssetOrNoticeRecovery = @EffAssetNoticeRec,
            TaxAndOtherDeductions = @TaxAndOtherDeductions,
            GrossEarnings = @Gross,
            TotalDeductions = @TotDed,
            NetPayableAmount = @Net,
            Notes = @Notes,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SettlementId = @ExistingId AND CompanyId = @CompanyId;
        SET @SavedId = @ExistingId;
    END

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_FinalSettlement_UpdateStatus
    @CompanyId        INT,
    @SettlementId     INT,
    @SettlementStatus NVARCHAR(20), -- Approved, Paid
    @PaymentReference NVARCHAR(80) = NULL,
    @PaidDate         DATE = NULL,
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @SeparationId INT, @CurrStatus NVARCHAR(20);
    SELECT @SeparationId = SeparationId, @CurrStatus = SettlementStatus
    FROM dbo.tbl_FinalSettlements WITH (UPDLOCK, HOLDLOCK)
    WHERE CompanyId = @CompanyId AND SettlementId = @SettlementId AND IsDeleted = 0;

    IF @SeparationId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50178, N'Final settlement record not found.', 1;
    END

    IF @SettlementStatus = N'Approved' AND @CurrStatus <> N'Draft'
    BEGIN
        ROLLBACK TRAN;
        THROW 50179, N'Only Draft settlements can be approved.', 1;
    END

    IF @SettlementStatus = N'Paid' AND @CurrStatus <> N'Approved'
    BEGIN
        ROLLBACK TRAN;
        THROW 50180, N'Settlement must be Approved before marking as Paid.', 1;
    END

    UPDATE dbo.tbl_FinalSettlements
    SET SettlementStatus = @SettlementStatus,
        PaymentReference = CASE WHEN @SettlementStatus = N'Paid' THEN @PaymentReference ELSE PaymentReference END,
        PaidDate = CASE WHEN @SettlementStatus = N'Paid' THEN ISNULL(@PaidDate, CAST(SYSUTCDATETIME() AS DATE)) ELSE PaidDate END,
        ApprovedBy = CASE WHEN @SettlementStatus = N'Approved' THEN @UserId ELSE ApprovedBy END,
        ApprovedDate = CASE WHEN @SettlementStatus = N'Approved' THEN SYSUTCDATETIME() ELSE ApprovedDate END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE SettlementId = @SettlementId AND CompanyId = @CompanyId;

    IF @SettlementStatus = N'Paid'
    BEGIN
        UPDATE dbo.tbl_SeparationRequests
        SET SeparationStatus = CASE WHEN SeparationStatus = N'ClearanceInProgress' THEN N'Settled' ELSE SeparationStatus END,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE SeparationId = @SeparationId AND CompanyId = @CompanyId;
    END

    COMMIT TRAN;
END
GO
