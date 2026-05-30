using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Validates effective template definitions after inheritance and overrides have been resolved.
/// </summary>
public interface IEffectiveTemplateValidator
{
    Task<ValidationResult> ValidateAsync(
        EffectiveTemplateDefinition template,
        CancellationToken cancellationToken = default);
}
