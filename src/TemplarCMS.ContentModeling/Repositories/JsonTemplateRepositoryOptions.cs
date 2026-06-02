namespace TemplarCMS.ContentModeling.Repositories
{
    /// <summary>
    /// Configuration options for the JSON template repository.
    /// </summary>
    /// <remarks>
    /// These options control how template definition files are
    /// located and loaded by <see cref="JsonTemplateRepository"/>.
    /// </remarks>
    public sealed class JsonTemplateRepositoryOptions
    {
        /// <summary>
        /// Gets or sets the directory containing JSON template
        /// definition files.
        /// </summary>
        public string TemplatesPath { get; set; } = string.Empty;
    }
}
