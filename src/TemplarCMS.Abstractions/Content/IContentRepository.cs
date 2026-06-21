using TemplarCMS.Domain.Content;

namespace TemplarCMS.Abstractions.Content
{
    /// <summary>
    /// Provides access to stored content items and their field values.
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for persisting authored content
    /// item records and stored field values in a backing store such as a
    /// relational database, document database, or external content system.
    ///
    /// This repository exposes stored content only. It does not perform
    /// template lookup, inheritance resolution, effective template
    /// generation, field value fallback, caching, or resolved runtime
    /// projection.
    /// </remarks>
    public interface IContentRepository
    {
        /// <summary>
        /// Retrieves a content item by its stable identifier.
        /// </summary>
        /// <param name="itemId">
        /// The content item identifier.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// The stored content item when found; otherwise <see langword="null" />.
        /// </returns>
        Task<ContentItemDefinition?> GetItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the direct children for a parent content item.
        /// </summary>
        /// <param name="parentId">
        /// The parent content item identifier. Use <see langword="null" />
        /// to retrieve root items.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A read-only collection containing the direct child items for
        /// the requested parent.
        /// </returns>
        Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
            ContentItemId? parentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all stored field values for a content item.
        /// </summary>
        /// <param name="itemId">
        /// The content item identifier that owns the values.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A read-only collection containing the stored field values for
        /// the specified content item.
        /// </returns>
        Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a content item definition.
        /// </summary>
        /// <param name="item">
        /// The content item to create or update.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <remarks>
        /// Implementations may use upsert semantics, but callers should
        /// treat this method as persisting the supplied item as the source
        /// of truth for its current stored state.
        /// </remarks>
        Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists the stored field values for a content item.
        /// </summary>
        /// <param name="itemId">
        /// The content item identifier that owns the values.
        /// </param>
        /// <param name="values">
        /// The field values to persist for the content item.
        /// </param>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <remarks>
        /// Implementations should validate that supplied values belong to
        /// the specified content item and should document whether writes
        /// replace the current stored set or merge into it.
        /// </remarks>
        Task SaveFieldValuesAsync(
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a content item and its stored field values.
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
}
