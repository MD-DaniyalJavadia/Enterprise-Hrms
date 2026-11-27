USE [HRMS_DB];
GO

-- ============================================================================
-- Company Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Company_GetAll
    @SearchTerm NVARCHAR(100) = NULL,
    @IsActive   BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CompanyId, c.CompanyCode, c.CompanyName, c.LegalName, c.TaxId,
        c.Email, c.Phone, c.Website, c.AddressLine1, c.City, c.Country,
        c.CurrencyCode, c.TimeZoneId, c.IsActive, c.CreatedDate,
        (SELECT COUNT(1) FROM dbo.tbl_Branches b WHERE b.CompanyId = c.CompanyId AND b.IsDeleted = 0) AS BranchCount,
        (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.CompanyId = c.CompanyId AND e.IsDeleted = 0) AS EmployeeCount
    FROM dbo.tbl_Companies c
    WHERE c.IsDeleted = 0
      AND (@IsActive IS NULL OR c.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR c.CompanyName LIKE N'%' + @SearchTerm + N'%' OR c.CompanyCode LIKE N'%' + @SearchTerm + N'%')
    ORDER BY c.CompanyName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Company_GetById
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        CompanyId, CompanyCode, CompanyName, LegalName, TaxId,
        Email, Phone, Website, AddressLine1, City, Country,
        CurrencyCode, TimeZoneId, IsActive, IsDeleted, CreatedDate, ModifiedDate
    FROM dbo.tbl_Companies
    WHERE CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Company_Insert
    @CompanyCode  NVARCHAR(20),
    @CompanyName  NVARCHAR(150),
    @LegalName    NVARCHAR(200) = NULL,
    @TaxId        NVARCHAR(50) = NULL,
    @Email        NVARCHAR(120) = NULL,
    @Phone        NVARCHAR(30) = NULL,
    @Website      NVARCHAR(150) = NULL,
    @AddressLine1 NVARCHAR(250) = NULL,
    @City         NVARCHAR(80) = NULL,
    @Country      NVARCHAR(80) = NULL,
    @CurrencyCode CHAR(3),
    @TimeZoneId   NVARCHAR(60),
    @IsActive     BIT,
    @CreatedBy    INT,
    @NewCompanyId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Companies WHERE CompanyCode = @CompanyCode AND IsDeleted = 0)
    BEGIN
        THROW 50101, 'A company with this code already exists.', 1;
    END

    INSERT INTO dbo.tbl_Companies (
        CompanyCode, CompanyName, LegalName, TaxId, Email, Phone, Website,
        AddressLine1, City, Country, CurrencyCode, TimeZoneId, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyCode, @CompanyName, @LegalName, @TaxId, @Email, @Phone, @Website,
        @AddressLine1, @City, @Country, @CurrencyCode, @TimeZoneId, @IsActive, 0, @CreatedBy, SYSUTCDATETIME()
    );

    SET @NewCompanyId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Company_Update
    @CompanyId    INT,
    @CompanyCode  NVARCHAR(20),
    @CompanyName  NVARCHAR(150),
    @LegalName    NVARCHAR(200) = NULL,
    @TaxId        NVARCHAR(50) = NULL,
    @Email        NVARCHAR(120) = NULL,
    @Phone        NVARCHAR(30) = NULL,
    @Website      NVARCHAR(150) = NULL,
    @AddressLine1 NVARCHAR(250) = NULL,
    @City         NVARCHAR(80) = NULL,
    @Country      NVARCHAR(80) = NULL,
    @CurrencyCode CHAR(3),
    @TimeZoneId   NVARCHAR(60),
    @IsActive     BIT,
    @ModifiedBy   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Companies WHERE CompanyCode = @CompanyCode AND CompanyId <> @CompanyId AND IsDeleted = 0)
    BEGIN
        THROW 50101, 'Another company already uses this code.', 1;
    END

    UPDATE dbo.tbl_Companies
    SET CompanyCode = @CompanyCode,
        CompanyName = @CompanyName,
        LegalName = @LegalName,
        TaxId = @TaxId,
        Email = @Email,
        Phone = @Phone,
        Website = @Website,
        AddressLine1 = @AddressLine1,
        City = @City,
        Country = @Country,
        CurrencyCode = @CurrencyCode,
        TimeZoneId = @TimeZoneId,
        IsActive = @IsActive,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Company_Delete
    @CompanyId  INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND IsDeleted = 0)
    BEGIN
        THROW 50102, 'Cannot delete a company that has active employees.', 1;
    END

    UPDATE dbo.tbl_Companies
    SET IsDeleted = 1,
        IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Branch Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_Branch_GetAll
    @CompanyId  INT,
    @SearchTerm NVARCHAR(100) = NULL,
    @IsActive   BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        b.BranchId, b.CompanyId, c.CompanyName, b.BranchCode, b.BranchName,
        b.Email, b.Phone, b.AddressLine1, b.City, b.Country, b.IsHeadOffice,
        b.IsActive, b.CreatedDate,
        (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.BranchId = b.BranchId AND e.IsDeleted = 0) AS EmployeeCount
    FROM dbo.tbl_Branches b
    INNER JOIN dbo.tbl_Companies c ON b.CompanyId = c.CompanyId
    WHERE b.CompanyId = @CompanyId
      AND b.IsDeleted = 0
      AND (@IsActive IS NULL OR b.IsActive = @IsActive)
      AND (@SearchTerm IS NULL OR b.BranchName LIKE N'%' + @SearchTerm + N'%' OR b.BranchCode LIKE N'%' + @SearchTerm + N'%' OR b.City LIKE N'%' + @SearchTerm + N'%')
    ORDER BY b.IsHeadOffice DESC, b.BranchName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Branch_GetById
    @CompanyId INT,
    @BranchId  INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        b.BranchId, b.CompanyId, c.CompanyName, b.BranchCode, b.BranchName,
        b.Email, b.Phone, b.AddressLine1, b.City, b.Country, b.IsHeadOffice,
        b.IsActive, b.CreatedDate, b.ModifiedDate
    FROM dbo.tbl_Branches b
    INNER JOIN dbo.tbl_Companies c ON b.CompanyId = c.CompanyId
    WHERE b.BranchId = @BranchId
      AND b.CompanyId = @CompanyId
      AND b.IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Branch_Insert
    @CompanyId    INT,
    @BranchCode   NVARCHAR(20),
    @BranchName   NVARCHAR(120),
    @Email        NVARCHAR(120) = NULL,
    @Phone        NVARCHAR(30) = NULL,
    @AddressLine1 NVARCHAR(250) = NULL,
    @City         NVARCHAR(80) = NULL,
    @Country      NVARCHAR(80) = NULL,
    @IsHeadOffice BIT,
    @IsActive     BIT,
    @CreatedBy    INT,
    @NewBranchId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = @BranchCode AND IsDeleted = 0)
    BEGIN
        THROW 50111, 'A branch with this code already exists in your company.', 1;
    END

    IF @IsHeadOffice = 1
    BEGIN
        UPDATE dbo.tbl_Branches
        SET IsHeadOffice = 0, ModifiedBy = @CreatedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND IsHeadOffice = 1 AND IsDeleted = 0;
    END

    INSERT INTO dbo.tbl_Branches (
        CompanyId, BranchCode, BranchName, Email, Phone, AddressLine1, City, Country,
        IsHeadOffice, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @BranchCode, @BranchName, @Email, @Phone, @AddressLine1, @City, @Country,
        @IsHeadOffice, @IsActive, 0, @CreatedBy, SYSUTCDATETIME()
    );

    SET @NewBranchId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Branch_Update
    @BranchId     INT,
    @CompanyId    INT,
    @BranchCode   NVARCHAR(20),
    @BranchName   NVARCHAR(120),
    @Email        NVARCHAR(120) = NULL,
    @Phone        NVARCHAR(30) = NULL,
    @AddressLine1 NVARCHAR(250) = NULL,
    @City         NVARCHAR(80) = NULL,
    @Country      NVARCHAR(80) = NULL,
    @IsHeadOffice BIT,
    @IsActive     BIT,
    @ModifiedBy   INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Branches WHERE CompanyId = @CompanyId AND BranchCode = @BranchCode AND BranchId <> @BranchId AND IsDeleted = 0)
    BEGIN
        THROW 50111, 'Another branch already uses this code.', 1;
    END

    IF @IsHeadOffice = 1
    BEGIN
        UPDATE dbo.tbl_Branches
        SET IsHeadOffice = 0, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
        WHERE CompanyId = @CompanyId AND BranchId <> @BranchId AND IsHeadOffice = 1 AND IsDeleted = 0;
    END

    UPDATE dbo.tbl_Branches
    SET BranchCode = @BranchCode,
        BranchName = @BranchName,
        Email = @Email,
        Phone = @Phone,
        AddressLine1 = @AddressLine1,
        City = @City,
        Country = @Country,
        IsHeadOffice = @IsHeadOffice,
        IsActive = @IsActive,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE BranchId = @BranchId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Branch_Delete
    @CompanyId  INT,
    @BranchId   INT,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND BranchId = @BranchId AND IsDeleted = 0)
    BEGIN
        THROW 50112, 'Cannot delete a branch with assigned employees.', 1;
    END

    UPDATE dbo.tbl_Branches
    SET IsDeleted = 1,
        IsActive = 0,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = SYSUTCDATETIME()
    WHERE BranchId = @BranchId AND CompanyId = @CompanyId;
END
GO
