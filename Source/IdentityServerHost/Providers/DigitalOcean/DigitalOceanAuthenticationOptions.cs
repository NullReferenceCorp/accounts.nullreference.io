/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

namespace AspNet.Security.OAuth.DigitalOcean;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

/// <summary>
/// Defines a set of options used by <see cref="DigitalOceanAuthenticationHandler"/>.
/// </summary>
public class DigitalOceanAuthenticationOptions : OAuthOptions
{
    public DigitalOceanAuthenticationOptions()
    {
        this.ClaimsIssuer = DigitalOceanAuthenticationDefaults.Issuer;
        this.CallbackPath = DigitalOceanAuthenticationDefaults.CallbackPath;

        this.AuthorizationEndpoint = DigitalOceanAuthenticationDefaults.AuthorizationEndpoint;
        this.TokenEndpoint = DigitalOceanAuthenticationDefaults.TokenEndpoint;

        this.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "uuid");
        this.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        this.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    }
}
