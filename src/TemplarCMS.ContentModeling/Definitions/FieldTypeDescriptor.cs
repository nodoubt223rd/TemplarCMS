namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Describes a supported field type and the default authoring metadata exposed to clients.
/// </summary>
public sealed record FieldTypeDescriptor(
    FieldType FieldType,
    string Label,
    string EditorKind,
    string InputType,
    string? Placeholder,
    int? Rows,
    string? Step,
    string? HelpText);
