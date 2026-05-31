using System;
using System.Collections.Generic;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Catalog
{
    /// <summary>
    /// Represents an immutable runtime snapshot of authored and effective
    /// content model definitions.
    /// </summary>
    /// <remarks>
    /// A snapshot is built during catalog refresh and then published as a
    /// complete unit. Runtime consumers should only ever read from a fully
    /// constructed snapshot.
    ///
    /// This avoids exposing partially refreshed template state while the
    /// catalog is rebuilding.
    /// </remarks>
    internal sealed class ContentModelSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentModelSnapshot"/> class.
        /// </summary>
        /// <param name="templates">
        /// Authored template definitions indexed by template identifier.
        /// </param>
        /// <param name="effectiveTemplates">
        /// Effective runtime template definitions indexed by template identifier.
        /// </param>
        /// <param name="templateKeys">
        /// Template key lookups indexed by template key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any dictionary is <c>null</c>.
        /// </exception>
        public ContentModelSnapshot(
            IReadOnlyDictionary<Guid, TemplateDefinition> templates,
            IReadOnlyDictionary<Guid, EffectiveTemplateDefinition> effectiveTemplates,
            IReadOnlyDictionary<string, Guid> templateKeys)
        {
            Templates = templates ?? throw new ArgumentNullException(nameof(templates));
            EffectiveTemplates = effectiveTemplates ?? throw new ArgumentNullException(nameof(effectiveTemplates));
            TemplateKeys = templateKeys ?? throw new ArgumentNullException(nameof(templateKeys));
        }

        /// <summary>
        /// Gets an empty content model snapshot.
        /// </summary>
        /// <remarks>
        /// This is used as the catalog's initial state before the first
        /// successful refresh.
        /// </remarks>
        public static ContentModelSnapshot Empty { get; } =
            new ContentModelSnapshot(
                new Dictionary<Guid, TemplateDefinition>(),
                new Dictionary<Guid, EffectiveTemplateDefinition>(),
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets authored template definitions indexed by template identifier.
        /// </summary>
        public IReadOnlyDictionary<Guid, TemplateDefinition> Templates { get; }

        /// <summary>
        /// Gets effective runtime template definitions indexed by template identifier.
        /// </summary>
        public IReadOnlyDictionary<Guid, EffectiveTemplateDefinition> EffectiveTemplates { get; }

        /// <summary>
        /// Gets template identifier lookups indexed by template key.
        /// </summary>
        public IReadOnlyDictionary<string, Guid> TemplateKeys { get; }
    }
}
