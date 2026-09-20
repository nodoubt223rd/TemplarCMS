using TemplarCMS.Domain.Content;

namespace TemplarCMS.Abstractions.Content;

public interface IContentOrderingRepository
{
    Task<ContentOrderSnapshot> ReadAsync(Guid? parentId, CancellationToken cancellationToken = default);
    Task<ContentOrderSnapshot> ReorderAsync(Guid itemId, ContentOrderDirection direction, string expectedRevision, CancellationToken cancellationToken = default);
}
