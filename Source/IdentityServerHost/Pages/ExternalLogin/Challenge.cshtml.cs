namespace IdentityServer.Pages.ExternalLogin;
using System;
using Duende.IdentityServer.Services;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
[SecurityHeaders]
public class Challenge : PageModel
{
    private readonly IIdentityServerInteractionService interactionService;

    public Challenge(IIdentityServerInteractionService interactionService) => this.interactionService = interactionService;

    public IActionResult OnGet(string scheme, string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "~/";
        }

        // validate returnUrl - either it is a valid OIDC URL or back to a local page
        if (!this.Url.IsLocalUrl(returnUrl) && !this.interactionService.IsValidReturnUrl(returnUrl))
        {
            // user might have clicked on a malicious link - should be logged
            throw new Exception("invalid return URL");
        }

        // start challenge and roundtrip the return URL and scheme 
        var props = new AuthenticationProperties
        {
            RedirectUri = this.Url.Page("/externallogin/callback"),

            Items =
            {
                { "returnUrl", returnUrl },
                { "scheme", scheme },
            }
        };

        return this.Challenge(props, scheme);
    }
}
