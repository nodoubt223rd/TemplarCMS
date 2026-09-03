using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Abstractions.Media;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Content;

/// <summary>
/// Resolves stored content items into runtime content representations.
/// </summary>
public sealed class ContentItemService : IContentItemService
{
    private static readonly ContentLanguage SharedFieldLanguage = new("shared");

    private readonly IContentRepository _contentRepository;
    private readonly IContentModelCatalog _contentModelCatalog;
    private readonly IContentItemResolver _contentItemResolver;
    private readonly IContentPathResolver _contentPathResolver;
    private readonly ITypedFieldValueConverter _typedFieldValueConverter;
    private readonly IMediaAssetRepository? _mediaAssetRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemService"/> class.
    /// </summary>
    public ContentItemService(
        IContentRepository contentRepository,
        IContentModelCatalog contentModelCatalog,
        IContentItemResolver contentItemResolver,
        IContentPathResolver contentPathResolver,
        ITypedFieldValueConverter typedFieldValueConverter,
        IMediaAssetRepository? mediaAssetRepository = null)
    {
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _contentModelCatalog = contentModelCatalog ?? throw new ArgumentNullException(nameof(contentModelCatalog));
        _contentItemResolver = contentItemResolver ?? throw new ArgumentNullException(nameof(contentItemResolver));
        _contentPathResolver = contentPathResolver ?? throw new ArgumentNullException(nameof(contentPathResolver));
        _typedFieldValueConverter = typedFieldValueConverter ?? throw new ArgumentNullException(nameof(typedFieldValueConverter));
        _mediaAssetRepository = mediaAssetRepository;
    }

