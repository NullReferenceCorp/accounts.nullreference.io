// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace IdentityServer.Pages.Ciba;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[SecurityHeaders]
[Authorize]
public class AllModel : PageModel
{
    public IEnumerable<BackchannelUserLoginRequest> Logins { get; set; }

    [BindProperty, Required]
    public string Id { get; set; }
    [BindProperty, Required]
    public string Button { get; set; }

    private readonly IBackchannelAuthenticationInteractionService backchannelAuthenticationInteraction;

    public AllModel(IBackchannelAuthenticationInteractionService backchannelAuthenticationInteractionService) => this.backchannelAuthenticationInteraction = backchannelAuthenticationInteractionService;

    public async Task OnGet() => this.Logins = await this.backchannelAuthenticationInteraction.GetPendingLoginRequestsForCurrentUserAsync().ConfigureAwait(false);
}
