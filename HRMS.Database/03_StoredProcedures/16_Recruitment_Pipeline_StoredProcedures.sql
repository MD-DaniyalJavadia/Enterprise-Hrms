USE [HRMS_DB];
GO

-- ============================================================================
-- Recruitment Pipeline, Interviews, Offers & Employee Onboarding Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_JobApplication_GetPipeline
    @CompanyId     INT,
    @RequisitionId INT = NULL,
    @PipelineStage NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Applications
    SELECT
        ja.*,
        r.RequisitionCode,
        r.JobTitle,
        d.DepartmentName,
        c.FullName AS CandidateName,
        c.Email AS CandidateEmail,
        c.Phone AS CandidatePhone,
        c.TotalExperienceYears,
        c.CurrentJobTitle,
        c.ResumeStoredName
    FROM dbo.tbl_JobApplications ja
    INNER JOIN dbo.tbl_JobRequisitions r ON ja.RequisitionId = r.RequisitionId
    INNER JOIN dbo.tbl_Departments d ON r.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Candidates c ON ja.CandidateId = c.CandidateId
    WHERE ja.CompanyId = @CompanyId
      AND ja.IsDeleted = 0
      AND r.IsDeleted = 0
      AND c.IsDeleted = 0
      AND (@RequisitionId IS NULL OR ja.RequisitionId = @RequisitionId)
      AND (@PipelineStage IS NULL OR ja.PipelineStage = @PipelineStage)
    ORDER BY ja.ApplicationId DESC;

    -- 2. Stage Breakdown Counts
    SELECT
        SUM(CASE WHEN PipelineStage = N'Applied' THEN 1 ELSE 0 END) AS AppliedCount,
        SUM(CASE WHEN PipelineStage = N'Screening' THEN 1 ELSE 0 END) AS ScreeningCount,
        SUM(CASE WHEN PipelineStage = N'Interview' THEN 1 ELSE 0 END) AS InterviewCount,
        SUM(CASE WHEN PipelineStage = N'Assessment' THEN 1 ELSE 0 END) AS AssessmentCount,
        SUM(CASE WHEN PipelineStage = N'Offer' THEN 1 ELSE 0 END) AS OfferCount,
        SUM(CASE WHEN PipelineStage = N'Hired' THEN 1 ELSE 0 END) AS HiredCount,
        SUM(CASE WHEN PipelineStage IN (N'Rejected', N'Withdrawn') THEN 1 ELSE 0 END) AS ClosedCount
    FROM dbo.tbl_JobApplications
    WHERE CompanyId = @CompanyId
      AND IsDeleted = 0
      AND (@RequisitionId IS NULL OR RequisitionId = @RequisitionId);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobApplication_Submit
    @CompanyId     INT,
    @RequisitionId INT,
    @CandidateId   INT,
    @MatchScore    INT = NULL,
    @UserId        INT,
    @NewId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReqStatus NVARCHAR(30);
    SELECT @ReqStatus = RequisitionStatus
    FROM dbo.tbl_JobRequisitions
    WHERE RequisitionId = @RequisitionId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @ReqStatus IS NULL
        THROW 50611, 'Job requisition not found.', 1;
    IF @ReqStatus <> N'Open'
        THROW 50612, 'Applications can only be submitted to Open requisitions.', 1;

    DECLARE @ExistingId INT, @IsDeleted BIT;
    SELECT @ExistingId = ApplicationId, @IsDeleted = IsDeleted
    FROM dbo.tbl_JobApplications
    WHERE RequisitionId = @RequisitionId AND CandidateId = @CandidateId;

    IF @ExistingId IS NOT NULL AND @IsDeleted = 0
        THROW 50613, 'This candidate has already applied for the selected requisition.', 1;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE dbo.tbl_JobApplications
        SET PipelineStage = N'Applied',
            MatchScore = @MatchScore,
            AppliedDate = SYSUTCDATETIME(),
            StageUpdatedDate = SYSUTCDATETIME(),
            RejectionReason = NULL,
            IsDeleted = 0,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ApplicationId = @ExistingId;
        SET @NewId = @ExistingId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tbl_JobApplications (
            CompanyId, RequisitionId, CandidateId, PipelineStage, MatchScore, AppliedDate, StageUpdatedDate, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @RequisitionId, @CandidateId, N'Applied', @MatchScore, SYSUTCDATETIME(), SYSUTCDATETIME(), 0, @UserId, SYSUTCDATETIME()
        );
        SET @NewId = SCOPE_IDENTITY();
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobApplication_MoveStage
    @CompanyId       INT,
    @ApplicationId   INT,
    @PipelineStage   NVARCHAR(30),
    @RejectionReason NVARCHAR(300) = NULL,
    @UserId          INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStage NVARCHAR(30);
    SELECT @CurrentStage = PipelineStage
    FROM dbo.tbl_JobApplications
    WHERE ApplicationId = @ApplicationId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @CurrentStage IS NULL
        THROW 50614, 'Job application not found.', 1;
    IF @CurrentStage = N'Hired'
        THROW 50615, 'Hired applications cannot be moved to another stage.', 1;

    UPDATE dbo.tbl_JobApplications
    SET PipelineStage = @PipelineStage,
        RejectionReason = CASE WHEN @PipelineStage IN (N'Rejected', N'Withdrawn') THEN @RejectionReason ELSE NULL END,
        StageUpdatedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ApplicationId = @ApplicationId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Interviews Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Interview_GetAll
    @CompanyId       INT,
    @InterviewStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        iv.*,
        ja.RequisitionId,
        r.RequisitionCode,
        r.JobTitle,
        c.FullName AS CandidateName,
        c.Email AS CandidateEmail,
        emp.FullName AS InterviewerName
    FROM dbo.tbl_Interviews iv
    INNER JOIN dbo.tbl_JobApplications ja ON iv.ApplicationId = ja.ApplicationId
    INNER JOIN dbo.tbl_JobRequisitions r ON ja.RequisitionId = r.RequisitionId
    INNER JOIN dbo.tbl_Candidates c ON ja.CandidateId = c.CandidateId
    LEFT JOIN dbo.tbl_Employees emp ON iv.InterviewerEmployeeId = emp.EmployeeId
    WHERE iv.CompanyId = @CompanyId
      AND iv.IsDeleted = 0
      AND ja.IsDeleted = 0
      AND (@InterviewStatus IS NULL OR iv.InterviewStatus = @InterviewStatus)
    ORDER BY CASE WHEN iv.InterviewStatus = N'Scheduled' THEN 0 ELSE 1 END, iv.ScheduledStart ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Interview_Schedule
    @InterviewId           INT = 0,
    @CompanyId             INT,
    @ApplicationId         INT,
    @InterviewRound        NVARCHAR(50),
    @InterviewMode         NVARCHAR(30),
    @ScheduledStart        DATETIME2(0),
    @DurationMinutes       INT,
    @LocationOrLink        NVARCHAR(250) = NULL,
    @InterviewerEmployeeId INT = NULL,
    @UserId                INT,
    @SavedId               INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_JobApplications WHERE ApplicationId = @ApplicationId AND CompanyId = @CompanyId AND IsDeleted = 0)
        THROW 50616, 'Job application not found.', 1;

    BEGIN TRAN;

    IF @InterviewId = 0
    BEGIN
        INSERT INTO dbo.tbl_Interviews (
            CompanyId, ApplicationId, InterviewRound, InterviewMode, ScheduledStart,
            DurationMinutes, LocationOrLink, InterviewerEmployeeId, InterviewStatus, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @ApplicationId, @InterviewRound, @InterviewMode, @ScheduledStart,
            @DurationMinutes, @LocationOrLink, @InterviewerEmployeeId, N'Scheduled', 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_Interviews
        SET InterviewRound = @InterviewRound,
            InterviewMode = @InterviewMode,
            ScheduledStart = @ScheduledStart,
            DurationMinutes = @DurationMinutes,
            LocationOrLink = @LocationOrLink,
            InterviewerEmployeeId = @InterviewerEmployeeId,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE InterviewId = @InterviewId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @InterviewId;
    END

    -- Auto-advance application stage if still in Applied or Screening
    UPDATE dbo.tbl_JobApplications
    SET PipelineStage = N'Interview',
        StageUpdatedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ApplicationId = @ApplicationId AND CompanyId = @CompanyId AND PipelineStage IN (N'Applied', N'Screening');

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Interview_SubmitFeedback
    @CompanyId             INT,
    @InterviewId           INT,
    @InterviewStatus       NVARCHAR(20),
    @TechnicalScore        INT = NULL,
    @CommunicationScore    INT = NULL,
    @OverallRecommendation NVARCHAR(30) = NULL,
    @FeedbackNotes         NVARCHAR(1000) = NULL,
    @UserId                INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Interviews
    SET InterviewStatus = @InterviewStatus,
        TechnicalScore = @TechnicalScore,
        CommunicationScore = @CommunicationScore,
        OverallRecommendation = @OverallRecommendation,
        FeedbackNotes = @FeedbackNotes,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE InterviewId = @InterviewId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- ============================================================================
-- Job Offers & One-Click Convert to Employee
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_JobOffer_GetAll
    @CompanyId   INT,
    @OfferStatus NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jo.*,
        ja.RequisitionId,
        ja.CandidateId,
        ja.HiredEmployeeId,
        r.RequisitionCode,
        r.JobTitle,
        d.DepartmentName,
        b.BranchName,
        c.FullName AS CandidateName,
        c.Email AS CandidateEmail
    FROM dbo.tbl_JobOffers jo
    INNER JOIN dbo.tbl_JobApplications ja ON jo.ApplicationId = ja.ApplicationId
    INNER JOIN dbo.tbl_JobRequisitions r ON ja.RequisitionId = r.RequisitionId
    INNER JOIN dbo.tbl_Departments d ON r.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Branches b ON r.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Candidates c ON ja.CandidateId = c.CandidateId
    WHERE jo.CompanyId = @CompanyId
      AND jo.IsDeleted = 0
      AND ja.IsDeleted = 0
      AND (@OfferStatus IS NULL OR jo.OfferStatus = @OfferStatus)
    ORDER BY jo.OfferId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobOffer_Save
    @OfferId             INT = 0,
    @CompanyId           INT,
    @ApplicationId       INT,
    @OfferedSalaryAnnual DECIMAL(18,2),
    @JoiningDate         DATE,
    @OfferExpiryDate     DATE,
    @EmploymentType      NVARCHAR(30),
    @BenefitsSummary     NVARCHAR(500) = NULL,
    @UserId              INT,
    @SavedId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @OfferExpiryDate > @JoiningDate
        THROW 50617, 'Offer expiry date cannot be after the proposed joining date.', 1;

    BEGIN TRAN;

    IF @OfferId = 0
    BEGIN
        DECLARE @NextNum INT = ISNULL((
            SELECT MAX(TRY_CAST(REPLACE(OfferCode, N'OFR-', N'') AS INT))
            FROM dbo.tbl_JobOffers WHERE CompanyId = @CompanyId AND OfferCode LIKE N'OFR-%'
        ), 1000) + 1;
        DECLARE @OfferCode NVARCHAR(30) = CONCAT(N'OFR-', @NextNum);

        INSERT INTO dbo.tbl_JobOffers (
            CompanyId, ApplicationId, OfferCode, OfferedSalaryAnnual, JoiningDate, OfferExpiryDate,
            EmploymentType, OfferStatus, BenefitsSummary, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @ApplicationId, @OfferCode, @OfferedSalaryAnnual, @JoiningDate, @OfferExpiryDate,
            @EmploymentType, N'Draft', @BenefitsSummary, 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_JobOffers
        SET OfferedSalaryAnnual = @OfferedSalaryAnnual,
            JoiningDate = @JoiningDate,
            OfferExpiryDate = @OfferExpiryDate,
            EmploymentType = @EmploymentType,
            BenefitsSummary = @BenefitsSummary,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE OfferId = @OfferId AND CompanyId = @CompanyId AND IsDeleted = 0;
        SET @SavedId = @OfferId;
    END

    UPDATE dbo.tbl_JobApplications
    SET PipelineStage = N'Offer',
        StageUpdatedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ApplicationId = @ApplicationId AND CompanyId = @CompanyId AND PipelineStage NOT IN (N'Hired');

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobOffer_UpdateStatus
    @CompanyId     INT,
    @OfferId       INT,
    @OfferStatus   NVARCHAR(20),
    @ResponseNotes NVARCHAR(400) = NULL,
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Current NVARCHAR(20);
    SELECT @Current = OfferStatus FROM dbo.tbl_JobOffers WHERE OfferId = @OfferId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @Current IS NULL
        THROW 50618, 'Job offer not found.', 1;
    IF @Current = N'Converted'
        THROW 50619, 'This offer has already been converted to an employee record.', 1;

    UPDATE dbo.tbl_JobOffers
    SET OfferStatus = @OfferStatus,
        ResponseNotes = ISNULL(@ResponseNotes, ResponseNotes),
        SentDate = CASE WHEN @OfferStatus = N'Sent' THEN SYSUTCDATETIME() ELSE SentDate END,
        RespondedDate = CASE WHEN @OfferStatus IN (N'Accepted', N'Declined') THEN SYSUTCDATETIME() ELSE RespondedDate END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE OfferId = @OfferId AND CompanyId = @CompanyId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobOffer_ConvertToEmployee
    @CompanyId     INT,
    @OfferId       INT,
    @UserId        INT,
    @NewEmployeeId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @AppId INT, @ReqId INT, @CandId INT, @OfferStatus NVARCHAR(20), @Salary DECIMAL(18,2), @JoiningDate DATE;
    SELECT
        @AppId = jo.ApplicationId,
        @OfferStatus = jo.OfferStatus,
        @Salary = jo.OfferedSalaryAnnual,
        @JoiningDate = jo.JoiningDate,
        @ReqId = ja.RequisitionId,
        @CandId = ja.CandidateId
    FROM dbo.tbl_JobOffers jo
    INNER JOIN dbo.tbl_JobApplications ja ON jo.ApplicationId = ja.ApplicationId
    WHERE jo.OfferId = @OfferId AND jo.CompanyId = @CompanyId AND jo.IsDeleted = 0;

    IF @AppId IS NULL
        THROW 50620, 'Job offer not found.', 1;
    IF @OfferStatus <> N'Accepted'
        THROW 50621, 'Only Accepted offers can be converted into an employee record.', 1;

    DECLARE @FirstName NVARCHAR(80), @LastName NVARCHAR(80), @Email NVARCHAR(120), @Phone NVARCHAR(30);
    SELECT @FirstName = FirstName, @LastName = LastName, @Email = Email, @Phone = Phone
    FROM dbo.tbl_Candidates WHERE CandidateId = @CandId AND CompanyId = @CompanyId;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND WorkEmail = @Email AND IsDeleted = 0)
        THROW 50622, 'An employee with this candidate email already exists in the Employee Master.', 1;

    DECLARE @BranchId INT, @DeptId INT, @DesigId INT, @PosId INT, @MgrId INT, @Openings INT;
    SELECT
        @BranchId = BranchId, @DeptId = DepartmentId, @DesigId = DesignationId,
        @PosId = JobPositionId, @MgrId = HiringManagerId, @Openings = OpeningsCount
    FROM dbo.tbl_JobRequisitions WHERE RequisitionId = @ReqId AND CompanyId = @CompanyId;

    DECLARE @NextEmpNum INT = ISNULL((
        SELECT MAX(TRY_CAST(REPLACE(EmployeeCode, N'EMP-', N'') AS INT))
        FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode LIKE N'EMP-%'
    ), 1000) + 1;
    DECLARE @EmpCode NVARCHAR(30) = CONCAT(N'EMP-', @NextEmpNum);

    DECLARE @BranchName NVARCHAR(120) = (SELECT BranchName FROM dbo.tbl_Branches WHERE BranchId = @BranchId);
    DECLARE @DeptName   NVARCHAR(120) = (SELECT DepartmentName FROM dbo.tbl_Departments WHERE DepartmentId = @DeptId);
    DECLARE @DesigTitle NVARCHAR(120) = (SELECT DesignationTitle FROM dbo.tbl_Designations WHERE DesignationId = @DesigId);

    BEGIN TRAN;

    INSERT INTO dbo.tbl_Employees (
        CompanyId, BranchId, DepartmentId, DesignationId, ReportingManagerId, JobPositionId,
        EmployeeCode, FirstName, LastName, Gender, DateOfBirth, WorkEmail, PersonalEmail, Phone, MobilePhone,
        HireDate, EmploymentStatus, BaseSalary, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @BranchId, @DeptId, @DesigId, @MgrId, @PosId,
        @EmpCode, @FirstName, @LastName, N'NotSpecified', DATEADD(YEAR, -28, @JoiningDate),
        @Email, @Email, @Phone, @Phone,
        @JoiningDate, 2, @Salary, 1, 0, @UserId, SYSUTCDATETIME()
    );

    SET @NewEmployeeId = SCOPE_IDENTITY();

    INSERT INTO dbo.tbl_EmployeeEmploymentHistory (
        CompanyId, EmployeeId, EventType, EffectiveDate, ToBranchName, ToDepartmentName, ToDesignation, Remarks, CreatedBy
    )
    VALUES (
        @CompanyId, @NewEmployeeId, N'Hired', @JoiningDate, @BranchName, @DeptName, @DesigTitle,
        CONCAT(N'Onboarded via Recruitment Offer #', @OfferId), @UserId
    );

    INSERT INTO dbo.tbl_EmployeeSalaryHistory (
        CompanyId, EmployeeId, PreviousSalary, NewSalary, EffectiveDate, ChangeReason, CreatedBy
    )
    VALUES (
        @CompanyId, @NewEmployeeId, 0, @Salary, @JoiningDate, N'Initial Offer Compensation', @UserId
    );

    UPDATE dbo.tbl_JobOffers
    SET OfferStatus = N'Converted', ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE OfferId = @OfferId AND CompanyId = @CompanyId;

    UPDATE dbo.tbl_JobApplications
    SET PipelineStage = N'Hired',
        HiredEmployeeId = @NewEmployeeId,
        StageUpdatedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE ApplicationId = @AppId AND CompanyId = @CompanyId;

    DECLARE @HiredTotal INT = (
        SELECT COUNT(1) FROM dbo.tbl_JobApplications
        WHERE RequisitionId = @ReqId AND CompanyId = @CompanyId AND IsDeleted = 0 AND PipelineStage = N'Hired'
    );
    IF @HiredTotal >= @Openings
    BEGIN
        UPDATE dbo.tbl_JobRequisitions
        SET RequisitionStatus = N'Filled', ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE RequisitionId = @ReqId AND CompanyId = @CompanyId;
    END

    COMMIT TRAN;
END
GO
