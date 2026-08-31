using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions
{
    /// <summary>
    /// Represents a logical template definition used by the content modeling engine.
    /// </summary>
    public sealed class TemplateDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateDefinition" /> class.
        /// </summary>
        /// <param name="id">The id of the template</param>
        /// <param name="name">The display name of the template.</param>
        /// <param name="key">The unique template key used for lookup and serialization.</param>
        /// <param name="baseTemplate">The base template this template inherits from.</param>
        /// <param name="sections">The local sections defined on this template.</param>
        public TemplateDefinition(
            TemplateId id,
            string name,
            TemplateKey key,
            TemplateDefinition? baseTemplate = null,
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
            BaseTemplate = baseTemplate;
            Sections = sections?.ToArray() ?? [];
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        }

        /// <summary>
        /// Get the id of the template.
        /// </summary>
        public TemplateId Id { get; }

        /// <summary>
        /// Gets the display name of the template.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the unique template key used for lookup and serialization.
        /// </summary>
        public TemplateKey Key { get; }

        /// <summary>
        /// Gets the base template this template inherits from.
        /// </summary>
        public TemplateDefinition? BaseTemplate { get; }

        /// <summary>
        /// Gets the local sections defined on this template.
        /// </summary>
        public IReadOnlyCollection<TemplateSectionDefinition> Sections { get; }

        public string? Icon { get; }
    }
}
