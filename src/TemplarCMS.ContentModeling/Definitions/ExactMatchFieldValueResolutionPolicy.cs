namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Resolves field values using strict exact-match semantics.
/// </summary>
/// <remarks>
/// Shared fields match values stored with <see cref="ContentVersion.Shared"/>.
/// Unversioned fields match language and <see cref="ContentVersion.Shared"/>.
/// Versioned fields match both language and version exactly.
/// No fallback behavior is performed.
/// </remarks>
public sealed class ExactMatchFieldValueResolutionPolicy : IFieldValueResolutionPolicy
{
    /// <inheritdoc />
    public ContentFieldValue? Resolve(
        FieldDefinition fieldDefinition,
        IReadOnlyCollection<ContentFieldValue> values,
        ContentLanguage language,
        ContentVersion version)
    {
        ArgumentNullException.ThrowIfNull(fieldDefinition);
        ArgumentNullException.ThrowIfNull(values);

        return fieldDefinition.ValueScope switch
        {
            FieldValueScope.Shared =>
                values.FirstOrDefault(
                    value => value.Version == ContentVersion.Shared),

            FieldValueScope.Unversioned =>
                values.FirstOrDefault(
                    value =>
                        value.Language == language &&
                        value.Version == ContentVersion.Shared),

            FieldValueScope.Versioned =>
                values.FirstOrDefault(
                    value =>
                        value.Language == language &&
                        value.Version == version),

            _ => throw new InvalidOperationException(
                $"Unsupported field value scope '{fieldDefinition.ValueScope}'.")
        };
    }
}
