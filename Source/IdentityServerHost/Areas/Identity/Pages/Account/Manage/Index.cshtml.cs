// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Username { get; set; }

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
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        var userName = await this.userManager.GetUserNameAsync(user).ConfigureAwait(false);
        var phoneNumber = await this.userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);

        this.Username = userName;

        this.Input = new InputModel
        {
            PhoneNumber = phoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
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

    public async Task<IActionResult> OnPostAsync()
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

        var phoneNumber = await this.userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);
        if (this.Input.PhoneNumber != phoneNumber)
        {
            var setPhoneResult = await this.userManager.SetPhoneNumberAsync(user, this.Input.PhoneNumber).ConfigureAwait(false);
            if (!setPhoneResult.Succeeded)
            {
                this.StatusMessage = "Unexpected error when trying to set phone number.";
                return this.RedirectToPage();
            }
        }

        if (this.Input.FirstName != user.FirstName)
        {
            user.FirstName = this.Input.FirstName;
        }

        if (this.Input.LastName != user.LastName)
        {
            user.LastName = this.Input.LastName;
        }

        await this.userManager.UpdateAsync(user).ConfigureAwait(false);

        await this.signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
        this.StatusMessage = "Your profile has been updated";
        return this.RedirectToPage();
    }
}
