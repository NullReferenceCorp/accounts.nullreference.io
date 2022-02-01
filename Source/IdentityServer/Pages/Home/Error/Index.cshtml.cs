namespace IdentityServer.Pages.Home.Error;
using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

[AllowAnonymous]
[SecurityHeaders]
public class Index : PageModel
{
    private readonly IIdentityServerInteractionService interaction;
    private readonly IWebHostEnvironment environment;

    public ViewModel View { get; set; }

    public Index(IIdentityServerInteractionService interaction, IWebHostEnvironment environment)
    {
        this.interaction = interaction;
        this.environment = environment;
    }

    public async Task OnGet(string errorId)
    {
        this.View = new ViewModel();

        // retrieve error details from identityserver
        var message = await this.interaction.GetErrorContextAsync(errorId).ConfigureAwait(false);
        if (message != null)
        {
            this.View.Error = message;

            if (!this.environment.IsDevelopment())
            {
                // only show in development
                message.ErrorDescription = null;
            }
        }
    }
}
