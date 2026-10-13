using Microsoft.AspNetCore.Identity;

namespace TemplarCMS.Persistence.Security;

/// <summary>Credential identity linked by its ID to a separate directory profile.</summary>
public sealed class CmsIdentityUser : IdentityUser<Guid>
{
    public bool MustChangePassword { get; set; }
}
