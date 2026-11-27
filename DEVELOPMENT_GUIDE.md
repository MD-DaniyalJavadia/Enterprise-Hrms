# Enterprise HRMS Development Guide

## Layer Communication Flow
`Controller (HRMS.Web)` -> `IService / Service (HRMS.Business)` -> `IRepository / Repository (HRMS.Data)` -> `Dapper (CommandType.StoredProcedure)` -> `SQL Server 2022 (HRMS_DB)`

## Mandatory Coding Rules
1. Never write inline SQL in controllers or repositories; always invoke stored procedures via `BaseRepository` / `DynamicParameters`.
2. Enforce `CompanyId` isolation from `User.GetCompanyId()` claims in every query and mutation.
3. Protect state-changing actions with `[ValidateAntiForgeryToken]` and `[HasPermission("MODULE.ACTION")]`.
4. Use `dotnet user-secrets` or environment variables (`ConnectionStrings__DefaultConnection`) for SQL credentials in local/prod environments.

## Running & Testing Phase 1
1. Run all scripts in `HRMS.Database` against SQL Server 2022.
2. Set your connection string:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=HRMS_DB;Trusted_Connection=True;TrustServerCertificate=True;" --project HRMS.Web
   ```
3. Build & Run:
   ```bash
   dotnet build HRMS.sln
   dotnet run --project HRMS.Web
   ```
4. Navigate to `/Account/Setup` on first run to create the SuperAdmin user, then sign in at `/Account/Login` and verify the Executive Dashboard at `/Dashboard`.
