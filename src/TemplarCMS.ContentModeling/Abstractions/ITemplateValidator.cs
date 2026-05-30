using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Validates template definitions before inheritance resolution or effective template generation.
/// </summary>
public interface ITemplateValidator
{
    Task<ValidationResult> ValidateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default);
}
