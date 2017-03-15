﻿using DotNetLive.AccountWeb.ApiClients;
using DotNetLive.AccountWeb.Models.AccountViewModels;
using DotNetLive.AccountWeb.Services;
using DotNetLive.Framework.Web.Models;
using DotNetLive.Framework.Web.WebFramework;
using DotNetLive.Framework.WebApiClient.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SignInStatus = Microsoft.AspNetCore.Identity.SignInResult;

namespace DotNetLive.AccountWeb.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private IHostingEnvironment _hostingEnvironment;
        private AccountApiClient _accountApiClient;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory,
            IHostingEnvironment hostingEnvironment, AccountApiClient accountApiClient)
        {
            //_userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _hostingEnvironment = hostingEnvironment;
            _accountApiClient = accountApiClient;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                if (!Request.IsLocal() && !string.IsNullOrWhiteSpace(returnUrl))
                {
                    //var builder = new UriBuilder(UrlHelper.AddHost(returnUrl, Request.Url));
                    //if (!builder.Host.EndsWith(ConfigurationManager.AppSettings["CookieDomain"], StringComparison.OrdinalIgnoreCase))
                    //{
                    //    var newUrl = string.Format("{0}://{1}{2}/account/loginBySession?sessionKey={3}&isRemeber={4}&returnUrl={5}",
                    //        builder.Scheme, //http
                    //        builder.Host, //host
                    //        (builder.Port == 80 || builder.Port == 443) ? string.Empty : ":" + builder.Port.ToString(),  //port
                    //        SessionKey, //sessionkey
                    //        false, //IsRemember
                    //     WebUtility.UrlEncode(returnUrl));//Return Url
                    //    return RedirectToLocal(newUrl);
                    //}
                }
                return RedirectToUrl(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null, bool forceLoginBySession = false)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                return RedirectToUrl(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                //var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                var result = SignInStatus.Failed;
                var loginResult = _accountApiClient.Login(new LoginQuery() { Email = model.Email, Password = model.Password, DeviceType = 1 });
                if (loginResult.Success)
                {
                    result = SignInStatus.Success;
                    var loginUser = loginResult.ResponseResult.LoginUser;
                    var applicationUser = new ApplicationUser(loginUser.Id, loginUser.UserName, loginUser.Email);
                    applicationUser.AddClaim(new Claim(ApplicationUser.JwtClaimName, loginResult.ResponseResult.Token));
                    await _signInManager.SignInAsync(applicationUser,
                        new AuthenticationProperties()
                        {
                            AllowRefresh = true,
                            IsPersistent = true
                        });
                }

                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToUrl(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser(model.Email, model.Email);// { UserName = model.Email, Email = model.Email };
                throw new NotImplementedException();
                //var result = await _userManager.CreateAsync(user, model.Password);
                //if (result.Succeeded)
                //{
                //    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                //    // Send an email with this link
                //    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                //    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                //    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                //    await _signInManager.SignInAsync(user, isPersistent: false);
                //    _logger.LogInformation(3, "User created a new account with password.");
                //    return RedirectToUrl(returnUrl);
                //}
                //AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost, ActionName("LogOff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet, ActionName("LogOff")]
        public async Task<IActionResult> LogOff(string returnUrl = "")
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            if (_hostingEnvironment.IsDevelopment())
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToUrl(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser(model.Email, model.Email);// { UserName = model.Email, Email = model.Email };
                //var result = await _userManager.CreateAsync(user);
                //if (result.Succeeded)
                //{
                //    result = await _userManager.AddLoginAsync(user, info);
                //    if (result.Succeeded)
                //    {
                //        await _signInManager.SignInAsync(user, isPersistent: false);
                //        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                //        return RedirectToUrl(returnUrl);
                //    }
                //}
                //AddErrors(result);
                throw new NotImplementedException();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            //var user = await _userManager.FindByIdAsync(userId);
            //if (user == null)
            //{
            //    return View("Error");
            //}
            //var result = await _userManager.ConfirmEmailAsync(user, code);
            //return View(result.Succeeded ? "ConfirmEmail" : "Error");
            throw new NotImplementedException();
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                //var user = await _userManager.FindByNameAsync(model.Email);
                //if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                //{
                //    // Don't reveal that the user does not exist or is not confirmed
                //    return View("ForgotPasswordConfirmation");
                //}
                throw new NotImplementedException();

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                //var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                //var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                //return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //var user = await _userManager.FindByNameAsync(model.Email);
            //if (user == null)
            //{
            //    // Don't reveal that the user does not exist
            //    return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            //}
            //var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            //if (result.Succeeded)
            //{
            //    return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            //}
            //AddErrors(result);
            throw new NotImplementedException();
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            //var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            //var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            //return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
            throw new NotImplementedException();
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            //var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            //if (string.IsNullOrWhiteSpace(code))
            //{
            //    return View("Error");
            //}
            throw new NotImplementedException();

            //var message = "Your security code is: " + code;
            //if (model.SelectedProvider == "Email")
            //{
            //    await _emailSender.SendEmailAsync(await _userManager.GetEmailAsync(user), "Security Code", message);
            //}
            //else if (model.SelectedProvider == "Phone")
            //{
            //    await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            //}
            throw new NotImplementedException();

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToUrl(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            throw new NotImplementedException();
            //return _userManager.GetUserAsync(HttpContext.User);
        }

        private IActionResult RedirectToUrl(string returnUrl)
        {
            // if (Url.IsLocalUrl(returnUrl))
            // {
            //     return Redirect(returnUrl);
            // }
            // else
            // {
            //     return RedirectToAction(nameof(HomeController.Index), "Home");
            // }
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            return Redirect(returnUrl);
        }

        #endregion
    }
}
