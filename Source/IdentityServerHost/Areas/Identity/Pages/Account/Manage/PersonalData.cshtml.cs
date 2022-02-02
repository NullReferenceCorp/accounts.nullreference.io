// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class PersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<PersonalDataModel> logger;

    public PersonalDataModel(
        UserManager<ApplicationUser> userManager,
        ILogger<PersonalDataModel> logger)
    {
        this.userManager = userManager;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        return this.Page();
    }
}
