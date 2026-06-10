namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Defines how a field value varies across languages and versions.
/// </summary>
public enum FieldValueScope
{
    /// <summary>
    /// The field has one value shared across all languages and versions.
    /// </summary>
    Shared,

    /// <summary>
    /// The field varies by language but not by version.
    /// </summary>
    Unversioned,

    /// <summary>
    /// The field varies by language and version.
    /// </summary>
    Versioned
}
