namespace TemplarCMS.ContentModeling.Definitions
{
    /// <summary>
    /// Represents a template definition after inheritance has been resolved,
    /// but before the final effective template has been built.
    /// </summary>
    /// <remarks>
    /// An inherited template preserves the authored template identity while
    /// exposing the ordered inheritance chain used to build the final effective
    /// template.
    ///
    /// This type is intentionally separate from <see cref="EffectiveTemplateDefinition" />.
    /// Inheritance resolution answers which templates participate in the final
    /// shape, while effective template building answers what the final runtime
    /// shape actually is.
    /// </remarks>
    public sealed class InheritedTemplateDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InheritedTemplateDefinition" /> class.
        /// </summary>
        /// <param name="template">
        /// The authored template being resolved.
        /// </param>
        /// <param name="inheritanceChain">
        /// The ordered inheritance chain used to build the effective template.
        /// </param>
        public InheritedTemplateDefinition(
            TemplateDefinition template,
            IReadOnlyCollection<TemplateDefinition> inheritanceChain)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (inheritanceChain == null || inheritanceChain.Count == 0)
            {
                throw new ArgumentException(
                    "Inheritance chain is required.",
                    nameof(inheritanceChain));
            }

            Template = template;
            InheritanceChain = inheritanceChain.ToArray();
        }

        /// <summary>
        /// Gets the authored template being resolved.
        /// </summary>
        public TemplateDefinition Template { get; }

        /// <summary>
        /// Gets the ordered inheritance chain used to build the effective template.
        /// </summary>
        /// <remarks>
        /// The chain should include the root base templates first and the current
        /// template last.
        ///
        /// Example:
        ///
        /// BaseContent
        ///     ↓
        /// BasePage
        ///     ↓
        /// ArticlePage
        /// </remarks>
        public IReadOnlyList<TemplateDefinition> InheritanceChain { get; }
    }
}