    /// <inheritdoc />
    public async Task<ResolvedContentItem?> GetItemAsync(
        ContentItemId itemId,
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

        var path =
            await _contentPathResolver.ResolveAsync(
                item,
                cancellationToken);

        return await ResolveItemAsync(
            item,
            path,
            context,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResolvedContentItem?> GetItemAsync(
        ContentPath path,
        FieldValueResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var item =
            await _contentRepository.GetItemAsync(
                path,
                cancellationToken);

        if (item == null)
        {
            return null;
        }

        return await ResolveItemAsync(
            item,
            path,
            context,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
        ContentItemId? parentId,
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
        var paths =
            await _contentPathResolver.ResolveAsync(
                items,
                cancellationToken);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            resolvedItems.Add(
                await ResolveItemAsync(
                    item,
                    paths[item.Id],
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

        await EnsureParentIsValidAsync(
            item,
            cancellationToken);

        await EnsurePathAffectingChangesAreAllowedAsync(
            item,
            cancellationToken);

        await EnsureEffectiveTemplateExistsAsync(
            item.TemplateId,
            item.Id,
            cancellationToken);

        await EnsureSiblingKeyIsAvailableAsync(
            item,
            cancellationToken);

        await _contentRepository.SaveItemAsync(
            item,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task RenameItemAsync(
        ContentItemId itemId,
        string name,
        ContentItemKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingItem =
            await _contentRepository.GetItemAsync(
                itemId,
                cancellationToken);

        if (existingItem == null)
        {
            throw new InvalidOperationException(
                $"Content item '{itemId}' was not found.");
        }

        var renamedItem =
            existingItem.Rename(
                name,
                key);

        await EnsureSiblingKeyIsAvailableAsync(
            renamedItem,
            cancellationToken);

        await _contentRepository.SaveItemAsync(
            renamedItem,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task MoveItemAsync(
        ContentItemId itemId,
        ContentItemId? parentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingItem =
            await _contentRepository.GetItemAsync(
                itemId,
                cancellationToken);

        if (existingItem == null)
        {
            throw new InvalidOperationException(
                $"Content item '{itemId}' was not found.");
        }

        var movedItem =
            existingItem.MoveTo(parentId);

        await EnsureParentIsValidAsync(
            movedItem,
            cancellationToken);

        await EnsureMoveDoesNotCreateCycleAsync(
            existingItem.Id,
            movedItem.ParentId,
            cancellationToken);

        await EnsureSiblingKeyIsAvailableAsync(
            movedItem,
            cancellationToken);

        await _contentRepository.SaveItemAsync(
            movedItem,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFieldValuesAsync(
        ContentItemId itemId,
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

        var normalizedValues = new List<ContentFieldValue>(values.Count);

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

            var conversion =
                _typedFieldValueConverter.Convert(
                    field,
                    value);

            if (!conversion.IsValid)
            {
                var error = conversion.Errors.First();

                throw new InvalidOperationException(error.Message);
            }

            if (field.FieldType == FieldType.Image && value.Value != null)
            {
                if (!Guid.TryParse(value.Value, out var mediaAssetId) ||
                    _mediaAssetRepository == null ||
                    await _mediaAssetRepository.GetAsync(mediaAssetId, cancellationToken) == null)
                {
                    throw new InvalidOperationException($"Field '{field.Key}' must reference an existing media asset.");
                }
            }

            normalizedValues.Add(field.FieldType == FieldType.RichText && value.Value != null
                ? new ContentFieldValue(value.ItemId, value.FieldId, value.FieldKey, value.Language, value.Version, RichTextHtmlSanitizer.Sanitize(value.Value))
                : value);
        }

        await _contentRepository.SaveFieldValuesAsync(
            itemId,
            normalizedValues,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFieldValuesAsync(
        ContentItemId itemId,
        FieldValueResolutionContext context,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
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
        var fieldsByKey =
            template.Fields.ToDictionary(
                field => field.Key,
                StringComparer.OrdinalIgnoreCase);
        var storedValues =
            new List<ContentFieldValue>(values.Count);

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException(
                    "Field key is required.",
                    nameof(values));
            }

            if (!fieldsByKey.TryGetValue(pair.Key, out var field))
            {
                throw new InvalidOperationException(
                    $"Field key '{pair.Key}' is not defined by template '{template.Id}'.");
            }

            storedValues.Add(
                new ContentFieldValue(
                    itemId,
                    field.Id,
                    field.Key,
                    GetStoredLanguage(field, context),
                    GetStoredVersion(field, context),
                    pair.Value));
        }

        await SaveFieldValuesAsync(
            itemId,
            storedValues,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(
        ContentItemId itemId,
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
        ContentPath path,
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

        var resolvedFields =
            _contentItemResolver.Resolve(
                item,
                template,
                values,
                context);

        return new ResolvedContentItem(
            item,
            path,
            resolvedFields.Fields,
            resolvedFields.ConvertedFields);
    }

    private async Task EnsureParentIsValidAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken)
    {
        if (item.ParentId == null)
        {
            return;
        }

        if (item.ParentId == item.Id)
        {
            throw new InvalidOperationException(
                $"Content item '{item.Id}' cannot be its own parent.");
        }

        var parent =
            await _contentRepository.GetItemAsync(
                item.ParentId.Value,
                cancellationToken);

        if (parent == null)
        {
            throw new InvalidOperationException(
                $"Parent content item '{item.ParentId.Value}' was not found for content item '{item.Id}'.");
        }
    }

    private async Task EnsureSiblingKeyIsAvailableAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken)
    {
        var siblings =
            await _contentRepository.GetChildItemsAsync(
                item.ParentId,
                cancellationToken);

        var conflictingSibling =
            siblings.FirstOrDefault(
                sibling =>
                    sibling.Id != item.Id &&
                    sibling.Key == item.Key);

        if (conflictingSibling != null)
        {
            throw new InvalidOperationException(
                $"Content item key '{item.Key}' already exists under parent '{item.ParentId?.ToString() ?? "<root>"}'.");
        }
    }

    private async Task EnsurePathAffectingChangesAreAllowedAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken)
    {
        var existingItem =
            await _contentRepository.GetItemAsync(
                item.Id,
                cancellationToken);

        if (existingItem == null)
        {
            return;
        }

        if (existingItem.ParentId != item.ParentId)
        {
            throw new InvalidOperationException(
                $"Content item '{item.Id}' cannot change parent until explicit move semantics are implemented.");
        }

        if (existingItem.Key != item.Key)
        {
            throw new InvalidOperationException(
                $"Content item '{item.Id}' cannot change key until explicit rename semantics are implemented.");
        }
    }

    private async Task EnsureMoveDoesNotCreateCycleAsync(
        ContentItemId itemId,
        ContentItemId? parentId,
        CancellationToken cancellationToken)
    {
        var currentParentId = parentId;

        while (currentParentId != null)
        {
            if (currentParentId == itemId)
            {
                throw new InvalidOperationException(
                    $"Content item '{itemId}' cannot be moved beneath itself or one of its descendants.");
            }

            var parent =
                await _contentRepository.GetItemAsync(
                    currentParentId.Value,
                    cancellationToken);

            if (parent == null)
            {
                throw new InvalidOperationException(
                    $"Parent content item '{currentParentId.Value}' was not found for content item '{itemId}'.");
            }

            currentParentId = parent.ParentId;
        }
    }

    private async Task<EffectiveTemplateDefinition> EnsureEffectiveTemplateExistsAsync(
        TemplateId templateId,
        ContentItemId itemId,
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

    private static ContentLanguage GetStoredLanguage(
        FieldDefinition field,
        FieldValueResolutionContext context)
    {
        return field.ValueScope == FieldValueScope.Shared
            ? SharedFieldLanguage
            : context.Language;
    }

    private static ContentVersion GetStoredVersion(
        FieldDefinition field,
        FieldValueResolutionContext context)
    {
        return field.ValueScope == FieldValueScope.Versioned
            ? context.Version
            : ContentVersion.Shared;
    }
}
