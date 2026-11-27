USE [HRMS_DB];
GO

-- Emergency Contacts
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeEmergencyContact_Insert
    @CompanyId INT, @EmployeeId INT, @ContactName NVARCHAR(120), @Relationship NVARCHAR(50),
    @PrimaryPhone NVARCHAR(30), @AlternatePhone NVARCHAR(30) = NULL, @Address NVARCHAR(250) = NULL,
    @IsPrimary BIT, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeEmergencyContacts (CompanyId, EmployeeId, ContactName, Relationship, PrimaryPhone, AlternatePhone, Address, IsPrimary, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @ContactName, @Relationship, @PrimaryPhone, @AlternatePhone, @Address, @IsPrimary, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeEmergencyContact_Delete
    @CompanyId INT, @EmergencyContactId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeEmergencyContacts SET IsDeleted = 1 WHERE EmergencyContactId = @EmergencyContactId AND CompanyId = @CompanyId;
END
GO

-- Family Members
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeFamily_Insert
    @CompanyId INT, @EmployeeId INT, @FullName NVARCHAR(120), @Relationship NVARCHAR(50),
    @DateOfBirth DATE = NULL, @ContactPhone NVARCHAR(30) = NULL, @IsDependent BIT, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeFamilyMembers (CompanyId, EmployeeId, FullName, Relationship, DateOfBirth, ContactPhone, IsDependent, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @FullName, @Relationship, @DateOfBirth, @ContactPhone, @IsDependent, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeFamily_Delete
    @CompanyId INT, @FamilyMemberId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeFamilyMembers SET IsDeleted = 1 WHERE FamilyMemberId = @FamilyMemberId AND CompanyId = @CompanyId;
END
GO

-- Education
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeEducation_Insert
    @CompanyId INT, @EmployeeId INT, @DegreeTitle NVARCHAR(120), @InstitutionName NVARCHAR(150),
    @FieldOfStudy NVARCHAR(120) = NULL, @StartYear INT = NULL, @CompletionYear INT = NULL, @GradeOrGpa NVARCHAR(30) = NULL, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeEducation (CompanyId, EmployeeId, DegreeTitle, InstitutionName, FieldOfStudy, StartYear, CompletionYear, GradeOrGpa, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @DegreeTitle, @InstitutionName, @FieldOfStudy, @StartYear, @CompletionYear, @GradeOrGpa, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeEducation_Delete
    @CompanyId INT, @EducationId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeEducation SET IsDeleted = 1 WHERE EducationId = @EducationId AND CompanyId = @CompanyId;
END
GO

-- Experience
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeExperience_Insert
    @CompanyId INT, @EmployeeId INT, @EmployerName NVARCHAR(150), @JobTitle NVARCHAR(120),
    @StartDate DATE, @EndDate DATE = NULL, @Responsibilities NVARCHAR(500) = NULL, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeExperience (CompanyId, EmployeeId, EmployerName, JobTitle, StartDate, EndDate, Responsibilities, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @EmployerName, @JobTitle, @StartDate, @EndDate, @Responsibilities, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeExperience_Delete
    @CompanyId INT, @ExperienceId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeExperience SET IsDeleted = 1 WHERE ExperienceId = @ExperienceId AND CompanyId = @CompanyId;
END
GO

-- Skills & Certifications
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeSkill_Insert
    @CompanyId INT, @EmployeeId INT, @SkillName NVARCHAR(100), @ProficiencyLevel NVARCHAR(30),
    @CertificationName NVARCHAR(150) = NULL, @IssuingAuthority NVARCHAR(120) = NULL, @ExpiryDate DATE = NULL, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeSkills (CompanyId, EmployeeId, SkillName, ProficiencyLevel, CertificationName, IssuingAuthority, ExpiryDate, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @SkillName, @ProficiencyLevel, @CertificationName, @IssuingAuthority, @ExpiryDate, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeSkill_Delete
    @CompanyId INT, @SkillId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeSkills SET IsDeleted = 1 WHERE SkillId = @SkillId AND CompanyId = @CompanyId;
END
GO

-- Documents
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeDocument_Insert
    @CompanyId INT, @EmployeeId INT, @DocumentType NVARCHAR(60), @DocumentTitle NVARCHAR(150),
    @OriginalFileName NVARCHAR(255), @StoredFileName NVARCHAR(255), @ContentType NVARCHAR(120),
    @FileSizeBytes BIGINT, @ExpiryDate DATE = NULL, @CreatedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tbl_EmployeeDocuments (CompanyId, EmployeeId, DocumentType, DocumentTitle, OriginalFileName, StoredFileName, ContentType, FileSizeBytes, ExpiryDate, CreatedBy)
    VALUES (@CompanyId, @EmployeeId, @DocumentType, @DocumentTitle, @OriginalFileName, @StoredFileName, @ContentType, @FileSizeBytes, @ExpiryDate, @CreatedBy);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeDocument_GetById
    @CompanyId INT, @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 DocumentId, CompanyId, EmployeeId, DocumentType, DocumentTitle, OriginalFileName, StoredFileName, ContentType, FileSizeBytes
    FROM dbo.tbl_EmployeeDocuments
    WHERE DocumentId = @DocumentId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeDocument_Delete
    @CompanyId INT, @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tbl_EmployeeDocuments SET IsDeleted = 1 WHERE DocumentId = @DocumentId AND CompanyId = @CompanyId;
END
GO
