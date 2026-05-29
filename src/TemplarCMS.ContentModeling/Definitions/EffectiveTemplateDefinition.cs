namespace TemplarCMS.ContentModeling.Definitions
{
    /// <summary>
    /// Represents a fully resolved template after inheritance and overrides have been applied.
    /// Consumers should prefer effective templates over raw template definitions whenever possible.
    /// </summary>
    sealed class EffectiveTemplateDefinition
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
            Fields = fields ?? Array.Empty<FieldDefinition>();
        }

        /// <summary>
        /// Gets the display name of the resolved template.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the unique template key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the fully resolved field collection including inherited and overridden fields.
        /// </summary>
        public IReadOnlyCollection<FieldDefinition> Fields { get; }
    }
}
