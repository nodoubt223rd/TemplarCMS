using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Application.Tests.Content;

public sealed class ContentItemServiceTests
{
    [Fact]
    public async Task GetItemAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var (service, _) = CreateService();

        var result =
            await service.GetItemAsync(
                new ContentItemId(Guid.NewGuid()),
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemAsync_ShouldResolveStoredItem()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));
        var values =
            new[]
            {
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "title").Id, "title", "Home", ContentVersion.Shared),
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "body").Id, "body", "Welcome")
            };

        var (service, _) = CreateService(
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
        var item = CreateItem(new TemplateId(Guid.NewGuid()));

        var (service, _) = CreateService(
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
        var parent = CreateItem(new TemplateId(template.Id));
        var childB = CreateItem(new TemplateId(template.Id), parent.Id, "Child B", "child-b");
        var childA = CreateItem(new TemplateId(template.Id), parent.Id, "Child A", "child-a");
        var grandChild = CreateItem(new TemplateId(template.Id), childA.Id, "Grand Child", "grand-child");

        var titleFieldId =
            template.Fields.Single(field => field.Key == "title").Id;

        var values =
            new[]
            {
                CreateValue(childA.Id, titleFieldId, "title", "A", ContentVersion.Shared),
                CreateValue(childB.Id, titleFieldId, "title", "B", ContentVersion.Shared),
                CreateValue(grandChild.Id, titleFieldId, "title", "C", ContentVersion.Shared)
            };

        var (service, _) = CreateService(
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

    [Fact]
    public async Task SaveItemAsync_ShouldPersistItem_WhenTemplateExists()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));
        var (service, repository) =
            CreateService(
                new[] { template });

        await service.SaveItemAsync(
            item,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Same(item, stored);
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenTemplateMissing()
    {
        var item = CreateItem(new TemplateId(Guid.NewGuid()));
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveItemAsync(
                item,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenParentMissing()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id), new ContentItemId(Guid.NewGuid()));
        var (service, _) =
            CreateService(
                new[] { template });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveItemAsync(
                item,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenItemIsItsOwnParent()
    {
        var template = CreateTemplate("article-page");
        var itemId = new ContentItemId(Guid.NewGuid());
        var item =
            new ContentItemDefinition(
                itemId,
                "Home",
                new ContentItemKey("home"),
                new TemplateId(template.Id),
                itemId);

        var (service, _) =
            CreateService(
                new[] { template });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveItemAsync(
                item,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenSiblingKeyAlreadyExists()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(new TemplateId(template.Id), name: "Parent", key: "parent");
        var existingChild = CreateItem(new TemplateId(template.Id), parent.Id, "Child A", "home");
        var newChild = CreateItem(new TemplateId(template.Id), parent.Id, "Child B", "HOME");

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { parent, existingChild });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveItemAsync(
                newChild,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveItemAsync_ShouldAllowExistingItemToKeepItsSiblingKey()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(new TemplateId(template.Id), name: "Parent", key: "parent");
        var existingChild = CreateItem(new TemplateId(template.Id), parent.Id, "Child A", "home");
        var updatedChild =
            new ContentItemDefinition(
                existingChild.Id,
                "Child A Updated",
                new ContentItemKey("HOME"),
                new TemplateId(template.Id),
                parent.Id);

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { parent, existingChild });

        await service.SaveItemAsync(
            updatedChild,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                existingChild.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Equal("Child A Updated", stored.Name);
        Assert.Equal(new ContentItemKey("home"), stored.Key);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldPersistValues_WhenItemAndTemplateFieldExist()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));
        var titleField =
            template.Fields.Single(field => field.Key == "title");

        var values =
            new[]
            {
                CreateValue(item.Id, titleField.Id, "title", "Saved")
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        await service.SaveFieldValuesAsync(
            item.Id,
            values,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        var value = Assert.Single(stored);
        Assert.Equal("Saved", value.Value);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenItemMissing()
    {
        var template = CreateTemplate("article-page");
        var field =
            template.Fields.Single(item => item.Key == "title");

        var values =
            new[]
            {
                CreateValue(new ContentItemId(Guid.NewGuid()), field.Id, "title", "Saved")
            };

        var (service, _) =
            CreateService(
                new[] { template });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFieldValuesAsync(
                new ContentItemId(Guid.NewGuid()),
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenFieldIdMissingFromTemplate()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));

        var values =
            new[]
            {
                CreateValue(item.Id, new FieldId(Guid.NewGuid()), "title", "Saved")
            };

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { item });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFieldValuesAsync(
                item.Id,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenFieldKeyDoesNotMatchTemplateField()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));
        var titleField =
            template.Fields.Single(field => field.Key == "title");

        var values =
            new[]
            {
                CreateValue(item.Id, titleField.Id, "headline", "Saved")
            };

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { item });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFieldValuesAsync(
                item.Id,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldDeleteItem_WhenNoDirectChildrenExist()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(new TemplateId(template.Id));
        var titleField =
            template.Fields.Single(field => field.Key == "title");

        var values =
            new[]
            {
                CreateValue(item.Id, titleField.Id, "title", "Saved")
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item },
                values);

        await service.DeleteItemAsync(
            item.Id,
            TestContext.Current.CancellationToken);

        var storedItem =
            await repository.GetItemAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        var storedValues =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Null(storedItem);
        Assert.Empty(storedValues);
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldThrow_WhenDirectChildrenExist()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(new TemplateId(template.Id));
        var child = CreateItem(new TemplateId(template.Id), parent.Id, "Child", "child");

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { parent, child });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteItemAsync(
                parent.Id,
                TestContext.Current.CancellationToken));

        var storedParent =
            await repository.GetItemAsync(
                parent.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(storedParent);
    }

    private static (ContentItemService Service, InMemoryContentRepository Repository) CreateService(
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

        return (
            new ContentItemService(
                repository,
                catalog,
                resolver),
            repository);
    }

    private static FieldValueResolutionContext CreateContext()
    {
        return new(
            new ContentLanguage("en"),
            ContentVersion.First);
    }

    private static ContentItemDefinition CreateItem(
        TemplateId templateId,
        ContentItemId? parentId = null,
        string name = "Home",
        string key = "home")
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            name,
            new ContentItemKey(key),
            templateId,
            parentId);
    }

    private static ContentFieldValue CreateValue(
        ContentItemId itemId,
        FieldId fieldId,
        string fieldKey,
        string? value,
        ContentVersion? version = null)
    {
        return new ContentFieldValue(
            itemId,
            fieldId,
            fieldKey,
            new ContentLanguage("en"),
            version ?? ContentVersion.First,
            value);
    }

    private static EffectiveTemplateDefinition CreateTemplate(string key)
    {
        var titleField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Title",
                "title",
                FieldType.SingleLineText,
                isUnversioned: true);

        var bodyField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
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
