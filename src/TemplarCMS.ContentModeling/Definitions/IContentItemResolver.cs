namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Resolves content items.
/// </summary>
public interface IContentItemResolver
{
    /// <summary>
    /// Resolves a content item.
    /// </summary>
    ResolvedContentItem Resolve(
        ContentItemDefinition item,
        InheritedTemplateDefinition template,
        IReadOnlyCollection<ContentFieldValue> values,
        FieldValueResolutionContext context);
}
