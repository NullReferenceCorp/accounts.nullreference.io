namespace IdentityServerHost.Services;

using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public class ClaimsFactory<T> : UserClaimsPrincipalFactory<T>
  where T : IdentityUser
{
    private readonly UserManager<T> userManager;

    public ClaimsFactory(
        UserManager<T> userManager,
        IOptions<IdentityOptions> optionsAccessor) : base(userManager, optionsAccessor) => this.userManager = userManager;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(T user)
    {
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);
        var roles = await this.userManager.GetRolesAsync(user).ConfigureAwait(false);

        identity.AddClaims(roles.Select(role => new Claim(JwtClaimTypes.Role, role)));

        return identity;
    }
}
