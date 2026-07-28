using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;

namespace TemplarCMS.ContentModeling.Builders;

/// <summary>
/// Builds effective templates by resolving inheritance and applying section and field overrides.
/// </summary>
public sealed class EffectiveTemplateBuilder : IEffectiveTemplateBuilder
{
    private readonly ITemplateInheritanceResolver _inheritanceResolver;

    public EffectiveTemplateBuilder(ITemplateInheritanceResolver inheritanceResolver)
    {
        _inheritanceResolver = inheritanceResolver ?? throw new ArgumentNullException(nameof(inheritanceResolver));
    }

    public async Task<ValidationResult<EffectiveTemplateDefinition>> BuildEffectiveTemplateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        var inheritanceResult =
            await _inheritanceResolver.ResolveAsync(
                template,
                cancellationToken);

        if (!inheritanceResult.IsValid || inheritanceResult.Value == null)
        {
            return new ValidationResult<EffectiveTemplateDefinition>(
                null,
                inheritanceResult.Errors);
        }

        var sections = BuildSections(
            inheritanceResult.Value.InheritanceChain);

        var effectiveTemplate =
            new EffectiveTemplateDefinition(
                template.Id,
                template.Name,
                template.Key,
                sections);

        return new ValidationResult<EffectiveTemplateDefinition>(
            effectiveTemplate);
    }

    private static IReadOnlyCollection<TemplateSectionDefinition> BuildSections(
        IReadOnlyList<TemplateDefinition> inheritanceChain)
    {
        var sectionsByKey = new Dictionary<string, SectionAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var template in inheritanceChain)
        {
            foreach (var section in template.Sections.OrderBy(section => section.SortOrder))
            {
                var sectionKey = section.Key;

                if (!sectionsByKey.TryGetValue(sectionKey, out var accumulator))
                {
                    accumulator = new SectionAccumulator(
                        section.Id,
                        section.Name,
                        section.Key,
                        section.SortOrder,
                        section.Metadata);

                    sectionsByKey.Add(sectionKey, accumulator);
                }
                else
                {
                    accumulator.ApplySectionOverride(section);
                }

                foreach (var field in section.Fields)
                {
                    accumulator.AddOrReplaceField(field);
                }
            }
        }

        return [.. sectionsByKey
            .Values
            .OrderBy(section => section.SortOrder)
            .Select(section => section.ToSectionDefinition())];
    }

    private sealed class SectionAccumulator
    {
        private readonly Dictionary<string, FieldDefinition> _fieldsByKey;

        public SectionAccumulator(
            Guid id,
            string name,
            string key,
            int sortOrder,
            IReadOnlyDictionary<string, string> metadata)
        {
            Id = id;
            Name = name;
            Key = key;
            SortOrder = sortOrder;
            Metadata = new Dictionary<string, string>(metadata, StringComparer.Ordinal);
            _fieldsByKey = new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public string Key { get; }

        public int SortOrder { get; private set; }

        public IReadOnlyDictionary<string, string> Metadata { get; private set; }

        public void ApplySectionOverride(TemplateSectionDefinition section)
        {
            Id = section.Id;
            Name = section.Name;
            SortOrder = section.SortOrder;
            Metadata = new Dictionary<string, string>(section.Metadata, StringComparer.Ordinal);
        }

        public void AddOrReplaceField(FieldDefinition field)
        {
            _fieldsByKey[field.Key] = field;
        }

        public TemplateSectionDefinition ToSectionDefinition()
        {
            return new TemplateSectionDefinition(
                Id,
                Name,
                Key,
                SortOrder,
                _fieldsByKey.Values.ToArray(),
                Metadata);
        }
    }
}
