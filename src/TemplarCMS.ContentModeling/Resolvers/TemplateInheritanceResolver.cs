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
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ResolveTemplate(
            template,
            resolvedTemplates,
            errorsList,
            visiting,
            visited);

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
        ISet<string> visiting,
        ISet<string> visited)
    {
        var key = NormalizeKey(template.Key);

        if (visiting.Contains(key))
        {
            errors.Add(
                new ValidationError(
                    "CircularTemplateInheritance",
                    $"Circular template inheritance detected for template '{template.Key}'.",
                    template.Key));

            return;
        }

        if (visited.Contains(key))
        {
            return;
        }

        visiting.Add(key);

        foreach (var baseTemplate in template.BaseTemplates)
        {
            ResolveTemplate(
                baseTemplate,
                resolvedTemplates,
                errors,
                visiting,
                visited);
        }

        visiting.Remove(key);
        visited.Add(key);

        resolvedTemplates.Add(template);
    }

    private static string NormalizeKey(string key)
    {
        return key.Trim().ToUpperInvariant();
    }
}
