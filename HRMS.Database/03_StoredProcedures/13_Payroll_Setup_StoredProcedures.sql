USE [HRMS_DB];
GO

-- ============================================================================
-- Salary Components Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_SalaryComponent_GetAll
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ComponentId, CompanyId, ComponentCode, ComponentName, ComponentType,
        CalculationType, DefaultValue, IsTaxable, IsStatutory, IsSystemComponent,
        DisplayOrder, IsActive
    FROM dbo.tbl_SalaryComponents
    WHERE CompanyId = @CompanyId AND IsDeleted = 0
    ORDER BY ComponentType, DisplayOrder, ComponentName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalaryComponent_Save
    @ComponentId     INT = 0,
    @CompanyId       INT,
    @ComponentCode   NVARCHAR(20),
    @ComponentName   NVARCHAR(100),
    @ComponentType   NVARCHAR(30),
    @CalculationType NVARCHAR(20),
    @DefaultValue    DECIMAL(18,2),
    @IsTaxable       BIT,
    @IsStatutory     BIT,
    @DisplayOrder    INT,
    @IsActive        BIT,
    @UserId          INT,
    @SavedId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tbl_SalaryComponents
        WHERE CompanyId = @CompanyId AND ComponentCode = @ComponentCode AND ComponentId <> @ComponentId AND IsDeleted = 0
    )
        THROW 50511, 'A salary component with this code already exists.', 1;

    IF @ComponentId = 0
    BEGIN
        INSERT INTO dbo.tbl_SalaryComponents (
            CompanyId, ComponentCode, ComponentName, ComponentType, CalculationType, DefaultValue,
            IsTaxable, IsStatutory, IsSystemComponent, DisplayOrder, IsActive, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @ComponentCode, @ComponentName, @ComponentType, @CalculationType, @DefaultValue,
            @IsTaxable, @IsStatutory, 0, @DisplayOrder, @IsActive, 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_SalaryComponents
        SET ComponentCode = @ComponentCode,
            ComponentName = @ComponentName,
            ComponentType = @ComponentType,
            CalculationType = @CalculationType,
            DefaultValue = @DefaultValue,
            IsTaxable = @IsTaxable,
            IsStatutory = @IsStatutory,
            DisplayOrder = @DisplayOrder,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ComponentId = @ComponentId AND CompanyId = @CompanyId AND IsDeleted = 0;

        SET @SavedId = @ComponentId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SalaryComponent_Delete
    @CompanyId   INT,
    @ComponentId INT,
    @UserId      INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tbl_SalaryComponents WHERE ComponentId = @ComponentId AND CompanyId = @CompanyId AND IsSystemComponent = 1)
        THROW 50512, 'System salary components cannot be deleted.', 1;

    UPDATE dbo.tbl_SalaryComponents
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE ComponentId = @ComponentId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Tax Slabs Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_TaxSlab_GetAll
    @CompanyId INT,
    @TaxYear   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        TaxSlabId, CompanyId, TaxYear, SlabName, MinAnnualIncome, MaxAnnualIncome,
        TaxRatePercent, FixedTaxAmount, IsActive
    FROM dbo.tbl_TaxSlabs
    WHERE CompanyId = @CompanyId
      AND IsDeleted = 0
      AND (@TaxYear IS NULL OR TaxYear = @TaxYear)
    ORDER BY TaxYear DESC, MinAnnualIncome ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TaxSlab_Save
    @TaxSlabId       INT = 0,
    @CompanyId       INT,
    @TaxYear         INT,
    @SlabName        NVARCHAR(80),
    @MinAnnualIncome DECIMAL(18,2),
    @MaxAnnualIncome DECIMAL(18,2) = NULL,
    @TaxRatePercent  DECIMAL(5,2),
    @FixedTaxAmount  DECIMAL(18,2),
    @IsActive        BIT,
    @UserId          INT,
    @SavedId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxAnnualIncome IS NOT NULL AND @MaxAnnualIncome <= @MinAnnualIncome
        THROW 50513, 'Max Annual Income must be greater than Min Annual Income.', 1;

    IF @TaxSlabId = 0
    BEGIN
        INSERT INTO dbo.tbl_TaxSlabs (
            CompanyId, TaxYear, SlabName, MinAnnualIncome, MaxAnnualIncome,
            TaxRatePercent, FixedTaxAmount, IsActive, IsDeleted, CreatedBy, CreatedDate
        )
        VALUES (
            @CompanyId, @TaxYear, @SlabName, @MinAnnualIncome, @MaxAnnualIncome,
            @TaxRatePercent, @FixedTaxAmount, @IsActive, 0, @UserId, SYSUTCDATETIME()
        );
        SET @SavedId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.tbl_TaxSlabs
        SET TaxYear = @TaxYear,
            SlabName = @SlabName,
            MinAnnualIncome = @MinAnnualIncome,
            MaxAnnualIncome = @MaxAnnualIncome,
            TaxRatePercent = @TaxRatePercent,
            FixedTaxAmount = @FixedTaxAmount,
            IsActive = @IsActive,
            ModifiedBy = @UserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE TaxSlabId = @TaxSlabId AND CompanyId = @CompanyId AND IsDeleted = 0;

        SET @SavedId = @TaxSlabId;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TaxSlab_Delete
    @CompanyId INT,
    @TaxSlabId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tbl_TaxSlabs
    SET IsDeleted = 1, IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE TaxSlabId = @TaxSlabId AND CompanyId = @CompanyId;
