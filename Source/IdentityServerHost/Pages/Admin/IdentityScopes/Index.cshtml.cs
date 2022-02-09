namespace IdentityServer.Pages.Admin.IdentityScopes;
using IdentityServerHost.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

[SecurityHeaders]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IdentityScopeRepository _repository;

    public IndexModel(IdentityScopeRepository repository)
    {
        this._repository = repository;
    }

    public IEnumerable<IdentityScopeSummaryModel> Scopes { get; private set; }
    public string Filter { get; set; }

    public async Task OnGetAsync(string filter)
    {
        this.Filter = filter;
        this.Scopes = await this._repository.GetAllAsync(filter);
    }
}
