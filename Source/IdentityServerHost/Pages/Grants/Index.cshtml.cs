namespace IdentityServer.Pages.Grants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using IdentityServer.Pages;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    private readonly IIdentityServerInteractionService interaction;
    private readonly IClientStore clients;
    private readonly IResourceStore resources;
    private readonly IEventService events;

    public Index(IIdentityServerInteractionService interaction,
        IClientStore clients,
        IResourceStore resources,
        IEventService events)
    {
        this.interaction = interaction;
        this.clients = clients;
        this.resources = resources;
        this.events = events;
    }

    public ViewModel View { get; set; }

    public async Task OnGet()
    {
        var grants = await this.interaction.GetAllUserGrantsAsync().ConfigureAwait(false);

        var list = new List<GrantViewModel>();
        foreach (var grant in grants)
        {
            var client = await this.clients.FindClientByIdAsync(grant.ClientId).ConfigureAwait(false);
            if (client != null)
            {
                var resources = await this.resources.FindResourcesByScopeAsync(grant.Scopes).ConfigureAwait(false);

                var item = new GrantViewModel()
                {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName ?? client.ClientId,
                    ClientLogoUrl = client.LogoUri,
                    ClientUrl = client.ClientUri,
                    Description = grant.Description,
                    Created = grant.CreationTime,
                    Expires = grant.Expiration,
                    IdentityGrantNames = resources.IdentityResources.Select(x => x.DisplayName ?? x.Name).ToArray(),
                    ApiGrantNames = resources.ApiScopes.Select(x => x.DisplayName ?? x.Name).ToArray()
                };

                list.Add(item);
            }
        }

        this.View = new ViewModel
        {
            Grants = list
        };
    }

    [BindProperty]
    [Required]
    public string ClientId { get; set; }

    public async Task<IActionResult> OnPost()
    {
        await this.interaction.RevokeUserConsentAsync(this.ClientId).ConfigureAwait(false);
        await this.events.RaiseAsync(new GrantsRevokedEvent(this.User.GetSubjectId(), this.ClientId)).ConfigureAwait(false);

        return this.RedirectToPage("/Grants/Index");
    }
}
