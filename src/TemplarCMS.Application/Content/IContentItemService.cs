using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.Application.Content;

/// <summary>
/// Provides application-layer access to resolved content items.
/// </summary>
public interface IContentItemService
{
    /// <summary>
    /// Retrieves a resolved content item by its stable identifier.
    /// </summary>
    /// <param name="itemId">
    /// The content item identifier.
    /// </param>
    /// <param name="context">
    /// The field value resolution request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The resolved content item when found; otherwise <see langword="null" />.
    /// </returns>
    Task<ResolvedContentItem?> GetItemAsync(
        Guid itemId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves resolved direct child items for a parent content item.
    /// </summary>
    /// <param name="parentId">
    /// The parent content item identifier. Use <see langword="null" />
    /// to retrieve root items.
    /// </param>
    /// <param name="context">
    /// The field value resolution request.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A read-only collection of resolved direct child items.
    /// </returns>
    Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
        Guid? parentId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default);
}
