using Microsoft.Extensions.Logging;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Bootstrap;

/// <summary>
/// Seeds the default template set and starter content tree for a new CMS instance.
/// </summary>
public sealed class DefaultContentBootstrapper : IDefaultContentBootstrapper
{
    private readonly IContentModelCatalog _contentModelCatalog;
    private readonly IContentRepository _contentRepository;
    private readonly IContentItemService _contentItemService;
    private readonly ILogger<DefaultContentBootstrapper> _logger;

    public DefaultContentBootstrapper(
        IContentModelCatalog contentModelCatalog,
        IContentRepository contentRepository,
        IContentItemService contentItemService,
        ILogger<DefaultContentBootstrapper> logger)
    {
        _contentModelCatalog = contentModelCatalog ?? throw new ArgumentNullException(nameof(contentModelCatalog));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _contentItemService = contentItemService ?? throw new ArgumentNullException(nameof(contentItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureInitializedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _contentModelCatalog.RefreshAsync(cancellationToken);

        var folderTemplate =
            await RequireTemplateAsync(
                BuiltInTemplateKeys.Folder,
                cancellationToken);
        var itemTemplate =
            await RequireTemplateAsync(
                BuiltInTemplateKeys.Item,
                cancellationToken);

        var templar =
            await EnsureItemAsync(
                SystemSeedContentIds.TemplarRoot,
                "Templar",
                "templar",
                folderTemplate.Id,
                parentId: null,
                cancellationToken);
        var content =
            await EnsureItemAsync(
                SystemSeedContentIds.ContentRoot,
                "Content",
                "content",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        var home =
            await EnsureItemAsync(
                SystemSeedContentIds.Home,
                "Home",
                "home",
                itemTemplate.Id,
                content.Id,
                cancellationToken);
        var system =
            await EnsureItemAsync(
                SystemSeedContentIds.SystemRoot,
                "System",
                "system",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            SystemSeedContentIds.Settings,
            "Settings",
            "settings",
            folderTemplate.Id,
            system.Id,
            cancellationToken);

        var media =
            await EnsureItemAsync(
                SystemSeedContentIds.MediaRoot,
                "Media",
                "media",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            SystemSeedContentIds.Images,
            "Images",
            "images",
            folderTemplate.Id,
            media.Id,
            cancellationToken);
        await EnsureItemAsync(
            SystemSeedContentIds.Files,
            "Files",
            "files",
            folderTemplate.Id,
            media.Id,
            cancellationToken);

        var templates =
            await EnsureItemAsync(
                SystemSeedContentIds.TemplatesRoot,
                "Templates",
                "templates",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            SystemSeedContentIds.StandardTemplateItem,
            "Standard",
            "standard",
            folderTemplate.Id,
            templates.Id,
            cancellationToken);

        await EnsureHomeFieldValuesAsync(
            home.Id,
            cancellationToken);
    }

    private async Task<ContentItemDefinition> EnsureItemAsync(
        ContentItemId defaultId,
        string name,
        string key,
        TemplateId templateId,
        ContentItemId? parentId,
        CancellationToken cancellationToken)
    {
        var existing =
            await FindChildByKeyAsync(
                parentId,
                key,
                cancellationToken);

        var item =
            new ContentItemDefinition(
                existing?.Id ?? defaultId,
                existing?.Name ?? name,
                new ContentItemKey(key),
                existing?.TemplateId ?? templateId,
                parentId);

        if (existing == null)
        {
            await _contentItemService.SaveItemAsync(
                item,
                cancellationToken);

            _logger.LogInformation(
                "Seeded default content item '{ContentKey}'.",
                key);
        }

        return existing ?? item;
    }

    private async Task EnsureHomeFieldValuesAsync(
        ContentItemId homeItemId,
        CancellationToken cancellationToken)
    {
        var context =
            new FieldValueResolutionContext(
                new ContentLanguage("en"),
                ContentVersion.First);

        await _contentItemService.SaveFieldValuesAsync(
            homeItemId,
            context,
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "Home",
                ["navigationTitle"] = "Home",
                ["metaDescription"] = "Starter home item for Templar CMS.",
                ["body"] = "<p>Welcome to Templar CMS.</p>"
            },
            cancellationToken);
    }

    private async Task<EffectiveTemplateDefinition> RequireTemplateAsync(
        TemplateKey key,
        CancellationToken cancellationToken)
    {
        var template =
            await _contentModelCatalog.GetEffectiveTemplateAsync(
                key,
                cancellationToken);

        if (template != null)
        {
            return template;
        }

        throw new InvalidOperationException(
            $"Default template '{key}' was not available after bootstrap refresh.");
    }

    private async Task<ContentItemDefinition?> FindChildByKeyAsync(
        ContentItemId? parentId,
        string key,
        CancellationToken cancellationToken)
    {
        var children =
            await _contentRepository.GetChildItemsAsync(
                parentId,
                cancellationToken);

        return children.FirstOrDefault(
            child =>
                string.Equals(
                    child.Key.Value,
                    key,
                    StringComparison.OrdinalIgnoreCase));
    }
}
