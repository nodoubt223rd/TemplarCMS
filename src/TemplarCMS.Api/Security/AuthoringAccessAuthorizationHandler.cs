using Microsoft.AspNetCore.Authorization;

namespace TemplarCMS.Api.Security;

internal sealed class AuthoringAccessAuthorizationHandler
    : AuthorizationHandler<AuthoringAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthoringAccessRequirement requirement)
    {
        if (AccountEligibility.CanAuthor(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
