# Full Code Review Fixes — 2026-11-11

Remediation plan for the full-repository review of `eCommerce`. The working tree
was clean, so the review covered the whole repository rather than a diff. Items
are grouped by severity; each one lists the affected files, the problem, the fix,
and the tests that guard it.

> **Status: implemented on 2026-11-11.** See the verification note at the bottom.

## High

### H-1 — `OrderHasBeenPaidFor` is never mapped
- **Files:** `eCommerce.Storefront.Services/MapsterBootStrapper.cs`,
  `eCommerce.Storefront.Controllers/Controllers/PaymentController.cs`,
  `eCommerce.Storefront.UI.Web.MVC/Views/Order/Detail.cshtml`.
- **Problem:** `Order.OrderHasBeenPaidFor()` is a method without a `Get` prefix,
  so Mapster leaves `OrderView.OrderHasBeenPaidFor` at `false`. Paid orders keep
  showing “This order has not been paid” and the customer can start a second
  payment.
- **Fix:** add
  `.Map(dest => dest.OrderHasBeenPaidFor, src => src.OrderHasBeenPaidFor())` and
  guard `PaymentController.CreatePaymentFor` so an already-paid order redirects
  to the order detail instead of opening PayPal again.
- **Tests:** mapping test for a paid `Order → OrderView`; controller test for the
  already-paid guard.

## Medium

### M-1 — Backoffice emails contain HTML but are sent as plain text
- **Files:** `eCommerce.Storefront.Services/Interfaces/IEmailService.cs`,
  `eCommerce.Storefront.Services/Implementations/SmtpService.cs`,
  `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`.
- **Fix:** add an optional `isBodyHtml` argument to `SendMailAsync` (default
  `false`) and pass `true` for the two backoffice HTML bodies. Storefront mail
  stays plain text.

### M-2 — Delivery-address POSTs return HTTP 500 on validation errors
- **Files:** `eCommerce.Storefront.Controllers/Controllers/CheckoutController.cs`,
  `eCommerce.Storefront.Controllers/Controllers/CustomerController.cs`,
  delivery-address views.
- **Problem:** `CustomerService` calls `ThrowExceptionIfInvalid()`, but the POST
  actions only catch “not found” exceptions.
- **Fix:** catch `EntityBaseIsInvalidException`, set `ViewData["IssueMessage"]`
  and re-render the form with the submitted values. Display the message in the
  address views.
- **Tests:** controller tests that an invalid address is re-rendered instead of
  throwing.

### M-3 — Changing the email address does not require re-verification
- **Files:** `eCommerce.Storefront.Services/Implementations/CustomerService.cs`,
  `eCommerce.Storefront.Services/Messaging/CustomerService/ModifyCustomerResponse.cs`,
  `eCommerce.Storefront.Repository.EntityFrameworkCore/Repositories/Implementations/CustomerRepository.cs`,
  `eCommerce.Storefront.Controllers/Controllers/CustomerController.cs`,
  `eCommerce.Storefront.UI.Web.MVC/Views/AccountRegister/EmailChangePending.cshtml`.
- **Fix:** only save the Identity email when it actually changed; reset
  `EmailConfirmed`, update the security stamp, email a confirmation link to the
  new address, sign the customer out and show an “email change pending” page.
- **Tests:** `CustomerService` saves the email only on a real change; controller
  signs out and redirects to the pending page.

### M-4 — Cache locks are per-scoped-instance and two methods are `async` without `await`
- **Files:** `eCommerce.Storefront.Services.Cache/CachedProductCatalogService.cs`.
- **Fix:** make the `Lock` fields `static readonly` so all scoped instances share
  them, convert the two repository-reading helpers to synchronous methods,
  remove the unused `FindAllProductsAsync`, and return deep copies of cached view
  models so callers cannot mutate cache state.
- **Tests:** fake `ICacheStorage` verifies stampede-free reads and that mutating
  a returned view model does not affect the cache.

