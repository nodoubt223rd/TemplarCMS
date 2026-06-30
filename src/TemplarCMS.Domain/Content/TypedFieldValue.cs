namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a strongly typed runtime field value.
/// </summary>
public abstract class TypedFieldValue
{
}

/// <summary>
/// Represents the absence of a converted value.
/// </summary>
public sealed class NullTypedFieldValue : TypedFieldValue
{
}

/// <summary>
/// Represents a converted string field value.
/// </summary>
public sealed class StringTypedFieldValue : TypedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringTypedFieldValue" /> class.
    /// </summary>
    /// <param name="value">The converted string value.</param>
    public StringTypedFieldValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    /// <summary>
    /// Gets the converted string value.
    /// </summary>
    public string Value { get; }
}

/// <summary>
/// Represents a converted integer field value.
/// </summary>
public sealed class IntegerTypedFieldValue : TypedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerTypedFieldValue" /> class.
    /// </summary>
    /// <param name="value">The converted integer value.</param>
    public IntegerTypedFieldValue(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the converted integer value.
    /// </summary>
    public int Value { get; }
}

/// <summary>
/// Represents a converted decimal field value.
/// </summary>
public sealed class DecimalTypedFieldValue : TypedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalTypedFieldValue" /> class.
    /// </summary>
    /// <param name="value">The converted decimal value.</param>
    public DecimalTypedFieldValue(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the converted decimal value.
    /// </summary>
    public decimal Value { get; }
}

/// <summary>
/// Represents a converted date and time field value.
/// </summary>
public sealed class DateTimeTypedFieldValue : TypedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeTypedFieldValue" /> class.
    /// </summary>
    /// <param name="value">The converted date and time value.</param>
    public DateTimeTypedFieldValue(DateTime value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the converted date and time value.
    /// </summary>
    public DateTime Value { get; }
}

/// <summary>
/// Represents a converted boolean field value.
/// </summary>
public sealed class BooleanTypedFieldValue : TypedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanTypedFieldValue" /> class.
    /// </summary>
    /// <param name="value">The converted boolean value.</param>
    public BooleanTypedFieldValue(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the converted boolean value.
    /// </summary>
    public bool Value { get; }
}
