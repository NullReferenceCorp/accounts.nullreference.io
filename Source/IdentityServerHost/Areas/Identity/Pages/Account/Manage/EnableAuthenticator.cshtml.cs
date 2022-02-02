// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class EnableAuthenticatorModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<EnableAuthenticatorModel> logger;
    private readonly UrlEncoder urlEncoder;

    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public EnableAuthenticatorModel(
        UserManager<ApplicationUser> userManager,
        ILogger<EnableAuthenticatorModel> logger,
        UrlEncoder urlEncoder)
    {
        this.userManager = userManager;
        this.logger = logger;
        this.urlEncoder = urlEncoder;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string SharedKey { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string AuthenticatorUri { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string[] RecoveryCodes { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

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
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        await this.LoadSharedKeyAndQrCodeUriAsync(user).ConfigureAwait(false);

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        if (!this.ModelState.IsValid)
        {
            await this.LoadSharedKeyAndQrCodeUriAsync(user).ConfigureAwait(false);
            return this.Page();
        }

        // Strip spaces and hyphens
        var verificationCode = this.Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var is2faTokenValid = await this.userManager.VerifyTwoFactorTokenAsync(
            user, this.userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode).ConfigureAwait(false);

        if (!is2faTokenValid)
        {
            this.ModelState.AddModelError("Input.Code", "Verification code is invalid.");
            await this.LoadSharedKeyAndQrCodeUriAsync(user).ConfigureAwait(false);
            return this.Page();
        }

        await this.userManager.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
        var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
        this.logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

        this.StatusMessage = "Your authenticator app has been verified.";

        if (await this.userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false) == 0)
        {
            var recoveryCodes = await this.userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10).ConfigureAwait(false);
            this.RecoveryCodes = recoveryCodes.ToArray();
            return this.RedirectToPage("./ShowRecoveryCodes");
        }

        return this.RedirectToPage("./TwoFactorAuthentication");
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        // Load the authenticator key & QR code URI to display on the form
        var unformattedKey = await this.userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await this.userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            unformattedKey = await this.userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        this.SharedKey = this.FormatKey(unformattedKey);

        var email = await this.userManager.GetEmailAsync(user).ConfigureAwait(false);
        this.AuthenticatorUri = this.GenerateQrCodeUri(email, unformattedKey);
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey) => string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            this.urlEncoder.Encode("accounts.nullreference.io"),
            this.urlEncoder.Encode(email),
            unformattedKey);
}
