using TemplarCMS.Domain.Content;
using TemplarCMS.ContentModeling.Repositories;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Repositories;

public sealed class InMemoryContentRepositoryTests
{
    [Fact]
    public async Task GetItemAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var repository = new InMemoryContentRepository();

        var result =
            await repository.GetItemAsync(
                new ContentItemId(Guid.NewGuid()),
                TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveItemAsync_ShouldPersistItem()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();

        await repository.SaveItemAsync(
            item,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Same(item, stored);
    }

    [Fact]
    public async Task GetItemAsync_ByPath_ShouldReturnStoredRootItem()
    {
        var repository = new InMemoryContentRepository();
        var item = CreateItem(key: "home");

        await repository.SaveItemAsync(
            item,
            TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                new ContentPath("/HOME"),
                TestContext.Current.CancellationToken);

        Assert.Same(item, stored);
    }

    [Fact]
    public async Task GetItemAsync_ByPath_ShouldReturnStoredNestedItem()
    {
        var repository = new InMemoryContentRepository();
        var home = CreateItem(key: "home");
        var articles = CreateItem(home.Id, "Articles", "articles");
        var helloWorld = CreateItem(articles.Id, "Hello World", "hello-world");

        await repository.SaveItemAsync(home, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(articles, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(helloWorld, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                new ContentPath("/home/articles/hello-world"),
                TestContext.Current.CancellationToken);

        Assert.Same(helloWorld, stored);
    }

    [Fact]
    public async Task GetChildItemsAsync_ShouldReturnOnlyDirectChildren_ForParent()
    {
        var repository = new InMemoryContentRepository();

        var root = CreateItem(key: "home");
        var childB = CreateItem(parentId: root.Id, name: "Child B", key: "child-b");
        var childA = CreateItem(parentId: root.Id, name: "Child A", key: "child-a");
        var otherRoot = CreateItem(name: "Other Root", key: "other-root");
        var grandChild = CreateItem(parentId: childA.Id, name: "Grand Child", key: "grand-child");

        await repository.SaveItemAsync(root, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(childB, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(childA, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(otherRoot, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(grandChild, TestContext.Current.CancellationToken);

        var children =
            await repository.GetChildItemsAsync(
                root.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { childA.Id, childB.Id },
            children.Select(item => item.Id));
    }

    [Fact]
    public async Task GetChildItemsAsync_ShouldReturnRootItems_WhenParentIdNull()
    {
        var repository = new InMemoryContentRepository();

        var rootB = CreateItem(name: "Root B", key: "root-b");
        var rootA = CreateItem(name: "Root A", key: "root-a");
        var child = CreateItem(parentId: rootA.Id, name: "Child", key: "child");

        await repository.SaveItemAsync(rootB, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(rootA, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(child, TestContext.Current.CancellationToken);

        var roots =
            await repository.GetChildItemsAsync(
                null,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { rootA.Id, rootB.Id },
            roots.Select(item => item.Id));
    }

    [Fact]
    public async Task GetFieldValuesAsync_ShouldReturnEmptyCollection_WhenItemHasNoValues()
    {
        var repository = new InMemoryContentRepository();

        var values =
            await repository.GetFieldValuesAsync(
                new ContentItemId(Guid.NewGuid()),
                TestContext.Current.CancellationToken);

        Assert.Empty(values);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldPersistValues()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();
        var titleFieldId = new FieldId(Guid.NewGuid());
        var bodyFieldId = new FieldId(Guid.NewGuid());
        var values = new[]
        {
            CreateValue(item.Id, titleFieldId, "title", "Home"),
            CreateValue(item.Id, bodyFieldId, "body", "Welcome")
        };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, values, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { "title", "body" },
            stored.Select(value => value.FieldKey));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldMergeIntoExistingStoredSet()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();
        var titleFieldId = new FieldId(Guid.NewGuid());
        var bodyFieldId = new FieldId(Guid.NewGuid());
        var initialValues = new[]
        {
            CreateValue(item.Id, titleFieldId, "title", "Home"),
            CreateValue(item.Id, bodyFieldId, "body", "Old Body")
        };

        var mergedValues = new[]
        {
            CreateValue(item.Id, bodyFieldId, "body", "New Body"),
            CreateValue(item.Id, new FieldId(Guid.NewGuid()), "summary", "New Summary")
        };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, initialValues, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, mergedValues, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(3, stored.Count);
        Assert.Contains(stored, value => value.FieldKey == "title" && value.Value == "Home");
        Assert.Contains(stored, value => value.FieldKey == "body" && value.Value == "New Body");
        Assert.Contains(stored, value => value.FieldKey == "summary" && value.Value == "New Summary");
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenValueDoesNotBelongToRequestedItem()
    {
        var repository = new InMemoryContentRepository();

        var itemId = new ContentItemId(Guid.NewGuid());
        var values =
            new[]
            {
                CreateValue(new ContentItemId(Guid.NewGuid()), new FieldId(Guid.NewGuid()), "title", "Home")
            };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveFieldValuesAsync(
                itemId,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldRemoveItemAndItsValues()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();
        var values =
            new[]
            {
                CreateValue(item.Id, new FieldId(Guid.NewGuid()), "title", "Home")
            };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, values, TestContext.Current.CancellationToken);

        await repository.DeleteItemAsync(
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

    private static ContentItemDefinition CreateItem(
        ContentItemId? parentId = null,
        string name = "Home",
        string key = "home")
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            name,
            new ContentItemKey(key),
            new TemplateId(Guid.NewGuid()),
            parentId);
    }

    private static ContentFieldValue CreateValue(
        ContentItemId itemId,
        FieldId fieldId,
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
}
