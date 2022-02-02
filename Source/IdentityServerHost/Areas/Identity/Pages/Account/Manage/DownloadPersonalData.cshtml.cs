// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account.Manage;
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class DownloadPersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<DownloadPersonalDataModel> logger;

    public DownloadPersonalDataModel(
        UserManager<ApplicationUser> userManager,
        ILogger<DownloadPersonalDataModel> logger)
    {
        this.userManager = userManager;
        this.logger = logger;
    }

    public IActionResult OnGet() => this.NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await this.userManager.GetUserAsync(this.User).ConfigureAwait(false);
        if (user == null)
        {
            return this.NotFound($"Unable to load user with ID '{this.userManager.GetUserId(this.User)}'.");
        }

        this.logger.LogInformation("User with ID '{UserId}' asked for their personal data.", this.userManager.GetUserId(this.User));

        // Only include personal data for download
        var personalData = new Dictionary<string, string>();
        var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                        prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
        foreach (var p in personalDataProps)
        {
            personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
        }

        var logins = await this.userManager.GetLoginsAsync(user).ConfigureAwait(false);
        foreach (var l in logins)
        {
            personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
        }

        personalData.Add("Authenticator Key", await this.userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false));

        this.Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
        return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
    }
}
