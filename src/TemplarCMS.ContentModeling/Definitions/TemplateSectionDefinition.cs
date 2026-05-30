namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a logical section within a template definition.
/// </summary>
public sealed class TemplateSectionDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateSectionDefinition" /> class.
    /// </summary>
    /// <param name="name">The display name of the section.</param>
    /// <param name="sortOrder">The sort order used when rendering or resolving sections.</param>
    /// <param name="fields">The field definitions contained by this section.</param>
    public TemplateSectionDefinition(
        string name,
        int sortOrder = 100,
        IReadOnlyCollection<FieldDefinition>? fields = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Section name is required.", nameof(name));
        }

        Name = name.Trim();
        SortOrder = sortOrder;
        Fields = fields?.ToArray() ?? Array.Empty<FieldDefinition>();
    }

    /// <summary>
    /// Gets the display name of the section.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the sort order used when rendering or resolving sections.
    /// </summary>
    public int SortOrder { get; }

    /// <summary>
    /// Gets the field definitions contained by this section.
    /// </summary>
    public IReadOnlyCollection<FieldDefinition> Fields { get; }
}
