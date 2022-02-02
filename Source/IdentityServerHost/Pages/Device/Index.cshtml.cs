namespace IdentityServer.Pages.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using IdentityServer.Pages.Consent;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IdentityServerHost.Attributes;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    private readonly IDeviceFlowInteractionService _interaction;
    private readonly IEventService _events;
    private readonly IOptions<IdentityServerOptions> _options;
    private readonly ILogger<Index> _logger;

    public Index(
        IDeviceFlowInteractionService interaction,
        IEventService eventService,
        IOptions<IdentityServerOptions> options,
        ILogger<Index> logger)
    {
        this._interaction = interaction;
        this._events = eventService;
        this._options = options;
        this._logger = logger;
    }

    public ViewModel View { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public async Task<IActionResult> OnGet(string userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            this.View = new ViewModel();
            this.Input = new InputModel();
            return this.Page();
        }

        this.View = await this.BuildViewModelAsync(userCode).ConfigureAwait(false);
        if (this.View == null)
        {
            this.ModelState.AddModelError("", DeviceOptions.InvalidUserCode);
            this.View = new ViewModel();
            this.Input = new InputModel();
            return this.Page();
        }

        this.Input = new InputModel
        {
            UserCode = userCode,
        };

        return this.Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var request = await this._interaction.GetAuthorizationContextAsync(this.Input.UserCode).ConfigureAwait(false);
        if (request == null)
        {
            return this.RedirectToPage("/Error/Index");
        }

        ConsentResponse grantedConsent = null;

        // user clicked 'no' - send back the standard 'access_denied' response
        if (this.Input.Button == "no")
        {
            grantedConsent = new ConsentResponse
            {
                Error = AuthorizationError.AccessDenied
            };

            // emit event
            await this._events.RaiseAsync(new ConsentDeniedEvent(this.User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues)).ConfigureAwait(false);
        }
        // user clicked 'yes' - validate the data
        else if (this.Input.Button == "yes")
        {
            // if the user consented to some scope, build the response model
            if (this.Input.ScopesConsented?.Any() == true)
            {
                var scopes = this.Input.ScopesConsented;
                if (!ConsentOptions.EnableOfflineAccess)
                {
                    scopes = scopes.Where(x => x != Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess);
                }

                grantedConsent = new ConsentResponse
                {
                    RememberConsent = this.Input.RememberConsent,
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = this.Input.Description
                };

                // emit event
                await this._events.RaiseAsync(new ConsentGrantedEvent(this.User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent)).ConfigureAwait(false);
            }
            else
            {
                this.ModelState.AddModelError("", ConsentOptions.MustChooseOneErrorMessage);
            }
        }
        else
        {
            this.ModelState.AddModelError("", ConsentOptions.InvalidSelectionErrorMessage);
        }

        if (grantedConsent != null)
        {
            // communicate outcome of consent back to identityserver
            await this._interaction.HandleRequestAsync(this.Input.UserCode, grantedConsent).ConfigureAwait(false);

            // indicate that's it ok to redirect back to authorization endpoint
            return this.RedirectToPage("/Device/Success");
        }

        // we need to redisplay the consent UI
        this.View = await this.BuildViewModelAsync(this.Input.UserCode, this.Input).ConfigureAwait(false);
        return this.Page();
    }

    private async Task<ViewModel> BuildViewModelAsync(string userCode, InputModel model = null)
    {
        var request = await this._interaction.GetAuthorizationContextAsync(userCode).ConfigureAwait(false);
        if (request != null)
        {
            return this.CreateConsentViewModel(model, request);
        }

        return null;
    }

    private ViewModel CreateConsentViewModel(InputModel model, DeviceFlowAuthorizationRequest request)
    {
        var vm = new ViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x => this.CreateScopeViewModel(x, model == null || model.ScopesConsented?.Contains(x.Name) == true)).ToArray();

        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
        {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null)
            {
                var scopeVm = this.CreateScopeViewModel(parsedScope, apiScope, model == null || model.ScopesConsented?.Contains(parsedScope.RawValue) == true);
                apiScopes.Add(scopeVm);
            }
        }
        if (DeviceOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
        {
            apiScopes.Add(this.GetOfflineAccessScope(model == null || model.ScopesConsented?.Contains(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess) == true));
        }
        vm.ApiScopes = apiScopes;

        return vm;
    }

    private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new()
    {
        Value = identity.Name,
        DisplayName = identity.DisplayName ?? identity.Name,
        Description = identity.Description,
        Emphasize = identity.Emphasize,
        Required = identity.Required,
        Checked = check || identity.Required
    };

    public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) => new()
    {
        Value = parsedScopeValue.RawValue,
        // todo: use the parsed scope value in the display?
        DisplayName = apiScope.DisplayName ?? apiScope.Name,
        Description = apiScope.Description,
        Emphasize = apiScope.Emphasize,
        Required = apiScope.Required,
        Checked = check || apiScope.Required
    };

    private ScopeViewModel GetOfflineAccessScope(bool check) => new()
    {
        Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
        DisplayName = DeviceOptions.OfflineAccessDisplayName,
        Description = DeviceOptions.OfflineAccessDescription,
        Emphasize = true,
        Checked = check
    };
}
