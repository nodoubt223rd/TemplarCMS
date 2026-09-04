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
        var about =
            await EnsureItemAsync(
                SystemSeedContentIds.About,
                "About",
                "about",
                itemTemplate.Id,
                content.Id,
                cancellationToken);
        await MoveLegacyAboutItemAsync(
            about,
            home.Id,
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
        await EnsureAboutFieldValuesAsync(
            about.Id,
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
        var existingById =
            await _contentRepository.GetItemAsync(
                defaultId,
                cancellationToken);
        var existing =
            await FindChildByKeyAsync(
                parentId,
                key,
                cancellationToken);

        if (existing != null)
        {
            LogSeedItemDriftIfDetected(
                defaultId,
                key,
                templateId,
                parentId,
                existing,
                existingById);

            return existing;
        }

        if (existingById != null)
        {
            _logger.LogWarning(
                "Seed content item '{ContentKey}' drift detected. Canonical id '{ExpectedId}' already exists as runtime item '{ActualId}' beneath parent '{ActualParentId}' instead of expected parent '{ExpectedParentId}'. Bootstrap preserved the existing runtime item instead of relocating it.",
                key,
                defaultId,
                existingById.Id,
                existingById.ParentId?.ToString() ?? "<root>",
                parentId?.ToString() ?? "<root>");

            return existingById;
        }

        var item =
            new ContentItemDefinition(
                defaultId,
                name,
                new ContentItemKey(key),
                templateId,
                parentId);

        await _contentItemService.SaveItemAsync(
            item,
            cancellationToken);

        _logger.LogInformation(
            "Seeded default content item '{ContentKey}'.",
            key);

        return item;
    }

    private async Task EnsureHomeFieldValuesAsync(
        ContentItemId homeItemId,
        CancellationToken cancellationToken)
    {
        var context =
            new FieldValueResolutionContext(
                new ContentLanguage("en"),
                ContentVersion.First);
        var existingFieldValues =
            await _contentRepository.GetFieldValuesAsync(
                homeItemId,
                cancellationToken);
        var existingFieldKeys =
            existingFieldValues
                .Select(value => value.FieldKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingDefaults =
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "Home",
                ["navigationTitle"] = "Home",
                ["metaDescription"] = "Starter home item for Templar CMS.",
                ["body"] = "<p>Welcome to Templar CMS.</p>"
            }
            .Where(pair => !existingFieldKeys.Contains(pair.Key))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

        if (missingDefaults.Count == 0)
        {
            return;
        }

        await _contentItemService.SaveFieldValuesAsync(
            homeItemId,
            context,
            missingDefaults,
            cancellationToken);
    }

    private async Task MoveLegacyAboutItemAsync(
        ContentItemDefinition about,
        ContentItemId legacyParentId,
        ContentItemId expectedParentId,
        CancellationToken cancellationToken)
    {
        // Correct the one legacy starter-tree relationship without moving user-authored items.
        if (about.Id != SystemSeedContentIds.About || about.ParentId != legacyParentId)
        {
            return;
        }

        await _contentItemService.MoveItemAsync(
            about.Id,
            expectedParentId,
            cancellationToken);

        _logger.LogInformation("Moved legacy starter item 'about' beneath Content.");
    }

    private async Task EnsureAboutFieldValuesAsync(
        ContentItemId aboutItemId,
        CancellationToken cancellationToken)
    {
        var context =
            new FieldValueResolutionContext(
                new ContentLanguage("en"),
                ContentVersion.First);
        var existingFieldValues =
            await _contentRepository.GetFieldValuesAsync(
                aboutItemId,
                cancellationToken);
        var existingFieldKeys =
            existingFieldValues
                .Select(value => value.FieldKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingDefaults =
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "About Templar CMS",
                ["navigationTitle"] = "About",
                ["metaDescription"] = "Learn about the Templar CMS starter site.",
                ["body"] = "<p>Templar CMS is a template-driven, API-first headless CMS built for clear content modeling and flexible delivery.</p>"
            }
            .Where(pair => !existingFieldKeys.Contains(pair.Key))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

        if (missingDefaults.Count == 0)
        {
            return;
        }

        await _contentItemService.SaveFieldValuesAsync(
            aboutItemId,
            context,
            missingDefaults,
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

    private void LogSeedItemDriftIfDetected(
        ContentItemId expectedId,
        string key,
        TemplateId expectedTemplateId,
        ContentItemId? expectedParentId,
        ContentItemDefinition existingAtExpectedLocation,
        ContentItemDefinition? existingByCanonicalId)
    {
        if (existingAtExpectedLocation.Id != expectedId)
        {
            _logger.LogWarning(
                "Seed content item '{ContentKey}' drift detected. Expected canonical id '{ExpectedId}' beneath parent '{ExpectedParentId}', but runtime item '{ActualId}' occupies the seeded location. Bootstrap preserved the runtime item instead of replacing it.",
                key,
                expectedId,
                expectedParentId?.ToString() ?? "<root>",
                existingAtExpectedLocation.Id);
        }

        if (existingAtExpectedLocation.TemplateId != expectedTemplateId)
        {
            _logger.LogWarning(
                "Seed content item '{ContentKey}' drift detected. Expected template '{ExpectedTemplateId}' for canonical id '{ExpectedId}', but runtime item '{ActualId}' uses template '{ActualTemplateId}'. Bootstrap preserved the runtime template assignment.",
                key,
                expectedTemplateId,
                expectedId,
                existingAtExpectedLocation.Id,
                existingAtExpectedLocation.TemplateId);
        }

        if (existingByCanonicalId != null
            && existingByCanonicalId.Id == expectedId
            && existingByCanonicalId.Id != existingAtExpectedLocation.Id)
        {
            _logger.LogWarning(
                "Seed content item '{ContentKey}' drift detected. Canonical id '{ExpectedId}' exists separately from the runtime item '{ActualId}' that occupies the seeded location. Bootstrap preserved the current runtime state and did not merge the conflict automatically.",
                key,
                expectedId,
                existingAtExpectedLocation.Id);
        }
    }
}
