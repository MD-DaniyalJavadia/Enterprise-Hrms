USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 11: Asset Management Stored Procedures
-- ============================================================================

-- 1. Asset Categories
CREATE OR ALTER PROCEDURE dbo.sp_AssetCategory_GetAll
    @CompanyId       INT,
    @IncludeInactive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ac.CategoryId,
        ac.CompanyId,
        ac.CategoryCode,
        ac.CategoryName,
        ac.AssetClass,
        ac.RequiresSerialNumber,
        ac.DefaultDepreciationMonths,
        ac.Description,
        ac.IsActive,
        (SELECT COUNT(1) FROM dbo.tbl_Assets a WHERE a.CategoryId = ac.CategoryId AND a.IsDeleted = 0) AS AssetCount
    FROM dbo.tbl_AssetCategories ac
    WHERE ac.CompanyId = @CompanyId
      AND ac.IsDeleted = 0
      AND (@IncludeInactive = 1 OR ac.IsActive = 1)
    ORDER BY ac.CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetCategory_Save
    @CategoryId                INT,
    @CompanyId                 INT,
    @CategoryCode              NVARCHAR(20),
    @CategoryName              NVARCHAR(100),
    @AssetClass                NVARCHAR(30),
    @RequiresSerialNumber      BIT,
    @DefaultDepreciationMonths INT = NULL,
    @Description               NVARCHAR(300) = NULL,
    @IsActive                  BIT,
    @UserId                    INT,
    @SavedId                   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_AssetCategories
        WHERE CompanyId = @CompanyId AND CategoryCode = @CategoryCode AND CategoryId <> @CategoryId AND IsDeleted = 0
    )
    BEGIN
        THROW 50131, N'An asset category with this code already exists.', 1;
    END

    IF @CategoryId = 0
    BEGIN
        INSERT INTO dbo.tbl_AssetCategories (
            CompanyId, CategoryCode, CategoryName, AssetClass, RequiresSerialNumber,
            DefaultDepreciationMonths, Description, IsActive, CreatedBy
        )
        VALUES (
            @CompanyId, @CategoryCode, @CategoryName, @AssetClass, @RequiresSerialNumber,
            @DefaultDepreciationMonths, @Description, @IsActive, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_AssetCategories
        SET CategoryCode = @CategoryCode,
            CategoryName = @CategoryName,
            AssetClass = @AssetClass,
            RequiresSerialNumber = @RequiresSerialNumber,
            DefaultDepreciationMonths = @DefaultDepreciationMonths,
            Description = @Description,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50132, N'Asset category not found.', 1;
        SET @SavedId = @CategoryId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetCategory_Delete
    @CompanyId  INT,
    @CategoryId INT,
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId AND CategoryId = @CategoryId AND IsDeleted = 0)
    BEGIN
        THROW 50133, N'Cannot delete an asset category that is assigned to existing assets.', 1;
    END

    UPDATE dbo.tbl_AssetCategories
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CategoryId = @CategoryId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO

