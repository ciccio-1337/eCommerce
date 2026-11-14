# Security Hardening — 2026-11-12

Remediation plan for the full-repository security review of `eCommerce`
(working tree was clean, so the review covered every tracked source/config file
plus git history). Items are grouped by severity; each lists the affected code,
the attack scenario, the fix, and the tests that guard it.

Operational steps that cannot be performed from the repository (credential
rotation, history rewrite, push-protection enablement) are listed in
`SECURITY.md` and flagged **OPERATIONAL** below.

## High

### H-1 — Historical credentials reachable from git history
- **Affected:** historical `appsettings.json` (`sa` password), historical
  `appsettings.Development.json` (`Jwt:SecurityKey`), deleted `Shop.db`
  (seeded admin password hash), SMTP username.
- **Attack:** anyone with a clone recovers the values with `git log -p`; a
  reused DB credential means direct database takeover.
- **Fix:** rotate every value (**OPERATIONAL**), purge history
  (**OPERATIONAL**), enable GitHub push protection (**OPERATIONAL**), and make
  CI scan the full history instead of only new commits. Add the burned values
  to an explicitly documented gitleaks allow-list so the full-history scan can
  go green *after* rotation, with instructions to remove the entries once the
  history is rewritten.
- **Tests:** CI step runs `gitleaks detect --log-opts="--all"` with an explicit
  `--config=/repo/.gitleaks.toml` (inside the container the working directory is
  not the mounted repository, so automatic config lookup fails). Allow-list
  entries are regexes, so `+` in the burned hashes is escaped.

## Medium

### M-1 — Data-protection keys unencrypted at rest
- **Affected:** `Program.cs` (`PersistKeysToFileSystem`, unused
  `DataProtection:CertificateThumbprint`).
- **Attack:** key-directory leak/backup/image snapshot allows forging auth and
  antiforgery cookies for any user.
- **Fix:** `ProtectKeysWithCertificate(thumbprint)` when configured; fail closed
  outside Development when no thumbprint is configured; restrict the key
  directory to the app user on Unix.
- **Tests:** configuration validation helper (production without thumbprint
  throws, development does not, configured thumbprint is accepted).

### M-2 — Email change without re-authentication or old-address notice
- **Affected:** `CustomerController.Detail` POST, `CustomerService`,
  `Views/Customer/Detail.cshtml`.
- **Attack:** a stolen session changes the email to the attacker's address and
  permanently takes over the account; the victim is never warned.
- **Fix:** require the current password when the email changes
  (`ILocalAuthenticationService.ValidatePasswordAsync`), send a notification to
  the **old** address, keep the existing confirmation to the new address.
- **Tests:** wrong/missing password blocks the change; correct password allows
  it; notification is sent to the old address; confirmation still goes to the
  new address.

### M-3 — Admin bootstrap promotes a pre-existing account
- **Affected:** `AdminBootstrapper`.
- **Attack:** an attacker registers the configured `Admin:Email` as a customer
  before first deployment; the next startup promotes it to Admin and the
  attacker keeps their own password.
- **Fix:** never promote an existing account. If a non-admin user with the
  configured email exists, refuse and log an error.
- **Tests:** policy helper + bootstrap decision tests.

### M-4 — Cookie `SecurePolicy` tied to `IsProduction()`
- **Affected:** `BootStrapper` (auth, backoffice, antiforgery cookies).
- **Attack:** Staging/QA behind a TLS terminator issues non-Secure cookies that
  can leak over HTTP.
- **Fix:** `Always` in every environment except local Development.
- **Tests:** policy helper for Development vs. Staging vs. Production.

### M-5 — Admin MFA silently bypassed
- **Affected:** `AspNetCoreIdentityAuthentication.LoginAsync`,
  `eCommerce.Backoffice.Server/Controllers/AccountsController.Login`.
- **Attack:** an admin with 2FA enabled still gets a full session from a
  password alone.
- **Fix:** fail closed when the account requires a second factor; do not issue
  a cookie until a supported second-factor flow completes. (A full TOTP UI is
  out of scope; failing closed removes the bypass.)
- **Tests:** 2FA-enabled account is rejected by both sign-in paths.

## Low

### L-1 — Unauthenticated DoS on product search / unbounded basket quantity
- **Affected:** `ProductController`, `ProductCatalogService`,
  `ProductSearchRequestQueryGenerator`, `BasketController`, `Program.cs`.
- **Fix:** clamp `Index` and page size, cap refinement arrays, bound basket
  quantity and line count, add a catalog rate-limit policy.
- **Tests:** oversized/negative `Index` is rejected or clamped; oversized
  refinement arrays are rejected; excessive quantity is rejected.

### L-2 — Missing length validation vs. DB constraints
- **Affected:** `Customer`, `DeliveryAddress`, `EmailConfirmationRequest`.
- **Fix:** validate 100/50 character limits in the domain model before EF
  persists; add `[Required]` to the confirmation request.
- **Tests:** over-long first/second name and address fields raise
  `EntityBaseIsInvalidException`; valid values pass.

### L-3 — CSP allows `unsafe-inline` / `unsafe-eval`
- **Affected:** `BootStrapper.UseSecurityHeaders`, inline `<script>` blocks.
- **Fix:** per-request nonce for inline scripts; remove `'unsafe-inline'` from
  `script-src`; keep `'wasm-unsafe-eval'` for Blazor. Removed the jTemplates
  dependency entirely (basket and search results are now rendered with DOM
  APIs), so `'unsafe-eval'` is gone as well. **Residual:** `style-src` still
  allows `'unsafe-inline'` because the legacy views use `style="..."` attributes;
  removing it requires moving those styles to classes.
