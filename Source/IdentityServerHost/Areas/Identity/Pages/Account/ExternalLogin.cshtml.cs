// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account;
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using IdentityServerHost.Models;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserStore<ApplicationUser> userStore;
    private readonly IUserEmailStore<ApplicationUser> emailStore;
    private readonly IEmailSender emailSender;
    private readonly ILogger<ExternalLoginModel> logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        ILogger<ExternalLoginModel> logger,
        IEmailSender emailSender)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.userStore = userStore;
        this.emailStore = this.GetEmailStore();
        this.logger = logger;
        this.emailSender = emailSender;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string ProviderDisplayName { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string ErrorMessage { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public IActionResult OnGet() => this.RedirectToPage("./Login");

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = this.Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = this.signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl ??= this.Url.Content("~/");

        if (remoteError != null)
        {
            this.ErrorMessage = $"Error from external provider: {remoteError}";

            return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await this.signInManager.GetExternalLoginInfoAsync().ConfigureAwait(false);

        if (info == null)
        {
            this.ErrorMessage = "Error loading external login information.";
            return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await this.signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true).ConfigureAwait(false);

        if (result.Succeeded)
        {
            this.logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);

            var user = await this.userManager.FindByLoginAsync(info.LoginProvider,
                info.ProviderKey).ConfigureAwait(false);
            var userClaims = await this.userManager.GetClaimsAsync(user).ConfigureAwait(false);
            var refreshSignIn = false;

            foreach (var addedClaim in info.Principal?.Claims ?? info.Principal.Identities.FirstOrDefault()?.Claims)
            {
                var userClaim = userClaims
                    .FirstOrDefault(c => c.Type == addedClaim.Type);

                if (info.Principal.HasClaim(c => c.Type == addedClaim.Type))
                {
                    var externalClaim = info.Principal.FindFirst(addedClaim.Type);

                    if (userClaim == null)
                    {
                        _ = await this.userManager.AddClaimAsync(user, new Claim(addedClaim.Type, externalClaim.Value)).ConfigureAwait(false);
                        refreshSignIn = true;
                    }
                    else if (userClaim.Value != externalClaim.Value)
                    {
                        _ = await this.userManager.ReplaceClaimAsync(user, userClaim, externalClaim).ConfigureAwait(false);
                        refreshSignIn = true;
                    }
                }
                else if (userClaim == null)
                {
                    // Fill with a default value
                    _ = await this.userManager.AddClaimAsync(user, new Claim(addedClaim.Type, addedClaim.Value)).ConfigureAwait(false);
                    refreshSignIn = true;
                }
            }

            if (refreshSignIn)
            {
                await this.signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            }

            return this.LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return this.RedirectToPage("./Lockout");
        }
        // If the user does not have an account, then ask the user to create an account.
        this.ReturnUrl = returnUrl;
        this.ProviderDisplayName = info.ProviderDisplayName;
        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            this.Input = new InputModel
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = info.Principal.FindFirstValue(ClaimTypes.Name),
            };
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl ??= this.Url.Content("~/");
        // Get the information about the user from the external login provider
        var info = await this.signInManager.GetExternalLoginInfoAsync().ConfigureAwait(false);
        if (info == null)
        {
            this.ErrorMessage = "Error loading external login information during confirmation.";
            return this.RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (this.ModelState.IsValid)
        {
            var user = this.CreateUser();
            user.FirstName = this.Input.FirstName;
            user.LastName = this.Input.LastName;

            await this.userStore.SetUserNameAsync(user, this.Input.Email, CancellationToken.None).ConfigureAwait(false);
            await this.emailStore.SetEmailAsync(user, this.Input.Email, CancellationToken.None).ConfigureAwait(false);

            var result = await this.userManager.CreateAsync(user).ConfigureAwait(false);
            if (result.Succeeded)
            {
                result = await this.userManager.AddLoginAsync(user, info).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    // Add external claims

                    _ = await this.userManager.AddClaimsAsync(user, info.Principal.Claims).ConfigureAwait(false);

                    this.logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                    var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
                    var code = await this.userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = this.Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code },
                        protocol: this.Request.Scheme);

                    await this.emailSender.SendEmailAsync(this.Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.").ConfigureAwait(false);

                    // If account confirmation is required, we need to show the link if we don't have a real email sender
                    if (this.userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return this.RedirectToPage("./RegisterConfirmation", new { this.Input.Email });
                    }

                    await this.signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider).ConfigureAwait(false);
                    return this.LocalRedirect(returnUrl);
                }
            }
            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        this.ProviderDisplayName = info.ProviderDisplayName;
        this.ReturnUrl = returnUrl;
        return this.Page();
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                "override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!this.userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<ApplicationUser>)this.userStore;
    }
}
