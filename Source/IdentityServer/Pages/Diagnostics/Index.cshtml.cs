namespace IdentityServer.Pages.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using IdentityServer.Pages;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    public ViewModel View { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var localAddresses = new string[] { "127.0.0.1", "::1", this.HttpContext.Connection.LocalIpAddress.ToString() };
        if (!localAddresses.Contains(this.HttpContext.Connection.RemoteIpAddress.ToString()))
        {
            return this.NotFound();
        }

        this.View = new ViewModel(await this.HttpContext.AuthenticateAsync().ConfigureAwait(false));

        return this.Page();
    }
}