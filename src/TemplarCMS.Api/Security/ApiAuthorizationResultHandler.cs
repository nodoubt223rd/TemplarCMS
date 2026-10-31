using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace TemplarCMS.Api.Security;

/// <summary>Writes a single ProblemDetails response instead of challenging each scheme separately.</summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(RequestDelegate next, HttpContext http, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Succeeded)
        {
            // Account operations and antiforgery must bind to the cookie's user id,
            // never a delivery key's name when both credentials are present.
            http.User = new ClaimsPrincipal(http.User.Identities.OrderByDescending(AccountEligibility.IsAccount));
            await next(http);
            return;
        }
        var status = result.Forbidden ? 403 : 401;
        var restricted = http.User.Identities.Any(identity => AccountEligibility.IsAccount(identity) &&
            identity.HasClaim(AccountSessionValidator.ChangeRequiredClaim, "true"));
        var code = status == 401 ? "authoring-authentication-required" : restricted ? "password-change-required" : "authoring-access-forbidden";
        var detail = status == 401 ? "Provide an eligible account session or delivery credential for this operation." :
            restricted ? "Change your temporary password before accessing the CMS." : "This credential cannot access this operation.";
        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(ApiProblems.Auth(status, code, detail), options: null,
            contentType: "application/problem+json", cancellationToken: http.RequestAborted);
    }
}
