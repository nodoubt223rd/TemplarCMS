using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Provides access to authored and effective content model definitions.
/// </summary>
public interface IContentModelCatalog
{
    /// <summary>
    /// Gets an authored template definition by its stable id.
    /// </summary>
    Task<TemplateDefinition?> GetTemplateAsync(
        TemplateId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an authored template definition by its key.
    /// </summary>
    Task<TemplateDefinition?> GetTemplateAsync(
        TemplateKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fully resolved effective template definition by its stable id.
    /// </summary>
    Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
        TemplateId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fully resolved effective template definition by its key.
    /// </summary>
    Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
        TemplateKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all fully resolved effective template definitions.
    /// </summary>
    Task<IReadOnlyCollection<EffectiveTemplateDefinition>> GetEffectiveTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached content model definitions so the next read reloads them.
    /// </summary>
    Task InvalidateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately reloads authored templates and rebuilds effective templates.
    /// </summary>
    Task RefreshAsync(
        CancellationToken cancellationToken = default);
}
