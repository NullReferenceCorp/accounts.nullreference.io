namespace IdentityServer.Pages.Admin.ApiScopes;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

[SecurityHeaders]
[Authorize]
public class NewModel : PageModel
{
    private readonly ApiScopeRepository _repository;

    public NewModel(ApiScopeRepository repository)
    {
        this._repository = repository;
    }

    [BindProperty]
    public ApiScopeModel InputModel { get; set; }
        
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (this.ModelState.IsValid)
        {
            await this._repository.CreateAsync(this.InputModel);
            return this.RedirectToPage("/Admin/ApiScopes/Edit", new { id = this.InputModel.Name });
        }

        return this.Page();
    }
}