### M-5 — `RolesController`: silent failures and N+1
- **Files:** `eCommerce.Backoffice.Server/Controllers/RolesController.cs`.
- **Fix:** load the users and `GetUsersInRoleAsync` once instead of calling
  `IsInRoleAsync` per user; null-check the role after creation; check the
  `IdentityResult` of add/remove role and `UpdateSecurityStampAsync`, returning a
  400 instead of a false success.
- **Tests:** covered by code review; the controller depends on Identity managers
  and is exercised through the backoffice UI.

### M-6 — `DeleteAccount` ignores the result of `DeleteAsync`
- **Files:** `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`.
- **Fix:** dispose the transaction with `await using`, check
  `IdentityResult.Succeeded`, roll back and return a 400 on failure, and only
  then delete the `Customer` profiles and commit.

## Low

### L-1 — `SizesController` does not handle `EntityBaseIsInvalidException`
- **Files:** `eCommerce.Backoffice.Server/Controllers/SizesController.cs`.
- **Fix:** mirror `BrandsController`: catch the exception and return 400.

### L-2 — Transactions are not disposed explicitly
- **Files:** `eCommerce.Storefront.Controllers/Controllers/AccountRegisterController.cs`,
  `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`.
- **Fix:** use `await using var transaction = await …BeginTransactionAsync()`.

### L-3 — Creating a basket without a delivery option throws a generic validation error
- **Files:** `eCommerce.Storefront.Services/Implementations/BasketService.cs`,
  `eCommerce.Storefront.Controllers/Controllers/BasketController.cs`.
- **Fix:** throw `DeliveryOptionNotFoundException` when no option exists; return a
  clear 400 from `AddToBasket` instead of a 500.

### L-4 — Malformed IPN or unexpected exceptions return 500 to PayPal
- **Files:** `eCommerce.Storefront.Controllers/Services/Implementations/PayPalPaymentService.cs`,
  `eCommerce.Storefront.Controllers/Controllers/PaymentController.cs`.
- **Fix:** reject a blank `txn_id` before setting `PaymentOk`; wrap the callback
  in a catch-all that logs and returns `Ok()` so PayPal does not retry forever.
- **Tests:** `PayPalPaymentService` tests for missing/blank `txn_id`; controller
  test for an unexpected exception.

### L-5 — Backoffice-created accounts receive no role
- **Files:** `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`.
- **Fix:** assign the `Admin` role by default (the endpoint is already
  administrator-only), deleting the user if the role assignment fails.

### L-6 — `BuildPublicUrl` falls back to the request host
- **Files:** `eCommerce.Storefront.Controllers/Controllers/AccountRegisterController.cs`,
  `eCommerce.Storefront.Controllers/Controllers/CustomerController.cs`,
  `eCommerce.Backoffice.Server/Controllers/AccountsController.cs`.
- **Fix:** inject `IWebHostEnvironment`; use `App:PublicBaseUrl` and throw outside
  Development when it is missing instead of trusting `Request.Host`.

### L-7 — Redundant `string.Format("{0:F}", <string>)` in views
- **Files:** `eCommerce.Storefront.UI.Web.MVC/Views/Checkout/ConfirmOrder.cshtml`,
  `eCommerce.Storefront.UI.Web.MVC/Views/Order/Detail.cshtml`.
- **Fix:** render the already-formatted strings directly.

### L-8 — AJAX “add to basket” leaves the overlay visible on failure
- **Files:** `eCommerce.Storefront.UI.Web.MVC/Views/Product/Detail.cshtml`.
- **Fix:** add a `.fail(...)` handler that hides the overlay and shows an error.

### L-9 — `Role/View.razor` reloads without error handling
- **Files:** `eCommerce.Backoffice.Client/Pages/Role/View.razor`.
- **Fix:** wrap the user-list reloads in `try/catch`, surface an error and remove
  the misleading “Error handled below” comments.

### L-10 — Cache entries are shared and `CategoryView.Id` is `int`
- **Files:** `eCommerce.Storefront.Services.Cache/CachedProductCatalogService.cs`,
  `eCommerce.Storefront.Services/ViewModels/CategoryView.cs`.
