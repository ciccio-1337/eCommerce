# Security Hardening Plan — 2026-10-30

> **Status: implemented on 2026-10-31.** All code-level fixes below are in the
> working tree. Operational steps (secret rotation, history rewrite, production
> host/key configuration) remain for the deployer; see `SECURITY.md`.
>
> **Verification:** `dotnet build` succeeds with 0 warnings, `dotnet test` passes
> 64/64, the NuGet vulnerability audit reports no known vulnerabilities, and a
> smoke test confirmed the security headers/CSP, antiforgery rejection without a
> token, admin bootstrap, backoffice login plus `/api/accounts/me`, and an
> authenticated antiforgery-protected `POST /api/brands` (201). Production startup
> also fails closed when `App:PublicBaseUrl` is missing or not HTTPS.

Remediation plan for the full-repository security review. Each item lists the
severity, the affected code, the fix, and whether the fix is code-only or also
requires an operational step (secret rotation, history rewrite, infrastructure).

## Critical

### C-1 — Remove the seeded admin account
- **Files:** `eCommerce.Storefront.Repository.EntityFrameworkCore/Migrations/20220225191914_*.cs`,
  `Migrations/ShopDataContextModelSnapshot.cs`.
- **Fix:** add a migration that deletes the seeded `admin@ecommerce.com` user and
  its `AspNetUserRoles` row; remove the user `HasData` from the model snapshot;
  add a startup bootstrap that creates the first admin only from
  `Admin:Email` / `Admin:Password` configuration, refuses weak/default
  passwords, and never ships a credential in the repo.
- **Operational:** rotate/disable the existing account on every deployed
  environment.

### C-2 — Stop tracking the SQLite database
- **Files:** `eCommerce.Storefront.UI.Web.MVC/Shop.db`, `.gitignore`.
- **Fix:** untrack `Shop.db`, add `*.db`/`*.sqlite`/`*.sqlite3` to `.gitignore`,
  apply the C-1 migration to the local database so the hash is removed.
- **Operational:** purge the old blobs from git history and rotate credentials.

## High

### H-1 — Secrets in git history
- **Fix:** add a `gitleaks` configuration and a CI secret-scanning step; add
  `SECURITY.md` with rotation/history-purge instructions; keep all current
  secrets in environment variables/secret stores.
- **Operational:** rotate the DB password, JWT signing key, and SMTP
  credentials; rewrite history with `git filter-repo` and force-push.

### H-2 — Host-header injection in reset/confirmation links
- **Files:** `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`,
  `appsettings.json`, `appsettings.Development.json`, `Program.cs`.
- **Fix:** build links from a configured canonical `App:PublicBaseUrl`; replace
  `AllowedHosts: "*"` with an explicit allow-list; add
  `UseForwardedHeaders` with configurable trusted proxies/networks; fail closed
  in Production if `AllowedHosts` is still `*` or empty.

## Medium

### M-1 — Cookie security-stamp validation and absolute expiry
- **Files:** `BootStrapper.cs`, `AspNetCoreCookieAuthentication.cs`,
  `AccountsController.cs`, `RolesController.cs`.
- **Fix:** add the security-stamp claim at sign-in, validate it with
  `SecurityStampValidator` on every cookie request, disable sliding expiration,
  and refresh the stamp on password/role/account changes.

### M-2 — Backoffice auth state from the server
- **Files:** `CustomAuthStateProvider.cs`, `AccountsController.cs`,
  `eCommerce.Backoffice.Client/Program.cs`, login page.
- **Fix:** add `GET /api/accounts/me`; build the client principal from that
  endpoint instead of `sessionStorage`; remove the hardcoded `Admin` role.

### M-3 — Require verified email on the storefront
- **Files:** `AccountRegisterController.cs`, `AspNetCoreIdentityAuthentication.cs`,
  `BootStrapper.cs`, storefront views, `ILocalAuthenticationService`.
- **Fix:** register with `confirmEmail: false`, send a confirmation link, add a
  storefront confirmation endpoint/view, and set
  `SignIn.RequireConfirmedEmail = true`. Backoffice-created accounts receive a
  confirmation link; when SMTP is not configured they are confirmed directly by
  the creating administrator.

### M-4 — Rate limiting and lockout protection
- **Files:** `Program.cs`, `BootStrapper.cs`, storefront and backoffice auth
  controllers.
- **Fix:** add ASP.NET Core rate limiting policies for login, registration,
  forgot-password, and confirmation endpoints; keep Identity lockout but make
  brute force expensive per IP and per account.

### M-5 — Account enumeration
- **Files:** `AccountsController.cs`.
- **Fix:** return an identical forgot-password response for every email; map
  missing-user and invalid-token change-password failures to the same message;
  keep password-policy errors only when the token itself is valid.

### M-6 — SMTP TLS
- **Files:** `eCommerce.Storefront.Services/Implementations/SmtpService.cs`,
  `appsettings*.json`.
- **Fix:** enable STARTTLS (`EnableSsl = true`) by default, make it configurable,
  and fail closed when TLS is not available.

## Low / Informational

- **L-1:** remove `[IgnoreAntiforgeryToken]` from all backoffice controllers; add
  an antiforgery-token endpoint and a client `DelegatingHandler` that sends the
  `RequestVerificationToken` header on non-GET requests.
- **L-2:** stop logging customer emails and payment tokens; log opaque ids and
  masked addresses.
- **L-3:** add a Content-Security-Policy (and `Permissions-Policy`), move the
  inline loading script in `index.html` to an external file.
- **L-4:** remove the global `NU1903` suppression; add a NuGet vulnerability
  audit to CI.
- **L-5:** run the container as a non-root user, listen on 8080, publish
  8080:8080, and document TLS termination.
- **L-6:** update GitHub Actions to current majors, pin to SHAs, and set
  least-privilege permissions.
- **L-7:** persist data-protection keys to a configurable directory and ignore
  that directory in git.
- **L-8:** prevent deleting the last admin, removing the last admin role, or
  renaming/deleting the built-in `Admin` role.
- **L-9:** remove the unused `ICookieStorageService`/`CookieStorageService`
  (it used `Secure = Request.IsHttps`).
- **L-10:** replace the `innerHTML` loading-message sink with DOM APIs.

## Verification

1. `dotnet build` succeeds with no warnings.
2. `dotnet test` passes.
3. `dotnet list package --vulnerable --include-transitive` reports no known
   vulnerabilities.
4. `gitleaks detect` reports no secrets in the working tree.
5. Manual smoke test: registration → confirmation → login; backoffice login →
   admin API call with antiforgery token; forgot-password link uses the
   configured base URL; admin bootstrap works from environment variables.