-- 2. Asset Register & KPI Summary
CREATE OR ALTER PROCEDURE dbo.sp_Asset_GetAll
    @CompanyId  INT,
    @CategoryId INT = NULL,
    @BranchId   INT = NULL,
    @Status     NVARCHAR(20) = NULL,
    @EmployeeId INT = NULL,
    @Search     NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @Soon  DATE = DATEADD(DAY, 45, @Today);

    SELECT
        a.AssetId,
        a.CompanyId,
        a.CategoryId,
        ac.CategoryCode,
        ac.CategoryName,
        ac.AssetClass,
        a.BranchId,
        b.BranchName,
        a.AssetTag,
        a.AssetName,
        a.Brand,
        a.ModelNumber,
        a.SerialNumber,
        a.PurchaseDate,
        a.PurchaseCost,
        a.WarrantyExpiryDate,
        a.VendorName,
        a.ConditionStatus,
        a.AssetStatus,
        a.CurrentEmployeeId,
        e.EmployeeCode AS CurrentEmployeeCode,
        e.FullName AS CurrentEmployeeName,
        d.DepartmentName AS CurrentDepartmentName,
        aa.AllocationId AS ActiveAllocationId,
        aa.AssignedDate AS ActiveAssignedDate,
        aa.EmployeeAcknowledged AS ActiveAllocationAcknowledged,
        a.Notes
    FROM dbo.tbl_Assets a
    INNER JOIN dbo.tbl_AssetCategories ac ON a.CategoryId = ac.CategoryId
    LEFT JOIN dbo.tbl_Branches b ON a.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Employees e ON a.CurrentEmployeeId = e.EmployeeId
    LEFT JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_AssetAllocations aa ON a.AssetId = aa.AssetId AND aa.AllocationStatus = N'Active' AND aa.IsDeleted = 0
    WHERE a.CompanyId = @CompanyId
      AND a.IsDeleted = 0
      AND (@CategoryId IS NULL OR a.CategoryId = @CategoryId)
      AND (@BranchId IS NULL OR a.BranchId = @BranchId)
      AND (@Status IS NULL OR a.AssetStatus = @Status)
      AND (@EmployeeId IS NULL OR a.CurrentEmployeeId = @EmployeeId)
      AND (@Search IS NULL OR @Search = N'' OR
           a.AssetTag LIKE N'%' + @Search + N'%' OR
           a.AssetName LIKE N'%' + @Search + N'%' OR
           ISNULL(a.SerialNumber, N'') LIKE N'%' + @Search + N'%' OR
           ISNULL(a.Brand, N'') LIKE N'%' + @Search + N'%' OR
           ISNULL(e.FullName, N'') LIKE N'%' + @Search + N'%')
    ORDER BY a.AssetId DESC;

    -- KPI Summary (scoped cleanly when EmployeeId is passed for non-managers)
    SELECT
        COUNT(1) AS TotalAssets,
        SUM(CASE WHEN a.AssetStatus = N'Available' THEN 1 ELSE 0 END) AS AvailableAssets,
        SUM(CASE WHEN a.AssetStatus = N'Assigned' THEN 1 ELSE 0 END) AS AssignedAssets,
        SUM(CASE WHEN a.AssetStatus = N'Maintenance' THEN 1 ELSE 0 END) AS MaintenanceAssets,
        SUM(CASE WHEN a.AssetStatus IN (N'Retired', N'Lost') THEN 1 ELSE 0 END) AS RetiredOrLostAssets,
        ISNULL(SUM(a.PurchaseCost), 0) AS TotalInventoryValue,
        SUM(CASE WHEN a.WarrantyExpiryDate IS NOT NULL AND a.WarrantyExpiryDate BETWEEN @Today AND @Soon THEN 1 ELSE 0 END) AS ExpiringWarrantyCount
    FROM dbo.tbl_Assets a
    WHERE a.CompanyId = @CompanyId
      AND a.IsDeleted = 0
      AND (@EmployeeId IS NULL OR a.CurrentEmployeeId = @EmployeeId);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Asset_GetById
    @CompanyId INT,
    @AssetId   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        a.AssetId,
        a.CompanyId,
        a.CategoryId,
        ac.CategoryCode,
        ac.CategoryName,
        ac.AssetClass,
        a.BranchId,
        b.BranchName,
        a.AssetTag,
        a.AssetName,
        a.Brand,
        a.ModelNumber,
        a.SerialNumber,
        a.PurchaseDate,
        a.PurchaseCost,
        a.WarrantyExpiryDate,
        a.VendorName,
        a.ConditionStatus,
        a.AssetStatus,
        a.CurrentEmployeeId,
        e.EmployeeCode AS CurrentEmployeeCode,
        e.FullName AS CurrentEmployeeName,
        d.DepartmentName AS CurrentDepartmentName,
        aa.AllocationId AS ActiveAllocationId,
        aa.AssignedDate AS ActiveAssignedDate,
        aa.EmployeeAcknowledged AS ActiveAllocationAcknowledged,
        a.Notes
    FROM dbo.tbl_Assets a
    INNER JOIN dbo.tbl_AssetCategories ac ON a.CategoryId = ac.CategoryId
    LEFT JOIN dbo.tbl_Branches b ON a.BranchId = b.BranchId
    LEFT JOIN dbo.tbl_Employees e ON a.CurrentEmployeeId = e.EmployeeId
    LEFT JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    LEFT JOIN dbo.tbl_AssetAllocations aa ON a.AssetId = aa.AssetId AND aa.AllocationStatus = N'Active' AND aa.IsDeleted = 0
    WHERE a.CompanyId = @CompanyId AND a.AssetId = @AssetId AND a.IsDeleted = 0;

    -- Allocation History
    SELECT
        aa.AllocationId,
        aa.CompanyId,
        aa.AssetId,
        a.AssetTag,
        a.AssetName,
        ac.CategoryName,
        aa.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        aa.AssignedDate,
        aa.ExpectedReturnDate,
        aa.ReturnedDate,
        aa.ConditionAtIssue,
        aa.ConditionAtReturn,
        aa.AllocationStatus,
        aa.IssueRemarks,
        aa.ReturnRemarks,
        aa.EmployeeAcknowledged,
        aa.AcknowledgedDate
    FROM dbo.tbl_AssetAllocations aa
    INNER JOIN dbo.tbl_Assets a ON aa.AssetId = a.AssetId
    INNER JOIN dbo.tbl_AssetCategories ac ON a.CategoryId = ac.CategoryId
    INNER JOIN dbo.tbl_Employees e ON aa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE aa.CompanyId = @CompanyId AND aa.AssetId = @AssetId AND aa.IsDeleted = 0
    ORDER BY aa.AssignedDate DESC, aa.AllocationId DESC;

    -- Maintenance Logs
    SELECT
        m.MaintenanceId,
        m.CompanyId,
        m.AssetId,
        a.AssetTag,
        a.AssetName,
        m.MaintenanceType,
        m.ServiceDate,
        m.CompletionDate,
        m.VendorOrTechnician,
        m.Cost,
        m.MaintenanceStatus,
        m.Description
    FROM dbo.tbl_AssetMaintenanceLogs m
    INNER JOIN dbo.tbl_Assets a ON m.AssetId = a.AssetId
    WHERE m.CompanyId = @CompanyId AND m.AssetId = @AssetId AND m.IsDeleted = 0
    ORDER BY m.ServiceDate DESC, m.MaintenanceId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Asset_Save
    @AssetId            INT,
    @CompanyId          INT,
    @CategoryId         INT,
    @BranchId           INT = NULL,
    @AssetTag           NVARCHAR(40) = NULL,
    @AssetName          NVARCHAR(150),
    @Brand              NVARCHAR(80) = NULL,
    @ModelNumber        NVARCHAR(80) = NULL,
    @SerialNumber       NVARCHAR(100) = NULL,
    @PurchaseDate       DATE = NULL,
    @PurchaseCost       DECIMAL(18,2),
    @WarrantyExpiryDate DATE = NULL,
    @VendorName         NVARCHAR(120) = NULL,
    @ConditionStatus    NVARCHAR(20),
    @AssetStatus        NVARCHAR(20),
    @Notes              NVARCHAR(500) = NULL,
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EffectiveTag NVARCHAR(40) = NULLIF(LTRIM(RTRIM(ISNULL(@AssetTag, N''))), N'');
    IF @EffectiveTag IS NULL
    BEGIN
        DECLARE @NextSeq INT = ISNULL((SELECT MAX(AssetId) FROM dbo.tbl_Assets WHERE CompanyId = @CompanyId), 0) + 1;
        SET @EffectiveTag = CONCAT(N'AST-', YEAR(SYSUTCDATETIME()), N'-', RIGHT(CONCAT(N'0000', @NextSeq), 4));
    END

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_Assets
        WHERE CompanyId = @CompanyId AND AssetTag = @EffectiveTag AND AssetId <> @AssetId AND IsDeleted = 0
    )
    BEGIN
        THROW 50134, N'An asset with this Asset Tag already exists.', 1;
    END

    IF @SerialNumber IS NOT NULL AND LTRIM(RTRIM(@SerialNumber)) <> N'' AND EXISTS (
        SELECT 1 FROM dbo.tbl_Assets
        WHERE CompanyId = @CompanyId AND SerialNumber = LTRIM(RTRIM(@SerialNumber)) AND AssetId <> @AssetId AND IsDeleted = 0
    )
    BEGIN
        THROW 50135, N'An asset with this Serial Number already exists.', 1;
    END

    IF @AssetId = 0
    BEGIN
        IF @AssetStatus = N'Assigned'
        BEGIN
            THROW 50136, N'Use Assign Asset to allocate a new asset to an employee.', 1;
        END

        INSERT INTO dbo.tbl_Assets (
            CompanyId, CategoryId, BranchId, AssetTag, AssetName, Brand, ModelNumber, SerialNumber,
            PurchaseDate, PurchaseCost, WarrantyExpiryDate, VendorName, ConditionStatus, AssetStatus, Notes, CreatedBy
        )
        VALUES (
            @CompanyId, @CategoryId, @BranchId, @EffectiveTag, @AssetName, @Brand, @ModelNumber, NULLIF(LTRIM(RTRIM(@SerialNumber)), N''),
            @PurchaseDate, @PurchaseCost, @WarrantyExpiryDate, @VendorName, @ConditionStatus, @AssetStatus, @Notes, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF EXISTS (
            SELECT 1 FROM dbo.tbl_AssetAllocations
            WHERE CompanyId = @CompanyId AND AssetId = @AssetId AND AllocationStatus = N'Active' AND IsDeleted = 0
        ) AND @AssetStatus <> N'Assigned' AND @AssetStatus <> N'Maintenance'
        BEGIN
            THROW 50137, N'This asset has an active employee allocation. Return the asset before changing its status.', 1;
        END

        UPDATE dbo.tbl_Assets
        SET CategoryId = @CategoryId,
            BranchId = @BranchId,
            AssetTag = @EffectiveTag,
            AssetName = @AssetName,
            Brand = @Brand,
            ModelNumber = @ModelNumber,
            SerialNumber = NULLIF(LTRIM(RTRIM(@SerialNumber)), N''),
            PurchaseDate = @PurchaseDate,
            PurchaseCost = @PurchaseCost,
            WarrantyExpiryDate = @WarrantyExpiryDate,
            VendorName = @VendorName,
            ConditionStatus = @ConditionStatus,
            AssetStatus = @AssetStatus,
            Notes = @Notes,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0 THROW 50138, N'Asset not found.', 1;
        SET @SavedId = @AssetId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Asset_Delete
    @CompanyId INT,
    @AssetId   INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_AssetAllocations WHERE CompanyId = @CompanyId AND AssetId = @AssetId AND AllocationStatus = N'Active' AND IsDeleted = 0)
    BEGIN
        THROW 50139, N'Cannot delete an asset that is currently assigned to an employee.', 1;
    END

    UPDATE dbo.tbl_Assets
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0 THROW 50138, N'Asset not found.', 1;
END
GO

