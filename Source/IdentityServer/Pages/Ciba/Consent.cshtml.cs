namespace IdentityServer.Pages.Ciba;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

[Authorize]
[SecurityHeaders]
public class Consent : PageModel
{
    private readonly IBackchannelAuthenticationInteractionService interaction;
    private readonly IEventService events;
    private readonly ILogger<Consent> logger;

    public Consent(
        IBackchannelAuthenticationInteractionService interaction,
        IEventService events,
        ILogger<Consent> logger)
    {
        this.interaction = interaction;
        this.events = events;
        this.logger = logger;
    }

    public ViewModel View { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        this.View = await this.BuildViewModelAsync(id).ConfigureAwait(false);
        if (this.View == null)
        {
            return this.RedirectToPage("/Home/Error/Index");
        }

        this.Input = new InputModel
        {
            Id = id
        };

        return this.Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // validate return url is still valid
        var request = await this.interaction.GetLoginRequestByInternalIdAsync(this.Input.Id).ConfigureAwait(false);
        if (request == null || request.Subject.GetSubjectId() != this.User.GetSubjectId())
        {
            this.logger.LogError("Invalid id {Id}", this.Input.Id);
            return this.RedirectToPage("/Home/Error/Index");
        }

        CompleteBackchannelLoginRequest result = null;

        // user clicked 'no' - send back the standard 'access_denied' response
        if (this.Input?.Button == "no")
        {
            result = new CompleteBackchannelLoginRequest(this.Input.Id);

            // emit event
            await this.events.RaiseAsync(new ConsentDeniedEvent(this.User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues)).ConfigureAwait(false);
        }
        // user clicked 'yes' - validate the data
        else if (this.Input?.Button == "yes")
        {
            // if the user consented to some scope, build the response model
            if (this.Input.ScopesConsented?.Any() == true)
            {
                var scopes = this.Input.ScopesConsented;
                if (!ConsentOptions.EnableOfflineAccess)
                {
                    scopes = scopes.Where(x => x != Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess);
                }

                result = new CompleteBackchannelLoginRequest(this.Input.Id)
                {
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = this.Input.Description
                };

                // emit event
                await this.events.RaiseAsync(new ConsentGrantedEvent(this.User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, result.ScopesValuesConsented, false)).ConfigureAwait(false);
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

        if (result != null)
        {
            // communicate outcome of consent back to identityserver
            await this.interaction.CompleteLoginRequestAsync(result).ConfigureAwait(false);

            return this.RedirectToPage("/Ciba/All");
        }

        // we need to redisplay the consent UI
        this.View = await this.BuildViewModelAsync(this.Input.Id, this.Input).ConfigureAwait(false);
        return this.Page();
    }

    private async Task<ViewModel> BuildViewModelAsync(string id, InputModel model = null)
    {
        var request = await this.interaction.GetLoginRequestByInternalIdAsync(id).ConfigureAwait(false);
        if (request != null && request.Subject.GetSubjectId() == this.User.GetSubjectId())
        {
            return this.CreateConsentViewModel(model, id, request);
        }

        this.logger.LogError("No backchannel login request matching id: {Id}", id);
        return null;
    }

    private ViewModel CreateConsentViewModel(
        InputModel model, string id,
        BackchannelUserLoginRequest request)
    {
        var vm = new ViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            BindingMessage = request.BindingMessage
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources
            .Select(x => this.CreateScopeViewModel(x, model?.ScopesConsented == null || model.ScopesConsented?.Contains(x.Name) == true))
            .ToArray();

        var resourceIndicators = request.RequestedResourceIndicators ?? Enumerable.Empty<string>();
        var apiResources = request.ValidatedResources.Resources.ApiResources.Where(x => resourceIndicators.Contains(x.Name));

        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
        {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null)
            {
                var scopeVm = this.CreateScopeViewModel(parsedScope, apiScope, model == null || model.ScopesConsented?.Contains(parsedScope.RawValue) == true);
                scopeVm.Resources = apiResources.Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                    .Select(x => new ResourceViewModel
                    {
                        Name = x.Name,
                        DisplayName = x.DisplayName ?? x.Name,
                    }).ToArray();
                apiScopes.Add(scopeVm);
            }
        }
        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
        {
            apiScopes.Add(this.GetOfflineAccessScope(model == null || model.ScopesConsented?.Contains(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess) == true));
        }
        vm.ApiScopes = apiScopes;

        return vm;
    }

    private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new()
    {
        Name = identity.Name,
        Value = identity.Name,
        DisplayName = identity.DisplayName ?? identity.Name,
        Description = identity.Description,
        Emphasize = identity.Emphasize,
        Required = identity.Required,
        Checked = check || identity.Required
    };

    public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
    {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
        {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }

        return new ScopeViewModel
        {
            Name = parsedScopeValue.ParsedName,
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };
    }

    private ScopeViewModel GetOfflineAccessScope(bool check) => new()
    {
        Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
        DisplayName = ConsentOptions.OfflineAccessDisplayName,
        Description = ConsentOptions.OfflineAccessDescription,
        Emphasize = true,
        Checked = check
    };
}
