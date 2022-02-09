using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Pages.Admin.IdentityScopes;

public class IdentityScopeSummaryModel
{
    [Required]
    public string Name { get; set; }
    public string DisplayName { get; set; }
}

public class IdentityScopeModel : IdentityScopeSummaryModel
{
    public string UserClaims { get; set; }
}


public class IdentityScopeRepository
{
    private readonly ConfigurationDbContext _context;

    public IdentityScopeRepository(ConfigurationDbContext context)
    {
        this._context = context;
    }

    public async Task<IEnumerable<IdentityScopeSummaryModel>> GetAllAsync(string filter = null)
    {
        var query = this._context.IdentityResources
            .Include(x => x.UserClaims)
            .AsQueryable();

        if (!String.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(x => x.Name.Contains(filter) || x.DisplayName.Contains(filter));
        }

        var result = query.Select(x => new IdentityScopeSummaryModel
        {
            Name = x.Name,
            DisplayName = x.DisplayName
        });

        return await result.ToArrayAsync();
    }

    public async Task<IdentityScopeModel> GetByIdAsync(string id)
    {
        var scope = await this._context.IdentityResources
            .Include(x => x.UserClaims)
            .SingleOrDefaultAsync(x => x.Name == id);

        if (scope == null) return null;

        return new IdentityScopeModel
        {
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            UserClaims = scope.UserClaims.Any() ? scope.UserClaims.Select(x => x.Type).Aggregate((a, b) => $"{a} {b}") : null,
        };
    }

    public async Task CreateAsync(IdentityScopeModel model)
    {
        var scope = new Duende.IdentityServer.Models.IdentityResource()
        {
            Name = model.Name,
            DisplayName = model.DisplayName?.Trim()
        };

        var claims = model.UserClaims?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Enumerable.Empty<string>();
        if (claims.Any())
        {
            scope.UserClaims = claims.ToList();
        }

        this._context.IdentityResources.Add(scope.ToEntity());
        await this._context.SaveChangesAsync();
    }

    public async Task UpdateAsync(IdentityScopeModel model)
    {
        var scope = await this._context.IdentityResources
            .Include(x => x.UserClaims)
            .SingleOrDefaultAsync(x => x.Name == model.Name);

        if (scope == null) throw new Exception("Invalid Identity Scope");

        if (scope.DisplayName != model.DisplayName)
        {
            scope.DisplayName = model.DisplayName?.Trim();
        }

        var claims = model.UserClaims?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Enumerable.Empty<string>();
        var currentClaims = (scope.UserClaims.Select(x => x.Type) ?? Enumerable.Empty<String>()).ToArray();

        var claimsToAdd = claims.Except(currentClaims).ToArray();
        var claimsToRemove = currentClaims.Except(claims).ToArray();

        if (claimsToRemove.Any())
        {
            scope.UserClaims.RemoveAll(x => claimsToRemove.Contains(x.Type));
        }
        if (claimsToAdd.Any())
        {
            scope.UserClaims.AddRange(claimsToAdd.Select(x => new IdentityResourceClaim
            {
                Type = x,
            }));
        }

        await this._context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var scope = await this._context.IdentityResources.SingleOrDefaultAsync(x => x.Name == id);

        if (scope == null) throw new Exception("Invalid Identity Scope");

        this._context.IdentityResources.Remove(scope);
        await this._context.SaveChangesAsync();
    }

}