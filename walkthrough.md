# Enterprise HRMS — Verification & Security Walkthrough

# Security Summary

**Reviewed Files**: All 22 MVC Controllers, Razor Views, Business Services, Dapper Repositories, `FileStorageHelper`, `CsvExportHelper`, `Pbkdf2PasswordHasher`, `PermissionAuthorization`, and `Program.cs`
**Issues Resolved**: 0 critical issues; hardened HTTP response headers, reverse-proxy forwarding, and anonymous health-probe isolation in Phase 15

### Safeguards Verified
| Area | File | How It's Protected |
|---|---|---|
| Passwords & Lockout | `HRMS.Business/Security/Pbkdf2PasswordHasher.cs`, `HRMS.Business/Services/AuthService.cs` | Uses PBKDF2-SHA256 (120,000 iterations) with constant-time comparison, 5-attempt account lockout, and login rate limiting. |
| Access Control & Tenant Isolation | `HRMS.Web/Authorization/PermissionAuthorization.cs`, `HRMS.Web/Controllers/*.cs` | Enforces `[Authorize]` and `[HasPermission(...)]` policies while scoping queries to server-verified `CompanyId`, `EmployeeId`, and manager subtree claims. |
| CSRF Protection | `HRMS.Web/Program.cs`, `HRMS.Web/Controllers/*.cs` | All 167 state-changing `[HttpPost]` endpoints require `[ValidateAntiForgeryToken]` with `X-CSRF-TOKEN` header verification for AJAX calls. |
| Database Queries | `HRMS.Data/Repositories/BaseRepository.cs`, `HRMS.Data/Repositories/*.cs` | All database operations execute SQL Server stored procedures (`CommandType.StoredProcedure`) with typed `DynamicParameters` and zero inline SQL. |
| File Vault Confinement | `HRMS.Web/Helpers/FileStorageHelper.cs` | Enforces extension allowlists, size limits, randomized filenames, private `App_Data` storage, and canonical path-boundary checks. |
| CSV Export & XSS Safety | `HRMS.Web/Helpers/CsvExportHelper.cs`, `HRMS.Web/Program.cs` | Neutralizes spreadsheet formula prefixes on CSV export and enforces `Content-Security-Policy`, `X-Frame-Options`, `Permissions-Policy`, and `nosniff` response headers. |
| Health Probe Isolation | `HRMS.Web/Controllers/HealthController.cs`, `HRMS.Web/Controllers/SystemAdminController.cs` | Anonymous `GET /health` exposes only connectivity status and UTC time, while detailed schema and tenant diagnostics require `ADMIN.VIEW`. |
