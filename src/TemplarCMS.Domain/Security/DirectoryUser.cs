namespace TemplarCMS.Domain.Security;

public enum DirectoryUserStatus { Invited, Active, Suspended, Deactivated }
public enum DirectoryRole { Author, Developer, FormsEditor, TemplarAdmin, MarketingAutomationEditors }

public sealed record DirectoryUser(
    Guid Id, string FirstName, string LastName, string Email, string Language,
    DirectoryUserStatus Status, IReadOnlyList<DirectoryRole> Roles,
    DateTimeOffset CreatedAt, DateTimeOffset? LastLogin, Guid Revision);

public sealed record DirectoryUserProfile(string FirstName, string LastName, string Email,
    string Language, IReadOnlyList<DirectoryRole> Roles);

public enum DirectoryWriteStatus { Saved, NotFound, Conflict, DuplicateEmail, InvalidRoles }
public sealed record DirectoryWriteResult(DirectoryWriteStatus Status, DirectoryUser? User = null);
