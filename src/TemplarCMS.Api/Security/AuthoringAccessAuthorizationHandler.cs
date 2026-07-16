using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TemplarCMS.Api.Security;

internal sealed class AuthoringAccessAuthorizationHandler
    : AuthorizationHandler<AuthoringAccessRequirement>
{
    private readonly IOptionsMonitor<AuthoringSecurityOptions> _authoringSecurityOptions;

    public AuthoringAccessAuthorizationHandler(
        IOptionsMonitor<AuthoringSecurityOptions> authoringSecurityOptions)
    {
        _authoringSecurityOptions = authoringSecurityOptions;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthoringAccessRequirement requirement)
    {
        var options =
            _authoringSecurityOptions.CurrentValue;

        if (!options.Enabled)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
