USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 9: Training Management Stored Procedures
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.sp_TrainingProgram_GetAll
    @CompanyId  INT,
    @Category   NVARCHAR(50) = NULL,
    @Status     NVARCHAR(20) = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        tp.ProgramId,
        tp.CompanyId,
        tp.ProgramCode,
        tp.ProgramTitle,
        tp.Category,
        tp.DeliveryMode,
        tp.DurationHours,
        tp.TrainerOrProvider,
        tp.StartDate,
        tp.EndDate,
        tp.MaxParticipants,
        tp.LocationOrUrl,
        tp.IsMandatory,
        tp.ProgramStatus,
        tp.Description,
        (SELECT COUNT(1) FROM dbo.tbl_TrainingEnrollments te WHERE te.ProgramId = tp.ProgramId AND te.IsDeleted = 0 AND te.AttendanceStatus <> N'Dropped') AS EnrolledCount,
        (SELECT COUNT(1) FROM dbo.tbl_TrainingEnrollments te WHERE te.ProgramId = tp.ProgramId AND te.IsDeleted = 0 AND te.AttendanceStatus = N'Completed') AS CompletedCount
    FROM dbo.tbl_TrainingPrograms tp
    WHERE tp.CompanyId = @CompanyId
      AND tp.IsDeleted = 0
      AND (@Category IS NULL OR tp.Category = @Category)
      AND (@Status IS NULL OR tp.ProgramStatus = @Status)
      AND (@SearchTerm IS NULL OR tp.ProgramTitle LIKE N'%' + @SearchTerm + N'%' OR tp.ProgramCode LIKE N'%' + @SearchTerm + N'%' OR tp.TrainerOrProvider LIKE N'%' + @SearchTerm + N'%')
    ORDER BY tp.StartDate DESC, tp.ProgramId DESC;

    -- Result set 2: KPI Summary
    DECLARE @TotalProg INT = (SELECT COUNT(1) FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId AND IsDeleted = 0);
    DECLARE @ActiveProg INT = (SELECT COUNT(1) FROM dbo.tbl_TrainingPrograms WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND ProgramStatus IN (N'Planned', N'Ongoing'));
    DECLARE @TotalEnr INT = (SELECT COUNT(1) FROM dbo.tbl_TrainingEnrollments WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND AttendanceStatus <> N'Dropped');
    DECLARE @CompEnr INT = (SELECT COUNT(1) FROM dbo.tbl_TrainingEnrollments WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND AttendanceStatus = N'Completed');

    SELECT
        @TotalProg AS TotalPrograms,
        @ActiveProg AS ActiveOrPlannedPrograms,
        @TotalEnr AS TotalEnrollments,
        @CompEnr AS CompletedEnrollments,
        CASE WHEN @TotalEnr > 0 THEN ROUND((CAST(@CompEnr AS DECIMAL(7,2)) / @TotalEnr) * 100.0, 1) ELSE 0 END AS CompletionRatePercent;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingProgram_Save
    @ProgramId         INT,
    @CompanyId         INT,
    @ProgramCode       NVARCHAR(25),
    @ProgramTitle      NVARCHAR(150),
    @Category          NVARCHAR(50),
    @DeliveryMode      NVARCHAR(30),
    @DurationHours     DECIMAL(5,1),
    @TrainerOrProvider NVARCHAR(150) = NULL,
    @StartDate         DATE,
    @EndDate           DATE,
    @MaxParticipants   INT,
    @LocationOrUrl     NVARCHAR(250) = NULL,
    @IsMandatory       BIT,
    @ProgramStatus     NVARCHAR(20),
    @Description       NVARCHAR(600) = NULL,
    @UserId            INT,
    @SavedId           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_TrainingPrograms
        WHERE CompanyId = @CompanyId AND ProgramCode = @ProgramCode AND ProgramId <> @ProgramId AND IsDeleted = 0
    )
    BEGIN
        THROW 50121, N'A training program with this code already exists.', 1;
    END

    IF @ProgramId = 0
    BEGIN
        INSERT INTO dbo.tbl_TrainingPrograms (
            CompanyId, ProgramCode, ProgramTitle, Category, DeliveryMode, DurationHours,
            TrainerOrProvider, StartDate, EndDate, MaxParticipants, LocationOrUrl,
            IsMandatory, ProgramStatus, Description, CreatedBy
        )
        VALUES (
            @CompanyId, @ProgramCode, @ProgramTitle, @Category, @DeliveryMode, @DurationHours,
            @TrainerOrProvider, @StartDate, @EndDate, @MaxParticipants, @LocationOrUrl,
            @IsMandatory, @ProgramStatus, @Description, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_TrainingPrograms
        SET ProgramCode = @ProgramCode,
            ProgramTitle = @ProgramTitle,
            Category = @Category,
            DeliveryMode = @DeliveryMode,
            DurationHours = @DurationHours,
            TrainerOrProvider = @TrainerOrProvider,
            StartDate = @StartDate,
            EndDate = @EndDate,
            MaxParticipants = @MaxParticipants,
            LocationOrUrl = @LocationOrUrl,
            IsMandatory = @IsMandatory,
            ProgramStatus = @ProgramStatus,
            Description = @Description,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ProgramId = @ProgramId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50122, N'Training program not found.', 1;
        SET @SavedId = @ProgramId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingProgram_Delete
    @CompanyId INT,
    @ProgramId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_TrainingEnrollments WHERE CompanyId = @CompanyId AND ProgramId = @ProgramId AND IsDeleted = 0)
    BEGIN
        THROW 50123, N'Cannot delete training program with enrolled employees. Cancel it instead.', 1;
    END

    UPDATE dbo.tbl_TrainingPrograms
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE ProgramId = @ProgramId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingEnrollment_GetAll
    @CompanyId  INT,
    @ProgramId  INT = NULL,
    @EmployeeId INT = NULL,
    @Status     NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        te.EnrollmentId,
        te.CompanyId,
        te.ProgramId,
        tp.ProgramCode,
        tp.ProgramTitle,
        tp.Category,
        tp.StartDate,
        tp.EndDate,
        te.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        te.EnrollmentDate,
        te.AttendanceStatus,
        te.AssessmentScore,
        te.CertificateNumber,
        te.CompletionDate,
        te.FeedbackRating,
        te.FeedbackNotes
    FROM dbo.tbl_TrainingEnrollments te
    INNER JOIN dbo.tbl_TrainingPrograms tp ON te.ProgramId = tp.ProgramId
    INNER JOIN dbo.tbl_Employees e ON te.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE te.CompanyId = @CompanyId
      AND te.IsDeleted = 0
      AND tp.IsDeleted = 0
      AND (@ProgramId IS NULL OR te.ProgramId = @ProgramId)
      AND (@EmployeeId IS NULL OR te.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR te.AttendanceStatus = @Status)
    ORDER BY tp.StartDate DESC, e.FullName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingEnrollment_Enroll
    @CompanyId  INT,
    @ProgramId  INT,
    @EmployeeId INT,
    @UserId     INT,
    @NewId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaxPart INT, @ProgStatus NVARCHAR(20);
    SELECT @MaxPart = MaxParticipants, @ProgStatus = ProgramStatus
    FROM dbo.tbl_TrainingPrograms
    WHERE ProgramId = @ProgramId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @ProgStatus IS NULL THROW 50124, N'Training program not found.', 1;
    IF @ProgStatus = N'Cancelled' THROW 50125, N'Cannot enroll employees in a cancelled training program.', 1;

    DECLARE @CurrentEnrolled INT = (
        SELECT COUNT(1) FROM dbo.tbl_TrainingEnrollments
        WHERE ProgramId = @ProgramId AND CompanyId = @CompanyId AND IsDeleted = 0 AND AttendanceStatus <> N'Dropped'
    );

    IF @CurrentEnrolled >= @MaxPart
    BEGIN
        THROW 50126, N'Training program has reached its maximum seat capacity.', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.tbl_TrainingEnrollments WHERE ProgramId = @ProgramId AND EmployeeId = @EmployeeId AND IsDeleted = 0)
    BEGIN
        THROW 50127, N'Employee is already enrolled in this training program.', 1;
    END

    DECLARE @SoftId INT = (SELECT EnrollmentId FROM dbo.tbl_TrainingEnrollments WHERE ProgramId = @ProgramId AND EmployeeId = @EmployeeId AND IsDeleted = 1);
    IF @SoftId IS NOT NULL
    BEGIN
        UPDATE dbo.tbl_TrainingEnrollments
        SET IsDeleted = 0,
            EnrollmentDate = CAST(SYSUTCDATETIME() AS DATE),
            AttendanceStatus = N'Enrolled',
            AssessmentScore = NULL,
            CertificateNumber = NULL,
            CompletionDate = NULL,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE EnrollmentId = @SoftId;
        SET @NewId = @SoftId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tbl_TrainingEnrollments (CompanyId, ProgramId, EmployeeId, AttendanceStatus, CreatedBy)
        VALUES (@CompanyId, @ProgramId, @EmployeeId, N'Enrolled', @UserId);
        SET @NewId = SCOPE_IDENTITY();
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingEnrollment_UpdateOutcome
    @CompanyId         INT,
    @EnrollmentId      INT,
    @AttendanceStatus  NVARCHAR(20),
    @AssessmentScore   DECIMAL(5,2) = NULL,
    @CertificateNumber NVARCHAR(50) = NULL,
    @CompletionDate    DATE = NULL,
    @FeedbackRating    INT = NULL,
    @FeedbackNotes     NVARCHAR(400) = NULL,
    @UserId            INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_TrainingEnrollments
    SET AttendanceStatus = @AttendanceStatus,
        AssessmentScore = @AssessmentScore,
        CertificateNumber = @CertificateNumber,
        CompletionDate = CASE
            WHEN @AttendanceStatus = N'Completed' THEN ISNULL(@CompletionDate, CAST(SYSUTCDATETIME() AS DATE))
            ELSE @CompletionDate
        END,
        FeedbackRating = @FeedbackRating,
        FeedbackNotes = @FeedbackNotes,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EnrollmentId = @EnrollmentId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0 THROW 50128, N'Training enrollment record not found.', 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TrainingEnrollment_Delete
    @CompanyId    INT,
    @EnrollmentId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_TrainingEnrollments
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE EnrollmentId = @EnrollmentId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO
