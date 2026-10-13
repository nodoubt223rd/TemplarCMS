using TemplarCMS.Domain.Security;

namespace TemplarCMS.Abstractions.Security;

public interface IAdminProvisioningService
{
    Task<AdminProvisioningResult> SetupAsync(string email, CancellationToken ct);
    Task<AdminProvisioningResult> ResetAsync(CancellationToken ct);
}
