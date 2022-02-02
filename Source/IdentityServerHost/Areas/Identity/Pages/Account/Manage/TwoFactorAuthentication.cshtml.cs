// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class TwoFactorAuthenticationModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<TwoFactorAuthenticationModel> logger;

    public TwoFactorAuthenticationModel(
        UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<TwoFactorAuthenticationModel> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool HasAuthenticator { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public int RecoveryCodesLeft { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public bool Is2faEnabled { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool IsMachineRemembered { get; set; }

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

        this.HasAuthenticator = await this.userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false) != null;
        this.Is2faEnabled = await this.userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
        this.IsMachineRemembered = await this.signInManager.IsTwoFactorClientRememberedAsync(user).ConfigureAwait(false);
        this.RecoveryCodesLeft = await this.userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false);

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        await this.signInManager.ForgetTwoFactorClientAsync().ConfigureAwait(false);
        this.StatusMessage = "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
        return this.RedirectToPage();
    }
}
