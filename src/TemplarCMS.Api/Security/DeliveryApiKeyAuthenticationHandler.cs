using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TemplarCMS.Api.Security;

public sealed class DeliveryApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    DeliveryKeyValidator validator) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DeliveryApiKeyDefaults.HeaderName, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());
        if (!Request.IsHttps || values.Count != 1 || !validator.TryAuthenticate(values[0]!, out var name))
            return Task.FromResult(AuthenticateResult.Fail("Invalid delivery credential."));
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, name!), new Claim(DeliveryApiKeyDefaults.ClaimType, "read")
        ], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
