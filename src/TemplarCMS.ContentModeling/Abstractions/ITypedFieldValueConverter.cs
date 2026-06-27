using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Converts stored content field values into typed runtime values.
/// </summary>
public interface ITypedFieldValueConverter
{
    /// <summary>
    /// Converts a stored field value according to the supplied field definition.
    /// </summary>
    /// <param name="fieldDefinition">The field definition that describes the stored value.</param>
    /// <param name="value">The stored content field value to convert.</param>
    /// <returns>
    /// A conversion result that either contains a typed runtime value or
    /// validation errors when the stored value cannot be interpreted for
    /// the requested field type.
    /// </returns>
    ValidationResult<ConvertedFieldValue> Convert(
        FieldDefinition fieldDefinition,
        ContentFieldValue? value);
}
