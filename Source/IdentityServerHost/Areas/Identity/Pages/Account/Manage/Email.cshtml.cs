// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

public class EmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IEmailSender emailSender;

    public EmailModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.emailSender = emailSender;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool IsEmailConfirmed { get; set; }

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
        [EmailAddress]
        [Display(Name = "New email")]
        public string NewEmail { get; set; }
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        var email = await this.userManager.GetEmailAsync(user).ConfigureAwait(false);
        this.Email = email;

        this.Input = new InputModel
        {
            NewEmail = email,
        };

        this.IsEmailConfirmed = await this.userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        await this.LoadAsync(user).ConfigureAwait(false);
        return this.Page();
    }

    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        if (!this.ModelState.IsValid)
        {
            await this.LoadAsync(user).ConfigureAwait(false);
            return this.Page();
        }

        var email = await this.userManager.GetEmailAsync(user).ConfigureAwait(false);
        if (this.Input.NewEmail != email)
        {
            var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var code = await this.userManager.GenerateChangeEmailTokenAsync(user, this.Input.NewEmail).ConfigureAwait(false);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = this.Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new { area = "Identity", userId, email = this.Input.NewEmail, code },
                protocol: this.Request.Scheme);
            await this.emailSender.SendEmailAsync(
                this.Input.NewEmail,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.").ConfigureAwait(false);

            this.StatusMessage = "Confirmation link to change email sent. Please check your email.";
            return this.RedirectToPage();
        }

        this.StatusMessage = "Your email is unchanged.";
        return this.RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        if (!this.ModelState.IsValid)
        {
            await this.LoadAsync(user).ConfigureAwait(false);
            return this.Page();
        }

        var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
        var email = await this.userManager.GetEmailAsync(user).ConfigureAwait(false);
        var code = await this.userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = this.Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId, code },
            protocol: this.Request.Scheme);
        await this.emailSender.SendEmailAsync(
            email,
            "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.").ConfigureAwait(false);

        this.StatusMessage = "Verification email sent. Please check your email.";
        return this.RedirectToPage();
    }
}
