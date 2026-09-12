using TemplarCMS.Domain.Security;

namespace TemplarCMS.Abstractions.Security;

public interface IUserDirectoryRepository
{
    Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken);
    Task<DirectoryUser?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DirectoryWriteResult> CreateAsync(DirectoryUserProfile profile, CancellationToken cancellationToken);
    Task<DirectoryWriteResult> UpdateAsync(Guid id, Guid revision, DirectoryUserProfile profile, CancellationToken cancellationToken);
}
