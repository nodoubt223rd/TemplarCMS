using System.Collections.Concurrent;

namespace TemplarCMS.ContentModeling.Cache
{
    /// <summary>
    /// Provides an in-memory implementation of <see cref="IContentModelCache"/>.
    /// </summary>
    /// <remarks>
    /// This implementation stores content model artifacts in process memory.
    /// It is intended for local development, tests, and single-node runtime
    /// scenarios.
    ///
    /// The cache does not apply expiration policies. Refresh and invalidation
    /// semantics are owned by the content model catalog.
    /// </remarks>
    public sealed class InMemoryContentModelCache : IContentModelCache
    {
        private readonly ConcurrentDictionary<string, object> _entries =
            new ConcurrentDictionary<string, object>();

        /// <inheritdoc />
        public Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                _entries.TryGetValue(key, out var value)
                    ? value as T
                    : null);
        }

        /// <inheritdoc />
        public Task SetAsync<T>(
            string key,
            T value,
            CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);
            cancellationToken.ThrowIfCancellationRequested();

            _entries[key] = value;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            cancellationToken.ThrowIfCancellationRequested();

            _entries.TryRemove(key, out _);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ClearAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _entries.Clear();

            return Task.CompletedTask;
        }
    }
}
