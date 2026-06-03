namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a fully resolved template after inheritance and overrides have been applied.
/// </summary>
public sealed class EffectiveTemplateDefinition
{
    public EffectiveTemplateDefinition(
        Guid id,
        string name,
        string key,
        IReadOnlyCollection<TemplateSectionDefinition>? sections = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Template id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Template key is required.", nameof(key));
        }

        Id = id;
        Name = name.Trim();
        Key = key.Trim();
        Sections = sections?.ToArray() ?? [];
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Key { get; }

    public IReadOnlyCollection<TemplateSectionDefinition> Sections { get; }

    public IReadOnlyCollection<FieldDefinition> Fields =>
        [.. Sections.SelectMany(section => section.Fields)];
}
