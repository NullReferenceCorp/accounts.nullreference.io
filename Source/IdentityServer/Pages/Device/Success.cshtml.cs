namespace IdentityServer.Pages.Device;
using IdentityServer.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

[SecurityHeaders]
[Authorize]
public class SuccessModel : PageModel
{
    public void OnGet()
    {
    }
}