END
GO

-- ============================================================================
-- Employee Salary Structures Stored Procedures
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.sp_EmployeeSalaryStructure_GetAll
    @CompanyId    INT,
    @DepartmentId INT = NULL,
    @SearchTerm   NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Like NVARCHAR(104) = CASE WHEN @SearchTerm IS NULL OR LTRIM(RTRIM(@SearchTerm)) = N'' THEN NULL ELSE N'%' + LTRIM(RTRIM(@SearchTerm)) + N'%' END;

    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        d.DepartmentName,
        desig.DesignationTitle,
        e.BaseSalary AS AnnualBaseSalary,
        ss.SalaryStructureId,
        ss.EffectiveFrom,
        ISNULL(ss.BasicSalaryMonthly, ROUND(e.BaseSalary / 12.0, 2)) AS BasicSalaryMonthly,
        ISNULL(ss.HourlyOvertimeRate, 0) AS HourlyOvertimeRate,
        ISNULL(ss.TaxRegime, N'Standard') AS TaxRegime,
        ISNULL(ss.PaymentMethod, N'BankTransfer') AS PaymentMethod,
        CASE WHEN ss.SalaryStructureId IS NULL THEN 0 ELSE 1 END AS HasCustomStructure,
        ISNULL(ss.BasicSalaryMonthly, ROUND(e.BaseSalary / 12.0, 2)) + ISNULL((
            SELECT SUM(si.CalculatedMonthlyAmount)
            FROM dbo.tbl_EmployeeSalaryStructureItems si
            INNER JOIN dbo.tbl_SalaryComponents sc ON si.ComponentId = sc.ComponentId
            WHERE si.SalaryStructureId = ss.SalaryStructureId AND si.IsDeleted = 0 AND sc.ComponentType = N'Earning' AND sc.IsDeleted = 0
        ), 0) AS MonthlyGrossPackage,
        ISNULL((
            SELECT SUM(si.CalculatedMonthlyAmount)
            FROM dbo.tbl_EmployeeSalaryStructureItems si
            INNER JOIN dbo.tbl_SalaryComponents sc ON si.ComponentId = sc.ComponentId
            WHERE si.SalaryStructureId = ss.SalaryStructureId AND si.IsDeleted = 0 AND sc.ComponentType = N'Deduction' AND sc.IsDeleted = 0
        ), 0) AS MonthlyRecurringDeductions
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations desig ON e.DesignationId = desig.DesignationId
    OUTER APPLY (
        SELECT TOP 1 *
        FROM dbo.tbl_EmployeeSalaryStructures s
        WHERE s.CompanyId = @CompanyId AND s.EmployeeId = e.EmployeeId AND s.IsActive = 1 AND s.IsDeleted = 0
        ORDER BY s.EffectiveFrom DESC, s.SalaryStructureId DESC
    ) ss
    WHERE e.CompanyId = @CompanyId
      AND e.IsDeleted = 0
      AND e.IsActive = 1
      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
      AND (@Like IS NULL OR e.FullName LIKE @Like OR e.EmployeeCode LIKE @Like)
    ORDER BY e.EmployeeCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeSalaryStructure_GetByEmployee
    @CompanyId  INT,
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StructureId INT = (
        SELECT TOP 1 SalaryStructureId
        FROM dbo.tbl_EmployeeSalaryStructures
        WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsActive = 1 AND IsDeleted = 0
        ORDER BY EffectiveFrom DESC, SalaryStructureId DESC
    );

    -- 1. Header
    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.FullName,
        e.BaseSalary AS AnnualBaseSalary,
        ISNULL(ss.SalaryStructureId, 0) AS SalaryStructureId,
        ISNULL(ss.EffectiveFrom, CAST(GETDATE() AS DATE)) AS EffectiveFrom,
        ISNULL(ss.BasicSalaryMonthly, ROUND((e.BaseSalary / 12.0) * 0.65, 2)) AS BasicSalaryMonthly,
        ISNULL(ss.HourlyOvertimeRate, ROUND(((e.BaseSalary / 12.0) / 160.0) * 1.5, 2)) AS HourlyOvertimeRate,
        ISNULL(ss.TaxRegime, N'Standard') AS TaxRegime,
        ISNULL(ss.PaymentMethod, N'BankTransfer') AS PaymentMethod,
        ss.Remarks
    FROM dbo.tbl_Employees e
    LEFT JOIN dbo.tbl_EmployeeSalaryStructures ss ON ss.SalaryStructureId = @StructureId
    WHERE e.CompanyId = @CompanyId AND e.EmployeeId = @EmployeeId AND e.IsDeleted = 0;

    -- 2. Items joined with all active non-BASIC components
    SELECT
        sc.ComponentId,
        sc.ComponentCode,
        sc.ComponentName,
        sc.ComponentType,
        ISNULL(si.CalculationType, sc.CalculationType) AS CalculationType,
        ISNULL(si.AmountOrPercent, sc.DefaultValue) AS AmountOrPercent,
        ISNULL(si.CalculatedMonthlyAmount, 0) AS CalculatedMonthlyAmount,
        CAST(CASE WHEN si.StructureItemId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsIncluded
    FROM dbo.tbl_SalaryComponents sc
    LEFT JOIN dbo.tbl_EmployeeSalaryStructureItems si
        ON si.SalaryStructureId = @StructureId AND si.ComponentId = sc.ComponentId AND si.IsDeleted = 0
    WHERE sc.CompanyId = @CompanyId
      AND sc.IsDeleted = 0
      AND sc.IsActive = 1
      AND sc.ComponentCode <> N'BASIC'
    ORDER BY sc.ComponentType, sc.DisplayOrder, sc.ComponentName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EmployeeSalaryStructure_Save
    @CompanyId          INT,
    @EmployeeId         INT,
    @EffectiveFrom      DATE,
    @BasicSalaryMonthly DECIMAL(18,2),
    @HourlyOvertimeRate DECIMAL(18,2),
    @TaxRegime          NVARCHAR(40),
    @PaymentMethod      NVARCHAR(30),
    @Remarks            NVARCHAR(300) = NULL,
    @ItemsJson          NVARCHAR(MAX),
    @UserId             INT,
    @SavedId            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsDeleted = 0)
        THROW 50514, 'Employee not found.', 1;

    BEGIN TRAN;

    -- Deactivate previous active structures for this employee
    UPDATE dbo.tbl_EmployeeSalaryStructures
    SET IsActive = 0, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE CompanyId = @CompanyId AND EmployeeId = @EmployeeId AND IsActive = 1 AND IsDeleted = 0;

    INSERT INTO dbo.tbl_EmployeeSalaryStructures (
        CompanyId, EmployeeId, EffectiveFrom, BasicSalaryMonthly, HourlyOvertimeRate,
        TaxRegime, PaymentMethod, Remarks, IsActive, IsDeleted, CreatedBy, CreatedDate
    )
    VALUES (
        @CompanyId, @EmployeeId, @EffectiveFrom, @BasicSalaryMonthly, @HourlyOvertimeRate,
        @TaxRegime, @PaymentMethod, @Remarks, 1, 0, @UserId, SYSUTCDATETIME()
    );

    SET @SavedId = SCOPE_IDENTITY();

    IF @ItemsJson IS NOT NULL AND ISJSON(@ItemsJson) = 1
    BEGIN
        INSERT INTO dbo.tbl_EmployeeSalaryStructureItems (
            CompanyId, SalaryStructureId, ComponentId, CalculationType, AmountOrPercent, CalculatedMonthlyAmount, IsDeleted
        )
        SELECT
            @CompanyId,
            @SavedId,
            j.ComponentId,
            j.CalculationType,
            j.AmountOrPercent,
            CASE
                WHEN j.CalculationType = N'PercentOfBasic' THEN ROUND(@BasicSalaryMonthly * (j.AmountOrPercent / 100.0), 2)
                ELSE ROUND(j.AmountOrPercent, 2)
            END,
            0
        FROM OPENJSON(@ItemsJson)
        WITH (
            ComponentId     INT           '$.ComponentId',
            CalculationType NVARCHAR(20)  '$.CalculationType',
            AmountOrPercent DECIMAL(18,2) '$.AmountOrPercent',
            IsIncluded      BIT           '$.IsIncluded'
        ) j
        INNER JOIN dbo.tbl_SalaryComponents sc ON j.ComponentId = sc.ComponentId AND sc.CompanyId = @CompanyId AND sc.IsDeleted = 0
        WHERE j.IsIncluded = 1;
    END

    -- Sync annual BaseSalary on tbl_Employees with annualized gross package
    DECLARE @AnnualPackage DECIMAL(18,2) = (
        @BasicSalaryMonthly + ISNULL((
            SELECT SUM(si.CalculatedMonthlyAmount)
            FROM dbo.tbl_EmployeeSalaryStructureItems si
            INNER JOIN dbo.tbl_SalaryComponents sc ON si.ComponentId = sc.ComponentId
            WHERE si.SalaryStructureId = @SavedId AND si.IsDeleted = 0 AND sc.ComponentType = N'Earning'
        ), 0)
    ) * 12.0;

    UPDATE dbo.tbl_Employees
    SET BaseSalary = @AnnualPackage, ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE EmployeeId = @EmployeeId AND CompanyId = @CompanyId;

    COMMIT TRAN;
END
GO
