# Code Review Fixes - 2026-10-30

## Issues Found and Fixes to Implement

### High Severity

#### 1. Hardcoded admin credentials in seed data (Security)
- **File:** `eCommerce.Storefront.Repository.EntityFrameworkCore/ShopDataContext.cs:39-61`
- **Issue:** Seeds admin user with hardcoded password hash for `Admin-1234`. Bad practice even for dev/test.
- **Fix:** Remove hardcoded seed data for admin user/role assignments.

#### 2. NRE risk in Product model
- **File:** `eCommerce.Storefront.Model/Products/Product.cs:9-33`
- **Issue:** `Name`, `Price`, `Brand`, `Color`, `Category` dereference `Title` without null checks. If Title is null, accessing these throws NRE.
- **Fix:** Add null-conditional operators with safe defaults.

### Medium Severity

#### 3. Typo in Order validation message
- **File:** `eCommerce.Storefront.Model/Orders/Order.cs:177`
- **Issue:** "delilvery address" should be "delivery address"
- **Fix:** Correct the typo.

#### 4. Basket can become empty via ModifyBasketAsync
- **File:** `eCommerce.Storefront.Services/Implementations/BasketService.cs:132-157`
- **Issue:** `ModifyBasketAsync` saves even if basket becomes empty, while `CreateBasketAsync` prevents persisting empty baskets. Inconsistent behavior.
- **Fix:** Add check to prevent persisting empty baskets in ModifyBasketAsync.

#### 5. Static file security headers
- **File:** `eCommerce.Storefront.UI.Web.MVC/Program.cs:37-48`
- **Issue:** `UseSecurityHeaders()` added after `UseStaticFiles()`. Static files short-circuited by static files middleware won't get headers.
- **Fix:** Move `UseSecurityHeaders()` before `UseStaticFiles()` so headers are added to all responses.

## Implementation
All fixes applied.
