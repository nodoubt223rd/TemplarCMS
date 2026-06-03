using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Validation;

/// <summary>
/// Validates authored template definitions before inheritance resolution or effective template generation.
/// </summary>
public sealed class TemplateValidator : ITemplateValidator
{
    public Task<ValidationResult> ValidateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (template == null)
        {
            return Task.FromResult(
                new ValidationResult(
                    [
                        new ValidationError(
                            "TemplateRequired",
                            "Template definition is required.")
                    ]));
        }

        var errors = new List<ValidationError>();

        ValidateDuplicateSectionKeys(template, errors);
        ValidateDuplicateFieldKeysWithinSections(template, errors);
        ValidateDuplicateFieldKeysAcrossTemplate(template, errors);
        ValidateSectionFieldCollisions(template, errors);

        return Task.FromResult(new ValidationResult(errors));
    }

    private static void ValidateDuplicateSectionKeys(
        TemplateDefinition template,
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
                    "DuplicateSectionKey",
                    $"Template '{template.Key}' contains multiple sections with key '{duplicateSectionKey}'.",
                    duplicateSectionKey));
        }
    }

    private static void ValidateDuplicateFieldKeysWithinSections(
        TemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        foreach (var section in template.Sections)
        {
            var duplicateFieldKeys = section.Fields
                .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var duplicateFieldKey in duplicateFieldKeys)
            {
                errors.Add(
                    new ValidationError(
                        "DuplicateFieldKeyInSection",
                        $"Section '{section.Key}' contains multiple fields with key '{duplicateFieldKey}'.",
                        $"{section.Key}.{duplicateFieldKey}"));
            }
        }
    }

    private static void ValidateDuplicateFieldKeysAcrossTemplate(
        TemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        var duplicateFieldKeys = template.Sections
            .SelectMany(section => section.Fields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateFieldKey in duplicateFieldKeys)
        {
            errors.Add(
                new ValidationError(
                    "DuplicateFieldKeyInTemplate",
                    $"Template '{template.Key}' contains multiple fields with key '{duplicateFieldKey}'.",
                    duplicateFieldKey));
        }
    }

    private static void ValidateSectionFieldCollisions(
        TemplateDefinition template,
        ICollection<ValidationError> errors)
    {
        var sectionKeys = new HashSet<string>(
            template.Sections.Select(section => section.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var field in template.Sections.SelectMany(section => section.Fields))
        {
            if (!sectionKeys.Contains(field.Key))
            {
                continue;
            }

            errors.Add(
                new ValidationError(
                    "SectionFieldKeyCollision",
                    $"Template '{template.Key}' contains a section and field with the same key '{field.Key}'.",
                    field.Key));
        }
    }
}
