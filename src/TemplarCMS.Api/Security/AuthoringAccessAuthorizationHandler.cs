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
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.IsInRole(nameof(TemplarCMS.Domain.Security.DirectoryRole.TemplarAdmin))
            && context.User.HasClaim(AccountSessionValidator.ChangeRequiredClaim, "false"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
