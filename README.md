# Enterprise HRMS (.NET 8 MVC + Dapper + SQL Server 2022)

A complete, production-ready, multi-company Human Resource Management System built with **ASP.NET Core 8 MVC**, **Razor Views**, **Dapper ORM**, and **SQL Server 2022 Stored Procedures** across all 15 delivery phases.

## Solution Architecture
- `HRMS.Models` — Entities, DTOs, Enums, Request/Response Models, Strongly Typed ViewModels
- `HRMS.Data` — `SqlConnectionFactory`, `BaseRepository`, Repository Interfaces, Dapper Repositories (`CommandType.StoredProcedure`)
- `HRMS.Business` — Service Interfaces, Business Services, PBKDF2-SHA256 Password Hasher
- `HRMS.Web` — MVC Controllers, Razor Views, Cookie Auth, Permission Policy Authorization, Compression, Security Headers & Health Endpoint
- `HRMS.Tests` — Automated xUnit + Moq Unit Test Suite covering security, RBAC, authentication, and business validations
- `HRMS.Database` — Normalized SQL Server 2022 schema, stored procedures, seed scripts, covering indexes, and verification scripts

## Quick Start & Deployment
1. Execute `HRMS.Database` scripts in SQL Server 2022 in numeric order:
   - `01_CreateDatabase.sql`
   - `02_Tables/01_*` through `16_*`
   - `03_StoredProcedures/01_*` through `27_*`
   - `04_SeedData/01_*` through `14_*`
   - `05_Optimization_Deployment/01_Performance_Indexes_And_HealthCheck.sql` & `02_Master_Verify_Deployment.sql`
2. Configure `ConnectionStrings:DefaultConnection` using `dotnet user-secrets` or environment variable `ConnectionStrings__DefaultConnection`.
3. Build, test, and run:
   ```bash
   dotnet build HRMS.sln
   dotnet test HRMS.Tests/HRMS.Tests.csproj
   dotnet run --project HRMS.Web
   ```
4. For containerized production deployment, security hardening checklist, and operations instructions, see `DEPLOYMENT_GUIDE.md` and `SECURITY_REVIEW.md`.
