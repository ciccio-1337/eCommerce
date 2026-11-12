# Security Policy

## Reporting a vulnerability

Do not open a public issue for a security problem. Report it privately to the
repository maintainers with:

- a description of the issue and the affected endpoint or component;
- reproduction steps or a proof of concept;
- the impact you believe it has;
- any suggested remediation.

You will receive an acknowledgement and a status update as the report is
triaged. Please give the maintainers reasonable time to release a fix before
disclosing the issue publicly.

## Supported versions

Security fixes are applied to the `main` branch. Older branches are not
maintained.

## Security controls in this repository

- **No default credentials.** The seeded administrator was removed. The first
  administrator is provisioned at startup from `Admin:Email` and
  `Admin:Password`, and only when no administrator exists. The bootstrap
  refuses short or known-default passwords and will **not** promote a
  pre-existing account (an attacker could otherwise register the configured
  address first).
- **Confirmed email required.** Storefront customers must confirm their email
  address before they can sign in.
- **Email changes require re-authentication.** Changing the sign-in address
  requires the current password, revokes other sessions, and notifies the old
  address.
- **Two-factor accounts fail closed.** If an account has 2FA enabled, the
  password-only storefront and backoffice sign-in paths refuse to issue a
  cookie until a supported second-factor flow exists.
- **Server-side session validation.** Authentication cookies carry the Identity
  security stamp and are revalidated every minute. Password changes, role
  changes, and account deletion invalidate existing sessions. Sliding
  expiration is disabled.
- **Antiforgery protection.** All unsafe HTTP methods require an antiforgery
  token. The backoffice client fetches a token from `/api/antiforgery/token`
  and sends it in the `RequestVerificationToken` header.
- **Rate limiting.** Authentication and registration endpoints are throttled
  per client IP, in addition to Identity's per-account lockout. Product search
  and basket mutations have a separate catalog limit.
- **Bounded anonymous input.** Product search clamps paging and refinement
  counts; basket quantities and line counts are bounded.
- **No account enumeration.** Forgot-password and change-password responses do
  not reveal whether an account exists, and unknown accounts perform the same
  password-hashing work.
- **Canonical links.** Emails containing security tokens use
  `App:PublicBaseUrl`, never the request `Host` header.
- **SMTP over TLS.** `MailSettings:Smtp:Network:EnableSsl` defaults to `true`.
- **CSP and hardening headers.** The application sends a Content-Security-Policy
  with a per-request script nonce (no `script-src 'unsafe-inline'` or
  `'unsafe-eval'`; only `'wasm-unsafe-eval'` is kept for Blazor),
  `Permissions-Policy`, `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, COOP, and CORP headers.
- **Secure by default authorization.** A global fallback policy requires an
  authenticated user; public endpoints opt out explicitly with
  `[AllowAnonymous]`.
- **Encrypted data-protection keys.** Outside development the key ring must be
  encrypted with `DataProtection:CertificateThumbprint`; startup fails closed
  otherwise.

## Configuration

Secrets must not be committed. Supply them through environment variables or a
secret store. The following settings are security-sensitive:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Database credentials |
| `Admin:Email`, `Admin:Password` | One-time first-admin bootstrap |
| `App:PublicBaseUrl` | Canonical base URL used in emailed links |
| `AllowedHosts` | Explicit host allow-list; must not be `*` in production |
| `DataProtection:KeysPath` | Persistent data-protection key directory |
| `DataProtection:CertificateThumbprint` | Certificate used to encrypt persisted keys (mandatory outside development) |
| `ReverseProxy:KnownProxies` | Trusted reverse proxies for forwarded headers |
| `MailSettings:Smtp:Network:*` | SMTP host and credentials |

Example production environment variables:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export AllowedHosts='shop.example.com;www.shop.example.com'
export App__PublicBaseUrl=https://shop.example.com
export Admin__Email=admin@example.com
export Admin__Password='<strong unique password>'
export ConnectionStrings__DefaultConnection='Host=db;Database=ecommerce;Username=ecommerce;Password=<secret>'
export DataProtection__KeysPath=/var/lib/ecommerce/keys
export DataProtection__CertificateThumbprint='<certificate thumbprint>'
export ReverseProxy__KnownProxies__0=10.0.0.10
```

The data-protection key directory and local databases are ignored by git and
must be backed up securely. Losing the keys invalidates all cookies and
antiforgery tokens.

## Secret rotation and git history

If a credential was ever committed, changing it is mandatory; deleting the file
in a later commit is not enough. Rotate the database password, SMTP credentials,
any PayPal credentials that appeared in the repository, and the data-protection
keys (rotating the keys invalidates existing cookies and antiforgery tokens).
Then purge the old blobs from history and force-push:

```bash
git filter-repo --path eCommerce.Storefront.UI.Web.MVC/Shop.db --invert-paths
git filter-repo --replace-text replacements.txt   # one "old-secret==>***REMOVED***" per line
git push --force --all
git push --force --tags
```

Coordinate the force-push with every collaborator and re-clone afterwards.
Enable secret scanning and push protection on the hosting platform. CI runs a
full-history gitleaks scan; the explicitly burned values in `.gitleaks.toml`
are temporary allow-list entries and must be deleted after the rewrite.

## Deployment checklist

1. Set an explicit `AllowedHosts` list and `App:PublicBaseUrl`.
2. Provision the first administrator with `Admin:Email`/`Admin:Password`, then
   remove those variables. `Admin:Email` must not already belong to a registered
   account (the bootstrap refuses to promote existing users).
3. Persist `DataProtection:KeysPath` on a volume that survives restarts.
4. Configure `DataProtection:CertificateThumbprint` (or development-only
   plaintext keys if running locally); non-development startup fails closed
   without it.
5. Terminate TLS at the reverse proxy or the application; do not serve the
   storefront over plain HTTP.
6. Run the container as the non-root `app` user (the provided `Dockerfile`
   already does), with a read-only root filesystem, no added capabilities, and
   only port 8080 exposed.
7. Configure SMTP with TLS enabled.
7. Run `dotnet list package --vulnerable --include-transitive` and the gitleaks
   scan as part of every release.
