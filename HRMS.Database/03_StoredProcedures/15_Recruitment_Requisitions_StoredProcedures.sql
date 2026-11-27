USE [HRMS_DB];
GO

-- ============================================================================
-- Job Requisitions Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_JobRequisition_GetAll
    @CompanyId    INT,
    @DepartmentId INT = NULL,
    @Status       NVARCHAR(30) = NULL,
    @SearchTerm   NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Like NVARCHAR(104) = CASE WHEN @SearchTerm IS NULL OR LTRIM(RTRIM(@SearchTerm)) = N'' THEN NULL ELSE N'%' + LTRIM(RTRIM(@SearchTerm)) + N'%' END;

    -- 1. Requisitions list
    SELECT
        r.*,
        b.BranchName,
        d.DepartmentName,
        desig.DesignationTitle,
        mgr.FullName AS HiringManagerName,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = r.RequisitionId AND ja.IsDeleted = 0) AS ApplicantsCount,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.RequisitionId = r.RequisitionId AND ja.IsDeleted = 0 AND ja.PipelineStage = N'Hired') AS HiredCount
    FROM dbo.tbl_JobRequisitions r
    INNER JOIN dbo.tbl_Branches b ON r.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON r.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON r.DesignationId = desig.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON r.HiringManagerId = mgr.EmployeeId
    WHERE r.CompanyId = @CompanyId
      AND r.IsDeleted = 0
      AND (@DepartmentId IS NULL OR r.DepartmentId = @DepartmentId)
      AND (@Status IS NULL OR r.RequisitionStatus = @Status)
      AND (@Like IS NULL OR r.JobTitle LIKE @Like OR r.RequisitionCode LIKE @Like OR d.DepartmentName LIKE @Like)
    ORDER BY CASE WHEN r.RequisitionStatus = N'Open' THEN 0 ELSE 1 END, r.RequisitionId DESC;

    -- 2. KPI Summary
    SELECT
        SUM(CASE WHEN RequisitionStatus = N'Open' THEN 1 ELSE 0 END) AS OpenRequisitions,
        SUM(CASE WHEN RequisitionStatus = N'Open' THEN OpeningsCount ELSE 0 END) AS TotalOpenPositions,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.CompanyId = @CompanyId AND ja.IsDeleted = 0 AND ja.PipelineStage NOT IN (N'Rejected', N'Withdrawn', N'Hired')) AS ActiveApplicants,
        (SELECT COUNT(1) FROM dbo.tbl_Interviews iv WHERE iv.CompanyId = @CompanyId AND iv.IsDeleted = 0 AND iv.InterviewStatus = N'Scheduled') AS ScheduledInterviews,
        (SELECT COUNT(1) FROM dbo.tbl_JobOffers jo WHERE jo.CompanyId = @CompanyId AND jo.IsDeleted = 0 AND jo.OfferStatus IN (N'Sent', N'Accepted')) AS ActiveOffers
    FROM dbo.tbl_JobRequisitions
    WHERE CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobRequisition_Save
    @RequisitionId      INT = 0,
    @CompanyId          INT,
    @RequisitionCode    NVARCHAR(30) = NULL,
    @JobTitle           NVARCHAR(150),
    @BranchId           INT,
    @DepartmentId       INT,
    @DesignationId      INT,
    @JobPositionId      INT = NULL,
    @HiringManagerId    INT = NULL,
    @EmploymentType     NVARCHAR(30),
    @WorkMode           NVARCHAR(30),
    @OpeningsCount      INT,
    @MinSalary          DECIMAL(18,2) = NULL,
    @MaxSalary          DECIMAL(18,2) = NULL,
    @ExperienceMinYears INT,
    @ExperienceMaxYears INT = NULL,
    @TargetHireDate     DATE = NULL,
    @RequisitionStatus  NVARCHAR(30),
    @JobSummary         NVARCHAR(1000) = NULL,
    @RequiredSkills     NVARCHAR(500) = NULL,
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @MinSalary IS NOT NULL AND @MaxSalary IS NOT NULL AND @MaxSalary < @MinSalary
        THROW 50601, 'Maximum salary cannot be lower than minimum salary.', 1;

    IF @RequisitionCode IS NULL OR LTRIM(RTRIM(@RequisitionCode)) = N''
    BEGIN
        DECLARE @NextNum INT = ISNULL((
            SELECT MAX(TRY_CAST(REPLACE(RequisitionCode, N'REQ-', N'') AS INT))
            FROM dbo.tbl_JobRequisitions WHERE CompanyId = @CompanyId AND RequisitionCode LIKE N'REQ-%'
        ), 1000) + 1;
        SET @RequisitionCode = CONCAT(N'REQ-', @NextNum);
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_JobRequisitions
        WHERE CompanyId = @CompanyId AND RequisitionCode = @RequisitionCode AND RequisitionId <> @RequisitionId AND IsDeleted = 0
    )
        THROW 50602, 'A job requisition with this code already exists.', 1;

    IF @RequisitionId = 0
    BEGIN
        INSERT INTO dbo.tbl_JobRequisitions (
            CompanyId, RequisitionCode, JobTitle, BranchId, DepartmentId, DesignationId, JobPositionId,
            HiringManagerId, EmploymentType, WorkMode, OpeningsCount, MinSalary, MaxSalary,
            ExperienceMinYears, ExperienceMaxYears, TargetHireDate, RequisitionStatus,
            JobSummary, RequiredSkills, ApprovedBy, ApprovedDate, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @RequisitionCode, @JobTitle, @BranchId, @DepartmentId, @DesignationId, @JobPositionId,
            @HiringManagerId, @EmploymentType, @WorkMode, @OpeningsCount, @MinSalary, @MaxSalary,
            @ExperienceMinYears, @ExperienceMaxYears, @TargetHireDate, @RequisitionStatus,
            @JobSummary, @RequiredSkills,
            CASE WHEN @RequisitionStatus = N'Open' THEN @UserId ELSE NULL END,
            CASE WHEN @RequisitionStatus = N'Open' THEN SYSUTCDATETIME() ELSE NULL END,
            0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_JobRequisitions
        SET RequisitionCode = @RequisitionCode,
            JobTitle = @JobTitle,
            BranchId = @BranchId,
            DepartmentId = @DepartmentId,
            DesignationId = @DesignationId,
            JobPositionId = @JobPositionId,
            HiringManagerId = @HiringManagerId,
            EmploymentType = @EmploymentType,
            WorkMode = @WorkMode,
            OpeningsCount = @OpeningsCount,
            MinSalary = @MinSalary,
            MaxSalary = @MaxSalary,
            ExperienceMinYears = @ExperienceMinYears,
            ExperienceMaxYears = @ExperienceMaxYears,
            TargetHireDate = @TargetHireDate,
            RequisitionStatus = @RequisitionStatus,
            JobSummary = @JobSummary,
            RequiredSkills = @RequiredSkills,
            ApprovedBy = CASE WHEN @RequisitionStatus = N'Open' AND ApprovedBy IS NULL THEN @UserId ELSE ApprovedBy END,
            ApprovedDate = CASE WHEN @RequisitionStatus = N'Open' AND ApprovedDate IS NULL THEN SYSUTCDATETIME() ELSE ApprovedDate END,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE RequisitionId = @RequisitionId AND CompanyId = @CompanyId AND IsDeleted = 0;

        SET @SavedId = @RequisitionId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobRequisition_UpdateStatus
    @CompanyId         INT,
    @RequisitionId     INT,
    @RequisitionStatus NVARCHAR(30),
    @UserId            INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_JobRequisitions
    SET RequisitionStatus = @RequisitionStatus,
        ApprovedBy = CASE WHEN @RequisitionStatus = N'Open' AND ApprovedBy IS NULL THEN @UserId ELSE ApprovedBy END,
        ApprovedDate = CASE WHEN @RequisitionStatus = N'Open' AND ApprovedDate IS NULL THEN SYSUTCDATETIME() ELSE ApprovedDate END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE RequisitionId = @RequisitionId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_JobRequisition_Delete
    @CompanyId     INT,
    @RequisitionId INT,
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_JobApplications WHERE CompanyId = @CompanyId AND RequisitionId = @RequisitionId AND IsDeleted = 0)
        THROW 50603, 'Cannot delete a requisition that already has job applications. Close it instead.', 1;

    UPDATE dbo.tbl_JobRequisitions
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE RequisitionId = @RequisitionId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Candidate Pool Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Candidate_GetAll
    @CompanyId  INT,
    @Source     NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Like NVARCHAR(104) = CASE WHEN @SearchTerm IS NULL OR LTRIM(RTRIM(@SearchTerm)) = N'' THEN NULL ELSE N'%' + LTRIM(RTRIM(@SearchTerm)) + N'%' END;

    SELECT
        c.*,
        (SELECT COUNT(1) FROM dbo.tbl_JobApplications ja WHERE ja.CandidateId = c.CandidateId AND ja.IsDeleted = 0) AS ApplicationsCount
    FROM dbo.tbl_Candidates c
    WHERE c.CompanyId = @CompanyId
      AND c.IsDeleted = 0
      AND (@Source IS NULL OR c.Source = @Source)
      AND (@Like IS NULL OR c.FullName LIKE @Like OR c.Email LIKE @Like OR c.Skills LIKE @Like OR c.CurrentJobTitle LIKE @Like)
    ORDER BY c.CandidateId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Candidate_Save
    @CandidateId          INT = 0,
    @CompanyId            INT,
    @FirstName            NVARCHAR(80),
    @LastName             NVARCHAR(80),
    @Email                NVARCHAR(120),
    @Phone                NVARCHAR(30) = NULL,
    @CurrentCompany       NVARCHAR(150) = NULL,
    @CurrentJobTitle      NVARCHAR(120) = NULL,
    @TotalExperienceYears DECIMAL(4,1),
    @ExpectedSalary       DECIMAL(18,2) = NULL,
    @Skills               NVARCHAR(500) = NULL,
    @Source               NVARCHAR(50),
    @LinkedInUrl          NVARCHAR(250) = NULL,
    @ResumeFileName       NVARCHAR(255) = NULL,
    @ResumeStoredName     NVARCHAR(255) = NULL,
    @Notes                NVARCHAR(500) = NULL,
    @UserId               INT,
    @SavedId              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Candidates WHERE CompanyId = @CompanyId AND Email = @Email AND CandidateId <> @CandidateId AND IsDeleted = 0)
        THROW 50604, 'A candidate with this email address already exists.', 1;

    IF @CandidateId = 0
    BEGIN
        INSERT INTO dbo.tbl_Candidates (
            CompanyId, FirstName, LastName, Email, Phone, CurrentCompany, CurrentJobTitle,
            TotalExperienceYears, ExpectedSalary, Skills, Source, LinkedInUrl,
            ResumeFileName, ResumeStoredName, Notes, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @FirstName, @LastName, @Email, @Phone, @CurrentCompany, @CurrentJobTitle,
            @TotalExperienceYears, @ExpectedSalary, @Skills, @Source, @LinkedInUrl,
            @ResumeFileName, @ResumeStoredName, @Notes, 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_Candidates
        SET FirstName = @FirstName,
            LastName = @LastName,
            Email = @Email,
            Phone = @Phone,
            CurrentCompany = @CurrentCompany,
            CurrentJobTitle = @CurrentJobTitle,
            TotalExperienceYears = @TotalExperienceYears,
            ExpectedSalary = @ExpectedSalary,
            Skills = @Skills,
            Source = @Source,
            LinkedInUrl = @LinkedInUrl,
            ResumeFileName = ISNULL(@ResumeFileName, ResumeFileName),
            ResumeStoredName = ISNULL(@ResumeStoredName, ResumeStoredName),
            Notes = @Notes,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CandidateId = @CandidateId AND CompanyId = @CompanyId AND IsDeleted = 0;

        SET @SavedId = @CandidateId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Candidate_GetResume
    @CompanyId   INT,
    @CandidateId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CandidateId, ResumeFileName, ResumeStoredName
    FROM dbo.tbl_Candidates
    WHERE CompanyId = @CompanyId AND CandidateId = @CandidateId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Candidate_Delete
    @CompanyId   INT,
    @CandidateId INT,
    @UserId      INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_JobApplications WHERE CompanyId = @CompanyId AND CandidateId = @CandidateId AND IsDeleted = 0)
        THROW 50605, 'Cannot delete a candidate linked to active job applications.', 1;

    UPDATE dbo.tbl_Candidates
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CandidateId = @CandidateId AND CompanyId = @CompanyId;
END
GO