- **Fix:** deep-copy cached view models on read; change `CategoryView.Id` to
  `long` to match `Category.Id`.

### L-11 — `SaveEmailAsync` bypasses the Identity normalizer
- **Files:** `eCommerce.Storefront.Repository.EntityFrameworkCore/Repositories/Implementations/CustomerRepository.cs`.
- **Fix:** inject `ILookupNormalizer` and use `NormalizeEmail`/`NormalizeName`.

### L-12 — Duplicate `IPaymentService` registration
- **Files:** `eCommerce.Storefront.UI.Web.MVC/BootStrapper.cs`.
- **Fix:** keep `AddHttpClient<IPaymentService, PayPalPaymentService>()` and remove
  the later `AddScoped<IPaymentService, PayPalPaymentService>()`.

### L-13 — Antiforgery token is read from the first matching input
- **Files:** `eCommerce.Storefront.UI.Web.MVC/Views/Shared/Site.cshtml`,
  `eCommerce.Storefront.UI.Web.MVC/wwwroot/js/site.js`.
- **Fix:** render the global token in a dedicated container/id and read it from
  there.

### L-14 — CI vulnerability audit reports but does not fail
- **Files:** `.github/workflows/dotnet-core.yml`.
- **Fix:** capture the audit output and fail the step when vulnerable packages are
  reported.

### L-15 — Misleading comment about `CheckPasswordSignInAsync`
- **Files:** `eCommerce.Storefront.Controllers/Services/Implementations/AspNetCoreIdentityAuthentication.cs`.
- **Fix:** explain that the sign-in manager already enforces
  `RequireConfirmedEmail` and that the explicit check only provides a clearer
  message.

### L-16 — No way to resend the confirmation email
- **Files:** `eCommerce.Storefront.Controllers/Controllers/AccountRegisterController.cs`,
  `eCommerce.Storefront.UI.Web.MVC/Views/AccountRegister/RegistrationPending.cshtml`.
- **Fix:** add a rate-limited `ResendConfirmation` POST action and a form on the
  pending page. Keep the response neutral to avoid account enumeration.

### L-17 — `AddToBasket` does not handle `CustomerNotFoundException`
- **Files:** `eCommerce.Storefront.Controllers/Controllers/BasketController.cs`.
- **Fix:** return `Unauthorized` (or a clear 400) instead of letting the
  exception become a 500.

## Tests

- `MappingTests`: `Order → OrderView` maps `OrderHasBeenPaidFor`; `Category.Id`
  (`long`) survives `Category → CategoryView`.
- `PaymentControllerTests`: already-paid guard, unexpected callback exception.
- `CustomerControllerTests`: invalid delivery address re-renders with an issue
  message; email change signs out and shows the pending page.
- `CustomerServiceTests`: email is persisted only when it changes.
- `BasketServiceTests`: missing delivery option throws
  `DeliveryOptionNotFoundException`.
- `CachedProductCatalogServiceTests`: cached view models are deep-copied and the
  catalog repository is hit once.
- `PayPalPaymentServiceTests`: blank `txn_id` never marks the payment OK; a valid
  IPN does.

## Verification

1. `dotnet build --nologo -v q` succeeds with 0 warnings and 0 errors.
2. `dotnet test --nologo --no-build --verbosity normal` passes **85/85** tests
   (0 failed, 0 skipped).
3. `dotnet list package --vulnerable --include-transitive` reports no known
   vulnerabilities for any project (and CI now fails if that changes).

The NuGet audit initially flagged `Microsoft.AspNetCore.Components.WebAssembly.Server`
10.0.13 (GHSA-g27p-qf6j-c96c) in the backoffice server and storefront web
projects. Those references, plus `Microsoft.AspNetCore.WebUtilities` in the
backoffice client, were bumped to **10.0.15**, which the audit reports clean.

## Notes

- The implementation was committed on 2026-11-11 (`165294b`, "Fix full code
  review findings and add regression tests"). The `plans/` folder is tracked in
  source control.
