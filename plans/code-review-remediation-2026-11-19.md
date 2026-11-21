# Code Review Remediation — 2026-11-19

Remediation plan for the read-only full-repository review of `eCommerce`.
Each item lists the affected code, the fix, and the tests that guard it.
Items keep the severity from the review; nothing here changes the public
behaviour of the storefront except where the review found a defect.

## Critical

### C-1 — Checkout redirect lands on a POST-only action (HTTP 405)
- **Affected:** `CheckoutController.PlaceOrder` (redirect), `Views/Order/List.cshtml`
  (GET link), `PaymentController.CreatePaymentFor` (`[HttpPost]`).
- **Problem:** a 302 after a POST is replayed as GET, so the customer never
  reaches the PayPal form; only the order-detail form (a POST) works.
- **Fix:** accept `GET` and `POST` on `CreatePaymentFor` (the action only reads
  the order and renders a form, so it is safe to fetch).
- **Tests:** assert the action is reachable by GET and POST; keep the existing
  "already paid" redirect test.

## High

### H-1 — Removing the last basket line is never persisted
- **Affected:** `BasketService.ModifyBasketAsync`.
- **Problem:** the early return copied from `CreateBasketAsync` skips
  `Save`/`CommitAsync` for an existing basket, so the deletion is lost.
- **Fix:** persist every existing-basket modification, including an empty one;
  keep the "do not create an empty basket" guard only in `CreateBasketAsync`.
- **Tests:** remove the only line, assert `Save`/`CommitAsync` run and the
  reloaded basket is empty.

### H-2 — Static assets (including `dotnet.wasm`) are sent `no-store`
- **Affected:** `BootStrapper.UseSecurityHeaders`, `Program.cs` pipeline.
- **Problem:** the global middleware adds `no-cache, no-store` before
  `UseStaticFiles`/`UseBlazorFrameworkFiles`, disabling all caching.
- **Fix:** apply `no-store` only to dynamic responses; static requests are
  excluded and static middleware sets an explicit cache policy
  (`public, max-age=604800` for wwwroot assets, `public, max-age=3600,
  must-revalidate` for `/_framework` because those names are not
  content-addressed).
- **Tests:** unit-test the "is this a static asset" classifier and the
  cache-header helper.

### H-3 — Email-change step-up password check can be brute-forced
- **Affected:** `CustomerController.Detail` (POST), `ValidatePasswordAsync`.
- **Problem:** `CheckPasswordAsync` bypasses lockout and the endpoint has no
  rate limit, so a stolen cookie allows unlimited password guessing.
- **Fix:** use `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure:
  true)` for step-up and add `[EnableRateLimiting("auth")]`.
- **Tests:** wrong password goes through the lockout-aware API; the POST action
  carries the `auth` rate-limit policy.

## Medium

### M-1 — Concurrent first-basket recovery re-inserts the failed basket
- **Affected:** `BasketService.CreateBasketAsync`, `IUnitOfWork`.
- **Problem:** after `DbUpdateException` the failed basket is still tracked, so
  the retry re-attempts the same INSERT and throws again.
- **Fix:** add `IUnitOfWork.ClearChangeTracker()` and detach the failed graph
  before reloading the winning basket.
- **Tests:** mocked first commit throws `DbUpdateException`; the retry clears
  the tracker, reuses the winner, adds the product and succeeds.

### M-2 — IPN endpoint acknowledges transient failures
- **Affected:** `PaymentController.PaymentCallBack`, `PayPalPaymentService`.
- **Problem:** a transient DB/PayPal failure is answered with `200`, so PayPal
  never retries a paid order that was not recorded.
- **Fix:** keep `200` for malformed/validation failures, return `503` for
  transient failures (`DbUpdateException`, timeouts, HTTP failures); treat
  PayPal 5xx during verification as transient.
- **Tests:** transient exception returns 503; unexpected validation exception
  still returns 200; PayPal 5xx surfaces as `HttpRequestException`.

### M-3 — Backoffice login ignores `IsEmailConfirmed`
- **Affected:** `AccountsController.Login`.
- **Problem:** an admin-created account that still needs email confirmation can
  sign in because `SignInManager` cookie sign-in does not enforce the policy.
- **Fix:** check `IsEmailConfirmedAsync` before issuing the cookie and return
  the same generic error.
