namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Defines a strategy for resolving stored field values for a field value resolution request.
/// </summary>
public interface IFieldValueResolutionPolicy
{
    /// <summary>
    /// Resolves the best matching field value for the supplied field definition and request.
    /// </summary>
    /// <param name="fieldDefinition">
    /// The field definition that owns the value scope rules.
    /// </param>
    /// <param name="values">
    /// The candidate values for the field.
    /// </param>
    /// <param name="context">
    /// The field value resolution request.
    /// </param>
    /// <returns>
    /// The resolved content field value, or <see langword="null"/> when no value matches.
    /// </returns>
    ContentFieldValue? Resolve(
        FieldDefinition fieldDefinition,
        IReadOnlyCollection<ContentFieldValue> values,
        FieldValueResolutionContext context);
}
