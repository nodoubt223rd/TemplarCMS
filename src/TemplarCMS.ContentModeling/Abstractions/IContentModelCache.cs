namespace TemplarCMS.ContentModeling.Abstractions
{
    /// <summary>
    /// Provides caching services for content model artifacts.
    /// </summary>
    public interface IContentModelCache
    {
        /// <summary>
        /// Retrieves a cached value by key.
        /// </summary>
        Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a value in the cache.
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a cached value.
        /// </summary>
        Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all cached content model artifacts.
        /// </summary>
        Task ClearAsync(
            CancellationToken cancellationToken = default);
    }
}
