namespace TemplarCMS.ContentModeling.Validation;

/// <summary>
/// Represents a single validation error produced by the content modeling engine.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError" /> class.
    /// </summary>
    /// <param name="code">The stable validation error code.</param>
    /// <param name="message">The human-readable validation message.</param>
    /// <param name="target">The optional target associated with the validation error.</param>
    public ValidationError(string code, string message, string? target = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Validation error code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Validation error message is required.", nameof(message));
        }

        Code = code.Trim();
        Message = message.Trim();
        Target = string.IsNullOrWhiteSpace(target) ? null : target.Trim();
    }

    /// <summary>
    /// Gets the stable validation error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable validation message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional target associated with the validation error.
    /// </summary>
    public string? Target { get; }
}
