# Security Remediation — 2026-11-23

Remediation plan for the full-repository security review of `eCommerce`
(clean working tree, branch `main`). Each item lists the affected code, the
attack scenario, the fix, and the tests that guard it.

Operational steps that cannot be completed from the repository (credential
rotation, git-history rewrite, hosting push protection, TLS certificates) are
flagged **OPERATIONAL** and are also listed in `SECURITY.md`.

## High

### H-1 — Burned credentials still reachable from the working tree and history
- **Affected:** `.gitleaks.toml` (literal burned values used as allow-list
  regexes), historical `appsettings.json` / `appsettings.Development.json`,
  historical `Shop.db`, the seeded administrator password hash in the 2021
  migration and its designer snapshots, and git history.
- **Attack:** anyone with a clone can read `.gitleaks.toml` and recover the
  database password, signing key, token, and seeded password hash without
  touching history. A reused credential means database or mail-account
  takeover; the seeded hash is crackable (`Admin-1234`) on any database that
  has not applied `RemoveSeededAdminUser`.
- **Fix:** remove every literal secret from `.gitleaks.toml`; allow-list only
  the immutable migration files that still contain the seeded hash by path, and
  the historical commits by hash until the rewrite is done. Rotate every
  historical credential and purge history (**OPERATIONAL**).
- **Tests:** `dotnet test` stays green; the gitleaks config no longer contains
  any of the burned values (asserted by a repository-level test).

## Medium

### M-1 — Unbounded refinement groups in anonymous product search
- **Affected:** `ProductController.GetProducts`, `DTOs/ProductSearchRequest`.
- **Attack:** an anonymous caller posts a JSON body with a very large
  `RefinementGroups` array. The per-group and per-id limits do not cap the
  number of groups, so the server deserialises and iterates over an arbitrarily
  large payload.
- **Fix:** cap the number of groups (10), reject unknown `GroupId` values, and
  add `[RequestSizeLimit]` to the action.
- **Tests:** too many groups → 400; unknown group → 400; exactly the limit is
  accepted.

### M-2 — No maximum length on storefront credential and profile fields
- **Affected:** `AccountLogOnController.LogOn`, `AccountRegisterController.Register`,
  `CustomerController.Detail`, `Customer.Validate`, backoffice request DTOs.
- **Attack:** multi-megabyte passwords make PBKDF2 hashing expensive per
  request; overlong names/emails reach the database and surface as 500s.
- **Fix:** central `InputLimits` constants (email 256, password 128, name 100,
  address 50) and a `CredentialInputValidator` used by the storefront
  controllers. Raise the backoffice DTO email limit to 256 and keep the
  password at 128. Add `[RequestSizeLimit]` to credential actions.
- **Tests:** oversized email/password is rejected before any Identity call;
  oversized names/emails re-render the form with a validation message; the
  `Customer` model rejects an over-long email.

### M-3 — No MFA and no step-up re-authentication for destructive admin actions
- **Affected:** `AccountsController.CreateAccount/DeleteAccount`,
  `RolesController.AddOrRemoveRoleUser`, backoffice client pages.
- **Attack:** a stolen password yields a full admin session that can create a
  new administrator, grant the Admin role, or delete accounts.
- **Fix:** require the acting administrator's current password (step-up) for
  account creation, account deletion, and role grants/revocations. Keep the
  existing fail-closed 2FA behaviour; TOTP/WebAuthn enrolment remains a
  follow-up because it needs a challenge UI.
- **Tests:** missing/incorrect current password is rejected and no mutation
  happens; the correct password proceeds.

### M-4 — Rate limiting depends on correct proxy/forwarded-header configuration
- **Affected:** `Program.cs`, `appsettings.json`.
- **Attack:** behind an unconfigured proxy every request appears to come from
  the proxy, so one client can exhaust the shared bucket and lock everyone
  out; an over-broad trust list lets a client spoof `X-Forwarded-For`.
- **Fix:** add an explicit `ReverseProxy:Enabled` switch and fail startup
  outside Development when it is enabled without any `KnownProxies`/
  `KnownNetworks`. Document the setting in `SECURITY.md`.
- **Tests:** enabled + empty trust list fails closed; disabled or configured
  trust list passes; Development is exempt.

### M-5 — Registration enables enumeration and pre-account hijacking
- **Affected:** `AccountRegisterController.Register/ConfirmEmail`,
  `AspNetCoreIdentityAuthentication.RegisterUserAsync`.
- **Attack:** a duplicate registration returns a distinct error, revealing
  registered addresses. An attacker can register a victim's address with a
  password they know; the victim's confirmation click then confirms the
  attacker's password.
- **Fix:** return the same "check your email" page for duplicate registrations
  (and notify the existing address best-effort). On confirmation, rotate the
  account to a random password and require the email owner to set a new one
  before signing in. This is the double opt-in step.
- **Tests:** duplicate registration returns the neutral pending view; the
  confirmation path rotates the password and renders the set-password form;
  the reset endpoint rejects an invalid token.

## Low / hardening

