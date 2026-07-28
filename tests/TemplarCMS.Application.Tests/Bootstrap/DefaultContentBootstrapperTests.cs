using Microsoft.Extensions.Logging.Abstractions;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Application.Bootstrap;
using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Builders;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Resolvers;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Application.Tests.Bootstrap;

public sealed class DefaultContentBootstrapperTests
{
    [Fact]
    public async Task EnsureInitializedAsync_ShouldSeedDefaultTemplatesAndContentTree()
    {
        var (bootstrapper, templateRepository, contentRepository, catalog) = CreateBootstrapper();

        await bootstrapper.EnsureInitializedAsync(TestContext.Current.CancellationToken);

        var templates =
            await templateRepository.GetTemplatesAsync(TestContext.Current.CancellationToken);
        var templateKeys =
            templates.Select(template => template.Key.ToString()).OrderBy(key => key, StringComparer.Ordinal).ToArray();

        Assert.Equal(
            BuiltInTemplateKeys.All.Select(key => key.ToString()).OrderBy(key => key, StringComparer.Ordinal).ToArray(),
            templateKeys);

        var standardTemplate = Assert.Single(templates, template => template.Key == BuiltInTemplateKeys.Standard);
        var folderTemplate = Assert.Single(templates, template => template.Key == BuiltInTemplateKeys.Folder);
        var itemTemplate = Assert.Single(templates, template => template.Key == BuiltInTemplateKeys.Item);

        Assert.Null(standardTemplate.BaseTemplate);
        Assert.NotNull(folderTemplate.BaseTemplate);
        Assert.NotNull(itemTemplate.BaseTemplate);
        Assert.Equal(standardTemplate.Key, folderTemplate.BaseTemplate!.Key);
        Assert.Equal(standardTemplate.Key, itemTemplate.BaseTemplate!.Key);
        Assert.Contains(standardTemplate.Sections.SelectMany(section => section.Fields), field => field.Key == "title");
        Assert.Contains(standardTemplate.Sections.SelectMany(section => section.Fields), field => field.Key == "navigationTitle");
        Assert.Contains(standardTemplate.Sections.SelectMany(section => section.Fields), field => field.Key == "metaDescription");
        Assert.Contains(itemTemplate.Sections.SelectMany(section => section.Fields), field => field.Key == "body");

        var templar =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar"),
                TestContext.Current.CancellationToken);
        var home =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/content/home"),
                TestContext.Current.CancellationToken);
        var settings =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/system/settings"),
                TestContext.Current.CancellationToken);
        var images =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/media/images"),
                TestContext.Current.CancellationToken);
        var files =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/media/files"),
                TestContext.Current.CancellationToken);
        var standardItem =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/templates/standard"),
                TestContext.Current.CancellationToken);

        Assert.NotNull(templar);
        Assert.NotNull(home);
        Assert.NotNull(settings);
        Assert.NotNull(images);
        Assert.NotNull(files);
        Assert.NotNull(standardItem);
        Assert.Equal(SystemSeedContentIds.TemplarRoot, templar.Id);
        Assert.Equal(SystemSeedContentIds.Home, home.Id);
        Assert.Equal(SystemSeedContentIds.Settings, settings.Id);
        Assert.Equal(SystemSeedContentIds.Images, images.Id);
        Assert.Equal(SystemSeedContentIds.Files, files.Id);
        Assert.Equal(SystemSeedContentIds.StandardTemplateItem, standardItem.Id);
        Assert.Equal(folderTemplate.Id, templar.TemplateId);
        Assert.Equal(itemTemplate.Id, home.TemplateId);

        var homeValues =
            await contentRepository.GetFieldValuesAsync(
                home.Id,
                TestContext.Current.CancellationToken);

        Assert.Contains(homeValues, value => value.FieldKey == "title" && value.Value == "Home");
        Assert.Contains(homeValues, value => value.FieldKey == "navigationTitle" && value.Value == "Home");
        Assert.Contains(homeValues, value => value.FieldKey == "metaDescription" && value.Value == "Starter home item for Templar CMS.");
        Assert.Contains(homeValues, value => value.FieldKey == "body" && value.Value == "<p>Welcome to Templar CMS.</p>");

        var effectiveItemTemplate =
            await catalog.GetEffectiveTemplateAsync(
                BuiltInTemplateKeys.Item,
                TestContext.Current.CancellationToken);

        Assert.NotNull(effectiveItemTemplate);
        Assert.Contains(effectiveItemTemplate.Fields, field => field.Key == "title");
        Assert.Contains(effectiveItemTemplate.Fields, field => field.Key == "body");
    }

    [Fact]
    public async Task EnsureInitializedAsync_ShouldBeIdempotent()
    {
        var (bootstrapper, templateRepository, contentRepository, _) = CreateBootstrapper();

        await bootstrapper.EnsureInitializedAsync(TestContext.Current.CancellationToken);
        await bootstrapper.EnsureInitializedAsync(TestContext.Current.CancellationToken);

        var templates =
            await templateRepository.GetTemplatesAsync(TestContext.Current.CancellationToken);
        var rootItems =
            await contentRepository.GetChildItemsAsync(
                parentId: null,
                TestContext.Current.CancellationToken);
        var templar =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar"),
                TestContext.Current.CancellationToken);
        var templarChildren =
            await contentRepository.GetChildItemsAsync(
                templar!.Id,
                TestContext.Current.CancellationToken);
        var home =
            await contentRepository.GetItemAsync(
                new ContentPath("/templar/content/home"),
                TestContext.Current.CancellationToken);
        var homeValues =
            await contentRepository.GetFieldValuesAsync(
                home!.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(3, templates.Count);
        Assert.Single(rootItems);
        Assert.Equal(
            ["content", "media", "system", "templates"],
            templarChildren.Select(item => item.Key.ToString()).ToArray());
        Assert.Equal(4, homeValues.Count);
    }

    private static (
        DefaultContentBootstrapper Bootstrapper,
        ITemplateRepository TemplateRepository,
        InMemoryContentRepository ContentRepository,
        IContentModelCatalog Catalog) CreateBootstrapper()
    {
        var templateRepository = new InMemoryTemplateRepository();
        var builtInTemplateRepository =
            new BuiltInTemplateRepository(
                templateRepository,
                new BuiltInTemplateProvider());
        var contentRepository = new InMemoryContentRepository();
        var catalog =
            new ContentModelCatalog(
                builtInTemplateRepository,
                new TemplateValidator(),
                new EffectiveTemplateBuilder(
                    new TemplateInheritanceResolver()),
                new EffectiveTemplateValidator(),
                NullLogger<ContentModelCatalog>.Instance);
        var contentItemService =
            new ContentItemService(
                contentRepository,
                catalog,
                new ContentItemResolver(
                    new FieldValueResolver(
                        new ExactMatchFieldValueResolutionPolicy()),
                    new TypedFieldValueConverter()),
                new ContentPathResolver(contentRepository),
                new TypedFieldValueConverter());

        return (
            new DefaultContentBootstrapper(
                catalog,
                contentRepository,
                contentItemService,
                NullLogger<DefaultContentBootstrapper>.Instance),
            builtInTemplateRepository,
            contentRepository,
            catalog);
    }

    private sealed class InMemoryTemplateRepository : ITemplateRepository
    {
        private readonly Dictionary<TemplateKey, TemplateDefinition> _templates =
            new();

        public Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyCollection<TemplateDefinition> templates =
                _templates.Values
                    .OrderBy(template => template.Key.ToString(), StringComparer.Ordinal)
                    .ToArray();

            return Task.FromResult(templates);
        }

        public Task CreateTemplateAsync(
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _templates.Add(template.Key, template);
            return Task.CompletedTask;
        }

        public Task UpdateTemplateAsync(
            TemplateKey existingKey,
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
