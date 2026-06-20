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

    /// <inheritdoc />
    public async Task SaveItemAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureEffectiveTemplateExistsAsync(
            item.TemplateId,
            item.Id,
            cancellationToken);

        await _contentRepository.SaveItemAsync(
            item,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFieldValuesAsync(
        Guid itemId,
        IReadOnlyCollection<ContentFieldValue> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        cancellationToken.ThrowIfCancellationRequested();

        var item =
            await _contentRepository.GetItemAsync(
                itemId,
                cancellationToken);

        if (item == null)
        {
            throw new InvalidOperationException(
                $"Content item '{itemId}' was not found.");
        }

        var template =
            await EnsureEffectiveTemplateExistsAsync(
                item.TemplateId,
                item.Id,
                cancellationToken);

        var fieldsById =
            template.Fields.ToDictionary(
                field => field.Id);

        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.ItemId != itemId)
            {
                throw new InvalidOperationException(
                    $"Field value '{value.FieldKey}' does not belong to content item '{itemId}'.");
            }

            if (!fieldsById.TryGetValue(value.FieldId, out var field))
            {
                throw new InvalidOperationException(
                    $"Field '{value.FieldId}' is not defined by template '{template.Id}'.");
            }

            if (!string.Equals(
                    field.Key,
                    value.FieldKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Field key '{value.FieldKey}' does not match template field key '{field.Key}' for field '{field.Id}'.");
            }
        }

        await _contentRepository.SaveFieldValuesAsync(
            itemId,
            values,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var children =
            await _contentRepository.GetChildItemsAsync(
                itemId,
                cancellationToken);

        if (children.Count > 0)
        {
            throw new InvalidOperationException(
                $"Content item '{itemId}' cannot be deleted because it has direct child items.");
        }

        await _contentRepository.DeleteItemAsync(
            itemId,
            cancellationToken);
    }

    private async Task<ResolvedContentItem> ResolveItemAsync(
        ContentItemDefinition item,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken)
    {
        var template =
            await EnsureEffectiveTemplateExistsAsync(
                item.TemplateId,
                item.Id,
                cancellationToken);

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

    private async Task<EffectiveTemplateDefinition> EnsureEffectiveTemplateExistsAsync(
        Guid templateId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var template =
            await _contentModelCatalog.GetEffectiveTemplateAsync(
                templateId,
                cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException(
                $"Effective template '{templateId}' was not found for content item '{itemId}'.");
        }

        return template;
    }
}
