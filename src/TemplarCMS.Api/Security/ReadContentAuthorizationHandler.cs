using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace TemplarCMS.Api.Security;

internal static class AccountEligibility
{
    public static bool IsAccount(ClaimsIdentity identity) => identity.IsAuthenticated && identity.AuthenticationType == IdentityConstants.ApplicationScheme;
    public static bool CanAuthor(ClaimsPrincipal principal) => principal.Identities.Any(identity =>
        IsAccount(identity) && identity.HasClaim(identity.RoleClaimType, "TemplarAdmin") &&
        identity.HasClaim(AccountSessionValidator.ChangeRequiredClaim, "false"));
}

public sealed class ReadContentAuthorizationHandler : AuthorizationHandler<ReadContentRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ReadContentRequirement requirement)
    {
        if (AccountEligibility.CanAuthor(context.User) || context.User.Identities.Any(identity =>
            identity.IsAuthenticated && identity.AuthenticationType == DeliveryApiKeyDefaults.SchemeName &&
            identity.HasClaim(DeliveryApiKeyDefaults.ClaimType, "read"))) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
