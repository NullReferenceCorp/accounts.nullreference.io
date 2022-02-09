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
        ClaimsIssuer = DigitalOceanAuthenticationDefaults.Issuer;
        CallbackPath = DigitalOceanAuthenticationDefaults.CallbackPath;
        AuthorizationEndpoint = DigitalOceanAuthenticationDefaults.AuthorizationEndpoint;
        TokenEndpoint = DigitalOceanAuthenticationDefaults.TokenEndpoint;
        UserInformationEndpoint = DigitalOceanAuthenticationDefaults.UserInformationEndpoint;

        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "uuid");
        ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    }
}
