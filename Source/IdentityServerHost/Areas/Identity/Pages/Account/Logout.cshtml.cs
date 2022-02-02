// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace IdentityServerHost.Areas.Identity.Pages.Account;
#nullable disable

using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

[AllowAnonymous]

public class LogoutModel : PageModel
{
    private readonly IIdentityServerInteractionService interactionService;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<LogoutModel> logger;

    public bool LoggedOut { get; set; }
    public string PostLogoutRedirectUri { get; set; }
    public string SignOutIframeUrl { get; set; }


    public LogoutModel(IIdentityServerInteractionService interactionService, SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
    {
        this.interactionService = interactionService;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    public async Task<IActionResult> OnGet(string logoutId)
    {
        var request = await this.interactionService.GetLogoutContextAsync(logoutId).ConfigureAwait(false);
        if (request?.ShowSignoutPrompt == false || !this.User.Identity.IsAuthenticated)
        {
            return await this.OnPost(logoutId).ConfigureAwait(false);
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPost(string logoutId)
    {
        this.LoggedOut = true;

        await this.signInManager.SignOutAsync().ConfigureAwait(false);
        this.logger.LogInformation("User logged out.");

        var request = await this.interactionService.GetLogoutContextAsync(logoutId).ConfigureAwait(false);
        if (request != null)
        {
            this.PostLogoutRedirectUri = request.PostLogoutRedirectUri;
            this.SignOutIframeUrl = request.SignOutIFrameUrl;
        }

        return this.Page();
    }
}