| ID | Finding | Fix | Tests |
|---|---|---|---|
| L-1 | Vendored jQuery 3.3.1 / jQuery UI 1.12.1 / Bootstrap 4.3.1 | Upgrade to jQuery 3.7.1, jQuery UI 1.14.1, Bootstrap 4.6.2; remove unused validation scripts | Build + smoke tests |
| L-2 | Dead `eval`-based `json2.js` / `jquery-jtemplates.js` | Delete the files and their `<script>` reference | Build + view test |
| L-3 | HSTS default (30 days, no subdomains/preload) | Configure 1 year, `IncludeSubDomains`, `Preload` | `SecurityConfiguration` test |
| L-4 | `AllowedHosts` wildcard check accepts `*,host`; `UrlHelper.Resolve` falls back to `Host` | Reject any `*` entry; fail closed outside Development when `App:PublicBaseUrl` is missing | `SecurityConfiguration` + `UrlHelper` tests |
| L-5 | Payment initiation reachable by GET | Make `CreatePaymentFor` POST-only; redirect `PlaceOrder` to the order page whose Pay form is a POST | Controller reflection/behavior test |
| L-6 | `ChangePassword` does not verify the target is an Admin | Check `IsInRoleAsync(user, "Admin")` before resetting | `AccountsControllerSecurityTests` |
| L-7 | Npgsql TLS not enforced; default `postgres` user | Require `SSL Mode=VerifyCA`/`VerifyFull` outside Development; document a least-privilege role | `SecurityConfiguration` test |
| L-8 | Docker base image tag not digest-pinned | Pin the image digest | Dockerfile review |
| L-9 | Storefront and backoffice share the cookie path | Scope the admin cookie to `/api`, use a `__Secure-` name and `SameSite=Strict` | `SecurityConfiguration` test |
| L-10 | Admin email logged; no audit trail | Log opaque user IDs only; add audit logs for admin creation/deletion | Logger capture test |
| L-11 | Order service ownership relies on callers | Verify basket/order ownership in `OrderService` | `OrderServiceTests` |
| L-12 | No per-account throttle on reset/resend; no body limits | Add `IAccountActionThrottle` cooldown and `[RequestSizeLimit]` | Throttle tests |
| L-13 | Reset/confirmation tokens travel in URLs | Send `Referrer-Policy: no-referrer` on token endpoints | Header helper test |
| L-14 | Development config can be published; unused client packages | Exclude `appsettings.Development.json` from publish; drop unused client packages | Build |
| L-15 | Pre-registered `Admin:Email` blocks bootstrap | Document the risk and the out-of-band recovery path | `SECURITY.md` |

## Operational follow-ups

1. Rotate every historical credential and purge git history.
2. Enable secret scanning and push protection on the hosting platform.
3. Verify every deployed database has `RemoveSeededAdminUser` applied.
4. Provision the data-protection certificate in every non-development
   environment.
5. Configure `ReverseProxy:Enabled` and the trusted proxy list.
6. Supply database, SMTP, PayPal, and admin settings through environment
   variables or a secret store.
7. Keep the gitleaks full-history scan and the NuGet vulnerability audit in the
   release gate.

## Implementation status (2026-11-24)

All in-repository findings above are implemented and covered by the test suite
(`dotnet build -c Release` and `dotnet test` pass; 197 tests).

- **H-1:** `.gitleaks.toml` contains no literal credential. Historical commits
  are allow-listed by hash and the immutable EF migration snapshots by path.
  `SecretScanningConfigTests` asserts the burned values are absent.
- **M-1:** refinement groups capped at 10, unknown groups rejected, action body
  limited to 64 KiB. Covered by `ProductControllerBoundsTests`.
- **M-2:** `InputLimits`/`CredentialInputValidator` bound email, password, and
  name fields; the `Customer` model rejects an over-long email; backoffice DTOs
  use 256-character emails. Covered by `InputValidationTests`,
  `WhenValidatingLengthLimits`, `AccountLogOnControllerTests`.
- **M-3:** account creation, deletion, and role changes require the acting
  administrator's password. Covered by `AccountsControllerSecurityTests` and
  `RolesControllerTests`.
- **M-4:** startup fails closed when a proxy is declared without a trust list.
  Covered by `SecurityConfigurationTests`.
- **M-5:** duplicate registration returns the neutral pending page; confirmation
  discards the registration password and requires the mailbox owner to set a
  new one. Covered by `AccountRegisterControllerTests`.
- **L-1..L-15:** vendor upgrades/removals, HSTS, host/URL fail-closed checks,
  POST-only payment, admin-only password reset, database TLS, digest-pinned
  runtime image, scoped admin cookie, PII-free/audit logging, order ownership,
  per-account throttling and body limits, `no-referrer` token pages, publish
  exclusions, and documented bootstrap recovery are all in place.

Remaining work is operational only (the follow-up list above): rotate the
historical credentials, rewrite git history, enable hosting-side push
protection, confirm `RemoveSeededAdminUser` on every database, provision the
data-protection certificate, and configure the proxy trust list.
