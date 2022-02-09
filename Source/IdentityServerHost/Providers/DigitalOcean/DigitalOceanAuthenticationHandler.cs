/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

namespace AspNet.Security.OAuth.DigitalOcean;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public partial class DigitalOceanAuthenticationHandler : OAuthHandler<DigitalOceanAuthenticationOptions>
{
    public DigitalOceanAuthenticationHandler(
         IOptionsMonitor<DigitalOceanAuthenticationOptions> options,
         ILoggerFactory logger,
         UrlEncoder encoder,
         ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<OAuthTokenResponse> ExchangeCodeAsync( OAuthCodeExchangeContext context)
    {
        // See https://www.digitalocean.com/community/tutorials/how-to-use-oauth-authentication-with-digitalocean-as-a-user-or-developer for details
        var tokenRequestParameters = new Dictionary<string, string>()
        {
            ["client_id"] = this.Options.ClientId,
            ["client_secret"] = this.Options.ClientSecret,
            ["redirect_uri"] = context.RedirectUri,
            ["code"] = context.Code,
            ["grant_type"] = "authorization_code",
        };

        // PKCE https://tools.ietf.org/html/rfc7636#section-4.5, see BuildChallengeUrl
        if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier))
        {
            tokenRequestParameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier!);
            context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
        }

        var address = QueryHelpers.AddQueryString(this.Options.TokenEndpoint, tokenRequestParameters);

        using var request = new HttpRequestMessage(HttpMethod.Post, address);

        using var response = await this.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, this.Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            // await Log.UserProfileError(Logger, response, Context.RequestAborted);
            return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
        }

        using var stream = await response.Content.ReadAsStreamAsync(this.Context.RequestAborted);
        var payload = JsonDocument.Parse(stream);
        return OAuthTokenResponse.Success(payload);
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(
         ClaimsIdentity identity,
         AuthenticationProperties properties,
         OAuthTokenResponse tokens)
    {
        // Note: DigitalOcean doesn't provide a user information endpoint
        // so we rely on the details sent back in the token request.
        var user = tokens.Response!.RootElement.GetProperty("info");

        var principal = new ClaimsPrincipal(identity);

        var context = new OAuthCreatingTicketContext(principal, properties, this.Context, this.Scheme, this.Options, this.Backchannel, tokens, user);

        context.RunClaimActions();

        await this.Events.CreatingTicket(context);
        return new AuthenticationTicket(context.Principal!, context.Properties, this.Scheme.Name);
    }
}
