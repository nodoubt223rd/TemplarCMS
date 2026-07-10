using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Resolvers;

/// <summary>
/// Resolves template inheritance chains and detects circular inheritance references.
/// </summary>
public sealed class TemplateInheritanceResolver : ITemplateInheritanceResolver
{
    /// <inheritdoc />
    public Task<ValidationResult<InheritedTemplateDefinition>> ResolveAsync(
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
                new ValidationResult<InheritedTemplateDefinition>(
                    null,
                    errors));
        }

        var resolvedTemplates =
            new List<TemplateDefinition>();

        var errorsList =
            new List<ValidationError>();

        var visiting =
            new HashSet<TemplateId>();

        var visited =
            new HashSet<TemplateId>();

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
                new ValidationResult<InheritedTemplateDefinition>(
                    null,
                    errorsList));
        }

        var inheritedTemplate =
            new InheritedTemplateDefinition(
                template,
                resolvedTemplates);

        return Task.FromResult(
            new ValidationResult<InheritedTemplateDefinition>(
                inheritedTemplate));
    }

    private static void ResolveTemplate(
        TemplateDefinition template,
        ICollection<TemplateDefinition> resolvedTemplates,
        ICollection<ValidationError> errors,
        ISet<TemplateId> visiting,
        ISet<TemplateId> visited,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templateId =
            template.Id;

        if (visiting.Contains(templateId))
        {
            errors.Add(
                new ValidationError(
                    "CircularTemplateInheritance",
                    $"Circular template inheritance detected for template '{template.Key}'.",
                    template.Key.ToString()));

            return;
        }

        if (visited.Contains(templateId))
        {
            return;
        }

        visiting.Add(templateId);

        if (template.BaseTemplate != null)
        {
            ResolveTemplate(
                template.BaseTemplate,
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
