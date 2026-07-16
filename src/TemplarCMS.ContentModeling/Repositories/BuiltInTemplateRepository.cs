using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Repositories;

/// <summary>
/// Merges canonical built-in templates with mutable authored templates from an inner repository.
/// </summary>
public sealed class BuiltInTemplateRepository : ITemplateRepository
{
    private readonly ITemplateRepository _innerRepository;
    private readonly IReadOnlyDictionary<TemplateKey, TemplateDefinition> _builtInTemplates;

    public BuiltInTemplateRepository(
        ITemplateRepository innerRepository,
        IBuiltInTemplateProvider builtInTemplateProvider)
    {
        ArgumentNullException.ThrowIfNull(innerRepository);
        ArgumentNullException.ThrowIfNull(builtInTemplateProvider);

        _innerRepository = innerRepository;
        _builtInTemplates =
            builtInTemplateProvider
                .GetTemplates()
                .ToDictionary(template => template.Key);
    }

    public async Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var authoredTemplates =
            await _innerRepository.GetTemplatesAsync(cancellationToken);
        var mergedTemplates =
            new Dictionary<TemplateKey, TemplateDefinition>(_builtInTemplates);

        foreach (var template in authoredTemplates)
        {
            mergedTemplates[template.Key] = template;
        }

        return mergedTemplates.Values
            .OrderBy(template => template.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public Task CreateTemplateAsync(
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (_builtInTemplates.ContainsKey(template.Key))
        {
            throw new InvalidOperationException(
                $"Template key '{template.Key}' is reserved by a built-in system template.");
        }

        return _innerRepository.CreateTemplateAsync(
            template,
            cancellationToken);
    }

    public Task UpdateTemplateAsync(
        TemplateKey existingKey,
        TemplateDefinition template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (_builtInTemplates.ContainsKey(existingKey))
        {
            throw new InvalidOperationException(
                $"Built-in template '{existingKey}' is source-controlled and cannot be updated through the mutable template repository.");
        }

        if (_builtInTemplates.ContainsKey(template.Key))
        {
            throw new InvalidOperationException(
                $"Template key '{template.Key}' is reserved by a built-in system template.");
        }

        return _innerRepository.UpdateTemplateAsync(
            existingKey,
            template,
            cancellationToken);
    }

    public Task DeleteTemplateAsync(
        TemplateKey key,
        CancellationToken cancellationToken = default)
    {
        if (_builtInTemplates.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Built-in template '{key}' is source-controlled and cannot be deleted through the mutable template repository.");
        }

        return _innerRepository.DeleteTemplateAsync(
            key,
            cancellationToken);
    }
}
