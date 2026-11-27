# Enterprise HRMS — Deployment & Operations Guide

## 1. Prerequisites
- **.NET 8 SDK** (`8.0.x`) for building and running `HRMS.sln` / `HRMS.Tests`
- **SQL Server 2022** (Windows, Linux, or Docker container)
- **Reverse Proxy / TLS Terminator** (IIS, Nginx, Azure App Service, or Container Ingress) in front of Kestrel for HTTPS termination

---

## 2. Database Provisioning Order (`HRMS.Database`)
Execute scripts in exact numeric order using `sqlcmd` or SQL Server Management Studio (SSMS):

1. **Create Database**
   - `HRMS.Database/01_CreateDatabase.sql`
2. **Tables (`02_Tables/01_*` through `16_*`)**
   - `01_Core_MultiTenant_Tables.sql` ... `16_Notifications_Workflow_SystemAdmin_Tables.sql`
3. **Stored Procedures (`03_StoredProcedures/01_*` through `27_*`)**
   - `01_Auth_StoredProcedures.sql` ... `27_Notifications_Workflow_SystemAdmin_StoredProcedures.sql`
4. **Seed Data (`04_SeedData/01_*` through `14_*`)**
   - `01_InitialSeedData.sql` ... `14_NotificationsWorkflowAdminSeedData.sql`
5. **Phase 15 Optimization & Verification (`05_Optimization_Deployment/`)**
   - `01_Performance_Indexes_And_HealthCheck.sql` (adds covering nonclustered indexes, `sp_System_HealthCheck`, and `sp_System_MaintenanceCleanup`)
   - `02_Master_Verify_Deployment.sql` (confirms required tables, stored procedures, permissions, and health check)

---

## 3. Configuration & Secret Management
Never store database credentials inside source-controlled `appsettings*.json` files. Inject `ConnectionStrings:DefaultConnection` at runtime:

- **Local Development (`dotnet user-secrets`)**:
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=HRMS_DB;Trusted_Connection=True;TrustServerCertificate=True;" --project HRMS.Web
  ```
- **Linux / Container / Cloud Environment Variable**:
  ```bash
  export ConnectionStrings__DefaultConnection="Server=sqlserver,1433;Database=HRMS_DB;User Id=hrms_app;Password=<STRONG_PASSWORD>;Encrypt=True;TrustServerCertificate=False;"
  ```

---

## 4. Running Automated Tests (`HRMS.Tests`)
Execute the full xUnit unit test suite prior to release:
```bash
dotnet restore HRMS.sln
dotnet build HRMS.sln -c Release --no-restore
dotnet test HRMS.Tests/HRMS.Tests.csproj -c Release --no-build --verbosity normal
```

---

## 5. Production Deployment Options

### Option A: Docker / Container Deployment
1. Export deployment environment variables (or configure an untracked `.env` file on the host):
   ```bash
   export MSSQL_SA_PASSWORD="<STRONG_SQL_SA_PASSWORD>"
   export HRMS_CONNECTION_STRING="Server=sqlserver,1433;Database=HRMS_DB;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;"
   ```
2. Build and launch:
   ```bash
   docker compose up -d --build
   ```
3. Run the `HRMS.Database` scripts against the running SQL Server instance (`localhost:1433`), then verify `/health` returns `200 OK`:
   ```bash
   curl -f http://localhost:8080/health
   ```

### Option B: Bare-Metal / IIS / Linux Systemd Publish
```bash
dotnet publish HRMS.Web/HRMS.Web.csproj -c Release -o /var/www/hrms
```
- Ensure persistent read/write permissions for the service account on `App_Data/` (private employee documents, resumes, leave attachments, expense receipts, company policies) and `wwwroot/uploads/`.
- Ensure HTTPS forwarding headers (`X-Forwarded-For`, `X-Forwarded-Proto`) are enabled on the reverse proxy (`Program.cs` already registers `UseForwardedHeaders()`).

---

## 6. First-Run Setup & Ongoing Operations
1. Navigate to `/Account/Setup` on an uninitialized database to create the first **SuperAdmin** user (locked automatically once an admin exists).
2. Sign in at `/Account/Login` and verify **Workflows & Admin -> System Health & Ops** (`/SystemAdmin/SystemHealth`).
3. Periodically run **Database Retention Maintenance** from `/SystemAdmin/SystemHealth` (or schedule `dbo.sp_System_MaintenanceCleanup`) to prune aged read/expired notifications and historical login logs while retaining immutable `tbl_AuditLogs`.
