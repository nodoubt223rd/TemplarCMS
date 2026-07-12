using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

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
        ContentItemId itemId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a resolved content item by its computed absolute path.
    /// </summary>
    /// <param name="path">
    /// The canonical computed content path.
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
        ContentPath path,
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
        ContentItemId? parentId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a stored content item.
    /// </summary>
    /// <param name="item">
    /// The content item to create or update.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    Task SaveItemAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists stored field values for a content item.
    /// </summary>
    /// <param name="itemId">
    /// The content item identifier that owns the values.
    /// </param>
    /// <param name="values">
    /// The field values to persist.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    Task SaveFieldValuesAsync(
        ContentItemId itemId,
        IReadOnlyCollection<ContentFieldValue> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists field values for a content item using public field keys and
    /// the requested language/version context.
    /// </summary>
    /// <param name="itemId">
    /// The content item identifier that owns the values.
    /// </param>
    /// <param name="context">
    /// The requested language and version for the write operation.
    /// </param>
    /// <param name="values">
    /// The field values keyed by public field key.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    Task SaveFieldValuesAsync(
        ContentItemId itemId,
        FieldValueResolutionContext context,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a stored content item when it has no direct children.
    /// </summary>
    /// <param name="itemId">
    /// The content item identifier to delete.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    Task DeleteItemAsync(
        ContentItemId itemId,
        CancellationToken cancellationToken = default);
}
