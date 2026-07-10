using TemplarCMS.Abstractions.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Content;

/// <summary>
/// Resolves computed content paths by walking the stored parent chain.
/// </summary>
public sealed class ContentPathResolver : IContentPathResolver
{
    private readonly IContentRepository _contentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentPathResolver"/> class.
    /// </summary>
    /// <param name="contentRepository">The content repository used to load parent items.</param>
    public ContentPathResolver(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
    }

    /// <inheritdoc />
    public async Task<ContentPath> ResolveAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        var cache =
            new Dictionary<ContentItemId, ContentPath>();

        return await ResolveAsync(
            item,
            cache,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ContentItemId, ContentPath>> ResolveAsync(
        IReadOnlyCollection<ContentItemDefinition> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var cache =
            new Dictionary<ContentItemId, ContentPath>();

        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);

            await ResolveAsync(
                item,
                cache,
                cancellationToken);
        }

        return cache;
    }

    private async Task<ContentPath> ResolveAsync(
        ContentItemDefinition item,
        IDictionary<ContentItemId, ContentPath> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(item.Id, out var cachedPath))
        {
            return cachedPath;
        }

        ContentPath path;

        if (item.ParentId == null)
        {
            path =
                ContentPath.FromRoot(item.Key);
        }
        else
        {
            var parent =
                await _contentRepository.GetItemAsync(
                    item.ParentId.Value,
                    cancellationToken);

            if (parent == null)
            {
                throw new InvalidOperationException(
                    $"Parent content item '{item.ParentId.Value}' was not found for content item '{item.Id}' while computing its path.");
            }

            var parentPath =
                await ResolveAsync(
                    parent,
                    cache,
                    cancellationToken);

            path =
                ContentPath.Append(
                    parentPath,
                    item.Key);
        }

        cache[item.Id] = path;
        return path;
    }
}
