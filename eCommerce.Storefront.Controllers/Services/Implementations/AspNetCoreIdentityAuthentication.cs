using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class AspNetCoreIdentityAuthentication(SignInManager<IdentityUser> signInManager) : ILocalAuthenticationService
    {
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = new User();
            var identityUser = await _signInManager.UserManager.FindByEmailAsync(email ?? string.Empty);

            if (identityUser != null && (await _signInManager.CheckPasswordSignInAsync(identityUser, password, true)).Succeeded)
            {
                // RequireConfirmedEmail is enforced here explicitly: this method uses
                // CheckPasswordSignInAsync, which only validates the password.
                if (!await _signInManager.UserManager.IsEmailConfirmedAsync(identityUser))
                {
                    throw new InvalidOperationException(
                        "Please confirm your email address before signing in. Check your inbox for the confirmation link.");
                }

                user.Id = identityUser.Id;
                // Use the canonical email stored in the Identity store, not the raw login
                // input. The auth-token cookie drives later lookups (GetAuthenticationToken)
                // and the customer profile; echoing raw case variants would silently
                // re-canonicalise the registered address on the next profile save.
                user.Email = identityUser.Email ?? email;
                user.IsAuthenticated = true;
                user.Roles = await _signInManager.UserManager.GetRolesAsync(identityUser);
            }
            else
            {
                throw new InvalidOperationException("Sorry we could not log you in. Please try again.");
            }

            return user;
        }

        public async Task<User> RegisterUserAsync(string email, string password, bool confirmEmail, IEnumerable<string> roles)
        {
            var user = new User();
            var identityUser = new IdentityUser
            {
                UserName = email ?? string.Empty,
                Email = email ?? string.Empty,
                EmailConfirmed = confirmEmail
            };
            var result = await _signInManager.UserManager.CreateAsync(identityUser, password ?? string.Empty);

            if (result.Succeeded)
            {
                user.Id = identityUser.Id;
                user.Email = email;
                // A user who still has to confirm their email address must not be
                // issued an authentication cookie yet.
                user.IsAuthenticated = confirmEmail;

                if (roles?.Count() > 0)
                {
                    var rolesAssigned = true;

                    foreach (var role in roles)
                    {
                        result = await _signInManager.UserManager.AddToRoleAsync(identityUser, role);

                        if (!result.Succeeded)
                        {
                            rolesAssigned = false;
                            user.IsAuthenticated = false;
                            user.Id = user.Email = null;

                            if (result.Errors?.Count() > 0)
                            {
                                throw new InvalidOperationException(result.Errors?.FirstOrDefault()?.Description);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (rolesAssigned)
                    {
                        user.Roles = roles;
                    }
                }
            }
            else
            {
                var firstError = result.Errors?.FirstOrDefault();

                if (firstError != null)
                {
                    // Identity's duplicate-account error description quotes the email
                    // ("Username 'a@b.c' is already taken."). Echoing it verbatim lets
                    // attackers probe which addresses are registered, so map those codes
                    // to a neutral message that does not confirm the account exists.
                    if (string.Equals(firstError.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(firstError.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Registration could not be completed. If you already have an account, please log in.");
                    }

                    throw new InvalidOperationException(firstError.Description);
                }
                else
                {
                    throw new InvalidOperationException("There was a problem creating your account. Please try again.");
                }
            }

            return user;
        }

        public async Task DeleteUserAsync(string userId)
        {
            var identityUser = await _signInManager.UserManager.FindByIdAsync(userId);
            
            if (identityUser != null)
            {
                await _signInManager.UserManager.DeleteAsync(identityUser);
            }
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var identityUser = await _signInManager.UserManager.FindByIdAsync(userId);

            if (identityUser is null)
            {
                return false;
            }

            var result = await _signInManager.UserManager.ConfirmEmailAsync(identityUser, code);

            return result.Succeeded;
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
        {
            var identityUser = await _signInManager.UserManager.FindByIdAsync(userId);

            if (identityUser is null)
            {
                return null;
            }

            return await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(identityUser);
        }
    }
}
