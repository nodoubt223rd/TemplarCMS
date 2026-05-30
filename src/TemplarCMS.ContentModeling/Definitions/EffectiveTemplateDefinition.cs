namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a fully resolved template after inheritance and overrides have been applied.
/// </summary>
public sealed class EffectiveTemplateDefinition
{
    public EffectiveTemplateDefinition(
        string name,
        string key,
        IReadOnlyCollection<FieldDefinition>? fields = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Template key is required.", nameof(key));
        }

        Name = name.Trim();
        Key = key.Trim();
        Fields = fields?.ToArray() ?? Array.Empty<FieldDefinition>();
    }
    public Guid Id { get; }

    public string Name { get; }

    public string Key { get; }

    public IReadOnlyCollection<FieldDefinition> Fields { get; }
}
