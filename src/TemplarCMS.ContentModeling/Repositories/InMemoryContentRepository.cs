using TemplarCMS.Abstractions.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Repositories
{
    /// <summary>
    /// Stores content items and field values in memory.
    /// </summary>
    /// <remarks>
    /// This implementation is intended for tests, prototypes, and
    /// contract-shape validation. It is not durable and should not be
    /// used as a production persistence mechanism.
    /// </remarks>
    public sealed class InMemoryContentRepository : IContentRepository
    {
        private readonly Dictionary<Guid, ContentItemDefinition> _items =
            new();

        private readonly Dictionary<Guid, List<ContentFieldValue>> _fieldValues =
            new();

        /// <inheritdoc />
        public Task<ContentItemDefinition?> GetItemAsync(
            Guid itemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _items.TryGetValue(itemId, out var item);

            return Task.FromResult(item);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
            Guid? parentId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items =
                _items.Values
                    .Where(item => item.ParentId == parentId)
                    .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                    .ToArray();

            return Task.FromResult<IReadOnlyCollection<ContentItemDefinition>>(items);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
            Guid itemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_fieldValues.TryGetValue(itemId, out var values))
            {
                return Task.FromResult<IReadOnlyCollection<ContentFieldValue>>(
                    Array.Empty<ContentFieldValue>());
            }

            return Task.FromResult<IReadOnlyCollection<ContentFieldValue>>(
                values.ToArray());
        }

        /// <inheritdoc />
        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            cancellationToken.ThrowIfCancellationRequested();

            _items[item.Id] = item;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SaveFieldValuesAsync(
            Guid itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(values);

            if (itemId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Content item id is required.",
                    nameof(itemId));
            }

            foreach (var value in values)
            {
                ArgumentNullException.ThrowIfNull(value);

                if (value.ItemId != itemId)
                {
                    throw new ArgumentException(
                        $"Field value '{value.FieldKey}' does not belong to content item '{itemId}'.",
                        nameof(values));
                }
            }

            _fieldValues[itemId] = values.ToList();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteItemAsync(
            Guid itemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _items.Remove(itemId);
            _fieldValues.Remove(itemId);

            return Task.CompletedTask;
        }
    }
}
