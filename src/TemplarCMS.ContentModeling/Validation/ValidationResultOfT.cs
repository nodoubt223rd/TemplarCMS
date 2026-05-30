namespace TemplarCMS.ContentModeling.Validation;

/// <summary>
/// Represents the result of a content modeling operation that may produce both a value and validation errors.
/// </summary>
/// <typeparam name="T">The type of value produced by the operation.</typeparam>
public sealed class ValidationResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult{T}" /> class.
    /// </summary>
    /// <param name="value">The value produced by the operation.</param>
    /// <param name="errors">The validation errors produced by the operation.</param>
    public ValidationResult(
        T? value = default,
        IReadOnlyCollection<ValidationError>? errors = null)
    {
        Value = value;
        Errors = errors?.ToArray() ?? Array.Empty<ValidationError>();
    }

    /// <summary>
    /// Gets the value produced by the operation.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the validation errors produced by the operation.
    /// </summary>
    public IReadOnlyCollection<ValidationError> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed without validation errors.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the operation produced a value.
    /// </summary>
    public bool HasValue => Value is not null;

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully and produced a value.
    /// </summary>
    public bool Succeeded => IsValid && HasValue;
}
