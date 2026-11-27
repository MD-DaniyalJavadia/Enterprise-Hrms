USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 11: Document Management Stored Procedures
-- ============================================================================

-- 1. Document Categories
CREATE OR ALTER PROCEDURE dbo.sp_DocumentCategory_GetAll
    @CompanyId       INT,
    @IncludeInactive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dc.CategoryId,
        dc.CompanyId,
        dc.CategoryCode,
        dc.CategoryName,
        dc.ScopeType,
        dc.RequiresAcknowledgment,
        dc.DefaultVisibility,
        dc.RetentionMonths,
        dc.Description,
        dc.IsActive,
        (SELECT COUNT(1) FROM dbo.tbl_CompanyDocuments cd WHERE cd.CategoryId = dc.CategoryId AND cd.IsDeleted = 0) AS DocumentCount
    FROM dbo.tbl_DocumentCategories dc
    WHERE dc.CompanyId = @CompanyId
      AND dc.IsDeleted = 0
      AND (@IncludeInactive = 1 OR dc.IsActive = 1)
    ORDER BY dc.CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DocumentCategory_Save
    @CategoryId             INT,
    @CompanyId              INT,
    @CategoryCode           NVARCHAR(20),
    @CategoryName           NVARCHAR(100),
    @ScopeType              NVARCHAR(20),
    @RequiresAcknowledgment BIT,
    @DefaultVisibility      NVARCHAR(30),
    @RetentionMonths        INT = NULL,
    @Description            NVARCHAR(300) = NULL,
    @IsActive               BIT,
    @UserId                 INT,
    @SavedId                INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_DocumentCategories
        WHERE CompanyId = @CompanyId AND CategoryCode = @CategoryCode AND CategoryId <> @CategoryId AND IsDeleted = 0
    )
    BEGIN
        THROW 50151, N'A document category with this code already exists.', 1;
    END

    IF @CategoryId = 0
    BEGIN
        INSERT INTO dbo.tbl_DocumentCategories (
            CompanyId, CategoryCode, CategoryName, ScopeType, RequiresAcknowledgment,
            DefaultVisibility, RetentionMonths, Description, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @CategoryCode, @CategoryName, @ScopeType, @RequiresAcknowledgment,
            @DefaultVisibility, @RetentionMonths, @Description, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_DocumentCategories
        SET CategoryCode = @CategoryCode,
            CategoryName = @CategoryName,
            ScopeType = @ScopeType,
            RequiresAcknowledgment = @RequiresAcknowledgment,
            DefaultVisibility = @DefaultVisibility,
            RetentionMonths = @RetentionMonths,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50152, N'Document category not found.', 1;
        SET @SavedId = @CategoryId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DocumentCategory_Delete
    @CompanyId  INT,
    @CategoryId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_CompanyDocuments WHERE CompanyId = @CompanyId AND CategoryId = @CategoryId AND IsDeleted = 0)
    BEGIN
        THROW 50153, N'Cannot delete a document category that contains published or draft documents.', 1;
    END

    UPDATE dbo.tbl_DocumentCategories
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- 2. Company Document Repository
CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_GetAll
    @CompanyId  INT,
    @CategoryId INT = NULL,
    @Status     NVARCHAR(20) = NULL,
    @Visibility NVARCHAR(30) = NULL,
    @EmployeeId INT = NULL,
    @CanManage  BIT = 0,
    @IsManager  BIT = 0,
    @Search     NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @Soon  DATE = DATEADD(DAY, 30, @Today);
    DECLARE @EmpDeptId INT = NULL;
    DECLARE @ActiveEmployees INT = (SELECT COUNT(1) FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1);

    IF @EmployeeId IS NOT NULL
    BEGIN
        SELECT TOP 1 @EmpDeptId = DepartmentId
        FROM dbo.tbl_Employees
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0;
    END

    SELECT
        cd.DocumentId,
        cd.CompanyId,
        cd.CategoryId,
        dc.CategoryCode,
        dc.CategoryName,
        dc.ScopeType,
        cd.DepartmentId,
        d.DepartmentName,
        cd.DocumentCode,
        cd.Title,
        cd.VersionNumber,
        cd.Visibility,
        cd.RequiresAcknowledgment,
        cd.EffectiveDate,
        cd.ExpiryDate,
        cd.OriginalFileName,
        cd.StoredFileName,
        cd.ContentType,
        cd.FileSizeBytes,
        cd.Description,
        cd.DocumentStatus,
        cd.CreatedDate,
        CASE WHEN myAck.AcknowledgmentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAcknowledgedByMe,
        myAck.AcknowledgedDate AS MyAcknowledgedDate,
        (SELECT COUNT(1) FROM dbo.tbl_DocumentAcknowledgments da
         WHERE da.DocumentId = cd.DocumentId AND da.CompanyId = @CompanyId AND da.AcknowledgedVersion = cd.VersionNumber) AS AcknowledgedCount,
        CASE
            WHEN cd.Visibility = N'DepartmentSpecific' AND cd.DepartmentId IS NOT NULL
                THEN (SELECT COUNT(1) FROM dbo.tbl_Employees e WHERE e.CompanyId = @CompanyId AND e.DepartmentId = cd.DepartmentId AND e.IsDeleted = 0 AND e.IsActive = 1)
            ELSE @ActiveEmployees
        END AS TargetAudienceCount
    FROM dbo.tbl_CompanyDocuments cd
    INNER JOIN dbo.tbl_DocumentCategories dc ON cd.CategoryId = dc.CategoryId
    LEFT JOIN dbo.tbl_Departments d ON cd.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_DocumentAcknowledgments myAck
        ON myAck.DocumentId = cd.DocumentId
       AND myAck.CompanyId = @CompanyId
       AND myAck.EmployeeId = @EmployeeId
       AND myAck.AcknowledgedVersion = cd.VersionNumber
    WHERE cd.CompanyId = @CompanyId
      AND cd.IsDeleted = 0
      AND (@CategoryId IS NULL OR cd.CategoryId = @CategoryId)
      AND (@Status IS NULL OR cd.DocumentStatus = @Status)
      AND (@Visibility IS NULL OR cd.Visibility = @Visibility)
      AND (
          @CanManage = 1
          OR (
              cd.DocumentStatus = N'Published'
              AND (
                  cd.Visibility = N'AllEmployees'
                  OR (cd.Visibility = N'ManagersOnly' AND @IsManager = 1)
                  OR (cd.Visibility = N'DepartmentSpecific' AND @EmpDeptId IS NOT NULL AND cd.DepartmentId = @EmpDeptId)
              )
          )
      )
      AND (@Search IS NULL OR @Search = N'' OR
           cd.DocumentCode LIKE N'%' + @Search + N'%' OR
           cd.Title LIKE N'%' + @Search + N'%' OR
           ISNULL(cd.Description, N'') LIKE N'%' + @Search + N'%')
    ORDER BY cd.EffectiveDate DESC, cd.DocumentId DESC;

    -- KPI Summary obeying the same visibility scope
    SELECT
        COUNT(1) AS TotalDocuments,
        SUM(CASE WHEN cd.DocumentStatus = N'Published' THEN 1 ELSE 0 END) AS PublishedCount,
        SUM(CASE WHEN cd.RequiresAcknowledgment = 1 AND cd.DocumentStatus = N'Published' THEN 1 ELSE 0 END) AS MandatoryCount,
        SUM(CASE WHEN cd.RequiresAcknowledgment = 1 AND cd.DocumentStatus = N'Published' AND @EmployeeId IS NOT NULL AND myAck.AcknowledgmentId IS NULL THEN 1 ELSE 0 END) AS PendingMyAckCount,
        SUM(CASE WHEN cd.ExpiryDate IS NOT NULL AND cd.ExpiryDate BETWEEN @Today AND @Soon AND cd.DocumentStatus = N'Published' THEN 1 ELSE 0 END) AS ExpiringSoonCount
    FROM dbo.tbl_CompanyDocuments cd
    LEFT JOIN dbo.tbl_DocumentAcknowledgments myAck
        ON myAck.DocumentId = cd.DocumentId
       AND myAck.CompanyId = @CompanyId
       AND myAck.EmployeeId = @EmployeeId
       AND myAck.AcknowledgedVersion = cd.VersionNumber
    WHERE cd.CompanyId = @CompanyId
      AND cd.IsDeleted = 0
      AND (
          @CanManage = 1
          OR (
              cd.DocumentStatus = N'Published'
              AND (
                  cd.Visibility = N'AllEmployees'
                  OR (cd.Visibility = N'ManagersOnly' AND @IsManager = 1)
                  OR (cd.Visibility = N'DepartmentSpecific' AND @EmpDeptId IS NOT NULL AND cd.DepartmentId = @EmpDeptId)
              )
          )
      );
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_GetById
    @CompanyId  INT,
    @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        cd.DocumentId,
        cd.CompanyId,
        cd.CategoryId,
        dc.CategoryCode,
        dc.CategoryName,
        dc.ScopeType,
        cd.DepartmentId,
        d.DepartmentName,
        cd.DocumentCode,
        cd.Title,
        cd.VersionNumber,
        cd.Visibility,
        cd.RequiresAcknowledgment,
        cd.EffectiveDate,
        cd.ExpiryDate,
        cd.OriginalFileName,
        cd.StoredFileName,
        cd.ContentType,
        cd.FileSizeBytes,
        cd.Description,
        cd.DocumentStatus,
        cd.CreatedDate
    FROM dbo.tbl_CompanyDocuments cd
    INNER JOIN dbo.tbl_DocumentCategories dc ON cd.CategoryId = dc.CategoryId
    LEFT JOIN dbo.tbl_Departments d ON cd.DepartmentId = d.DepartmentId
    WHERE cd.CompanyId = @CompanyId AND cd.DocumentId = @DocumentId AND cd.IsDeleted = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_Save
    @DocumentId             INT,
    @CompanyId              INT,
    @CategoryId             INT,
    @DepartmentId           INT = NULL,
    @DocumentCode           NVARCHAR(30) = NULL,
    @Title                  NVARCHAR(180),
    @VersionNumber          NVARCHAR(20),
    @Visibility             NVARCHAR(30),
    @RequiresAcknowledgment BIT,
    @EffectiveDate          DATE,
    @ExpiryDate             DATE = NULL,
    @OriginalFileName       NVARCHAR(255) = NULL,
    @StoredFileName         NVARCHAR(255) = NULL,
    @ContentType            NVARCHAR(120) = NULL,
    @FileSizeBytes          BIGINT = NULL,
    @Description            NVARCHAR(500) = NULL,
    @DocumentStatus         NVARCHAR(20),
    @UserId                 INT,
    @SavedId                INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EffectiveCode NVARCHAR(30) = NULLIF(LTRIM(RTRIM(ISNULL(@DocumentCode, N''))), N'');
    IF @EffectiveCode IS NULL
    BEGIN
        DECLARE @NextSeq INT = ISNULL((SELECT MAX(DocumentId) FROM dbo.tbl_CompanyDocuments WHERE CompanyId = @CompanyId), 0) + 1;
        SET @EffectiveCode = CONCAT(N'DOC-', YEAR(SYSUTCDATETIME()), N'-', RIGHT(CONCAT(N'0000', @NextSeq), 4));
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_CompanyDocuments
        WHERE CompanyId = @CompanyId AND DocumentCode = @EffectiveCode AND DocumentId <> @DocumentId AND IsDeleted = 0
    )
    BEGIN
        THROW 50154, N'A document with this Document Code already exists.', 1;
    END

    IF @DocumentId = 0
    BEGIN
        IF @StoredFileName IS NULL OR @OriginalFileName IS NULL
        BEGIN
            THROW 50155, N'Please upload a document file.', 1;
        END

        INSERT INTO dbo.tbl_CompanyDocuments (
            CompanyId, CategoryId, DepartmentId, DocumentCode, Title, VersionNumber, Visibility,
            RequiresAcknowledgment, EffectiveDate, ExpiryDate, OriginalFileName, StoredFileName,
            ContentType, FileSizeBytes, Description, DocumentStatus, CreatedBy
        )
        VALUES (
            @CompanyId, @CategoryId, @DepartmentId, @EffectiveCode, @Title, @VersionNumber, @Visibility,
            @RequiresAcknowledgment, @EffectiveDate, @ExpiryDate, @OriginalFileName, @StoredFileName,
            ISNULL(@ContentType, N'application/octet-stream'), ISNULL(@FileSizeBytes, 0), @Description, @DocumentStatus, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_CompanyDocuments
        SET CategoryId = @CategoryId,
            DepartmentId = @DepartmentId,
            DocumentCode = @EffectiveCode,
            Title = @Title,
            VersionNumber = @VersionNumber,
            Visibility = @Visibility,
            RequiresAcknowledgment = @RequiresAcknowledgment,
            EffectiveDate = @EffectiveDate,
            ExpiryDate = @ExpiryDate,
            OriginalFileName = ISNULL(@OriginalFileName, OriginalFileName),
            StoredFileName = ISNULL(@StoredFileName, StoredFileName),
            ContentType = ISNULL(@ContentType, ContentType),
            FileSizeBytes = ISNULL(@FileSizeBytes, FileSizeBytes),
            Description = @Description,
            DocumentStatus = @DocumentStatus,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE DocumentId = @DocumentId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50156, N'Company document not found.', 1;
        SET @SavedId = @DocumentId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_Delete
    @CompanyId  INT,
    @DocumentId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_CompanyDocuments
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE DocumentId = @DocumentId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0 THROW 50156, N'Company document not found.', 1;
END
GO

-- 3. Policy Acknowledgments
CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_Acknowledge
    @CompanyId  INT,
    @DocumentId INT,
    @EmployeeId INT,
    @IpAddress  NVARCHAR(45) = NULL,
    @Remarks    NVARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ver NVARCHAR(20);
    DECLARE @Status NVARCHAR(20);

    SELECT TOP 1 @Ver = VersionNumber, @Status = DocumentStatus
    FROM dbo.tbl_CompanyDocuments
    WHERE CompanyId = @CompanyId AND DocumentId = @DocumentId AND IsDeleted = 0;

    IF @Ver IS NULL THROW 50156, N'Company document not found.', 1;
    IF @Status <> N'Published' THROW 50157, N'Only published documents can be acknowledged.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.tbl_DocumentAcknowledgments
        WHERE CompanyId = @CompanyId AND DocumentId = @DocumentId AND EmployeeId = @EmployeeId AND AcknowledgedVersion = @Ver
    )
    BEGIN
        INSERT INTO dbo.tbl_DocumentAcknowledgments (CompanyId, DocumentId, EmployeeId, AcknowledgedVersion, IpAddress, Remarks)
        VALUES (@CompanyId, @DocumentId, @EmployeeId, @Ver, @IpAddress, @Remarks);
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CompanyDocument_GetAcknowledgments
    @CompanyId  INT,
    @DocumentId INT = NULL,
    @EmployeeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        da.AcknowledgmentId,
        da.CompanyId,
        da.DocumentId,
        cd.DocumentCode,
        cd.Title AS DocumentTitle,
        cd.VersionNumber AS CurrentVersion,
        da.AcknowledgedVersion,
        da.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        da.AcknowledgedDate,
        da.IpAddress,
        da.Remarks
    FROM dbo.tbl_DocumentAcknowledgments da
    INNER JOIN dbo.tbl_CompanyDocuments cd ON da.DocumentId = cd.DocumentId
    INNER JOIN dbo.tbl_Employees e ON da.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE da.CompanyId = @CompanyId
      AND cd.IsDeleted = 0
      AND (@DocumentId IS NULL OR da.DocumentId = @DocumentId)
      AND (@EmployeeId IS NULL OR da.EmployeeId = @EmployeeId)
    ORDER BY da.AcknowledgedDate DESC, da.AcknowledgmentId DESC;
