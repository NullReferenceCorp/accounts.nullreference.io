namespace IdentityServer.Pages.Account.Login;
using System;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Test;
using IdentityServer.Pages;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[SecurityHeaders]
[AllowAnonymous]
public class Index : PageModel
{
    private readonly TestUserStore users;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IClientStore clientStore;
    private readonly IEventService events;
    private readonly IAuthenticationSchemeProvider schemeProvider;
    private readonly IIdentityProviderStore identityProviderStore;

    public ViewModel View { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public Index(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IAuthenticationSchemeProvider schemeProvider,
        IIdentityProviderStore identityProviderStore,
        IEventService events,
        TestUserStore users = null)
    {
        // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
        this.users = users ?? throw new Exception("Please call 'AddTestUsers(TestUsers.Users)' on the IIdentityServerBuilder in Startup or remove the TestUserStore from the AccountController.");

        this.interaction = interaction;
        this.clientStore = clientStore;
        this.schemeProvider = schemeProvider;
        this.identityProviderStore = identityProviderStore;
        this.events = events;
    }

    public async Task<IActionResult> OnGet(string returnUrl)
    {
        await this.BuildModelAsync(returnUrl).ConfigureAwait(false);

        if (this.View.IsExternalLoginOnly)
        {
            // we only have one option for logging in and it's an external provider
            return this.RedirectToPage("/ExternalLogin/Challenge/Index", new { scheme = this.View.ExternalLoginScheme, returnUrl });
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // check if we are in the context of an authorization request
        var context = await this.interaction.GetAuthorizationContextAsync(this.Input.ReturnUrl).ConfigureAwait(false);

        // the user clicked the "cancel" button
        if (this.Input.Button != "login")
        {
            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they 
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await this.interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied).ConfigureAwait(false);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage(this.Input.ReturnUrl);
                }

                return this.Redirect(this.Input.ReturnUrl);
            }
            // since we don't have a valid context, then we just go back to the home page
            return this.Redirect("~/");
        }

        if (this.ModelState.IsValid)
        {
            // validate username/password against in-memory store
            if (this.users.ValidateCredentials(this.Input.Username, this.Input.Password))
            {
                var user = this.users.FindByUsername(this.Input.Username);
                await this.events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username, clientId: context?.Client.ClientId)).ConfigureAwait(false);

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                AuthenticationProperties props = null;
                if (LoginOptions.AllowRememberLogin && this.Input.RememberLogin)
                {
                    props = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(LoginOptions.RememberMeLoginDuration)
                    };
                }

                // issue authentication cookie with subject ID and username
                var isuser = new IdentityServerUser(user.SubjectId)
                {
                    DisplayName = user.Username
                };

                await this.HttpContext.SignInAsync(isuser, props).ConfigureAwait(false);

                if (context != null)
                {
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return this.LoadingPage(this.Input.ReturnUrl);
                    }

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return this.Redirect(this.Input.ReturnUrl);
                }

                // request for a local page
                if (this.Url.IsLocalUrl(this.Input.ReturnUrl))
                {
                    return this.Redirect(this.Input.ReturnUrl);
                }
                else if (string.IsNullOrEmpty(this.Input.ReturnUrl))
                {
                    return this.Redirect("~/");
                }
                else
                {
                    // user might have clicked on a malicious link - should be logged
                    throw new Exception("invalid return URL");
                }
            }

            await this.events.RaiseAsync(new UserLoginFailureEvent(this.Input.Username, "invalid credentials", clientId: context?.Client.ClientId)).ConfigureAwait(false);
            this.ModelState.AddModelError(string.Empty, LoginOptions.InvalidCredentialsErrorMessage);
        }

        // something went wrong, show form with error
        await this.BuildModelAsync(this.Input.ReturnUrl).ConfigureAwait(false);
        return this.Page();
    }

    private async Task BuildModelAsync(string returnUrl)
    {
        this.Input = new InputModel
        {
            ReturnUrl = returnUrl
        };

        var context = await this.interaction.GetAuthorizationContextAsync(returnUrl).ConfigureAwait(false);
        if (context?.IdP != null && await this.schemeProvider.GetSchemeAsync(context.IdP).ConfigureAwait(false) != null)
        {
            var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;

            // this is meant to short circuit the UI and only trigger the one external IdP
            this.View = new ViewModel
            {
                EnableLocalLogin = local,
            };

            this.Input.Username = context?.LoginHint;

            if (!local)
            {
                this.View.ExternalProviders = new[] { new ViewModel.ExternalProvider { AuthenticationScheme = context.IdP } };
            }
        }

        var schemes = await this.schemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ViewModel.ExternalProvider
            {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            }).ToList();

        var dyanmicSchemes = (await this.identityProviderStore.GetAllSchemeNamesAsync().ConfigureAwait(false))
            .Where(x => x.Enabled)
            .Select(x => new ViewModel.ExternalProvider
            {
                AuthenticationScheme = x.Scheme,
                DisplayName = x.DisplayName
            });
        providers.AddRange(dyanmicSchemes);

        var allowLocal = true;
        if (context?.Client.ClientId != null)
        {
            var client = await this.clientStore.FindEnabledClientByIdAsync(context.Client.ClientId).ConfigureAwait(false);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;

                if (client.IdentityProviderRestrictions?.Count > 0)
                {
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }

        this.View = new ViewModel
        {
            AllowRememberLogin = LoginOptions.AllowRememberLogin,
            EnableLocalLogin = allowLocal && LoginOptions.AllowLocalLogin,
            ExternalProviders = providers.ToArray()
        };
    }
}
