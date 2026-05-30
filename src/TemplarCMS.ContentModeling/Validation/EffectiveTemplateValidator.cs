using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Validation;

/// <summary>
/// Validates effective template definitions after inheritance and overrides have been resolved.
/// </summary>
public sealed class EffectiveTemplateValidator : IEffectiveTemplateValidator
{
    public Task<ValidationResult> ValidateAsync(
        EffectiveTemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (template == null)
        {
            return Task.FromResult(
                new ValidationResult(
                    new[]
                    {
                        new ValidationError(
                            "EffectiveTemplateRequired",
                            "Effective template definition is required.")
                    }));
        }

        var errors = new List<ValidationError>();

        ValidateSectionKeys(template, errors);
        ValidateFieldKeys(template, errors);
        ValidateSectionFieldCollisions(template, errors);

        return Task.FromResult(new ValidationResult(errors));
    }

    private static void ValidateSectionKeys(
        EffectiveTemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        var duplicateSectionKeys = template.Sections
            .GroupBy(section => section.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateSectionKey in duplicateSectionKeys)
        {
            errors.Add(
                new ValidationError(
                    "DuplicateEffectiveSectionKey",
                    $"Effective template '{template.Key}' contains multiple sections with key '{duplicateSectionKey}'.",
                    duplicateSectionKey));
        }
    }

    private static void ValidateFieldKeys(
        EffectiveTemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        var duplicateFieldKeys = template.Fields
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateFieldKey in duplicateFieldKeys)
        {
            errors.Add(
                new ValidationError(
                    "DuplicateEffectiveFieldKey",
                    $"Effective template '{template.Key}' contains multiple fields with key '{duplicateFieldKey}'.",
                    duplicateFieldKey));
        }
    }

    private static void ValidateSectionFieldCollisions(
        EffectiveTemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        var sectionKeys = new HashSet<string>(
            template.Sections.Select(section => section.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var field in template.Fields)
        {
            if (!sectionKeys.Contains(field.Key))
            {
                continue;
            }

            errors.Add(
                new ValidationError(
                    "EffectiveSectionFieldKeyCollision",
                    $"Effective template '{template.Key}' contains a section and field with the same key '{field.Key}'.",
                    field.Key));
        }
    }
}
