using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Builds fully resolved effective template definitions.
/// </summary>
public interface IEffectiveTemplateBuilder
{
    Task<ValidationResult<EffectiveTemplateDefinition>> BuildEffectiveTemplateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default);
}
