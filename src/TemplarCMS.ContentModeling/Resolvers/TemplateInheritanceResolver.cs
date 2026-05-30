using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Resolvers;

/// <summary>
/// Resolves template inheritance chains and detects circular inheritance references.
/// </summary>
public sealed class TemplateInheritanceResolver : ITemplateInheritanceResolver
{
    public Task<ValidationResult<IReadOnlyCollection<TemplateDefinition>>> ResolveAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (template == null)
        {
            var errors = new[]
            {
                new ValidationError(
                    "TemplateRequired",
                    "Template definition is required.")
            };

            return Task.FromResult(
                new ValidationResult<IReadOnlyCollection<TemplateDefinition>>(null, errors));
        }

        var resolvedTemplates = new List<TemplateDefinition>();
        var errorsList = new List<ValidationError>();
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();

        ResolveTemplate(
            template,
            resolvedTemplates,
            errorsList,
            visiting,
            visited,
            cancellationToken);

        if (errorsList.Count > 0)
        {
            return Task.FromResult(
                new ValidationResult<IReadOnlyCollection<TemplateDefinition>>(null, errorsList));
        }

        return Task.FromResult(
            new ValidationResult<IReadOnlyCollection<TemplateDefinition>>(resolvedTemplates));
    }

    private static void ResolveTemplate(
        TemplateDefinition template,
        ICollection<TemplateDefinition> resolvedTemplates,
        ICollection<ValidationError> errors,
        ISet<Guid> visiting,
        ISet<Guid> visited,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templateId = template.Id;

        if (visiting.Contains(templateId))
        {
            errors.Add(
                new ValidationError(
                    "CircularTemplateInheritance",
                    $"Circular template inheritance detected for template '{template.Key}'.",
                    template.Key));

            return;
        }

        if (visited.Contains(templateId))
        {
            return;
        }

        visiting.Add(templateId);

        foreach (var baseTemplate in template.BaseTemplates)
        {
            ResolveTemplate(
                baseTemplate,
                resolvedTemplates,
                errors,
                visiting,
                visited,
                cancellationToken);
        }

        visiting.Remove(templateId);
        visited.Add(templateId);

        resolvedTemplates.Add(template);
    }
}
