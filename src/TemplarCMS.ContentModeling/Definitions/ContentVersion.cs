namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a content version number.
/// </summary>
/// <param name="Value">The version value.</param>
public readonly record struct ContentVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentVersion" /> struct.
    /// </summary>
    /// <param name="value">The version value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the version value is negative.</exception>
    public ContentVersion(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Content version cannot be negative.");
        }

        Value = value;
    }

    /// <summary>
    ///    
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the shared value version.
    /// </summary>
    public static ContentVersion Shared => new(0);

    /// <summary>
    /// Gets the first authored content version.
    /// </summary>
    public static ContentVersion First => new(1);

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
