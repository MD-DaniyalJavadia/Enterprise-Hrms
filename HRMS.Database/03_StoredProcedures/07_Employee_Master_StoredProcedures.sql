USE [HRMS_DB];
GO

-- ============================================================================
-- Generate Next Employee Code
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_GenerateNextCode
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NextNum INT;
    SELECT @NextNum = ISNULL(MAX(TRY_CAST(REPLACE(EmployeeCode, N'EMP-', N'') AS INT)), 1000) + 1
    FROM dbo.tbl_Employees
    WHERE CompanyId = @CompanyId AND EmployeeCode LIKE N'EMP-%';

    SELECT CONCAT(N'EMP-', @NextNum) AS NextEmployeeCode;
END
GO

-- ============================================================================
-- Get All Employees (Directory & Filtered Listing)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_GetAll
    @CompanyId        INT,
    @BranchId         INT = NULL,
    @DepartmentId     INT = NULL,
    @EmploymentStatus TINYINT = NULL,
    @SearchTerm       NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.EmployeeId,
        e.CompanyId,
        e.BranchId,
        b.BranchName,
        e.DepartmentId,
        d.DepartmentName,
        e.DesignationId,
        des.DesignationTitle,
        e.ReportingManagerId,
        mgr.FullName AS ReportingManagerName,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.FullName,
        e.Gender,
        e.WorkEmail,
        e.Phone,
        e.MobilePhone,
        e.HireDate,
        e.EmploymentStatus,
        e.BaseSalary,
        e.PhotoPath,
        e.IsActive
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@EmploymentStatus IS NULL OR e.EmploymentStatus = @EmploymentStatus)
      AND (@SearchTerm IS NULL
           OR e.FullName LIKE N'%' + @SearchTerm + N'%'
           OR e.EmployeeCode LIKE N'%' + @SearchTerm + N'%'
           OR e.WorkEmail LIKE N'%' + @SearchTerm + N'%'
           OR d.DepartmentName LIKE N'%' + @SearchTerm + N'%')
    ORDER BY e.EmployeeCode;
END
GO

-- ============================================================================
-- Get Complete Employee 360 Profile (Multi-Result-Set)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_GetById
    @CompanyId  INT,
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Core Employee
    SELECT TOP 1
        e.*,
        c.CompanyName,
        b.BranchName,
        d.DepartmentName,
        des.DesignationTitle,
        mgr.FullName AS ReportingManagerName,
        wl.LocationName AS WorkLocationName,
        jp.PositionTitle AS JobPositionTitle,
        s.ShiftName
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Companies c ON e.CompanyId = c.CompanyId
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    LEFT JOIN dbo.tbl_Employees mgr ON e.ReportingManagerId = mgr.EmployeeId
    LEFT JOIN dbo.tbl_WorkLocations wl ON e.WorkLocationId = wl.WorkLocationId
    LEFT JOIN dbo.tbl_JobPositions jp ON e.JobPositionId = jp.JobPositionId
    LEFT JOIN dbo.tbl_Shifts s ON e.ShiftId = s.ShiftId
    WHERE e.CompanyId = @CompanyId AND e.EmployeeId = @EmployeeId AND e.IsDeleted = 0;

    -- 2. Emergency Contacts
    SELECT EmergencyContactId, CompanyId, EmployeeId, ContactName, Relationship, PrimaryPhone, AlternatePhone, Address, IsPrimary
    FROM dbo.tbl_EmployeeEmergencyContacts
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY IsPrimary DESC, ContactName;

    -- 3. Family Members
    SELECT FamilyMemberId, CompanyId, EmployeeId, FullName, Relationship, DateOfBirth, ContactPhone, IsDependent
    FROM dbo.tbl_EmployeeFamilyMembers
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY FullName;

    -- 4. Education
    SELECT EducationId, CompanyId, EmployeeId, DegreeTitle, InstitutionName, FieldOfStudy, StartYear, CompletionYear, GradeOrGpa
    FROM dbo.tbl_EmployeeEducation
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY CompletionYear DESC;

    -- 5. Experience
    SELECT ExperienceId, CompanyId, EmployeeId, EmployerName, JobTitle, StartDate, EndDate, Responsibilities
    FROM dbo.tbl_EmployeeExperience
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY StartDate DESC;

    -- 6. Skills
    SELECT SkillId, CompanyId, EmployeeId, SkillName, ProficiencyLevel, CertificationName, IssuingAuthority, ExpiryDate
    FROM dbo.tbl_EmployeeSkills
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY SkillName;

    -- 7. Documents
    SELECT DocumentId, CompanyId, EmployeeId, DocumentType, DocumentTitle, OriginalFileName, ContentType, FileSizeBytes, ExpiryDate, CreatedDate
    FROM dbo.tbl_EmployeeDocuments
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0
    ORDER BY CreatedDate DESC;

    -- 8. Employment History Timeline
    SELECT HistoryId, CompanyId, EmployeeId, EventType, EffectiveDate, FromBranchName, ToBranchName, FromDepartmentName, ToDepartmentName, FromDesignation, ToDesignation, Remarks, CreatedDate
    FROM dbo.tbl_EmployeeEmploymentHistory
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId
    ORDER BY EffectiveDate DESC, HistoryId DESC;

    -- 9. Salary History
    SELECT SalaryHistoryId, CompanyId, EmployeeId, PreviousSalary, NewSalary, EffectiveDate, ChangeReason, CreatedDate
    FROM dbo.tbl_EmployeeSalaryHistory
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId
    ORDER BY EffectiveDate DESC, SalaryHistoryId DESC;
