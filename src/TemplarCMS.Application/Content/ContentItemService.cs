using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;

namespace TemplarCMS.Application.Content;

/// <summary>
/// Resolves stored content items into runtime content representations.
/// </summary>
public sealed class ContentItemService : IContentItemService
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentModelCatalog _contentModelCatalog;
    private readonly IContentItemResolver _contentItemResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemService"/> class.
    /// </summary>
    public ContentItemService(
        IContentRepository contentRepository,
        IContentModelCatalog contentModelCatalog,
        IContentItemResolver contentItemResolver)
    {
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _contentModelCatalog = contentModelCatalog ?? throw new ArgumentNullException(nameof(contentModelCatalog));
        _contentItemResolver = contentItemResolver ?? throw new ArgumentNullException(nameof(contentItemResolver));
    }

    /// <inheritdoc />
    public async Task<ResolvedContentItem?> GetItemAsync(
        Guid itemId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var item =
            await _contentRepository.GetItemAsync(
                itemId,
                cancellationToken);

        if (item == null)
        {
            return null;
        }

        return await ResolveItemAsync(
            item,
            context,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
        Guid? parentId,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var items =
            await _contentRepository.GetChildItemsAsync(
                parentId,
                cancellationToken);

        var resolvedItems =
            new List<ResolvedContentItem>(items.Count);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            resolvedItems.Add(
                await ResolveItemAsync(
                    item,
                    context,
                    cancellationToken));
        }

        return resolvedItems;
    }

    private async Task<ResolvedContentItem> ResolveItemAsync(
        ContentItemDefinition item,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken)
    {
        var template =
            await _contentModelCatalog.GetEffectiveTemplateAsync(
                item.TemplateId,
                cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException(
                $"Effective template '{item.TemplateId}' was not found for content item '{item.Id}'.");
        }

        var values =
            await _contentRepository.GetFieldValuesAsync(
                item.Id,
                cancellationToken);

        return _contentItemResolver.Resolve(
            item,
            template,
            values,
            context);
    }
}
