using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace IdentityServer.Pages.Admin.IdentityScopes;

[SecurityHeaders]
[Authorize]
public class NewModel : PageModel
{
    private readonly IdentityScopeRepository _repository;

    public NewModel(IdentityScopeRepository repository)
    {
        this._repository = repository;
    }

    [BindProperty]
    public IdentityScopeModel InputModel { get; set; }
        
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (this.ModelState.IsValid)
        {
            await this._repository.CreateAsync(this.InputModel);
            return this.RedirectToPage("/Admin/IdentityScopes/Edit", new { id = this.InputModel.Name });
        }

        return this.Page();
    }
}
