using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence.Security;

namespace TemplarCMS.Api.Security;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");
        group.AddEndpointFilter(async (context, next) => { context.HttpContext.Response.Headers.CacheControl = "private, no-store"; return await next(context); });
        group.MapGet("/antiforgery", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);
            return TypedResults.Ok(new { requestToken = tokens.RequestToken, headerName = "X-Templar-CSRF" });
        }).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous().AddEndpointFilter<AuthoringAntiforgeryFilter>().RequireRateLimiting("AdminLogin");
        group.MapGet("/session", async (HttpContext http, IAdminIdentityService service) =>
        {
            var snapshot = await service.GetAccountAsync(Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!), http.RequestAborted);
            return snapshot is null ? (IResult)TypedResults.Problem(ApiProblems.Auth(401, "authoring-authentication-required", "Sign in again.")) : TypedResults.Ok(AuthSessionResponse.From(snapshot));
        }).RequireAuthorization(ApiAuthorizationPolicies.AccountSession);
        group.MapPost("/change-password", ChangeAsync).RequireAuthorization(ApiAuthorizationPolicies.AccountSession).AddEndpointFilter<AuthoringAntiforgeryFilter>();
        group.MapPost("/logout", async (HttpContext http) => { await http.SignOutAsync(IdentityConstants.ApplicationScheme); return TypedResults.NoContent(); })
            .RequireAuthorization(ApiAuthorizationPolicies.AccountSession).AddEndpointFilter<AuthoringAntiforgeryFilter>();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, HttpContext http, IAdminIdentityService service,
        UserManager<CmsIdentityUser> users, SignInManager<CmsIdentityUser> signIn)
    {
        var result = await service.CheckPasswordAsync(request.LoginName ?? "", request.Password ?? "", http.RequestAborted);
        if (result.Status != AdminSignInStatus.SignedIn || result.Account is null)
            return TypedResults.Problem(ApiProblems.Auth(401, "invalid-credentials", "The login credentials could not be accepted."));
        var user = await users.FindByIdAsync(result.Account.UserId.ToString());
        if (user is null || user.SecurityStamp != result.VerifiedSecurityStamp) return TypedResults.Problem(ApiProblems.Auth(401, "invalid-credentials", "The login credentials could not be accepted."));
        await IssueCookieAsync(http, user, result.Account, signIn, result.VerifiedSecurityStamp!);
        return TypedResults.Ok(AuthSessionResponse.From(result.Account));
    }
    private static async Task<IResult> ChangeAsync(ChangePasswordRequest request, HttpContext http, IAdminIdentityService service,
        UserManager<CmsIdentityUser> users, SignInManager<CmsIdentityUser> signIn)
    {
        var id = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await service.ChangePasswordAsync(id, request.CurrentPassword ?? "", request.NewPassword ?? "", http.RequestAborted);
        if (result.Status != AdminPasswordChangeStatus.Changed || result.Account is null)
            return result.Status == AdminPasswordChangeStatus.Conflict
                ? TypedResults.Problem(ApiProblems.Auth(409, "password-change-conflict", "The password could not be saved. Try again after signing in."))
                : TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["password"] = result.Errors?.ToArray() ?? ["The current password could not be accepted."] });
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return TypedResults.Problem(ApiProblems.Auth(401, "authoring-authentication-required", "Sign in again."));
        await IssueCookieAsync(http, user, result.Account, signIn, user.SecurityStamp!);
        return TypedResults.Ok(AuthSessionResponse.From(result.Account));
    }
    private static async Task IssueCookieAsync(HttpContext http, CmsIdentityUser user, AdminAccountSnapshot account, SignInManager<CmsIdentityUser> signIn, string verifiedStamp)
    {
        var principal = await signIn.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var stamp in identity.FindAll("AspNet.Identity.SecurityStamp").ToArray()) identity.RemoveClaim(stamp);
        identity.AddClaim(new("AspNet.Identity.SecurityStamp", verifiedStamp));
        identity.AddClaim(new(AccountSessionValidator.ChangeRequiredClaim, account.MustChangePassword ? "true" : "false"));
        foreach (var role in account.Roles) identity.AddClaim(new(ClaimTypes.Role, role.ToString()));
        await http.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties { IsPersistent = false });
    }
}