- **Tests:** unconfirmed admin gets no session and a generic message.

### M-4 — Account deletion destroys order history
- **Affected:** `AccountsController.DeleteAccount`, `CustomerConfiguration`.
- **Problem:** deleting the customer cascades to every order and order item.
- **Fix:** preserve customers that have orders by anonymising the profile
  (`FirstName`, `SecondName`, rotating `UserId`) and removing addresses/basket;
  only delete the row when the customer has no orders.
- **Tests:** anonymiser preserves the order collection and clears PII; decision
  helper unit test.

### M-5 — `RegisterUserAsync` leaves an orphaned Identity user
- **Affected:** `AspNetCoreIdentityAuthentication.RegisterUserAsync`.
- **Problem:** role-assignment failure nulls `Id`/`Email` and throws, so the
  caller's cleanup cannot run.
- **Fix:** delete the just-created Identity user before throwing and stop
  mutating the returned object.
- **Tests:** role failure deletes the user and throws.

### M-6 — `GetCustomerAsync` overwrites the canonical email
- **Affected:** `CustomerService.GetCustomerAsync`.
- **Fix:** keep the value loaded by `CustomerRepository` (the Identity record is
  the source of truth).
- **Tests:** request case variants do not replace the canonical address.

### M-7 — Role editor swallows save/delete failures
- **Affected:** `eCommerce.Backoffice.Client/Pages/Role/View.razor`.
- **Fix:** surface an `EditRoleResponse` error on every catch/non-success path.
- **Tests:** covered by the existing server-side role guard tests; the Blazor
  component has no test harness in this repository.

### M-8 — Anonymous IPN endpoint is unthrottled
- **Affected:** `PaymentController.PaymentCallBack`, `Program.cs`.
- **Fix:** add a dedicated `ipn` rate-limit policy.
- **Tests:** assert the action carries the `ipn` policy.

## Low

### L-1 — Unknown product IDs are silently ignored
- **Affected:** `BasketService`, `BasketController`.
- **Fix:** throw `ProductNotFoundException` when requested products do not exist;
  the controller answers `404` with an error payload.
- **Tests:** service throws; controller maps it to `NotFound`.

### L-2 — Manual email normalisation in resend flow
- **Affected:** `AccountRegisterController.ResendConfirmation`.
- **Fix:** inject `ILookupNormalizer` and use `NormalizeEmail`.
- **Tests:** covered indirectly by the Identity-backed repository tests; add a
  normaliser-based assertion where practical.

### L-3 — Zero results reports one page
- **Affected:** `ProductCatalogService.NoOfResultPagesGiven`.
- **Fix:** return `0` when there are no results.
- **Tests:** search-result test for an empty result set.

### L-4 — Product cache stores tracked EF entities
- **Affected:** `CachedProductCatalogService`.
- **Fix:** cache immutable `ProductView` DTOs and return a fresh clone per call;
  remove the `title!` null-forgiving mapping.
- **Tests:** existing cache tests plus an "unknown product returns null" test.

### L-5 — Cache TTL comment lies
- **Affected:** `MemoryCacheAdapter`.
- **Fix:** correct the comment (TTL is fixed at ten minutes).
- **Tests:** none needed.

### L-6 — Dead/duplicate members
- **Affected:** `Order.OrderContains`, `Basket.NumberOfItemsInBasket`.
- **Fix:** remove `OrderContains`; make basket specs use `NumberOfItems` and
  delete the duplicate method.
- **Tests:** update the basket specifications.

### L-7 — Hard-coded root-relative URLs
- **Affected:** `Views/Basket/View.cshtml`, `Views/Product/Detail.cshtml`,
  `Views/Shared/Site.cshtml`, `Views/Shared/SiteFooter.cshtml`,
  `Views/Home/Index.cshtml`, `Views/AccountRegister/{RegistrationPending,
  EmailChangePending}.cshtml`.
- **Fix:** generate URLs with `Url.Action`/`Url.Content`.
- **Tests:** none needed; covered by the existing view smoke tests/build.

### L-8 — Null-forgiving product mapping
- **Affected:** `CachedProductCatalogService.GetProductAsync`.
- **Fix:** handled by L-4 (explicit dictionary lookup).

## Verification

- `dotnet build -c Release`
- `dotnet test`
