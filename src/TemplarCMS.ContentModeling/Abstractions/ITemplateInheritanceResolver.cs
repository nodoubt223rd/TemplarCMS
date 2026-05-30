using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Resolves template inheritance chains and validates inheritance graph rules.
/// </summary>
public interface ITemplateInheritanceResolver
{
    Task<ValidationResult<IReadOnlyCollection<TemplateDefinition>>> ResolveAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default);
}