END
GO

-- 4. Unified Employee Document & Expiry Vault (integrates Phase 3 tbl_EmployeeDocuments)
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeVault_GetAll
    @CompanyId    INT,
    @EmployeeId   INT = NULL,
    @DocumentType NVARCHAR(60) = NULL,
    @ExpiryFilter NVARCHAR(20) = NULL -- All, Expired, ExpiringSoon, Valid
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @Soon  DATE = DATEADD(DAY, 30, @Today);

    SELECT
        ed.DocumentId,
        ed.CompanyId,
        ed.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        ed.DocumentType,
        ed.DocumentTitle,
        ed.OriginalFileName,
        ed.StoredFileName,
        ed.ContentType,
        ed.FileSizeBytes,
        ed.ExpiryDate,
        ed.CreatedDate,
        CASE
            WHEN ed.ExpiryDate IS NULL THEN N'NoExpiry'
            WHEN ed.ExpiryDate < @Today THEN N'Expired'
            WHEN ed.ExpiryDate <= @Soon THEN N'ExpiringSoon'
            ELSE N'Valid'
        END AS ExpiryStatus,
        CASE
            WHEN ed.ExpiryDate IS NULL THEN NULL
            ELSE DATEDIFF(DAY, @Today, ed.ExpiryDate)
        END AS DaysUntilExpiry
    FROM dbo.tbl_EmployeeDocuments ed
    INNER JOIN dbo.tbl_Employees e ON ed.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE ed.CompanyId = @CompanyId
      AND ed.IsDeleted = 0
      AND e.IsDeleted = 0
      AND (@EmployeeId IS NULL OR ed.EmployeeId = @EmployeeId)
      AND (@DocumentType IS NULL OR @DocumentType = N'' OR ed.DocumentType = @DocumentType)
      AND (
          @ExpiryFilter IS NULL OR @ExpiryFilter = N'' OR @ExpiryFilter = N'All'
          OR (@ExpiryFilter = N'Expired' AND ed.ExpiryDate IS NOT NULL AND ed.ExpiryDate < @Today)
          OR (@ExpiryFilter = N'ExpiringSoon' AND ed.ExpiryDate IS NOT NULL AND ed.ExpiryDate BETWEEN @Today AND @Soon)
          OR (@ExpiryFilter = N'Valid' AND (ed.ExpiryDate IS NULL OR ed.ExpiryDate > @Soon))
      )
    ORDER BY
        CASE WHEN ed.ExpiryDate IS NOT NULL AND ed.ExpiryDate < @Today THEN 0
             WHEN ed.ExpiryDate IS NOT NULL AND ed.ExpiryDate <= @Soon THEN 1
             ELSE 2 END,
        ed.ExpiryDate ASC,
        ed.CreatedDate DESC;
END
GO
