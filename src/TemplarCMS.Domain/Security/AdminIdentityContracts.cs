namespace TemplarCMS.Domain.Security;

public sealed record AdminAccountSnapshot(Guid UserId, string LoginName, string Email,
    string Language, DirectoryUserStatus Status, IReadOnlyList<DirectoryRole> Roles,
    bool MustChangePassword);
public enum AdminProvisioningStatus { Created, AlreadyInitialized, DuplicateEmail, InvalidEmail, MissingAccount, InactiveAccount, Conflict }
public sealed record AdminProvisioningResult(AdminProvisioningStatus Status, Guid? UserId = null, string? TemporaryPassword = null);
public enum AdminSignInStatus { SignedIn, InvalidCredentials, LockedOut }
public sealed record AdminSignInResult(AdminSignInStatus Status, AdminAccountSnapshot? Account = null, string? VerifiedSecurityStamp = null);
public enum AdminPasswordChangeStatus { Changed, InvalidPassword, ValidationFailed, AccountUnavailable, Conflict }
public sealed record AdminPasswordChangeResult(AdminPasswordChangeStatus Status, AdminAccountSnapshot? Account = null, IReadOnlyList<string>? Errors = null);
