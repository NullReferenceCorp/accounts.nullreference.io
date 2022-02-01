namespace IdentityServer.Pages.Redirect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    public string RedirectUri { get; set; }

    public IActionResult OnGet(string redirectUri)
    {
        if (!this.Url.IsLocalUrl(redirectUri))
        {
            return this.RedirectToPage("/Error/Index");
        }

        this.RedirectUri = redirectUri;
        return this.Page();
    }
}