END
GO

-- ============================================================================
-- Insert Employee + Initial Timeline & Salary History
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_Insert
    @CompanyId               INT,
    @BranchId                INT,
    @DepartmentId            INT,
    @DesignationId           INT,
    @ReportingManagerId      INT = NULL,
    @WorkLocationId          INT = NULL,
    @JobPositionId           INT = NULL,
    @ShiftId                 INT = NULL,
    @EmployeeCode            NVARCHAR(30),
    @FirstName               NVARCHAR(80),
    @MiddleName              NVARCHAR(80) = NULL,
    @LastName                NVARCHAR(80),
    @Gender                  NVARCHAR(20),
    @DateOfBirth             DATE,
    @MaritalStatus           NVARCHAR(20) = NULL,
    @Nationality             NVARCHAR(60) = NULL,
    @NationalIdNumber        NVARCHAR(50) = NULL,
    @BloodGroup              NVARCHAR(10) = NULL,
    @WorkEmail               NVARCHAR(120),
    @PersonalEmail           NVARCHAR(120) = NULL,
    @Phone                   NVARCHAR(30) = NULL,
    @MobilePhone             NVARCHAR(30) = NULL,
    @CurrentAddress          NVARCHAR(300) = NULL,
    @PermanentAddress        NVARCHAR(300) = NULL,
    @City                    NVARCHAR(80) = NULL,
    @Country                 NVARCHAR(80) = NULL,
    @HireDate                DATE,
    @ConfirmationDate        DATE = NULL,
    @EmploymentStatus        TINYINT,
    @BaseSalary              DECIMAL(18,2),
    @BankName                NVARCHAR(120) = NULL,
    @BankAccountTitle        NVARCHAR(120) = NULL,
    @BankAccountNumber       NVARCHAR(50) = NULL,
    @BankRoutingCode         NVARCHAR(40) = NULL,
    @TaxIdentificationNumber NVARCHAR(50) = NULL,
    @TaxFilingStatus         NVARCHAR(40) = NULL,
    @PhotoPath               NVARCHAR(260) = NULL,
    @CreatedBy               INT,
    @NewEmployeeId           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = @EmployeeCode AND IsDeleted = 0)
        THROW 50201, 'An employee with this Employee Code already exists.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND WorkEmail = @WorkEmail AND IsDeleted = 0)
        THROW 50202, 'An employee with this Work Email already exists.', 1;

    DECLARE @BranchName NVARCHAR(120) = (SELECT BranchName FROM dbo.tbl_Branches WHERE BranchId = @BranchId);
    DECLARE @DeptName   NVARCHAR(120) = (SELECT DepartmentName FROM dbo.tbl_Departments WHERE DepartmentId = @DepartmentId);
    DECLARE @DesigTitle NVARCHAR(120) = (SELECT DesignationTitle FROM dbo.tbl_Designations WHERE DesignationId = @DesignationId);

    INSERT INTO dbo.tbl_Employees (
        CompanyId, BranchId, DepartmentId, DesignationId, ReportingManagerId, WorkLocationId, JobPositionId, ShiftId,
        EmployeeCode, FirstName, MiddleName, LastName, Gender, DateOfBirth, MaritalStatus, Nationality, NationalIdNumber, BloodGroup,
        WorkEmail, PersonalEmail, Phone, MobilePhone, CurrentAddress, PermanentAddress, City, Country,
        HireDate, ConfirmationDate, EmploymentStatus, BaseSalary,
        BankName, BankAccountTitle, BankAccountNumber, BankRoutingCode, TaxIdentificationNumber, TaxFilingStatus,
        PhotoPath, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @BranchId, @DepartmentId, @DesignationId, @ReportingManagerId, @WorkLocationId, @JobPositionId, @ShiftId,
        @EmployeeCode, @FirstName, @MiddleName, @LastName, @Gender, @DateOfBirth, @MaritalStatus, @Nationality, @NationalIdNumber, @BloodGroup,
        @WorkEmail, @PersonalEmail, @Phone, @MobilePhone, @CurrentAddress, @PermanentAddress, @City, @Country,
        @HireDate, @ConfirmationDate, @EmploymentStatus, @BaseSalary,
        @BankName, @BankAccountTitle, @BankAccountNumber, @BankRoutingCode, @TaxIdentificationNumber, @TaxFilingStatus,
        @PhotoPath, CASE WHEN @EmploymentStatus IN (4,5,6) THEN 0 ELSE 1 END, 0, @CreatedBy, SYSUTCDATETIME()
    );

    SET @NewEmployeeId = SCOPE_IDENTITY();

    INSERT INTO dbo.tbl_EmployeeEmploymentHistory (
        CompanyId, EmployeeId, EventType, EffectiveDate, ToBranchName, ToDepartmentName, ToDesignation, Remarks, CreatedBy
    )
    VALUES (
        @CompanyId, @NewEmployeeId, N'Hired', @HireDate, @BranchName, @DeptName, @DesigTitle, N'Initial onboarding record', @CreatedBy
    );

    INSERT INTO dbo.tbl_EmployeeSalaryHistory (
        CompanyId, EmployeeId, PreviousSalary, NewSalary, EffectiveDate, ChangeReason, CreatedBy
    )
    VALUES (
        @CompanyId, @NewEmployeeId, 0, @BaseSalary, @HireDate, N'Initial Hire Compensation', @CreatedBy
    );
