namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a logical section within a template definition.
/// </summary>
public sealed class TemplateSectionDefinition
{
    public TemplateSectionDefinition(
        Guid id,
        string name,
        string key,
        int sortOrder = 100,
        IReadOnlyCollection<FieldDefinition>? fields = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Section id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Section name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Section key is required.", nameof(key));
        }

        Id = id;
        Name = name.Trim();
        Key = key.Trim();
        SortOrder = sortOrder;
        Fields = fields?.ToArray() ?? Array.Empty<FieldDefinition>();
    }

    /// <summary>
    /// Gets the stable id of the section.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the display name of the section.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique section key used for resolution and serialization.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the sort order used when rendering or resolving sections.
    /// </summary>
    public int SortOrder { get; }

    /// <summary>
    /// Gets the field definitions contained by this section.
    /// </summary>
    public IReadOnlyCollection<FieldDefinition> Fields { get; }
}
