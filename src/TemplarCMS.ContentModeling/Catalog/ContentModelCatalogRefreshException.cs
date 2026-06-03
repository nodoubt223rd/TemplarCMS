using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Catalog
{
    /// <summary>
    /// Represents a failure that occurs while refreshing the content model catalog.
    /// </summary>
    /// <remarks>
    /// This exception is used when authored templates or effective templates fail
    /// validation during catalog refresh.
    ///
    /// The catalog does not publish a partially built snapshot when this exception
    /// is thrown.
    /// </remarks>
    public sealed class ContentModelCatalogRefreshException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentModelCatalogRefreshException"/> class.
        /// </summary>
        public ContentModelCatalogRefreshException(
            IReadOnlyCollection<ValidationError> errors)
            : base("The content model catalog could not be refreshed because one or more content model validation errors occurred.")
        {
            Errors = errors?.ToArray() ?? [];
        }

        /// <summary>
        /// Gets the validation errors that prevented the catalog from refreshing.
        /// </summary>
        public IReadOnlyCollection<ValidationError> Errors { get; }
    }
}
