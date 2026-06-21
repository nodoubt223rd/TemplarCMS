namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a field value resolution request.
/// </summary>
public sealed class FieldValueResolutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldValueResolutionContext"/> class.
    /// </summary>
    /// <param name="language">
    /// The requested content language.
    /// </param>
    /// <param name="version">
    /// The requested content version.
    /// </param>
    public FieldValueResolutionContext(
        ContentLanguage language,
        ContentVersion version)
    {
        Language = language;
        Version = version;
    }

    /// <summary>
    /// Gets the requested content language.
    /// </summary>
    public ContentLanguage Language { get; }

    /// <summary>
    /// Gets the requested content version.
    /// </summary>
    public ContentVersion Version { get; }
}
