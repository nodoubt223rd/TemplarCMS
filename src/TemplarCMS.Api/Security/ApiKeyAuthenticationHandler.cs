using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TemplarCMS.Api.Security;

internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<AuthoringSecurityOptions> _authoringSecurityOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<AuthoringSecurityOptions> authoringSecurityOptions)
        : base(options, logger, encoder)
    {
        _authoringSecurityOptions = authoringSecurityOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options =
            _authoringSecurityOptions.CurrentValue;

        if (!options.Enabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("The API key header name is not configured."));
        }

        if (!Request.Headers.TryGetValue(options.ApiKeyHeaderName, out var providedValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Authoring security is enabled but no API key is configured."));
        }

        var providedApiKey =
            providedValues.ToString();

        if (!string.Equals(providedApiKey, options.ApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("The provided API key is invalid."));
        }

        var claims =
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ApiKeyAuthenticationDefaults.SubjectClaimType),
                new Claim(ClaimTypes.Name, "Templar API Key"),
                new Claim(ClaimTypes.Role, ApiAuthorizationPolicies.AuthorContent)
            };
        var identity =
            new ClaimsIdentity(
                claims,
                ApiKeyAuthenticationDefaults.AuthenticationType);
        var principal =
            new ClaimsPrincipal(identity);
        var ticket =
            new AuthenticationTicket(
                principal,
                ApiKeyAuthenticationDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
