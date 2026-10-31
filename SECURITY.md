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
  refuses short or known-default passwords.
- **Confirmed email required.** Storefront customers must confirm their email
  address before they can sign in.
- **Server-side session validation.** Authentication cookies carry the Identity
  security stamp and are revalidated on each request. Password changes, role
  changes, and account deletion invalidate existing sessions. Sliding
  expiration is disabled.
- **Antiforgery protection.** All unsafe HTTP methods require an antiforgery
  token. The backoffice client fetches a token from `/api/antiforgery/token`
  and sends it in the `RequestVerificationToken` header.
- **Rate limiting.** Authentication and registration endpoints are throttled
  per client IP, in addition to Identity's per-account lockout.
- **No account enumeration.** Forgot-password and change-password responses do
  not reveal whether an account exists.
- **Canonical links.** Emails containing security tokens use
  `App:PublicBaseUrl`, never the request `Host` header.
- **SMTP over TLS.** `MailSettings:Smtp:Network:EnableSsl` defaults to `true`.
- **CSP and hardening headers.** The application sends a Content-Security-Policy,
  `Permissions-Policy`, `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, COOP, and CORP headers.

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
Enable secret scanning and push protection on the hosting platform.

## Deployment checklist

1. Set an explicit `AllowedHosts` list and `App:PublicBaseUrl`.
2. Provision the first administrator with `Admin:Email`/`Admin:Password`, then
   remove those variables.
3. Persist `DataProtection:KeysPath` on a volume that survives restarts.
4. Terminate TLS at the reverse proxy or the application; do not serve the
   storefront over plain HTTP.
5. Run the container as the non-root `app` user (the provided `Dockerfile`
   already does) and expose only port 8080.
6. Configure SMTP with TLS enabled.
7. Run `dotnet list package --vulnerable --include-transitive` and the gitleaks
   scan as part of every release.
