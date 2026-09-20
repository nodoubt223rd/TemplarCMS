using TemplarCMS.Abstractions.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Content;

public sealed class ContentOrderingService(IContentOrderingRepository repository)
{
    public Task<ContentOrderSnapshot> ReadAsync(Guid? parentId, CancellationToken ct) => repository.ReadAsync(parentId, ct);
    public Task<ContentOrderSnapshot> ReorderAsync(Guid id, string direction, string revision, CancellationToken ct)
    {
        if (!Enum.TryParse<ContentOrderDirection>(direction, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new ArgumentException("Choose Up, Down, First, or Last.");
        if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Sibling revision is required.");
        return repository.ReorderAsync(id, parsed, revision, ct);
    }
}