END
GO

-- ============================================================================
-- Update Employee Core Profile
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_Update
    @EmployeeId              INT,
    @CompanyId               INT,
    @BranchId                INT,
    @DepartmentId            INT,
    @DesignationId           INT,
    @ReportingManagerId      INT = NULL,
    @WorkLocationId          INT = NULL,
    @JobPositionId           INT = NULL,
    @ShiftId                 INT = NULL,
    @EmployeeCode            NVARCHAR(30),
    @FirstName               NVARCHAR(80),
    @MiddleName              NVARCHAR(80) = NULL,
    @LastName                NVARCHAR(80),
    @Gender                  NVARCHAR(20),
    @DateOfBirth             DATE,
    @MaritalStatus           NVARCHAR(20) = NULL,
    @Nationality             NVARCHAR(60) = NULL,
    @NationalIdNumber        NVARCHAR(50) = NULL,
    @BloodGroup              NVARCHAR(10) = NULL,
    @WorkEmail               NVARCHAR(120),
    @PersonalEmail           NVARCHAR(120) = NULL,
    @Phone                   NVARCHAR(30) = NULL,
    @MobilePhone             NVARCHAR(30) = NULL,
    @CurrentAddress          NVARCHAR(300) = NULL,
    @PermanentAddress        NVARCHAR(300) = NULL,
    @City                    NVARCHAR(80) = NULL,
    @Country                 NVARCHAR(80) = NULL,
    @HireDate                DATE,
    @ConfirmationDate        DATE = NULL,
    @EmploymentStatus        TINYINT,
    @BaseSalary              DECIMAL(18,2),
    @BankName                NVARCHAR(120) = NULL,
    @BankAccountTitle        NVARCHAR(120) = NULL,
    @BankAccountNumber       NVARCHAR(50) = NULL,
    @BankRoutingCode         NVARCHAR(40) = NULL,
    @TaxIdentificationNumber NVARCHAR(50) = NULL,
    @TaxFilingStatus         NVARCHAR(40) = NULL,
    @PhotoPath               NVARCHAR(260) = NULL,
    @ModifiedBy              INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ReportingManagerId = @EmployeeId
        THROW 50203, 'An employee cannot report to themselves.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = @EmployeeCode AND EmployeeId <> @EmployeeId AND IsDeleted = 0)
        THROW 50201, 'Another employee already uses this Employee Code.', 1;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND WorkEmail = @WorkEmail AND EmployeeId <> @EmployeeId AND IsDeleted = 0)
        THROW 50202, 'Another employee already uses this Work Email.', 1;

    UPDATE dbo.tbl_Employees
    SET BranchId = @BranchId,
        DepartmentId = @DepartmentId,
        DesignationId = @DesignationId,
        ReportingManagerId = @ReportingManagerId,
        WorkLocationId = @WorkLocationId,
        JobPositionId = @JobPositionId,
        ShiftId = @ShiftId,
        EmployeeCode = @EmployeeCode,
        FirstName = @FirstName,
        MiddleName = @MiddleName,
        LastName = @LastName,
        Gender = @Gender,
        DateOfBirth = @DateOfBirth,
        MaritalStatus = @MaritalStatus,
        Nationality = @Nationality,
        NationalIdNumber = @NationalIdNumber,
        BloodGroup = @BloodGroup,
        WorkEmail = @WorkEmail,
        PersonalEmail = @PersonalEmail,
        Phone = @Phone,
        MobilePhone = @MobilePhone,
        CurrentAddress = @CurrentAddress,
        PermanentAddress = @PermanentAddress,
        City = @City,
        Country = @Country,
        HireDate = @HireDate,
        ConfirmationDate = @ConfirmationDate,
        EmploymentStatus = @EmploymentStatus,
        BaseSalary = @BaseSalary,
        BankName = @BankName,
        BankAccountTitle = @BankAccountTitle,
        BankAccountNumber = @BankAccountNumber,
        BankRoutingCode = @BankRoutingCode,
        TaxIdentificationNumber = @TaxIdentificationNumber,
        TaxFilingStatus = @TaxFilingStatus,
        PhotoPath = ISNULL(@PhotoPath, PhotoPath),
        IsActive = CASE WHEN @EmploymentStatus IN (4,5,6) THEN 0 ELSE 1 END,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- ============================================================================
