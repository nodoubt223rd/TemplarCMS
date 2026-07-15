namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Parses external field type identifiers into logical <see cref="FieldType" /> values.
/// </summary>
public static class FieldTypeParser
{
    private static readonly IReadOnlyDictionary<string, FieldType> SupportedFieldTypes =
        new Dictionary<string, FieldType>(StringComparer.Ordinal)
        {
            ["singlelinetext"] = FieldType.SingleLineText,
            ["singleline"] = FieldType.SingleLineText,
            ["multilinetext"] = FieldType.MultiLineText,
            ["multiline"] = FieldType.MultiLineText,
            ["richtext"] = FieldType.RichText,
            ["checkbox"] = FieldType.Checkbox,
            ["datetime"] = FieldType.DateTime,
            ["integer"] = FieldType.Integer,
            ["int"] = FieldType.Integer,
            ["decimal"] = FieldType.Decimal,
            ["droplink"] = FieldType.Droplink,
            ["droptree"] = FieldType.Droplink,
            ["multilist"] = FieldType.Multilist,
            ["treelist"] = FieldType.Multilist,
            ["treelistex"] = FieldType.Multilist,
            ["checklist"] = FieldType.Multilist,
            ["multilistwithsearch"] = FieldType.Multilist,
            ["image"] = FieldType.Image,
            ["file"] = FieldType.File,
            ["serverfile"] = FieldType.File,
            ["json"] = FieldType.Json,
            ["generallink"] = FieldType.GeneralLink
        };

    private static readonly IReadOnlyDictionary<string, string> UnsupportedFieldTypeHints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["versionlink"] = "Version Link needs a dedicated version-aware link field type.",
            ["layout"] = "Layout needs a dedicated layout/rendering field type.",
            ["datasource"] = "Datasource needs a dedicated datasource field type.",
            ["pagepreview"] = "Page Preview needs a dedicated preview field type.",
            ["security"] = "Security needs a dedicated security field type.",
            ["tracking"] = "Tracking needs a dedicated tracking field type.",
            ["thumbnail"] = "Thumbnail likely needs dedicated media/thumbnail behavior.",
            ["icon"] = "Icon likely needs dedicated icon selection behavior.",
            ["text"] = "Text is ambiguous today; choose SingleLineText or MultiLineText explicitly."
        };

    /// <summary>
    /// Parses the supplied external field type identifier.
    /// </summary>
    /// <param name="fieldType">The external field type identifier.</param>
    /// <returns>The corresponding logical field type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the field type is missing or unsupported.
    /// </exception>
    public static FieldType Parse(string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType))
        {
            throw new InvalidOperationException(
                "Field type is required.");
        }

        var normalizedFieldType =
            Normalize(fieldType);

        if (SupportedFieldTypes.TryGetValue(normalizedFieldType, out var supportedFieldType))
        {
            return supportedFieldType;
        }

        var supportedTypes =
            string.Join(
                ", ",
                Enum.GetNames<FieldType>());

        if (UnsupportedFieldTypeHints.TryGetValue(normalizedFieldType, out var hint))
        {
            throw new InvalidOperationException(
                $"Unsupported field type '{fieldType}'. {hint} Supported logical field types: {supportedTypes}.");
        }

        throw new InvalidOperationException(
            $"Unsupported field type '{fieldType}'. Supported logical field types: {supportedTypes}.");
    }

    private static string Normalize(string fieldType)
    {
        return new string(
            fieldType
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
    }
}
