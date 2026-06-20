using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using Xunit;

namespace TemplarCMS.Application.Tests.Content;

public sealed class ContentItemServiceTests
{
    [Fact]
    public async Task GetItemAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var service = CreateService();

        var result =
            await service.GetItemAsync(
                Guid.NewGuid(),
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemAsync_ShouldResolveStoredItem()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var values =
            new[]
            {
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "title").Id, "title", "Home"),
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "body").Id, "body", "Welcome")
            };

        var service = CreateService(
            new[] { template },
            new[] { item },
            values);

        var result =
            await service.GetItemAsync(
                item.Id,
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Item.Id);
        Assert.Equal("Home", result.Fields["title"]?.Value);
        Assert.Equal("Welcome", result.Fields["body"]?.Value);
    }

    [Fact]
    public async Task GetItemAsync_ShouldThrow_WhenEffectiveTemplateMissing()
    {
        var item = CreateItem(Guid.NewGuid());

        var service = CreateService(
            Array.Empty<EffectiveTemplateDefinition>(),
            new[] { item },
            Array.Empty<ContentFieldValue>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetItemAsync(
                item.Id,
                CreateContext(),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetChildItemsAsync_ShouldResolveDirectChildren()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(template.Id);
        var childB = CreateItem(template.Id, parent.Id, "Child B", "child-b");
        var childA = CreateItem(template.Id, parent.Id, "Child A", "child-a");
        var grandChild = CreateItem(template.Id, childA.Id, "Grand Child", "grand-child");

        var titleFieldId =
            template.Fields.Single(field => field.Key == "title").Id;

        var values =
            new[]
            {
                CreateValue(childA.Id, titleFieldId, "title", "A"),
                CreateValue(childB.Id, titleFieldId, "title", "B"),
                CreateValue(grandChild.Id, titleFieldId, "title", "C")
            };

        var service = CreateService(
            new[] { template },
            new[] { parent, childB, childA, grandChild },
            values);

        var result =
            await service.GetChildItemsAsync(
                parent.Id,
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { childA.Id, childB.Id },
            result.Select(item => item.Item.Id));

        Assert.Equal(
            new[] { "A", "B" },
            result.Select(item => item.Fields["title"]?.Value));
    }

    private static ContentItemService CreateService(
        IReadOnlyCollection<EffectiveTemplateDefinition>? templates = null,
        IReadOnlyCollection<ContentItemDefinition>? items = null,
        IReadOnlyCollection<ContentFieldValue>? values = null)
    {
        var repository = new InMemoryContentRepository();

        if (items != null)
        {
            foreach (var item in items)
            {
                repository.SaveItemAsync(item, TestContext.Current.CancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        if (items != null && values != null)
        {
            foreach (var group in values.GroupBy(value => value.ItemId))
            {
                repository.SaveFieldValuesAsync(group.Key, group.ToArray(), TestContext.Current.CancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        var catalog =
            new FakeContentModelCatalog(
                templates ?? Array.Empty<EffectiveTemplateDefinition>());

        var resolver =
            new ContentItemResolver(
                new FieldValueResolver(
                    new ExactMatchFieldValueResolutionPolicy()));

        return new ContentItemService(
            repository,
            catalog,
            resolver);
    }

    private static FieldValueResolutionContext CreateContext()
    {
        return new(
            new ContentLanguage("en"),
            ContentVersion.First);
    }

    private static ContentItemDefinition CreateItem(
        Guid templateId,
        Guid? parentId = null,
        string name = "Home",
        string key = "home")
    {
        return new ContentItemDefinition(
            Guid.NewGuid(),
            name,
            key,
            templateId,
            parentId);
    }

    private static ContentFieldValue CreateValue(
        Guid itemId,
        Guid fieldId,
        string fieldKey,
        string? value)
    {
        return new ContentFieldValue(
            itemId,
            fieldId,
            fieldKey,
            new ContentLanguage("en"),
            ContentVersion.First,
            value);
    }

    private static EffectiveTemplateDefinition CreateTemplate(string key)
    {
        var titleField =
            new FieldDefinition(
                Guid.NewGuid(),
                "Title",
                "title",
                FieldType.SingleLineText,
                isUnversioned: true);

        var bodyField =
            new FieldDefinition(
                Guid.NewGuid(),
                "Body",
                "body",
                FieldType.RichText);

        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Content",
                "content",
                100,
                new[] { titleField, bodyField });

        return new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            key,
            new[] { section });
    }

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly Dictionary<Guid, EffectiveTemplateDefinition> _templates;

        public FakeContentModelCatalog(
            IReadOnlyCollection<EffectiveTemplateDefinition> templates)
        {
            _templates = templates.ToDictionary(template => template.Id);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TemplateDefinition?>(null);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TemplateDefinition?>(null);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _templates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var template =
                _templates.Values.FirstOrDefault(
                    value => string.Equals(
                        value.Key,
                        key,
                        StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(template);
        }

        public Task InvalidateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
