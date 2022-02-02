// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account;
#nullable disable

using System.Text;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;

    public ConfirmEmailChangeModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
    {
        if (userId == null || email == null || code == null)
        {
            return this.RedirectToPage("/Index");
        }

        var user = await this.userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{userId}'.");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await this.userManager.ChangeEmailAsync(user, email, code).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            this.StatusMessage = "Error changing email.";
            return this.Page();
        }

        // In our UI email and user name are one and the same, so when we update the email
        // we need to update the user name.
        var setUserNameResult = await this.userManager.SetUserNameAsync(user, email).ConfigureAwait(false);
        if (!setUserNameResult.Succeeded)
        {
            this.StatusMessage = "Error changing user name.";
            return this.Page();
        }

        await this.signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
        this.StatusMessage = "Thank you for confirming your email change.";
        return this.Page();
    }
}
