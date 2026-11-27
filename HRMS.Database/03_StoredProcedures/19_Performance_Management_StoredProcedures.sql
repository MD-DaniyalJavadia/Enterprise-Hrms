USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 9: Performance Management Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_AppraisalCycle_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ac.CycleId,
        ac.CompanyId,
        ac.CycleCode,
        ac.CycleName,
        ac.ReviewYear,
        ac.StartDate,
        ac.EndDate,
        ac.SelfReviewDeadline,
        ac.ManagerReviewDeadline,
        ac.CycleStatus,
        ac.Description,
        (SELECT COUNT(1) FROM dbo.tbl_PerformanceAppraisals pa WHERE pa.CycleId = ac.CycleId AND pa.IsDeleted = 0) AS AppraisalsCount
    FROM dbo.tbl_AppraisalCycles ac
    WHERE ac.CompanyId = @CompanyId AND ac.IsDeleted = 0
    ORDER BY ac.ReviewYear DESC, ac.StartDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AppraisalCycle_Save
    @CycleId               INT,
    @CompanyId             INT,
    @CycleCode             NVARCHAR(20),
    @CycleName             NVARCHAR(120),
    @ReviewYear            INT,
    @StartDate             DATE,
    @EndDate               DATE,
    @SelfReviewDeadline    DATE = NULL,
    @ManagerReviewDeadline DATE = NULL,
    @CycleStatus           NVARCHAR(20),
    @Description           NVARCHAR(400) = NULL,
    @UserId                INT,
    @SavedId               INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_AppraisalCycles
        WHERE CompanyId = @CompanyId AND CycleCode = @CycleCode AND CycleId <> @CycleId AND IsDeleted = 0
    )
    BEGIN
        THROW 50111, N'An appraisal cycle with this code already exists.', 1;
    END

    IF @CycleId = 0
    BEGIN
        INSERT INTO dbo.tbl_AppraisalCycles (
            CompanyId, CycleCode, CycleName, ReviewYear, StartDate, EndDate,
            SelfReviewDeadline, ManagerReviewDeadline, CycleStatus, Description, CreatedBy
        )
        VALUES (
            @CompanyId, @CycleCode, @CycleName, @ReviewYear, @StartDate, @EndDate,
            @SelfReviewDeadline, @ManagerReviewDeadline, @CycleStatus, @Description, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_AppraisalCycles
        SET CycleCode = @CycleCode,
            CycleName = @CycleName,
            ReviewYear = @ReviewYear,
            StartDate = @StartDate,
            EndDate = @EndDate,
            SelfReviewDeadline = @SelfReviewDeadline,
            ManagerReviewDeadline = @ManagerReviewDeadline,
            CycleStatus = @CycleStatus,
            Description = @Description,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CycleId = @CycleId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50112, N'Appraisal cycle not found.', 1;
        SET @SavedId = @CycleId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AppraisalCycle_Delete
    @CompanyId INT,
    @CycleId   INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_PerformanceAppraisals WHERE CompanyId = @CompanyId AND CycleId = @CycleId AND IsDeleted = 0)
    BEGIN
        THROW 50113, N'Cannot delete an appraisal cycle that contains review records.', 1;
    END

    UPDATE dbo.tbl_AppraisalCycles
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CycleId = @CycleId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceGoal_GetAll
    @CompanyId  INT,
    @CycleId    INT = NULL,
    @EmployeeId INT = NULL,
    @Status     NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.GoalId,
        g.CompanyId,
        g.CycleId,
        ac.CycleCode,
        ac.CycleName,
        g.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        g.GoalTitle,
        g.KpiMetric,
        g.WeightPercent,
        g.TargetValue,
        g.AchievedValue,
        g.ProgressPercent,
        g.GoalStatus,
        g.ManagerNotes
    FROM dbo.tbl_PerformanceGoals g
    INNER JOIN dbo.tbl_AppraisalCycles ac ON g.CycleId = ac.CycleId
    INNER JOIN dbo.tbl_Employees e ON g.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE g.CompanyId = @CompanyId
      AND g.IsDeleted = 0
      AND (@CycleId IS NULL OR g.CycleId = @CycleId)
      AND (@EmployeeId IS NULL OR g.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR g.GoalStatus = @Status)
    ORDER BY ac.ReviewYear DESC, e.FullName, g.GoalId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceGoal_Save
    @GoalId          INT,
    @CompanyId       INT,
    @CycleId         INT,
    @EmployeeId      INT,
    @GoalTitle       NVARCHAR(150),
    @KpiMetric       NVARCHAR(200) = NULL,
    @WeightPercent   DECIMAL(5,2),
    @TargetValue     NVARCHAR(100) = NULL,
    @AchievedValue   NVARCHAR(100) = NULL,
    @ProgressPercent INT,
    @GoalStatus      NVARCHAR(20),
    @ManagerNotes    NVARCHAR(400) = NULL,
    @UserId          INT,
    @SavedId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OtherWeight DECIMAL(5,2) = ISNULL((
        SELECT SUM(WeightPercent)
        FROM dbo.tbl_PerformanceGoals
        WHERE CompanyId = @CompanyId AND CycleId = @CycleId AND EmployeeId = @EmployeeId
          AND GoalId <> @GoalId AND IsDeleted = 0
    ), 0);

    IF (@OtherWeight + @WeightPercent) > 100.00
    BEGIN
        THROW 50114, N'Total KPI weight for this employee and cycle cannot exceed 100%.', 1;
    END

    IF @GoalId = 0
    BEGIN
        INSERT INTO dbo.tbl_PerformanceGoals (
            CompanyId, CycleId, EmployeeId, GoalTitle, KpiMetric, WeightPercent,
            TargetValue, AchievedValue, ProgressPercent, GoalStatus, ManagerNotes, CreatedBy
        )
        VALUES (
            @CompanyId, @CycleId, @EmployeeId, @GoalTitle, @KpiMetric, @WeightPercent,
            @TargetValue, @AchievedValue, @ProgressPercent, @GoalStatus, @ManagerNotes, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_PerformanceGoals
        SET CycleId = @CycleId,
            EmployeeId = @EmployeeId,
            GoalTitle = @GoalTitle,
            KpiMetric = @KpiMetric,
            WeightPercent = @WeightPercent,
            TargetValue = @TargetValue,
            AchievedValue = @AchievedValue,
            ProgressPercent = @ProgressPercent,
            GoalStatus = @GoalStatus,
            ManagerNotes = @ManagerNotes,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE GoalId = @GoalId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50115, N'Performance goal not found.', 1;
        SET @SavedId = @GoalId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceGoal_UpdateProgress
    @CompanyId       INT,
    @GoalId          INT,
    @AchievedValue   NVARCHAR(100) = NULL,
    @ProgressPercent INT,
    @GoalStatus      NVARCHAR(20),
    @UserId          INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_PerformanceGoals
    SET AchievedValue = @AchievedValue,
        ProgressPercent = @ProgressPercent,
        GoalStatus = @GoalStatus,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE GoalId = @GoalId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0 THROW 50115, N'Performance goal not found.', 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceGoal_Delete
    @CompanyId INT,
    @GoalId    INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_PerformanceGoals
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE GoalId = @GoalId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceAppraisal_GetAll
    @CompanyId  INT,
    @CycleId    INT = NULL,
    @EmployeeId INT = NULL,
    @Status     NVARCHAR(25) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pa.AppraisalId,
        pa.CompanyId,
        pa.CycleId,
        ac.CycleCode,
        ac.CycleName,
        pa.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        desig.DesignationTitle,
        pa.ReviewerEmployeeId,
        rev.FullName AS ReviewerName,
        pa.SelfRating,
        pa.SelfComments,
        pa.SelfSubmittedDate,
        pa.ManagerRating,
        pa.FinalRating,
        pa.RatingBand,
        pa.Strengths,
        pa.ImprovementAreas,
        pa.ManagerComments,
        pa.RecommendedIncrementPercent,
        pa.AppraisalStatus,
        pa.CompletedDate
    FROM dbo.tbl_PerformanceAppraisals pa
    INNER JOIN dbo.tbl_AppraisalCycles ac ON pa.CycleId = ac.CycleId
    INNER JOIN dbo.tbl_Employees e ON pa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees rev ON pa.ReviewerEmployeeId = rev.EmployeeId
    WHERE pa.CompanyId = @CompanyId
      AND pa.IsDeleted = 0
      AND (@CycleId IS NULL OR pa.CycleId = @CycleId)
      AND (@EmployeeId IS NULL OR pa.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR pa.AppraisalStatus = @Status)
    ORDER BY ac.ReviewYear DESC, e.FullName;

    -- Result set 2: KPI Summary
    SELECT
        COUNT(1) AS TotalAppraisals,
        COUNT(CASE WHEN pa.AppraisalStatus = N'NotStarted' THEN 1 END) AS PendingSelfReview,
        COUNT(CASE WHEN pa.AppraisalStatus = N'SelfSubmitted' THEN 1 END) AS PendingManagerReview,
        COUNT(CASE WHEN pa.AppraisalStatus = N'Completed' THEN 1 END) AS CompletedAppraisals,
        ISNULL(AVG(CAST(pa.FinalRating AS DECIMAL(5,2))), 0) AS AverageFinalRating
    FROM dbo.tbl_PerformanceAppraisals pa
    WHERE pa.CompanyId = @CompanyId
      AND pa.IsDeleted = 0
      AND (@CycleId IS NULL OR pa.CycleId = @CycleId)
      AND (@EmployeeId IS NULL OR pa.EmployeeId = @EmployeeId);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceAppraisal_InitCycle
    @CompanyId INT,
    @CycleId   INT,
    @UserId    INT,
    @CreatedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_AppraisalCycles WHERE CycleId = @CycleId AND CompanyId = @CompanyId AND IsDeleted = 0)
    BEGIN
        THROW 50116, N'Appraisal cycle not found.', 1;
    END

    INSERT INTO dbo.tbl_PerformanceAppraisals (
        CompanyId, CycleId, EmployeeId, ReviewerEmployeeId, AppraisalStatus, CreatedBy
    )
    SELECT
        @CompanyId, @CycleId, e.EmployeeId, e.ReportingManagerId, N'NotStarted', @UserId
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM dbo.tbl_PerformanceAppraisals pa
          WHERE pa.CycleId = @CycleId AND pa.EmployeeId = e.EmployeeId AND pa.IsDeleted = 0
      );

    SET @CreatedCount = @@ROWCOUNT;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceAppraisal_SubmitSelf
    @CompanyId    INT,
    @AppraisalId  INT,
    @SelfRating   DECIMAL(3,2),
    @SelfComments NVARCHAR(1000) = NULL,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status NVARCHAR(25);
    SELECT @Status = AppraisalStatus
    FROM dbo.tbl_PerformanceAppraisals
    WHERE AppraisalId = @AppraisalId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @Status IS NULL THROW 50117, N'Appraisal record not found.', 1;
    IF @Status = N'Completed' THROW 50118, N'Completed appraisals cannot be modified.', 1;

    UPDATE dbo.tbl_PerformanceAppraisals
    SET SelfRating = @SelfRating,
        SelfComments = @SelfComments,
        SelfSubmittedDate = SYSUTCDATETIME(),
        AppraisalStatus = CASE WHEN AppraisalStatus = N'NotStarted' THEN N'SelfSubmitted' ELSE AppraisalStatus END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AppraisalId = @AppraisalId AND CompanyId = @CompanyId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerformanceAppraisal_SubmitManagerReview
    @CompanyId                   INT,
    @AppraisalId                 INT,
    @ManagerRating               DECIMAL(3,2),
    @FinalRating                 DECIMAL(3,2),
    @Strengths                   NVARCHAR(600) = NULL,
    @ImprovementAreas            NVARCHAR(600) = NULL,
    @ManagerComments             NVARCHAR(1000) = NULL,
    @RecommendedIncrementPercent DECIMAL(5,2) = NULL,
    @Finalize                    BIT = 1,
    @UserId                      INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_PerformanceAppraisals WHERE AppraisalId = @AppraisalId AND CompanyId = @CompanyId AND IsDeleted = 0)
    BEGIN
        THROW 50117, N'Appraisal record not found.', 1;
    END

    DECLARE @Band NVARCHAR(40) = CASE
        WHEN @FinalRating >= 4.50 THEN N'Outstanding'
        WHEN @FinalRating >= 3.75 THEN N'Exceeds Expectations'
        WHEN @FinalRating >= 2.75 THEN N'Meets Expectations'
        WHEN @FinalRating >= 1.75 THEN N'Needs Improvement'
        ELSE N'Unsatisfactory'
    END;

    UPDATE dbo.tbl_PerformanceAppraisals
    SET ManagerRating = @ManagerRating,
        FinalRating = @FinalRating,
        RatingBand = @Band,
        Strengths = @Strengths,
        ImprovementAreas = @ImprovementAreas,
        ManagerComments = @ManagerComments,
        RecommendedIncrementPercent = @RecommendedIncrementPercent,
        AppraisalStatus = CASE WHEN @Finalize = 1 THEN N'Completed' ELSE N'ManagerReviewed' END,
        CompletedDate = CASE WHEN @Finalize = 1 THEN SYSUTCDATETIME() ELSE NULL END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AppraisalId = @AppraisalId AND CompanyId = @CompanyId;
END
GO
