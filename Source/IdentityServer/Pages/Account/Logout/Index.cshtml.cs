namespace IdentityServer.Pages.Account.Logout;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using IdentityModel;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly IIdentityServerInteractionService interaction;
    private readonly IEventService events;

    [BindProperty]
    public string LogoutId { get; set; }

    public Index(IIdentityServerInteractionService interaction, IEventService events)
    {
        this.interaction = interaction;
        this.events = events;
    }

    public async Task<IActionResult> OnGet(string logoutId)
    {
        this.LogoutId = logoutId;

        var showLogoutPrompt = LogoutOptions.ShowLogoutPrompt;

        if (this.User?.Identity.IsAuthenticated != true)
        {
            // if the user is not authenticated, then just show logged out page
            showLogoutPrompt = false;
        }
        else
        {
            var context = await this.interaction.GetLogoutContextAsync(this.LogoutId).ConfigureAwait(false);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                showLogoutPrompt = false;
            }
        }

        if (!showLogoutPrompt)
        {
            // if the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await this.OnPost().ConfigureAwait(false);
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (this.User?.Identity.IsAuthenticated == true)
        {
            // if there's no current logout context, we need to create one
            // this captures necessary info from the current logged in user
            // this can still return null if there is no context needed
            this.LogoutId ??= await this.interaction.CreateLogoutContextAsync().ConfigureAwait(false);

            // delete local authentication cookie
            await this.HttpContext.SignOutAsync().ConfigureAwait(false);

            // raise the logout event
            await this.events.RaiseAsync(new UserLogoutSuccessEvent(this.User.GetSubjectId(), this.User.GetDisplayName())).ConfigureAwait(false);

            // see if we need to trigger federated logout
            var idp = this.User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            // if it's a local login we can ignore this workflow
            if (idp is not null and not Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider)
            {
                // we need to see if the provider supports external logout
                if (await Extensions.GetSchemeSupportsSignOutAsync(this.HttpContext, idp)
                    .ConfigureAwait(false))
                {
                    // build a return URL so the upstream provider will redirect back
                    // to us after the user has logged out. this allows us to then
                    // complete our single sign-out processing.
                    var url = this.Url.Page("/Account/Logout/Loggedout", new { logoutId = this.LogoutId });

                    // this triggers a redirect to the external provider for sign-out
                    return this.SignOut(new AuthenticationProperties { RedirectUri = url }, idp);
                }
            }
        }

        return this.RedirectToPage("/Account/Logout/LoggedOut", new { logoutId = this.LogoutId });
    }
}
