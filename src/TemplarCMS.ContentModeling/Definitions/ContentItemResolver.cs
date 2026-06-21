using TemplarCMS.Domain.Content;
using TemplarCMS.ContentModeling.Abstractions;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Resolves content items using an effective template definition and
/// field value resolution services.
/// </summary>
/// <remarks>
/// A content item resolver is responsible for transforming stored field
/// values into a resolved runtime representation.
///
/// The resolver assumes the supplied template has already been fully
/// resolved by the template inheritance and effective template pipeline.
///
/// This class does not perform template inheritance resolution,
/// effective template building, persistence, caching, or fallback
/// selection. Those responsibilities belong to other components.
/// </remarks>
public sealed class ContentItemResolver
    : IContentItemResolver
{
    private readonly IFieldValueResolver _fieldValueResolver;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ContentItemResolver"/> class.
    /// </summary>
    /// <param name="fieldValueResolver">
    /// The field value resolver used to resolve individual field values.
    /// </param>
    public ContentItemResolver(
        IFieldValueResolver fieldValueResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldValueResolver);

        _fieldValueResolver = fieldValueResolver;
    }

    /// <inheritdoc />
    public ResolvedContentItem Resolve(
        ContentItemDefinition item,
        EffectiveTemplateDefinition template,
        IReadOnlyCollection<ContentFieldValue> values,
        FieldValueResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(context);

        var resolvedFields =
            new Dictionary<string, ContentFieldValue?>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var field in template.Fields)
        {
            var candidateValues =
                values
                    .Where(value => value.FieldId == field.Id)
                    .ToArray();

            var resolvedValue =
                _fieldValueResolver.Resolve(
                    field,
                    candidateValues,
                    context);

            resolvedFields[field.Key] = resolvedValue;
        }

        return new ResolvedContentItem(
            item,
            resolvedFields);
    }
}
