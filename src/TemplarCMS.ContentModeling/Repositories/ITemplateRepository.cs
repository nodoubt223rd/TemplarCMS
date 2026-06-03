using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Repositories
{
    /// <summary>
    /// Provides access to authored template definitions.
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for retrieving template definitions
    /// from a backing store such as JSON files, databases, source control,
    /// or external content systems.
    ///
    /// The repository exposes authored templates only. It does not perform
    /// inheritance resolution, validation, effective template generation,
    /// caching, or runtime model construction.
    ///
    /// Consumers should typically use <c>IContentModelCatalog</c> rather
    /// than interacting with repositories directly.
    /// </remarks>
    public interface ITemplateRepository
    {
        /// <summary>
        /// Retrieves all authored template definitions.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
        /// <returns>
        /// A read-only collection containing all authored template
        /// definitions available from the underlying source.
        /// </returns>
        /// <remarks>
        /// Inheritance resolution requires visibility into the complete
        /// template graph. For this reason repositories return the full
        /// collection of templates rather than providing individual
        /// template retrieval methods.
        ///
        /// The returned templates represent authored content models and
        /// have not yet been validated, resolved, or transformed into
        /// effective runtime models.
        /// </remarks>
        Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
            CancellationToken cancellationToken = default);
    }
}
