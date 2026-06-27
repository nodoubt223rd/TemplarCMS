using System.Globalization;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Converts stored string field values into typed runtime values for a
/// limited set of supported field types.
/// </summary>
public sealed class TypedFieldValueConverter : ITypedFieldValueConverter
{
    /// <inheritdoc />
    public ValidationResult<ConvertedFieldValue> Convert(
        FieldDefinition fieldDefinition,
        ContentFieldValue? value)
    {
        ArgumentNullException.ThrowIfNull(fieldDefinition);

        if (value == null || value.Value == null)
        {
            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    null));
        }

        return fieldDefinition.FieldType switch
        {
            FieldType.SingleLineText => ConvertAsString(fieldDefinition, value),
            FieldType.MultiLineText => ConvertAsString(fieldDefinition, value),
            FieldType.RichText => ConvertAsString(fieldDefinition, value),
            FieldType.Integer => ConvertAsInteger(fieldDefinition, value),
            FieldType.Checkbox => ConvertAsBoolean(fieldDefinition, value),
            _ => Unsupported(fieldDefinition, value)
        };
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsString(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        return new ValidationResult<ConvertedFieldValue>(
            new ConvertedFieldValue(
                fieldDefinition,
                value,
                value.Value));
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsInteger(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        if (int.TryParse(
                value.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var converted))
        {
            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    converted));
        }

        return InvalidValue(
            fieldDefinition,
            value,
            "InvalidIntegerFieldValue",
            $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid integer.");
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsBoolean(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        if (bool.TryParse(value.Value, out var converted))
        {
            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    converted));
        }

        return value.Value switch
        {
            "1" => new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    true)),
            "0" => new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    false)),
            _ => InvalidValue(
                fieldDefinition,
                value,
                "InvalidCheckboxFieldValue",
                $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid checkbox value.")
        };
    }

    private static ValidationResult<ConvertedFieldValue> Unsupported(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        return new ValidationResult<ConvertedFieldValue>(
            errors:
            [
                new ValidationError(
                    "UnsupportedFieldValueConversion",
                    $"Field type '{fieldDefinition.FieldType}' is not supported by the current typed field value converter.",
                    fieldDefinition.Key)
            ]);
    }

    private static ValidationResult<ConvertedFieldValue> InvalidValue(
        FieldDefinition fieldDefinition,
        ContentFieldValue value,
        string code,
        string message)
    {
        return new ValidationResult<ConvertedFieldValue>(
            errors:
            [
                new ValidationError(
                    code,
                    message,
                    fieldDefinition.Key)
            ]);
    }
}
