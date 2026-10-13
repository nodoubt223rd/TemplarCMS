using TemplarCMS.Domain.Security;

namespace TemplarCMS.Abstractions.Security;

public interface IAdminIdentityService
{
    Task<AdminSignInResult> CheckPasswordAsync(string loginName, string password, CancellationToken ct);
    Task<AdminAccountSnapshot?> GetAccountAsync(Guid userId, CancellationToken ct);
    Task<AdminPasswordChangeResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);
}
