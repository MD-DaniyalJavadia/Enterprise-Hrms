USE [HRMS_DB];
GO

-- ============================================================================
-- Department Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Department_GetAll
    @CompanyId  INT,
    @BranchId   INT = NULL,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DepartmentId,
        d.CompanyId,
        d.BranchId,
        b.BranchName,
        d.ParentDepartmentId,
        pd.DepartmentName AS ParentDepartmentName,
        d.DepartmentCode,
        d.DepartmentName,
        d.CostCenterCode,
        d.IsActive,
        d.CreatedDate,
        (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.DepartmentId = d.DepartmentId AND e.IsDeleted = 0) AS EmployeeCount
    FROM dbo.tbl_Departments d
    LEFT JOIN dbo.tbl_Branches b ON d.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Departments pd ON d.ParentDepartmentId = pd.DepartmentId
    WHERE d.CompanyId = @CompanyId
      AND d.IsDeleted = 0
      AND (@BranchId IS NULL OR d.BranchId = @BranchId)
      AND (@SearchTerm IS NULL OR d.DepartmentName LIKE N'%' + @SearchTerm + N'%' OR d.DepartmentCode LIKE N'%' + @SearchTerm + N'%')
    ORDER BY d.DepartmentName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Department_GetById
    @CompanyId    INT,
    @DepartmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        d.DepartmentId,
        d.CompanyId,
        d.BranchId,
        b.BranchName,
        d.ParentDepartmentId,
        pd.DepartmentName AS ParentDepartmentName,
        d.DepartmentCode,
        d.DepartmentName,
        d.CostCenterCode,
        d.IsActive,
        d.CreatedDate,
        d.ModifiedDate
    FROM dbo.tbl_Departments d
    LEFT JOIN dbo.tbl_Branches b ON d.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Departments pd ON d.ParentDepartmentId = pd.DepartmentId
    WHERE d.DepartmentId = @DepartmentId
      AND d.CompanyId = @CompanyId
      AND d.IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Department_Insert
    @CompanyId          INT,
    @BranchId           INT = NULL,
    @ParentDepartmentId INT = NULL,
    @DepartmentCode     NVARCHAR(20),
    @DepartmentName     NVARCHAR(120),
    @CostCenterCode     NVARCHAR(30) = NULL,
    @IsActive           BIT,
    @CreatedBy          INT,
    @NewDepartmentId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = @DepartmentCode AND IsDeleted = 0)
    BEGIN
        THROW 50121, 'A department with this code already exists.', 1;
    END

    INSERT INTO dbo.tbl_Departments (
        CompanyId, BranchId, ParentDepartmentId, DepartmentCode, DepartmentName,
        CostCenterCode, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @BranchId, @ParentDepartmentId, @DepartmentCode, @DepartmentName,
        @CostCenterCode, @IsActive, 0, @CreatedBy, SYSUTCDATETIME()
    );

    SET @NewDepartmentId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Department_Update
    @DepartmentId       INT,
    @CompanyId          INT,
    @BranchId           INT = NULL,
    @ParentDepartmentId INT = NULL,
    @DepartmentCode     NVARCHAR(20),
    @DepartmentName     NVARCHAR(120),
    @CostCenterCode     NVARCHAR(30) = NULL,
    @IsActive           BIT,
    @ModifiedBy         INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ParentDepartmentId = @DepartmentId
    BEGIN
        THROW 50122, 'A department cannot be its own parent.', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND DepartmentCode = @DepartmentCode AND DepartmentId <> @DepartmentId AND IsDeleted = 0)
    BEGIN
        THROW 50121, 'Another department already uses this code.', 1;
    END

    UPDATE dbo.tbl_Departments
    SET BranchId = @BranchId,
        ParentDepartmentId = @ParentDepartmentId,
        DepartmentCode = @DepartmentCode,
        DepartmentName = @DepartmentName,
        CostCenterCode = @CostCenterCode,
        IsActive = @IsActive,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE DepartmentId = @DepartmentId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Department_Delete
    @CompanyId    INT,
    @DepartmentId INT,
    @ModifiedBy   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND DepartmentId = @DepartmentId AND IsDeleted = 0)
    BEGIN
        THROW 50123, 'Cannot delete a department with assigned employees.', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.tbl_Departments WHERE CompanyId = @CompanyId AND ParentDepartmentId = @DepartmentId AND IsDeleted = 0)
    BEGIN
        THROW 50124, 'Cannot delete a department that has sub-departments.', 1;
    END

    UPDATE dbo.tbl_Departments
    SET IsDeleted = 1,
        IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE DepartmentId = @DepartmentId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Designation Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Designation_GetAll
    @CompanyId  INT,
    @SearchTerm NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        des.DesignationId,
        des.CompanyId,
        des.DesignationCode,
        des.DesignationTitle,
        des.JobGrade,
        des.IsActive,
        des.CreatedDate,
        (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.DesignationId = des.DesignationId AND e.IsDeleted = 0) AS EmployeeCount
    FROM dbo.tbl_Designations des
    WHERE des.CompanyId = @CompanyId
      AND des.IsDeleted = 0
      AND (@SearchTerm IS NULL OR des.DesignationTitle LIKE N'%' + @SearchTerm + N'%' OR des.DesignationCode LIKE N'%' + @SearchTerm + N'%')
    ORDER BY des.DesignationTitle;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Designation_GetById
    @CompanyId     INT,
    @DesignationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        DesignationId, CompanyId, DesignationCode, DesignationTitle, JobGrade, IsActive, CreatedDate, ModifiedDate
    FROM dbo.tbl_Designations
    WHERE DesignationId = @DesignationId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Designation_Insert
    @CompanyId        INT,
    @DesignationCode  NVARCHAR(20),
    @DesignationTitle NVARCHAR(120),
    @JobGrade         NVARCHAR(20) = NULL,
    @IsActive         BIT,
    @CreatedBy        INT,
    @NewDesignationId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = @DesignationCode AND IsDeleted = 0)
    BEGIN
        THROW 50131, 'A designation with this code already exists.', 1;
    END

    INSERT INTO dbo.tbl_Designations (
        CompanyId, DesignationCode, DesignationTitle, JobGrade, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @DesignationCode, @DesignationTitle, @JobGrade, @IsActive, 0, @CreatedBy, SYSUTCDATETIME()
    );

    SET @NewDesignationId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Designation_Update
    @DesignationId    INT,
    @CompanyId        INT,
    @DesignationCode  NVARCHAR(20),
    @DesignationTitle NVARCHAR(120),
    @JobGrade         NVARCHAR(20) = NULL,
    @IsActive         BIT,
    @ModifiedBy       INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Designations WHERE CompanyId = @CompanyId AND DesignationCode = @DesignationCode AND DesignationId <> @DesignationId AND IsDeleted = 0)
    BEGIN
        THROW 50131, 'Another designation already uses this code.', 1;
    END

    UPDATE dbo.tbl_Designations
    SET DesignationCode = @DesignationCode,
        DesignationTitle = @DesignationTitle,
        JobGrade = @JobGrade,
        IsActive = @IsActive,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE DesignationId = @DesignationId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Designation_Delete
    @CompanyId     INT,
    @DesignationId INT,
    @ModifiedBy    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND DesignationId = @DesignationId AND IsDeleted = 0)
    BEGIN
        THROW 50132, 'Cannot delete a designation assigned to employees.', 1;
    END

    UPDATE dbo.tbl_Designations
    SET IsDeleted = 1,
        IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE DesignationId = @DesignationId AND CompanyId = @CompanyId;
END
GO
