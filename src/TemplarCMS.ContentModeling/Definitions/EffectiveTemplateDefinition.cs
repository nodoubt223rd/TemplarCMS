namespace TemplarCMS.ContentModeling.Definitions;

using TemplarCMS.Domain.Content;

/// <summary>
/// Represents a fully resolved template after inheritance and overrides have been applied.
/// </summary>
public sealed class EffectiveTemplateDefinition
{
    public EffectiveTemplateDefinition(
        TemplateId id,
        string name,
        TemplateKey key,
        IReadOnlyCollection<TemplateSectionDefinition>? sections = null,
        string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Key = key;
        Sections = sections?.ToArray() ?? [];
        Fields = Sections
            .SelectMany(section => section.Fields)
            .ToArray();
        Icon = string.IsNullOrWhiteSpace(icon) ? "file" : icon.Trim();
    }

    public TemplateId Id { get; }

    public string Name { get; }

    public TemplateKey Key { get; }

    public IReadOnlyList<TemplateSectionDefinition> Sections { get; }

    public IReadOnlyList<FieldDefinition> Fields { get; }

    public string Icon { get; }
}
