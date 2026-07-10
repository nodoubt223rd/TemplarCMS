using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Content;

/// <summary>
/// Resolves computed content paths for stored content items.
/// </summary>
public interface IContentPathResolver
{
    /// <summary>
    /// Resolves the computed absolute path for a single content item.
    /// </summary>
    /// <param name="item">The content item to resolve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The computed content path.</returns>
    Task<ContentPath> ResolveAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves computed absolute paths for a collection of content items.
    /// </summary>
    /// <param name="items">The content items to resolve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A map of content item identifiers to computed content paths.</returns>
    Task<IReadOnlyDictionary<ContentItemId, ContentPath>> ResolveAsync(
        IReadOnlyCollection<ContentItemDefinition> items,
        CancellationToken cancellationToken = default);
}
