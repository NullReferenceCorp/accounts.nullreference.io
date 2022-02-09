namespace IdentityServer.Pages.Admin.Clients;
using IdentityModel;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

[SecurityHeaders]
[Authorize]
public class NewModel : PageModel
{
    private readonly ClientRepository _repository;

    public NewModel(ClientRepository repository)
    {
        this._repository = repository;
    }

    [BindProperty]
    public CreateClientModel InputModel { get; set; }
        
    public bool Created { get; set; }

    public void OnGet()
    {
        this.InputModel = new CreateClientModel
        { 
            Secret = Convert.ToBase64String(CryptoRandom.CreateRandomKey(16))
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (this.ModelState.IsValid)
        {
            await this._repository.CreateAsync(this.InputModel);
            this.Created = true;
        }

        return this.Page();
    }
}
