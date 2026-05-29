namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a logical template definition used by the content modeling engine.
/// </summary>
public sealed class TemplateDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateDefinition" /> class.
    /// </summary>
    /// <param name="name">The display name of the template.</param>
    /// <param name="key">The unique template key used for lookup and serialization.</param>
    /// <param name="baseTemplates">The base templates this template inherits from.</param>
    /// <param name="sections">The local sections defined on this template.</param>
    public TemplateDefinition(
        string name,
        string key,
        IReadOnlyCollection<TemplateDefinition>? baseTemplates = null,
        IReadOnlyCollection<TemplateSectionDefinition>? sections = null)
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
        BaseTemplates = baseTemplates ?? Array.Empty<TemplateDefinition>();
        Sections = sections ?? Array.Empty<TemplateSectionDefinition>();
    }

    /// <summary>
    /// Gets the display name of the template.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique template key used for lookup and serialization.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the base templates this template inherits from.
    /// </summary>
    public IReadOnlyCollection<TemplateDefinition> BaseTemplates { get; }

    /// <summary>
    /// Gets the local sections defined on this template.
    /// </summary>
    public IReadOnlyCollection<TemplateSectionDefinition> Sections { get; }
}
