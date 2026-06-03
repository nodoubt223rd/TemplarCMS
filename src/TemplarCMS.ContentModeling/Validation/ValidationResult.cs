namespace TemplarCMS.ContentModeling.Validation;

/// <summary>
/// Represents the result of a content modeling validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult" /> class.
    /// </summary>
    /// <param name="errors">The validation errors produced by the operation.</param>
    public ValidationResult(IReadOnlyCollection<ValidationError>? errors = null)
    {
        Errors = errors?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the validation errors produced by the operation.
    /// </summary>
    public IReadOnlyCollection<ValidationError> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether the validation operation completed without errors.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}
