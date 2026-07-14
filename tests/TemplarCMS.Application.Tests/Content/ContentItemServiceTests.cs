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
    public async Task GetItemAsync_ByPath_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var (service, _) = CreateService();

        var result =
            await service.GetItemAsync(
                new ContentPath("/home"),
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
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "title").Id, "title", "Home", ContentVersion.Shared),
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "body").Id, "body", "Welcome"),
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "price").Id, "price", "12.34", ContentVersion.Shared),
                CreateValue(item.Id, template.Fields.Single(field => field.Key == "publish-on").Id, "publish-on", "2026-06-30T13:45:00Z")
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
        Assert.Equal("/home", result.Path.ToString());
        Assert.Equal("Home", result.Fields["title"]?.Value);
        Assert.Equal("Welcome", result.Fields["body"]?.Value);

        var title = Assert.IsType<StringTypedFieldValue>(result.ConvertedFields["title"]);
        var body = Assert.IsType<StringTypedFieldValue>(result.ConvertedFields["body"]);
        var price = Assert.IsType<DecimalTypedFieldValue>(result.ConvertedFields["price"]);
        var publishOn = Assert.IsType<DateTimeTypedFieldValue>(result.ConvertedFields["publish-on"]);

        Assert.Equal("Home", title.Value);
        Assert.Equal("Welcome", body.Value);
        Assert.Equal(12.34m, price.Value);
        Assert.Equal(
            new DateTime(2026, 6, 30, 13, 45, 0, DateTimeKind.Utc),
            publishOn.Value);
    }

    [Fact]
    public async Task GetItemAsync_ByPath_ShouldResolveStoredItem()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);

        var (service, _) = CreateService(
            new[] { template },
            new[] { item },
            Array.Empty<ContentFieldValue>());

        var result =
            await service.GetItemAsync(
                new ContentPath("/HOME"),
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Item.Id);
        Assert.Equal("/home", result.Path.ToString());
    }

    [Fact]
    public async Task GetItemAsync_ByPath_ShouldResolveNestedStoredItem()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var articles = CreateItem(template.Id, root.Id, "Articles", "articles");
        var item = CreateItem(template.Id, articles.Id, "Hello World", "hello-world");

        var (service, _) = CreateService(
            new[] { template },
            new[] { root, articles, item },
            Array.Empty<ContentFieldValue>());

        var result =
            await service.GetItemAsync(
                new ContentPath("/home/articles/hello-world"),
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Item.Id);
        Assert.Equal("/home/articles/hello-world", result.Path.ToString());
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
    public async Task GetItemAsync_ShouldThrow_WhenStoredValueCannotBeConverted()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var values =
            new[]
            {
                CreateValue(
                    item.Id,
                    template.Fields.Single(field => field.Key == "is-visible").Id,
                    "is-visible",
                    "yes",
                    ContentVersion.Shared)
            };

        var (service, _) = CreateService(
            new[] { template },
            new[] { item },
            values);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetItemAsync(
                    item.Id,
                    CreateContext(),
                    TestContext.Current.CancellationToken));

        Assert.Contains("is-visible", exception.Message);
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
            result.Select(item => item.Item.Id).ToArray());

        Assert.Equal(
            new[] { "A", "B" },
            result.Select(item => item.Fields["title"]?.Value).ToArray());

        Assert.Equal(
            new[] { "/home/child-a", "/home/child-b" },
            result.Select(item => item.Path.ToString()).ToArray());

        Assert.All(
            result,
            item => Assert.IsType<StringTypedFieldValue>(item.ConvertedFields["title"]));
    }

    [Fact]
    public async Task GetItemAsync_ShouldComputeNestedPathFromParentChain()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var articles = CreateItem(template.Id, root.Id, "Articles", "articles");
        var item = CreateItem(template.Id, articles.Id, "Hello World", "hello-world");

        var (service, _) = CreateService(
            new[] { template },
            new[] { root, articles, item },
            Array.Empty<ContentFieldValue>());

        var result =
            await service.GetItemAsync(
                item.Id,
                CreateContext(),
                TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("/home/articles/hello-world", result.Path.ToString());
    }

    [Fact]
    public async Task SaveItemAsync_ShouldPersistItem_WhenTemplateExists()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
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
        var item = CreateItem(template.Id, new ContentItemId(Guid.NewGuid()));
        var (service, _) =
            CreateService(
                new[] { template });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveItemAsync(
                item,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SaveItemAsync_ShouldThrow_WhenItemIsItsOwnParent()
    {
        var template = CreateTemplate("article-page");
        var itemId = new ContentItemId(Guid.NewGuid());

        var exception =
            Assert.Throws<ArgumentException>(() =>
                new ContentItemDefinition(
                    itemId,
                    "Home",
                    new ContentItemKey("home"),
                    template.Id,
                    itemId));

        Assert.Contains("own parent", exception.Message);
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenSiblingKeyAlreadyExists()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(template.Id, name: "Parent", key: "parent");
        var existingChild = CreateItem(template.Id, parent.Id, "Child A", "home");
        var newChild = CreateItem(template.Id, parent.Id, "Child B", "HOME");

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
        var parent = CreateItem(template.Id, name: "Parent", key: "parent");
        var existingChild = CreateItem(template.Id, parent.Id, "Child A", "home");
        var updatedChild =
            new ContentItemDefinition(
                existingChild.Id,
                "Child A Updated",
                new ContentItemKey("HOME"),
                template.Id,
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
    public async Task SaveItemAsync_ShouldThrow_WhenExistingItemChangesKey()
    {
        var template = CreateTemplate("article-page");
        var existingItem = CreateItem(template.Id, name: "Home", key: "home");
        var renamedItem =
            new ContentItemDefinition(
                existingItem.Id,
                existingItem.Name,
                new ContentItemKey("landing-page"),
                existingItem.TemplateId,
                existingItem.ParentId);

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { existingItem });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SaveItemAsync(
                    renamedItem,
                    TestContext.Current.CancellationToken));

        Assert.Contains("rename semantics", exception.Message);
    }

    [Fact]
    public async Task SaveItemAsync_ShouldThrow_WhenExistingItemChangesParent()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var originalParent = CreateItem(template.Id, name: "Articles", key: "articles");
        var newParent = CreateItem(template.Id, name: "News", key: "news");
        var existingItem = CreateItem(template.Id, originalParent.Id, "Hello World", "hello-world");
        var movedItem =
            new ContentItemDefinition(
                existingItem.Id,
                existingItem.Name,
                existingItem.Key,
                existingItem.TemplateId,
                newParent.Id);

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { root, originalParent, newParent, existingItem });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SaveItemAsync(
                    movedItem,
                    TestContext.Current.CancellationToken));

        Assert.Contains("move semantics", exception.Message);
    }

    [Fact]
    public async Task RenameItemAsync_ShouldPersistRenamedItem()
    {
        var template = CreateTemplate("article-page");
        var existingItem = CreateItem(template.Id, name: "Home", key: "home");

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { existingItem });

        await service.RenameItemAsync(
            existingItem.Id,
            "Landing Page",
            new ContentItemKey("landing page"),
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                existingItem.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Equal("Landing Page", stored.Name);
        Assert.Equal(new ContentItemKey("landing-page"), stored.Key);
        Assert.Equal(existingItem.ParentId, stored.ParentId);
    }

    [Fact]
    public async Task RenameItemAsync_ShouldThrow_WhenSiblingKeyAlreadyExists()
    {
        var template = CreateTemplate("article-page");
        var parent = CreateItem(template.Id, name: "Parent", key: "parent");
        var existingItem = CreateItem(template.Id, parent.Id, "Child A", "child-a");
        var sibling = CreateItem(template.Id, parent.Id, "Child B", "child-b");

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { parent, existingItem, sibling });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RenameItemAsync(
                    existingItem.Id,
                    "Child A Updated",
                    new ContentItemKey("child-b"),
                    TestContext.Current.CancellationToken));

        Assert.Contains("already exists under parent", exception.Message);
    }

    [Fact]
    public async Task RenameItemAsync_ShouldThrow_WhenItemMissing()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RenameItemAsync(
                new ContentItemId(Guid.NewGuid()),
                "Home",
                new ContentItemKey("home"),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveItemAsync_ShouldPersistMovedItem()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var oldParent = CreateItem(template.Id, root.Id, "Articles", "articles");
        var newParent = CreateItem(template.Id, root.Id, "News", "news");
        var existingItem = CreateItem(template.Id, oldParent.Id, "Hello World", "hello-world");

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { root, oldParent, newParent, existingItem });

        await service.MoveItemAsync(
            existingItem.Id,
            newParent.Id,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                existingItem.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Equal(newParent.Id, stored.ParentId);
        Assert.Equal(existingItem.Key, stored.Key);
        Assert.Equal(existingItem.Name, stored.Name);
    }

    [Fact]
    public async Task MoveItemAsync_ShouldAllowMovingItemToRoot()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var existingItem = CreateItem(template.Id, root.Id, "Articles", "articles");

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { root, existingItem });

        await service.MoveItemAsync(
            existingItem.Id,
            null,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                existingItem.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Null(stored.ParentId);
    }

    [Fact]
    public async Task MoveItemAsync_ShouldThrow_WhenNewParentMissing()
    {
        var template = CreateTemplate("article-page");
        var existingItem = CreateItem(template.Id, name: "Home", key: "home");

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { existingItem });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MoveItemAsync(
                existingItem.Id,
                new ContentItemId(Guid.NewGuid()),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveItemAsync_ShouldThrow_WhenMoveCreatesCycle()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var parent = CreateItem(template.Id, root.Id, "Articles", "articles");
        var child = CreateItem(template.Id, parent.Id, "Hello World", "hello-world");

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { root, parent, child });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MoveItemAsync(
                    parent.Id,
                    child.Id,
                    TestContext.Current.CancellationToken));

        Assert.Contains("descendants", exception.Message);
    }

    [Fact]
    public async Task MoveItemAsync_ShouldThrow_WhenSiblingKeyAlreadyExistsUnderNewParent()
    {
        var template = CreateTemplate("article-page");
        var root = CreateItem(template.Id, name: "Home", key: "home");
        var oldParent = CreateItem(template.Id, root.Id, "Articles", "articles");
        var newParent = CreateItem(template.Id, root.Id, "News", "news");
        var existingItem = CreateItem(template.Id, oldParent.Id, "Hello World", "hello-world");
        var conflictingSibling = CreateItem(template.Id, newParent.Id, "Hello Again", "hello-world");

        var (service, _) =
            CreateService(
                new[] { template },
                new[] { root, oldParent, newParent, existingItem, conflictingSibling });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.MoveItemAsync(
                    existingItem.Id,
                    newParent.Id,
                    TestContext.Current.CancellationToken));

        Assert.Contains("already exists under parent", exception.Message);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldPersistValues_WhenItemAndTemplateFieldExist()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
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
    public async Task SaveFieldValuesAsync_ShouldPersistValues_WhenTypedValueIsValid()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var visibleField =
            template.Fields.Single(field => field.Key == "is-visible");
        var priceField =
            template.Fields.Single(field => field.Key == "price");
        var publishOnField =
            template.Fields.Single(field => field.Key == "publish-on");

        var values =
            new[]
            {
                CreateValue(item.Id, visibleField.Id, "is-visible", "true", ContentVersion.Shared),
                CreateValue(item.Id, priceField.Id, "price", "12.34", ContentVersion.Shared),
                CreateValue(item.Id, publishOnField.Id, "publish-on", "2026-06-30T13:45:00Z")
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

        Assert.Equal(3, stored.Count);
        Assert.Contains(stored, value => value.FieldKey == "is-visible" && value.Value == "true");
        Assert.Contains(stored, value => value.FieldKey == "price" && value.Value == "12.34");
        Assert.Contains(stored, value => value.FieldKey == "publish-on" && value.Value == "2026-06-30T13:45:00Z");
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldMergeWithExistingStoredValues()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var titleField =
            template.Fields.Single(field => field.Key == "title");
        var bodyField =
            template.Fields.Single(field => field.Key == "body");

        var existingValues =
            new[]
            {
                CreateValue(item.Id, titleField.Id, "title", "Home"),
                CreateValue(item.Id, bodyField.Id, "body", "Old Body")
            };

        var newValues =
            new[]
            {
                CreateValue(item.Id, bodyField.Id, "body", "New Body")
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item },
                existingValues);

        await service.SaveFieldValuesAsync(
            item.Id,
            newValues,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, value => value.FieldKey == "title" && value.Value == "Home");
        Assert.Contains(stored, value => value.FieldKey == "body" && value.Value == "New Body");
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
        var item = CreateItem(template.Id);

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
        var item = CreateItem(template.Id);
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
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenTypedValueIsInvalid()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var visibleField =
            template.Fields.Single(field => field.Key == "is-visible");

        var values =
            new[]
            {
                CreateValue(item.Id, visibleField.Id, "is-visible", "yes", ContentVersion.Shared)
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SaveFieldValuesAsync(
                    item.Id,
                    values,
                    TestContext.Current.CancellationToken));

        Assert.Contains("is-visible", exception.Message);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenDecimalValueIsInvalid()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var priceField =
            template.Fields.Single(field => field.Key == "price");

        var values =
            new[]
            {
                CreateValue(item.Id, priceField.Id, "price", "twelve", ContentVersion.Shared)
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SaveFieldValuesAsync(
                    item.Id,
                    values,
                    TestContext.Current.CancellationToken));

        Assert.Contains("price", exception.Message);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenDateTimeValueIsInvalid()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var publishOnField =
            template.Fields.Single(field => field.Key == "publish-on");

        var values =
            new[]
            {
                CreateValue(item.Id, publishOnField.Id, "publish-on", "tomorrow afternoon")
            };

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SaveFieldValuesAsync(
                    item.Id,
                    values,
                    TestContext.Current.CancellationToken));

        Assert.Contains("publish-on", exception.Message);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ByFieldKey_ShouldPersistValues_WithStorageConventions()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var values =
            new Dictionary<string, string?>
            {
                ["title"] = "Home",
                ["body"] = "<p>Welcome</p>",
                ["price"] = "12.34"
            };
        var context =
            new FieldValueResolutionContext(
                new ContentLanguage("fr-ca"),
                new ContentVersion(7));

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        await service.SaveFieldValuesAsync(
            item.Id,
            context,
            values,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(3, stored.Count);
        Assert.Contains(
            stored,
            value =>
                value.FieldKey == "title" &&
                value.Language == new ContentLanguage("fr-ca") &&
                value.Version == ContentVersion.Shared &&
                value.Value == "Home");
        Assert.Contains(
            stored,
            value =>
                value.FieldKey == "body" &&
                value.Language == new ContentLanguage("fr-ca") &&
                value.Version == new ContentVersion(7) &&
                value.Value == "<p>Welcome</p>");
        Assert.Contains(
            stored,
            value =>
                value.FieldKey == "price" &&
                value.Language == new ContentLanguage("fr-ca") &&
                value.Version == ContentVersion.Shared &&
                value.Value == "12.34");
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ByFieldKey_ShouldUseSharedStorageMarker_ForSharedFields()
    {
        var sharedField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Site Name",
                "site-name",
                FieldType.SingleLineText,
                isShared: true);
        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Metadata",
                "metadata",
                100,
                new[] { sharedField });
        var template =
            new EffectiveTemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Shared Page",
                new TemplateKey("shared-page"),
                new[] { section });
        var item = CreateItem(template.Id);
        var context =
            new FieldValueResolutionContext(
                new ContentLanguage("fr-ca"),
                new ContentVersion(5));

        var (service, repository) =
            CreateService(
                new[] { template },
                new[] { item });

        await service.SaveFieldValuesAsync(
            item.Id,
            context,
            new Dictionary<string, string?>
            {
                ["site-name"] = "Templar CMS"
            },
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        var value = Assert.Single(stored);
        Assert.Equal("site-name", value.FieldKey);
        Assert.Equal(new ContentLanguage("shared"), value.Language);
        Assert.Equal(ContentVersion.Shared, value.Version);
        Assert.Equal("Templar CMS", value.Value);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ByFieldKey_ShouldThrow_WhenFieldKeyMissingFromTemplate()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
        var (service, _) =
            CreateService(
                new[] { template },
                new[] { item });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFieldValuesAsync(
                item.Id,
                CreateContext(),
                new Dictionary<string, string?>
                {
                    ["headline"] = "Saved"
                },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldDeleteItem_WhenNoDirectChildrenExist()
    {
        var template = CreateTemplate("article-page");
        var item = CreateItem(template.Id);
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
        var parent = CreateItem(template.Id);
        var child = CreateItem(template.Id, parent.Id, "Child", "child");

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
                    new ExactMatchFieldValueResolutionPolicy()),
                new TypedFieldValueConverter());

        return (
            new ContentItemService(
                repository,
                catalog,
                resolver,
                new ContentPathResolver(repository),
                new TypedFieldValueConverter()),
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

        var visibleField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Is Visible",
                "is-visible",
                FieldType.Checkbox,
                isUnversioned: true);

        var priceField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Price",
                "price",
                FieldType.Decimal,
                isUnversioned: true);

        var publishOnField =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Publish On",
                "publish-on",
                FieldType.DateTime);

        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Content",
                "content",
                100,
                new[] { titleField, bodyField, visibleField, priceField, publishOnField });

        return new EffectiveTemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            "Article Page",
            new TemplateKey(key),
            new[] { section });
    }

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly Dictionary<TemplateId, EffectiveTemplateDefinition> _templates;

        public FakeContentModelCatalog(
            IReadOnlyCollection<EffectiveTemplateDefinition> templates)
        {
            _templates = templates.ToDictionary(template => template.Id);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TemplateDefinition?>(null);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TemplateDefinition?>(null);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _templates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var template =
                _templates.Values.FirstOrDefault(
                    value => value.Key == key);

            return Task.FromResult(template);
        }

        public Task<IReadOnlyCollection<EffectiveTemplateDefinition>> GetEffectiveTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<EffectiveTemplateDefinition>>(_templates.Values.ToArray());
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
