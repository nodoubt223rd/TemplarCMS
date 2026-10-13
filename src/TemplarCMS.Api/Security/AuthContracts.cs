using System.Text.Json.Serialization;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Api.Security;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LoginRequest(string? LoginName, string? Password);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
public sealed record AuthLink(string Href, string Method = "GET");
public sealed record AuthSessionResponse(Guid UserId, string LoginName, string Email, string Language, string[] Roles,
    bool MustChangePassword, [property: JsonPropertyName("_links")] Dictionary<string, AuthLink> Links)
{
    public static AuthSessionResponse From(AdminAccountSnapshot account) => new(account.UserId, account.LoginName, account.Email,
        account.Language, account.Roles.Select(value => value.ToString()).ToArray(), account.MustChangePassword,
        new() { ["self"] = new("/api/v1/auth/session"), ["change-password"] = new("/api/v1/auth/change-password", "POST"), ["logout"] = new("/api/v1/auth/logout", "POST") });
}