- **Tests:** response carries a nonce, and the `script-src` directive contains
  neither `'unsafe-inline'` nor `'unsafe-eval'` while keeping
  `'wasm-unsafe-eval'`.

### L-4 — Account enumeration / timing
- **Affected:** storefront login, backoffice forgot-password/login.
- **Fix:** already largely neutral in text; add constant-work password check
  for unknown users and keep identical responses.
- **Tests:** unknown and known-but-unconfirmed accounts produce the same
  message.

### L-5 — Payment initiation is a GET
- **Affected:** `PaymentController.CreatePaymentFor`, `Views/Order/Detail.cshtml`.
- **Fix:** make initiation POST-only with antiforgery; update the order view.
- **Tests:** GET is rejected; POST still works.

### L-6 — Slow revocation / no auth audit log
- **Affected:** `BootStrapper` (security-stamp interval).
- **Fix:** shorten the validation interval and log auth/role events with
  structured fields (no secrets).
- **Tests:** logging calls asserted on login failure/success paths.

### L-7 — Backoffice password reset serves customer accounts
- **Affected:** `eCommerce.Backoffice.Server/Controllers/AccountsController.ForgotPassword`.
- **Fix:** only issue reset mail for users in the `Admin` role; keep the
  response generic.
- **Tests:** a confirmed non-admin customer triggers no mail.

### L-8 — Any Admin can grant the Admin role
- **Affected:** `RolesController`.
- **Fix:** reject self-escalation and require the caller to be an Admin for
  `Admin` role changes (already true) plus block granting `Admin` to yourself;
  log role changes.
- **Tests:** self-grant is rejected; granting another user still works.

## Info

- Global fallback authorization policy + explicit `[AllowAnonymous]` on the
  public surface. This includes the pre-authentication backoffice endpoints
  (`login`, `forgotpassword`, `EmailConfirmation`, `ChangePassword`); without
  the attribute the fallback policy answers 401 before the action runs.
- Security headers emitted before HTTPS redirection.
- Backoffice `MainLayout` hides the admin sidebar and top bar until the
  `Admin` auth state resolves (`AuthorizeRouteView` renders authorizing and
  not-authorized content inside the default layout, which previously leaked the
  menu for a moment before the login redirect). `@Body` stays outside the
  `AuthorizeView` so the redirect still runs.
- Docker: pinned base image digest placeholder documented; run flags hardened
  in `docker.command` (`--read-only`, `--cap-drop=ALL`,
  `--security-opt no-new-privileges`, tmpfs for keys).

## Implementation status (2026-11-12)

All repository-side items are implemented; the **OPERATIONAL** items remain
pending because they require credentials/remote access.

| Item | Status | Key change | Tests |
| --- | --- | --- | --- |
| H-1 | Repository side done; rotation/history purge **OPERATIONAL** | `.gitleaks.toml` temporary allow-list, full-history CI scan, `SECURITY.md` runbook | CI gitleaks step |
| M-1 | Done | `SecurityConfiguration`, `Program.cs` key permissions + `ProtectKeysWithCertificate`, fail closed outside Development | `SecurityConfigurationTests` |
| M-2 | Done | Password re-auth on email change, old-address notice, session revocation | `CustomerControllerTests` |
| M-3 | Done | `AdminBootstrapper` refuses to promote existing accounts | `AdminBootstrapperTests` |
| M-4 | Done | `RequireSecureCookies` outside Development | `SecurityConfigurationTests` |
| M-5 | Done (fail closed) | Storefront and backoffice sign-in reject 2FA accounts | `AspNetCoreIdentityAuthenticationTests`, `AccountsControllerSecurityTests` |
| L-1 | Done | Page/refinement/basket bounds + `catalog` rate limit | `ProductControllerBoundsTests`, `BasketControllerBoundsTests` |
| L-2 | Done | Domain length validation + `[Required]` confirmation request | `WhenValidatingLengthLimits` |
| L-3 | Done | Per-request nonce, no `script-src 'unsafe-inline'`, jTemplates/`unsafe-eval` removed; `style-src` inline residual documented | `ContentSecurityPolicyTests` |
| L-4 | Done | Dummy-hash timing equalization and generic responses | `AspNetCoreIdentityAuthenticationTests`, `AccountsControllerSecurityTests` |
| L-5 | Done | `CreatePaymentFor` is `[HttpPost]` + antiforgery form | `PaymentControllerTests.CreatePaymentFor_IsPostOnly` |
| L-6 | Done | 1-minute security-stamp interval + structured auth/role logs | covered by build/DI; log calls added |
| L-7 | Done | Backoffice reset only for `Admin` | `AccountsControllerSecurityTests` |
| L-8 | Done | Self-grant of `Admin` blocked + audit log | `RolesControllerTests` |

### Remaining operational work

1. Rotate the historical database, SMTP, JWT, and seeded-admin credentials.
2. Rewrite git history to remove the old blobs, force-push, and re-clone.
3. Delete the temporary burned-value allow-list entries from `.gitleaks.toml`.
4. Enable secret scanning + push protection on the hosting platform.
5. Provision `DataProtection:CertificateThumbprint` in every non-development
   environment (startup now fails closed without it).

## Verification

- `dotnet build -c Release`: **0 warnings, 0 errors**.
- `dotnet test -c Release`: **122 tests passed**.
- No new secrets in tracked files.
- Views contain no `javascript:` links, inline `on*` handlers, or jTemplates
  calls; every inline `<script>` carries the per-request nonce.
