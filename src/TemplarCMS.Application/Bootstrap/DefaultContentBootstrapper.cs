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
    private static readonly TemplateKey StandardTemplateKey = new("standard");
    private static readonly TemplateKey FolderTemplateKey = new("folder");
    private static readonly TemplateKey ItemTemplateKey = new("item");

    private static readonly ContentItemId TemplarRootId = new(new Guid("0B6AFCDA-BBD1-4CB3-A392-A4078C899F2A"));
    private static readonly ContentItemId ContentRootId = new(new Guid("93D45FC9-C7A0-48EC-B78D-B8980D4C2DF0"));
    private static readonly ContentItemId HomeItemId = new(new Guid("EF88F020-C20C-47E5-99A8-1E2F6F69506A"));
    private static readonly ContentItemId SystemRootId = new(new Guid("5F27B5F5-20A7-4445-AEE4-7C955F804084"));
    private static readonly ContentItemId SettingsItemId = new(new Guid("FE75ED5F-EE55-4839-AE36-D1E7AB7E4A3A"));
    private static readonly ContentItemId MediaRootId = new(new Guid("705D37CB-9A7B-4DDE-AF97-6A0453452DBB"));
    private static readonly ContentItemId ImagesItemId = new(new Guid("6784EA0F-53A0-448E-B253-1586E51101F6"));
    private static readonly ContentItemId FilesItemId = new(new Guid("CE0F8D78-C857-483E-9A4B-1F88E31A8A89"));
    private static readonly ContentItemId TemplatesRootId = new(new Guid("4CF76720-EA11-431C-8DF0-93A057DFAD98"));
    private static readonly ContentItemId StandardItemId = new(new Guid("74922877-D8DF-466B-93CE-96E5C91D5B7E"));

    private readonly ITemplateRepository _templateRepository;
    private readonly IContentModelCatalog _contentModelCatalog;
    private readonly IContentRepository _contentRepository;
    private readonly IContentItemService _contentItemService;
    private readonly ILogger<DefaultContentBootstrapper> _logger;

    public DefaultContentBootstrapper(
        ITemplateRepository templateRepository,
        IContentModelCatalog contentModelCatalog,
        IContentRepository contentRepository,
        IContentItemService contentItemService,
        ILogger<DefaultContentBootstrapper> logger)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _contentModelCatalog = contentModelCatalog ?? throw new ArgumentNullException(nameof(contentModelCatalog));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _contentItemService = contentItemService ?? throw new ArgumentNullException(nameof(contentItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureInitializedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTemplatesAsync(cancellationToken);
        await _contentModelCatalog.RefreshAsync(cancellationToken);

        var folderTemplate =
            await RequireTemplateAsync(
                FolderTemplateKey,
                cancellationToken);
        var itemTemplate =
            await RequireTemplateAsync(
                ItemTemplateKey,
                cancellationToken);

        var templar =
            await EnsureItemAsync(
                TemplarRootId,
                "Templar",
                "templar",
                folderTemplate.Id,
                parentId: null,
                cancellationToken);
        var content =
            await EnsureItemAsync(
                ContentRootId,
                "Content",
                "content",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        var home =
            await EnsureItemAsync(
                HomeItemId,
                "Home",
                "home",
                itemTemplate.Id,
                content.Id,
                cancellationToken);
        var system =
            await EnsureItemAsync(
                SystemRootId,
                "System",
                "system",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            SettingsItemId,
            "Settings",
            "settings",
            folderTemplate.Id,
            system.Id,
            cancellationToken);

        var media =
            await EnsureItemAsync(
                MediaRootId,
                "Media",
                "media",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            ImagesItemId,
            "Images",
            "images",
            folderTemplate.Id,
            media.Id,
            cancellationToken);
        await EnsureItemAsync(
            FilesItemId,
            "Files",
            "files",
            folderTemplate.Id,
            media.Id,
            cancellationToken);

        var templates =
            await EnsureItemAsync(
                TemplatesRootId,
                "Templates",
                "templates",
                folderTemplate.Id,
                templar.Id,
                cancellationToken);
        await EnsureItemAsync(
            StandardItemId,
            "Standard",
            "standard",
            folderTemplate.Id,
            templates.Id,
            cancellationToken);

        await EnsureHomeFieldValuesAsync(
            home.Id,
            cancellationToken);
    }

    private async Task EnsureTemplatesAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TemplateDefinition> existingTemplates;

        try
        {
            existingTemplates =
                await _templateRepository.GetTemplatesAsync(cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            existingTemplates = [];
        }

        var existingTemplateKeys =
            new HashSet<TemplateKey>(
                existingTemplates.Select(template => template.Key));

        foreach (var template in GetDefaultTemplates())
        {
            if (existingTemplateKeys.Contains(template.Key))
            {
                continue;
            }

            await _templateRepository.CreateTemplateAsync(
                template,
                cancellationToken);

            _logger.LogInformation(
                "Seeded default template '{TemplateKey}'.",
                template.Key);
        }
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

    private static IReadOnlyCollection<TemplateDefinition> GetDefaultTemplates()
    {
        var standardTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("95071327-4AAB-4827-9641-1C45EF6A1D37")),
                "Standard",
                StandardTemplateKey,
                sections:
                [
                    new TemplateSectionDefinition(
                        new Guid("55081A71-C336-41F0-B070-F44B84E0D7C0"),
                        "Content",
                        "content",
                        100,
                        [
                            new FieldDefinition(
                                new FieldId(new Guid("BE9B2863-EB2D-4D2E-8990-884A87AB6A0B")),
                                "Title",
                                "title",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            new FieldDefinition(
                                new FieldId(new Guid("D315D9AF-F921-4385-BD24-8A97BCE1AFA3")),
                                "Navigation Title",
                                "navigationTitle",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            new FieldDefinition(
                                new FieldId(new Guid("B6A8A944-F09A-4779-83EB-1ABEA205F51C")),
                                "Meta Description",
                                "metaDescription",
                                FieldType.MultiLineText,
                                isUnversioned: true)
                        ])
                ]);
        var folderTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("6991D76D-6475-4A2B-B04F-D16E9E4AAE9F")),
                "Folder",
                FolderTemplateKey,
                standardTemplate,
                []);

        var itemTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("562BA716-A878-45E5-9BA7-397F46BA7B1D")),
                "Item",
                ItemTemplateKey,
                standardTemplate,
                [
                    new TemplateSectionDefinition(
                        new Guid("6A1AE5E6-BB4E-4EB9-90BE-FA03C50D9C6D"),
                        "Content",
                        "content",
                        100,
                        [
                            new FieldDefinition(
                                new FieldId(new Guid("EA54795D-4FBE-477B-A2CC-F8DA57485729")),
                                "Body",
                                "body",
                                FieldType.RichText)
                        ])
                ]);

        return [standardTemplate, folderTemplate, itemTemplate];
    }
}
