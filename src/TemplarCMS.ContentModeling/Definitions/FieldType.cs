namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents the supported logical field types for template field definitions.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// A single line of plain text.
    /// </summary>
    SingleLineText = 0,

    /// <summary>
    /// Multiple lines of plain text.
    /// </summary>
    MultiLineText = 1,

    /// <summary>
    /// Rich text or HTML content.
    /// </summary>
    RichText = 2,

    /// <summary>
    /// A boolean checkbox value.
    /// </summary>
    Checkbox = 3,

    /// <summary>
    /// A date and time value.
    /// </summary>
    DateTime = 4,

    /// <summary>
    /// An integer numeric value.
    /// </summary>
    Integer = 5,

    /// <summary>
    /// A decimal numeric value.
    /// </summary>
    Decimal = 6,

    /// <summary>
    /// A single content item reference.
    /// </summary>
    Droplink = 7,

    /// <summary>
    /// Multiple content item references.
    /// </summary>
    Multilist = 8,

    /// <summary>
    /// An image asset reference.
    /// </summary>
    Image = 9,

    /// <summary>
    /// A file asset reference.
    /// </summary>
    File = 10,

    /// <summary>
    /// Raw JSON content.
    /// </summary>
    Json = 11,

    /// <summary>
    /// A general-purpose link value.
    /// </summary>
    GeneralLink = 12
}
