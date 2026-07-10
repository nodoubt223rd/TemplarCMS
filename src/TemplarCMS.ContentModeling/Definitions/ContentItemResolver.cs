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
    private readonly ITypedFieldValueConverter _typedFieldValueConverter;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ContentItemResolver"/> class.
    /// </summary>
    /// <param name="fieldValueResolver">
    /// The field value resolver used to resolve individual field values.
    /// </param>
    /// <param name="typedFieldValueConverter">
    /// The typed field value converter used to project resolved field
    /// values into runtime typed values.
    /// </param>
    public ContentItemResolver(
        IFieldValueResolver fieldValueResolver,
        ITypedFieldValueConverter typedFieldValueConverter)
    {
        ArgumentNullException.ThrowIfNull(fieldValueResolver);
        ArgumentNullException.ThrowIfNull(typedFieldValueConverter);

        _fieldValueResolver = fieldValueResolver;
        _typedFieldValueConverter = typedFieldValueConverter;
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
        var convertedFields =
            new Dictionary<string, TypedFieldValue>(
                StringComparer.OrdinalIgnoreCase);
        var valuesByFieldId =
            values.ToLookup(value => value.FieldId);

        foreach (var field in template.Fields)
        {
            var candidateValues =
                valuesByFieldId[field.Id]
                    .ToArray();

            var resolvedValue =
                _fieldValueResolver.Resolve(
                    field,
                    candidateValues,
                    context);

            var convertedValue =
                _typedFieldValueConverter.Convert(
                    field,
                    resolvedValue);

            if (!convertedValue.Succeeded)
            {
                var error =
                    convertedValue.Errors.First();

                throw new InvalidOperationException(error.Message);
            }

            resolvedFields[field.Key] = resolvedValue;
            convertedFields[field.Key] = convertedValue.Value!.Value;
        }

        return new ResolvedContentItem(
            item,
            resolvedFields,
            convertedFields);
    }
}
