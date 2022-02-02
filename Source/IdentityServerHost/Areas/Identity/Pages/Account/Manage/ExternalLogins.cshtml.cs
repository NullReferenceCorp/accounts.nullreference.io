// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ExternalLoginsModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IUserStore<ApplicationUser> userStore;

    public ExternalLoginsModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserStore<ApplicationUser> userStore)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.userStore = userStore;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<UserLoginInfo> CurrentLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme> OtherLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool ShowRemoveButton { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        this.CurrentLogins = await this.userManager.GetLoginsAsync(user).ConfigureAwait(false);
        this.OtherLogins = (await this.signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false))
            .Where(auth => this.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
            .ToList();

        string passwordHash = null;
        if (this.userStore is IUserPasswordStore<ApplicationUser> userPasswordStore)
        {
            passwordHash = await userPasswordStore.GetPasswordHashAsync(user, this.HttpContext.RequestAborted).ConfigureAwait(false);
        }

        this.ShowRemoveButton = passwordHash != null || this.CurrentLogins.Count > 1;
        return this.Page();
    }

    public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        var result = await this.userManager.RemoveLoginAsync(user, loginProvider, providerKey).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            this.StatusMessage = "The external login was not removed.";
            return this.RedirectToPage();
        }

        await this.signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
        this.StatusMessage = "The external login was removed.";
        return this.RedirectToPage();
    }

    public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
    {
        // Clear the existing external cookie to ensure a clean login process
        await this.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);

        // Request a redirect to the external login provider to link a login for the current user
        var redirectUrl = this.Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
        var properties = this.signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, this.userManager.GetUserId(this.User));
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var info = await this.signInManager.GetExternalLoginInfoAsync(userId).ConfigureAwait(false);
        if (info == null)
        {
            throw new InvalidOperationException("Unexpected error occurred loading external login info.");
        }

        var result = await this.userManager.AddLoginAsync(user, info).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            this.StatusMessage = "The external login was not added. External logins can only be associated with one account.";
            return this.RedirectToPage();
        }

        // Clear the existing external cookie to ensure a clean login process
        await this.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);

        this.StatusMessage = "The external login was added.";
        return this.RedirectToPage();
    }
}
