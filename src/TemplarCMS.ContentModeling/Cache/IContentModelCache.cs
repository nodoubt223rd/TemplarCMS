using System.Threading;
using System.Threading.Tasks;

namespace TemplarCMS.ContentModeling.Cache
{
    /// <summary>
    /// Provides caching services for content model artifacts.
    /// </summary>
    public interface IContentModelCache
    {
        /// <summary>
        /// Retrieves a cached value by key.
        /// </summary>
        /// <typeparam name="T">The expected cached value type.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>
        /// The cached value if found; otherwise <c>null</c>.
        /// </returns>
        Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Stores a value in the cache.
        /// </summary>
        /// <typeparam name="T">The value type being cached.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        Task SetAsync<T>(
            string key,
            T value,
            CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Removes a cached value.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all cached content model artifacts.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        Task ClearAsync(
            CancellationToken cancellationToken = default);
    }
}
