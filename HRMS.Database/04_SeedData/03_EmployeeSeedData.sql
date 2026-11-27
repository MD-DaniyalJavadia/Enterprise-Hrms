USE [HRMS_DB];
GO

-- ============================================================================
-- Phase 3 Seed Data: Initial Employment History, Salary History & Profile Data
-- ============================================================================
DECLARE @CompanyId INT = (SELECT TOP 1 CompanyId FROM dbo.tbl_Companies WHERE CompanyCode = N'ACME01' AND IsDeleted = 0);
IF @CompanyId IS NOT NULL
BEGIN
    -- Backfill onboarding history for seeded employees if missing
    INSERT INTO dbo.tbl_EmployeeEmploymentHistory (CompanyId, EmployeeId, EventType, EffectiveDate, ToBranchName, ToDepartmentName, ToDesignation, Remarks)
    SELECT e.CompanyId, e.EmployeeId, N'Hired', e.HireDate, b.BranchName, d.DepartmentName, des.DesignationTitle, N'Initial hire record'
    FROM dbo.tbl_Employees e
    INNER JOIN dbo.tbl_Branches b ON e.BranchId = b.BranchId
    INNER JOIN dbo.tbl_Departments d ON e.DepartmentId = d.DepartmentId
    INNER JOIN dbo.tbl_Designations des ON e.DesignationId = des.DesignationId
    WHERE e.CompanyId = @CompanyId
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_EmployeeEmploymentHistory h WHERE h.EmployeeId = e.EmployeeId);

    INSERT INTO dbo.tbl_EmployeeSalaryHistory (CompanyId, EmployeeId, PreviousSalary, NewSalary, EffectiveDate, ChangeReason)
    SELECT e.CompanyId, e.EmployeeId, 0, e.BaseSalary, e.HireDate, N'Initial Hire Compensation'
    FROM dbo.tbl_Employees e
    WHERE e.CompanyId = @CompanyId
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_EmployeeSalaryHistory s WHERE s.EmployeeId = e.EmployeeId);

    DECLARE @Emp1 INT = (SELECT TOP 1 EmployeeId FROM dbo.tbl_Employees WHERE CompanyId = @CompanyId AND EmployeeCode = N'EMP-1001');
    IF @Emp1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_EmployeeSkills WHERE EmployeeId = @Emp1)
    BEGIN
        INSERT INTO dbo.tbl_EmployeeEmergencyContacts (CompanyId, EmployeeId, ContactName, Relationship, PrimaryPhone, IsPrimary)
        VALUES (@CompanyId, @Emp1, N'Victor Rostov', N'Spouse', N'+1-212-555-0192', 1);

        INSERT INTO dbo.tbl_EmployeeEducation (CompanyId, EmployeeId, DegreeTitle, InstitutionName, FieldOfStudy, StartYear, CompletionYear, GradeOrGpa)
        VALUES (@CompanyId, @Emp1, N'M.S. Computer Science', N'Columbia University', N'Distributed Systems', 2014, 2016, N'3.9 GPA');

        INSERT INTO dbo.tbl_EmployeeExperience (CompanyId, EmployeeId, EmployerName, JobTitle, StartDate, EndDate, Responsibilities)
        VALUES (@CompanyId, @Emp1, N'FinTech Core Systems', N'Software Engineer', '2016-07-01', '2022-05-01', N'Built high-throughput enterprise payment microservices.');

        INSERT INTO dbo.tbl_EmployeeSkills (CompanyId, EmployeeId, SkillName, ProficiencyLevel, CertificationName, IssuingAuthority)
        VALUES (@CompanyId, @Emp1, N'ASP.NET Core & SQL Server', N'Expert', N'Microsoft Certified: Azure Solutions Architect', N'Microsoft');
    END
END
GO
