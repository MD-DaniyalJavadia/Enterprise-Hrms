# Enterprise HRMS — Security Review & Hardening Report

## Scope
Comprehensive security review across all 4 application layers (`HRMS.Models`, `HRMS.Data`, `HRMS.Business`, `HRMS.Web`) and `HRMS.Database` covering authentication, session handling, role-based access control, CSRF protection, database parameterization, file vault isolation, CSV export safety, and HTTP response hardening.

## Verified Security Controls

### 1. Authentication, Password Storage & Brute-Force Protection
- **Password Hashing**: `Pbkdf2PasswordHasher` enforces PBKDF2-SHA256 with `120,000` iterations, 16-byte random salts (`RandomNumberGenerator.GetBytes`), and constant-time comparison (`CryptographicOperations.FixedTimeEquals`).
- **Account Lockout & Rate Limiting**: `AuthService` enforces account lockout for 15 minutes after 5 failed attempts; `AccountController.Login` applies `[EnableRateLimiting("LoginRateLimit")]` (10 requests/minute fixed window).
- **First-Run Setup Guard**: `/Account/Setup` is atomically disabled once any administrator exists (`HasAnyAdminUserAsync()`).
- **Open-Redirect Protection**: `AccountController.Login` validates `Url.IsLocalUrl(model.ReturnUrl)` before redirecting post-login.

### 2. Authorization & Multi-Tenant Isolation
- **Default Deny / Authenticated Access**: Every application controller requires `[Authorize]` and explicit `[HasPermission("MODULE.ACTION")]` policies except intentionally anonymous auth/error/health routes (`AccountController` login/setup/denied, `ErrorController`, `HealthController`).
- **Tenant & Self-Service Scoping**: All queries and mutations pass `User.GetCompanyId()` from authenticated server-side claims. `SelfServiceController` strictly scopes reads/writes to `User.GetEmployeeId()`, and `ManagerPortalController` / `ExitService` verify hierarchical subtree ownership (`sp_Manager_VerifyTeamMember`).

### 3. Cross-Site Request Forgery (CSRF) & State-Changing Endpoints
- Verified across all 22 controllers: **167 of 167** `[HttpPost]` actions enforce `[ValidateAntiForgeryToken]`.
- AJAX requests automatically transmit the anti-forgery token via the `X-CSRF-TOKEN` header configured in `Program.cs` and `hrms-core.js`.

### 4. SQL Injection & Data Access Hygiene
- **Zero Entity Framework & Zero Inline SQL**: Every repository inherits from `BaseRepository` and invokes named SQL Server stored procedures with `CommandType.StoredProcedure` and typed `DynamicParameters`.

### 5. XSS, URL Validation & CSV Export Safety
- Razor views bind model properties with standard HTML encoding (`@Model...`), and JavaScript modal payloads use `System.Text.Json.JsonSerializer.Serialize(...)` inside single-quoted attributes rather than unencoded string concatenation.
- `SystemAdminService.SendBroadcastNotificationAsync`, `RecruitmentService`, and `PerformanceTrainingService` validate URL inputs (`/` relative routes or `http`/`https` schemes) to block scriptable URI schemes.
- `CsvExportHelper` prepends a UTF-8 BOM, escapes quotes per RFC-4180, and neutralizes spreadsheet formula prefixes (`=`, `+`, `-`, `@`, `\t`, `\r`).

### 6. Private File Vault Isolation
- `FileStorageHelper` stores sensitive employee documents, leave attachments, candidate resumes, expense receipts, and company policies outside `wwwroot` under `App_Data/*`.
- All uploads validate extension allowlists, enforce size limits, assign randomized `Guid` filenames, and verify canonical paths (`Path.GetFullPath(...)` + `Path.DirectorySeparatorChar` prefix check) before reading or writing.

### 7. HTTP Transport, Security Headers & Health Probe Hardening (Phase 15)
- `Program.cs` enforces HTTPS redirection, HSTS in non-development environments, Brotli/Gzip response compression, static asset cache control, `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `X-XSS-Protection: 0`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, and a calibrated `Content-Security-Policy` matching the exact script/style/font CDNs used by `_Layout.cshtml`.
- Anonymous `/health` returns only connectivity status and UTC timestamp without leaking database versions, connection strings, tenant counts, or exception messages. Detailed diagnostics reside behind `[HasPermission("ADMIN.VIEW")]` at `/SystemAdmin/SystemHealth`.
