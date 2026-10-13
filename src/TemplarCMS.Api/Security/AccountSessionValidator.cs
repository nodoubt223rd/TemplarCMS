using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence;

namespace TemplarCMS.Api.Security;

public sealed class AccountSessionValidator(TemplarCmsDbContext db, IAdminIdentityService service) : CookieAuthenticationEvents
{
    public const string ChangeRequiredClaim = "templar:must-change-password";
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.RejectPrincipal(); return; }
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id, context.HttpContext.RequestAborted);
        var snapshot = await service.GetAccountAsync(id, context.HttpContext.RequestAborted);
        if (user is null || snapshot?.Status != DirectoryUserStatus.Active || string.IsNullOrEmpty(user.SecurityStamp) ||
            context.Principal!.FindFirstValue("AspNet.Identity.SecurityStamp") != user.SecurityStamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        var principal = context.Principal!.Clone();
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var claim in identity.FindAll(ClaimTypes.Role).Concat(identity.FindAll(ChangeRequiredClaim)).ToArray()) identity.RemoveClaim(claim);
        foreach (var role in snapshot.Roles) identity.AddClaim(new(ClaimTypes.Role, role.ToString()));
        identity.AddClaim(new(ChangeRequiredClaim, snapshot.MustChangePassword ? "true" : "false"));
        context.ReplacePrincipal(principal);
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) => ProblemAsync(context.HttpContext, 401, "authoring-authentication-required", "Sign in to the authoring workspace.");
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        context.HttpContext.User.HasClaim(ChangeRequiredClaim, "true")
            ? ProblemAsync(context.HttpContext, 403, "password-change-required", "Change your temporary password before accessing the CMS.")
            : ProblemAsync(context.HttpContext, 403, "authoring-access-forbidden", "This account cannot access this operation.");
    private static Task ProblemAsync(HttpContext http, int status, string code, string detail)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(ApiProblems.Auth(status, code, detail), options: null, contentType: "application/problem+json", cancellationToken: http.RequestAborted);
    }
}