-- 3. Asset Allocations (Assign, Return, Acknowledge)
CREATE OR ALTER PROCEDURE dbo.sp_AssetAllocation_GetAll
    @CompanyId  INT,
    @AssetId    INT = NULL,
    @EmployeeId INT = NULL,
    @Status     NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        aa.AllocationId,
        aa.CompanyId,
        aa.AssetId,
        a.AssetTag,
        a.AssetName,
        ac.CategoryName,
        aa.EmployeeId,
        e.EmployeeCode,
        e.FullName AS EmployeeName,
        d.DepartmentName,
        aa.AssignedDate,
        aa.ExpectedReturnDate,
        aa.ReturnedDate,
        aa.ConditionAtIssue,
        aa.ConditionAtReturn,
        aa.AllocationStatus,
        aa.IssueRemarks,
        aa.ReturnRemarks,
        aa.EmployeeAcknowledged,
        aa.AcknowledgedDate
    FROM dbo.tbl_AssetAllocations aa
    INNER JOIN dbo.tbl_Assets a ON aa.AssetId = a.AssetId
    INNER JOIN dbo.tbl_AssetCategories ac ON a.CategoryId = ac.CategoryId
    INNER JOIN dbo.tbl_Employees e ON aa.EmployeeId = e.EmployeeId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    WHERE aa.CompanyId = @CompanyId
      AND aa.IsDeleted = 0
      AND a.IsDeleted = 0
      AND (@AssetId IS NULL OR aa.AssetId = @AssetId)
      AND (@EmployeeId IS NULL OR aa.EmployeeId = @EmployeeId)
      AND (@Status IS NULL OR aa.AllocationStatus = @Status)
    ORDER BY CASE WHEN aa.AllocationStatus = N'Active' THEN 0 ELSE 1 END, aa.AssignedDate DESC, aa.AllocationId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetAllocation_Assign
    @CompanyId          INT,
    @AssetId            INT,
    @EmployeeId         INT,
    @AssignedDate       DATE,
    @ExpectedReturnDate DATE = NULL,
    @ConditionAtIssue   NVARCHAR(20),
    @IssueRemarks       NVARCHAR(400) = NULL,
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @CurrentStatus NVARCHAR(20);
    SELECT @CurrentStatus = AssetStatus
    FROM dbo.tbl_Assets WITH (UPDLOCK, HOLDLOCK)
    WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @CurrentStatus IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50140, N'Asset not found.', 1;
    END

    IF @CurrentStatus <> N'Available'
    BEGIN
        ROLLBACK TRAN;
        THROW 50141, N'Only Available assets can be assigned to an employee.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        ROLLBACK TRAN;
        THROW 50142, N'Selected employee is not active or not found.', 1;
    END

    INSERT INTO dbo.tbl_AssetAllocations (
        CompanyId, AssetId, EmployeeId, AssignedDate, ExpectedReturnDate,
        ConditionAtIssue, AllocationStatus, IssuedBy, IssueRemarks, CreatedBy
    )
    VALUES (
        @CompanyId, @AssetId, @EmployeeId, @AssignedDate, @ExpectedReturnDate,
        @ConditionAtIssue, N'Active', @UserId, @IssueRemarks, @UserId
    );
    SET @SavedId = SCOPE_IDENTITY();

    UPDATE dbo.tbl_Assets
    SET AssetStatus = N'Assigned',
        CurrentEmployeeId = @EmployeeId,
        ConditionStatus = @ConditionAtIssue,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AssetId = @AssetId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetAllocation_Return
    @CompanyId         INT,
    @AllocationId      INT,
    @ReturnedDate      DATE,
    @ConditionAtReturn NVARCHAR(20),
    @AllocationStatus  NVARCHAR(20), -- Returned, Damaged, Lost
    @ReturnRemarks     NVARCHAR(400) = NULL,
    @UserId            INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @AssetId INT;
    DECLARE @CurrStatus NVARCHAR(20);

    SELECT @AssetId = AssetId, @CurrStatus = AllocationStatus
    FROM dbo.tbl_AssetAllocations WITH (UPDLOCK, HOLDLOCK)
    WHERE AllocationId = @AllocationId AND CompanyId = @CompanyId AND IsDeleted = 0;

    IF @AssetId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50143, N'Allocation record not found.', 1;
    END

    IF @CurrStatus <> N'Active'
    BEGIN
        ROLLBACK TRAN;
        THROW 50144, N'This asset allocation has already been closed.', 1;
    END

    UPDATE dbo.tbl_AssetAllocations
    SET ReturnedDate = @ReturnedDate,
        ConditionAtReturn = @ConditionAtReturn,
        AllocationStatus = @AllocationStatus,
        ReturnedTo = @UserId,
        ReturnRemarks = @ReturnRemarks,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AllocationId = @AllocationId AND CompanyId = @CompanyId;

    UPDATE dbo.tbl_Assets
    SET CurrentEmployeeId = NULL,
        ConditionStatus = CASE WHEN @ConditionAtReturn IN (N'New', N'Good', N'Fair', N'Damaged') THEN @ConditionAtReturn ELSE ConditionStatus END,
        AssetStatus = CASE
            WHEN @AllocationStatus = N'Lost' THEN N'Lost'
            WHEN @AllocationStatus = N'Damaged' OR @ConditionAtReturn = N'Damaged' THEN N'Maintenance'
            ELSE N'Available'
        END,
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AssetId = @AssetId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetAllocation_Acknowledge
    @CompanyId    INT,
    @AllocationId INT,
    @EmployeeId   INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_AssetAllocations
    SET EmployeeAcknowledged = 1,
        AcknowledgedDate = SYSUTCDATETIME(),
        ModifiedBy = @UserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE AllocationId = @AllocationId
      AND CompanyId = @CompanyId
      AND EmployeeId = @EmployeeId
      AND AllocationStatus = N'Active'
      AND IsDeleted = 0;

    IF @@ROWCOUNT = 0 THROW 50145, N'Active asset allocation for this employee not found.', 1;
