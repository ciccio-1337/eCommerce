using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class AspNetCoreIdentityAuthentication : ILocalAuthenticationService
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AspNetCoreIdentityAuthentication(SignInManager<IdentityUser> signInManager,
                                                UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = new User();
            var result = await _signInManager.PasswordSignInAsync(email ?? string.Empty, password ?? string.Empty, false, true);

            if (result.Succeeded)
            {
                var identityUser = await _userManager.FindByEmailAsync(email);

                user.Id = identityUser.Id;
                user.Email = email;
                user.IsAuthenticated = true;
                user.Roles = await _userManager.GetRolesAsync(identityUser);
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
            var identityUser = new IdentityUser { UserName = email ?? string.Empty, Email = email ?? string.Empty, EmailConfirmed = confirmEmail };
            var result = await _userManager.CreateAsync(identityUser, password ?? string.Empty);
                  
            if (result.Succeeded)
            {
                user.Id = identityUser.Id;
                user.Email = email;
                user.IsAuthenticated = true;

                if (roles?.Count() > 0)
                {
                    foreach (var role in roles)
                    {
                        result = await _userManager.AddToRoleAsync(identityUser, role);    

                        if (!result.Succeeded)
                        {
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

                    if (user.IsAuthenticated)
                    {
                        user.Roles = roles;
                    }             
                }
            }
            else
            {
                if (result.Errors?.Count() > 0)
                {
                    throw new InvalidOperationException(result.Errors?.FirstOrDefault()?.Description);
                }
                else
                {
                    throw new InvalidOperationException("There was a problem creating your account. Please try again.");
                }                
            }

            return user;
        }
    }
}
