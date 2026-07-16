namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Provides the supported field types and their default authoring metadata.
/// </summary>
public static class FieldTypeCatalog
{
    private static readonly IReadOnlyList<FieldTypeDescriptor> SupportedFieldTypes =
    [
        new(FieldType.SingleLineText, "Single-Line Text", "text", "text", "Enter text", null, null, null),
        new(FieldType.MultiLineText, "Multi-Line Text", "textarea", "text", "Enter text", 3, null, null),
        new(FieldType.RichText, "Rich Text", "textarea", "text", "Enter rich text or HTML", 6, null, "Rich text currently saves as string content."),
        new(FieldType.Checkbox, "Checkbox", "checkbox", "checkbox", null, null, null, "Stored as true or false."),
        new(FieldType.DateTime, "Date/Time", "date-time", "datetime-local", null, null, null, "Use local date and time; the API still persists a string value."),
        new(FieldType.Integer, "Integer", "number", "number", "0", null, "1", "Whole numbers only."),
        new(FieldType.Decimal, "Decimal", "number", "number", "0.00", null, "0.01", "Decimal numbers are validated by the API."),
        new(FieldType.Droplink, "Droplink", "text", "text", "Enter referenced item id or path", null, null, "Reference-style fields still use a plain text value for now."),
        new(FieldType.Multilist, "Multilist", "textarea", "text", "Enter one or more referenced values", 3, null, "Multiple references are still authored as string content for now."),
        new(FieldType.GeneralLink, "General Link", "general-link", "text", null, null, null, "General links can point to an internal content item or an external URL."),
        new(FieldType.Image, "Image", "text", "text", "Enter media reference", null, null, "Image fields still store a string reference today."),
        new(FieldType.File, "File", "text", "text", "Enter file reference", null, null, "File fields still store a string reference today."),
        new(FieldType.Json, "JSON", "textarea", "text", "{ }", 6, null, "JSON is not schema-aware yet, but the editor keeps the field distinct.")
    ];

    private static readonly IReadOnlyDictionary<FieldType, FieldTypeDescriptor> SupportedFieldTypeMap =
        SupportedFieldTypes.ToDictionary(
            descriptor => descriptor.FieldType,
            descriptor => descriptor);

    /// <summary>
    /// Returns every supported field type descriptor in stable display order.
    /// </summary>
    public static IReadOnlyList<FieldTypeDescriptor> GetAll()
    {
        return SupportedFieldTypes;
    }

    /// <summary>
    /// Returns the descriptor for a supported field type.
    /// </summary>
    public static FieldTypeDescriptor Get(FieldType fieldType)
    {
        if (SupportedFieldTypeMap.TryGetValue(fieldType, out var descriptor))
        {
            return descriptor;
        }

        throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unsupported field type.");
    }
}
