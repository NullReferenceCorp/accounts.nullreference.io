// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<DeletePersonalDataModel> logger;

    public DeletePersonalDataModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<DeletePersonalDataModel> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
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
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public bool RequirePassword { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        this.RequirePassword = await this.userManager.HasPasswordAsync(user).ConfigureAwait(false);
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        this.RequirePassword = await this.userManager.HasPasswordAsync(user).ConfigureAwait(false);
        if (this.RequirePassword)
        {
            if (!await this.userManager.CheckPasswordAsync(user, this.Input.Password).ConfigureAwait(false))
            {
                this.ModelState.AddModelError(string.Empty, "Incorrect password.");
                return this.Page();
            }
        }

        var result = await this.userManager.DeleteAsync(user).ConfigureAwait(false);
        var userId = await this.userManager.GetUserIdAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Unexpected error occurred deleting user.");
        }

        await this.signInManager.SignOutAsync().ConfigureAwait(false);

        this.logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

        return this.Redirect("~/");
    }
}
