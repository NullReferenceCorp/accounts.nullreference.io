using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace IdentityServer.Pages.Admin.Clients;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly ClientRepository _repository;

    public EditModel(ClientRepository repository)
    {
        this._repository = repository;
    }

    [BindProperty]
    public ClientModel InputModel { get; set; }
    [BindProperty]
    public string Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        this.InputModel = await this._repository.GetByIdAsync(id);
        if (this.InputModel == null)
        {
            return this.RedirectToPage("/Admin/Clients/Index");
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (this.Button == "delete")
        {
            await this._repository.DeleteAsync(id);
            return this.RedirectToPage("/Admin/Clients/Index");
        }

        if (this.ModelState.IsValid)
        {
            await this._repository.UpdateAsync(this.InputModel);
            return this.RedirectToPage("/Admin/Clients/Edit", new { id });
        }

        return this.Page();
    }
}
