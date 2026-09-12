namespace TemplarCMS.Persistence.Security;

public sealed class DirectoryUserRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string Language { get; set; } = "en";
    public string Status { get; set; } = "Invited";
    public string RolesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public Guid Revision { get; set; }
}
