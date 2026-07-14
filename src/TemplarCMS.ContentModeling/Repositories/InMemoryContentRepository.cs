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
        private readonly Dictionary<ContentItemId, ContentItemDefinition> _items =
            new();

        private readonly Dictionary<ContentItemId, List<ContentFieldValue>> _fieldValues =
            new();

        /// <inheritdoc />
        public Task<ContentItemDefinition?> GetItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _items.TryGetValue(itemId, out var item);

            return Task.FromResult(item);
        }

        /// <inheritdoc />
        public Task<ContentItemDefinition?> GetItemAsync(
            ContentPath path,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(path);
            cancellationToken.ThrowIfCancellationRequested();

            var paths =
                ResolvePaths();

            var item =
                paths.FirstOrDefault(
                    pair => pair.Value == path)
                    .Key;

            if (item == default || !_items.TryGetValue(item, out var contentItem))
            {
                return Task.FromResult<ContentItemDefinition?>(null);
            }

            return Task.FromResult<ContentItemDefinition?>(contentItem);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
            ContentItemId? parentId,
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
        public Task<IReadOnlyCollection<ContentItemDefinition>> GetItemsByTemplateAsync(
            TemplateId templateId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items =
                _items.Values
                    .Where(item => item.TemplateId == templateId)
                    .OrderBy(item => item.Key.Value, StringComparer.Ordinal)
                    .ToArray();

            return Task.FromResult<IReadOnlyCollection<ContentItemDefinition>>(items);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
            ContentItemId itemId,
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
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(values);

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

            if (!_fieldValues.TryGetValue(itemId, out var storedValues))
            {
                storedValues = new List<ContentFieldValue>();
                _fieldValues[itemId] = storedValues;
            }

            foreach (var value in values)
            {
                var existingIndex =
                    storedValues.FindIndex(
                        stored =>
                            stored.FieldId == value.FieldId &&
                            stored.Language == value.Language &&
                            stored.Version == value.Version);

                if (existingIndex >= 0)
                {
                    storedValues[existingIndex] = value;
                    continue;
                }

                storedValues.Add(value);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _items.Remove(itemId);
            _fieldValues.Remove(itemId);

            return Task.CompletedTask;
        }

        private IReadOnlyDictionary<ContentItemId, ContentPath> ResolvePaths()
        {
            var cache =
                new Dictionary<ContentItemId, ContentPath>();

            foreach (var item in _items.Values)
            {
                ResolvePath(
                    item,
                    cache);
            }

            return cache;
        }

        private ContentPath ResolvePath(
            ContentItemDefinition item,
            IDictionary<ContentItemId, ContentPath> cache)
        {
            if (cache.TryGetValue(item.Id, out var cachedPath))
            {
                return cachedPath;
            }

            ContentPath path;

            if (item.IsRoot)
            {
                path = item.GetPath();
            }
            else
            {
                var parentId =
                    item.ParentId ??
                    throw new InvalidOperationException(
                        $"Content item '{item.Id}' is missing a parent identifier.");

                if (!_items.TryGetValue(parentId, out var parent))
                {
                    throw new InvalidOperationException(
                        $"Parent content item '{parentId}' was not found for content item '{item.Id}' while computing its path.");
                }

                var parentPath =
                    ResolvePath(
                        parent,
                        cache);

                path = item.GetPath(parentPath);
            }

            cache[item.Id] = path;
            return path;
        }
    }
}