END
GO

-- 4. Asset Maintenance Logs
CREATE OR ALTER PROCEDURE dbo.sp_AssetMaintenance_GetAll
    @CompanyId INT,
    @AssetId   INT = NULL,
    @Status    NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.MaintenanceId,
        m.CompanyId,
        m.AssetId,
        a.AssetTag,
        a.AssetName,
        m.MaintenanceType,
        m.ServiceDate,
        m.CompletionDate,
        m.VendorOrTechnician,
        m.Cost,
        m.MaintenanceStatus,
        m.Description
    FROM dbo.tbl_AssetMaintenanceLogs m
    INNER JOIN dbo.tbl_Assets a ON m.AssetId = a.AssetId
    WHERE m.CompanyId = @CompanyId
      AND m.IsDeleted = 0
      AND a.IsDeleted = 0
      AND (@AssetId IS NULL OR m.AssetId = @AssetId)
      AND (@Status IS NULL OR m.MaintenanceStatus = @Status)
    ORDER BY m.ServiceDate DESC, m.MaintenanceId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetMaintenance_Save
    @MaintenanceId      INT,
    @CompanyId          INT,
    @AssetId            INT,
    @MaintenanceType    NVARCHAR(30),
    @ServiceDate        DATE,
    @CompletionDate     DATE = NULL,
    @VendorOrTechnician NVARCHAR(120) = NULL,
    @Cost               DECIMAL(18,2),
    @MaintenanceStatus  NVARCHAR(20),
    @Description        NVARCHAR(500),
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Assets WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND IsDeleted = 0)
    BEGIN
        ROLLBACK TRAN;
        THROW 50146, N'Asset not found.', 1;
    END

    IF @MaintenanceId = 0
    BEGIN
        INSERT INTO dbo.tbl_AssetMaintenanceLogs (
            CompanyId, AssetId, MaintenanceType, ServiceDate, CompletionDate,
            VendorOrTechnician, Cost, MaintenanceStatus, Description, CreatedBy
        )
        VALUES (
            @CompanyId, @AssetId, @MaintenanceType, @ServiceDate, @CompletionDate,
            @VendorOrTechnician, @Cost, @MaintenanceStatus, @Description, @UserId
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_AssetMaintenanceLogs
        SET AssetId = @AssetId,
            MaintenanceType = @MaintenanceType,
            ServiceDate = @ServiceDate,
            CompletionDate = @CompletionDate,
            VendorOrTechnician = @VendorOrTechnician,
            Cost = @Cost,
            MaintenanceStatus = @MaintenanceStatus,
            Description = @Description,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE MaintenanceId = @MaintenanceId AND CompanyId = @CompanyId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRAN;
            THROW 50147, N'Maintenance log not found.', 1;
        END
        SET @SavedId = @MaintenanceId;
    END

    -- Sync Asset Status automatically when entering or completing maintenance
    IF @MaintenanceStatus = N'InProgress'
    BEGIN
        UPDATE dbo.tbl_Assets
        SET AssetStatus = N'Maintenance', ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
        WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND AssetStatus IN (N'Available', N'Assigned');
    END
    ELSE IF @MaintenanceStatus = N'Completed'
    BEGIN
        UPDATE dbo.tbl_Assets
        SET AssetStatus = CASE WHEN CurrentEmployeeId IS NOT NULL THEN N'Assigned' ELSE N'Available' END,
            ConditionStatus = CASE WHEN ConditionStatus = N'Damaged' THEN N'Good' ELSE ConditionStatus END,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE AssetId = @AssetId AND CompanyId = @CompanyId AND AssetStatus = N'Maintenance';
    END

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AssetMaintenance_Delete
    @CompanyId     INT,
    @MaintenanceId INT,
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_AssetMaintenanceLogs
    SET IsDeleted = 1, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE MaintenanceId = @MaintenanceId AND CompanyId = @CompanyId AND IsDeleted = 0;
END
GO
