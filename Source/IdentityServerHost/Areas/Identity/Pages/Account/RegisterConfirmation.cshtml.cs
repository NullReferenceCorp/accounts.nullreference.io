// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account;
#nullable disable

using System.Text;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IEmailSender sender;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender sender)
    {
        this.userManager = userManager;
        this.sender = sender;
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
    public bool DisplayConfirmAccountLink { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
    {
        if (email == null)
        {
            return this.RedirectToPage("/Index");
        }
        returnUrl ??= this.Url.Content("~/");

        var user = await this.userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with email '{email}'.");
        }

        this.Email = email;
        // Once you add a real email sender, you should remove this code that lets you confirm the account
        this.DisplayConfirmAccountLink = true;
        if (this.DisplayConfirmAccountLink)
        {
            var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var code = await this.userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            this.EmailConfirmationUrl = this.Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code, returnUrl },
                protocol: this.Request.Scheme);
        }

        return this.Page();
    }
}
