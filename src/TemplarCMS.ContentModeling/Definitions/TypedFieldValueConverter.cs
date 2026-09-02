using System.Globalization;
using System.Text.Json;
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
                    new NullTypedFieldValue()));
        }

        return fieldDefinition.FieldType switch
        {
            FieldType.SingleLineText => ConvertAsString(fieldDefinition, value),
            FieldType.MultiLineText => ConvertAsString(fieldDefinition, value),
            FieldType.RichText => ConvertAsString(fieldDefinition, value),
            FieldType.Droplist => ConvertAsDroplist(fieldDefinition, value),
            FieldType.GeneralLink => ConvertAsGeneralLink(fieldDefinition, value),
            FieldType.DateTime => ConvertAsDateTime(fieldDefinition, value),
            FieldType.Integer => ConvertAsInteger(fieldDefinition, value),
            FieldType.Decimal => ConvertAsDecimal(fieldDefinition, value),
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
                    new StringTypedFieldValue(value.Value!)));
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsDroplist(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        const string optionsKey = "templar.droplist.options";

        if (!fieldDefinition.Metadata.TryGetValue(optionsKey, out var optionsJson))
        {
            return InvalidValue(fieldDefinition, value, "DroplistOptionsRequired", $"Field '{fieldDefinition.Key}' requires configured choice options.");
        }

        try
        {
            using var document = JsonDocument.Parse(optionsJson);
            var containsValue = document.RootElement.ValueKind == JsonValueKind.Array &&
                document.RootElement.EnumerateArray().Any(option =>
                    option.ValueKind == JsonValueKind.Object &&
                    option.TryGetProperty("value", out var configuredValue) &&
                    configuredValue.ValueKind == JsonValueKind.String &&
                    string.Equals(configuredValue.GetString(), value.Value, StringComparison.Ordinal));

            return containsValue
                ? ConvertAsString(fieldDefinition, value)
                : InvalidValue(fieldDefinition, value, "InvalidDroplistFieldValue", $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a configured choice.");
        }
        catch (JsonException)
        {
            return InvalidValue(fieldDefinition, value, "InvalidDroplistOptions", $"Field '{fieldDefinition.Key}' has invalid choice option metadata.");
        }
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
                    new IntegerTypedFieldValue(converted)));
        }

        return InvalidValue(
            fieldDefinition,
            value,
            "InvalidIntegerFieldValue",
            $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid integer.");
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsDecimal(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        if (decimal.TryParse(
                value.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var converted))
        {
            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    new DecimalTypedFieldValue(converted)));
        }

        return InvalidValue(
            fieldDefinition,
            value,
            "InvalidDecimalFieldValue",
            $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid decimal.");
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsDateTime(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        if (DateTime.TryParse(
                value.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var converted))
        {
            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    new DateTimeTypedFieldValue(converted)));
        }

        return InvalidValue(
            fieldDefinition,
            value,
            "InvalidDateTimeFieldValue",
            $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid date/time.");
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
                    new BooleanTypedFieldValue(converted)));
        }

        return value.Value switch
        {
            "1" => new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    new BooleanTypedFieldValue(true))),
            "0" => new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    new BooleanTypedFieldValue(false))),
            _ => InvalidValue(
                fieldDefinition,
                value,
                "InvalidCheckboxFieldValue",
                $"Field '{fieldDefinition.Key}' value '{value.Value}' is not a valid checkbox value.")
        };
    }

    private static ValidationResult<ConvertedFieldValue> ConvertAsGeneralLink(
        FieldDefinition fieldDefinition,
        ContentFieldValue value)
    {
        try
        {
            var converted =
                GeneralLinkValueParser.Parse(
                    value.Value!,
                    fieldDefinition.Key);

            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    new GeneralLinkTypedFieldValue(converted)));
        }
        catch (InvalidOperationException exception)
        {
            return InvalidValue(
                fieldDefinition,
                value,
                "InvalidGeneralLinkFieldValue",
                exception.Message);
        }
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
