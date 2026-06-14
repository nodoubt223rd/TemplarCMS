namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Resolves content field values.
/// </summary>
public interface IFieldValueResolver
{
    /// <summary>
    /// Resolves a content field value.
    /// </summary>
    /// <param name="fieldDefinition">
    /// The field definition that owns the value scope rules.
    /// </param>
    /// <param name="values">
    /// The candidate values for the field. Callers are expected
    /// to provide values that belong to the supplied field definition.
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
