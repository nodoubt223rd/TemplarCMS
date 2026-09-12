using System.Net.Mail;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Application.Security;

public static class DirectoryProfileValidation
{
    public static Dictionary<string, string[]> Validate(DirectoryUserProfile profile)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(profile.FirstName) || profile.FirstName.Length > 100)
            errors["firstName"] = ["First name is required and must be at most 100 characters."];
        if (string.IsNullOrWhiteSpace(profile.LastName) || profile.LastName.Length > 100)
            errors["lastName"] = ["Last name is required and must be at most 100 characters."];
        if (string.IsNullOrWhiteSpace(profile.Email) || profile.Email.Length > 254 ||
            !MailAddress.TryCreate(profile.Email.Trim(), out var address) || address.Address != profile.Email.Trim())
            errors["email"] = ["A valid email address of at most 254 characters is required."];
        if (string.IsNullOrWhiteSpace(profile.Language) || profile.Language.Length > 35 ||
            profile.Language.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            errors["language"] = ["Specify a language code of at most 35 letters, digits, or hyphens."];
        if (profile.Roles is null || profile.Roles.Any(role => !Enum.IsDefined(role)) ||
            profile.Roles.Distinct().Count() != profile.Roles.Count)
            errors["roles"] = ["Roles must be distinct entries from the role catalog."];
        return errors;
    }

    public static DirectoryUserProfile Normalize(DirectoryUserProfile profile) => profile with
    {
        FirstName = profile.FirstName.Trim(), LastName = profile.LastName.Trim(),
        Email = profile.Email.Trim(), Language = profile.Language.ToLowerInvariant(),
        Roles = profile.Roles.OrderBy(role => role).ToArray()
    };
}