-- Soft Delete Employee
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_Delete
    @CompanyId  INT,
    @EmployeeId INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_Employees
    SET IsDeleted = 1,
        IsActive = 0,
        EmploymentStatus = 6,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Record Career Action (Promotion / Transfer / Salary Revision / Status Change)
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Employee_RecordCareerAction
    @CompanyId        INT,
    @EmployeeId       INT,
    @ActionType       NVARCHAR(40),
    @EffectiveDate    DATE,
    @NewBranchId      INT,
    @NewDepartmentId  INT,
    @NewDesignationId INT,
    @NewStatus        TINYINT,
    @NewBaseSalary    DECIMAL(18,2),
    @Remarks          NVARCHAR(500),
    @UserId           INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OldBranchName NVARCHAR(120), @OldDeptName NVARCHAR(120), @OldDesig NVARCHAR(120), @OldSalary DECIMAL(18,2);
    SELECT
        @OldBranchName = b.BranchName,
        @OldDeptName = d.DepartmentName,
        @OldDesig = des.DesignationTitle,
        @OldSalary = e.BaseSalary
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    WHERE e.EmployeeId = @EmployeeId AND e.CompanyId = @CompanyId AND e.IsDeleted = 0;

    IF @OldBranchName IS NULL
        THROW 50204, 'Employee not found.', 1;

    DECLARE @NewBranchName NVARCHAR(120) = (SELECT BranchName FROM dbo.tbl_Branches WHERE BranchId = @NewBranchId);
    DECLARE @NewDeptName   NVARCHAR(120) = (SELECT DepartmentName FROM dbo.tbl_Departments WHERE DepartmentId = @NewDepartmentId);
    DECLARE @NewDesig      NVARCHAR(120) = (SELECT DesignationTitle FROM dbo.tbl_Designations WHERE DesignationId = @NewDesignationId);

    UPDATE dbo.tbl_Employees
    SET BranchId = @NewBranchId,
        DepartmentId = @NewDepartmentId,
        DesignationId = @NewDesignationId,
        EmploymentStatus = @NewStatus,
        BaseSalary = @NewBaseSalary,
        IsActive = CASE WHEN @NewStatus IN (4,5,6) THEN 0 ELSE 1 END,
        ExitDate = CASE WHEN @NewStatus IN (4,5) THEN @EffectiveDate ELSE ExitDate END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId;

    INSERT INTO dbo.tbl_EmployeeEmploymentHistory (
        CompanyId, EmployeeId, EventType, EffectiveDate,
        FromBranchName, ToBranchName, FromDepartmentName, ToDepartmentName,
        FromDesignation, ToDesignation, Remarks, CreatedBy
    )
    VALUES (
        @CompanyId, @EmployeeId, @ActionType, @EffectiveDate,
        @OldBranchName, @NewBranchName, @OldDeptName, @NewDeptName,
        @OldDesig, @NewDesig, @Remarks, @UserId
    );

    IF @NewBaseSalary <> @OldSalary
    BEGIN
        INSERT INTO dbo.tbl_EmployeeSalaryHistory (
            CompanyId, EmployeeId, PreviousSalary, NewSalary, EffectiveDate, ChangeReason, CreatedBy
        )
        VALUES (
            @CompanyId, @EmployeeId, @OldSalary, @NewBaseSalary, @EffectiveDate, @Remarks, @UserId
        );
    END
END
GO
