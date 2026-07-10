using TemplarCMS.Domain.Content;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Resolves content items.
/// </summary>
public interface IContentItemResolver
{
    /// <summary>
    /// Resolves a content item using an effective template definition.
    /// </summary>
    /// <param name="item">
    /// The content item being resolved.
    /// </param>
    /// <param name="template">
    /// The effective template definition that describes the final
    /// runtime shape of the content item.
    /// </param>
    /// <param name="values">
    /// The candidate field values for the content item.
    /// </param>
    /// <param name="context">
    /// The field value resolution request.
    /// </param>
    /// <returns>
    /// The resolved field values for the content item.
    /// </returns>
    ResolvedContentFields Resolve(
        ContentItemDefinition item,
        EffectiveTemplateDefinition template,
        IReadOnlyCollection<ContentFieldValue> values,
        FieldValueResolutionContext context);
}
