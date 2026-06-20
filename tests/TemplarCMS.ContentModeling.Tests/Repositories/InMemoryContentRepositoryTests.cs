using TemplarCMS.ContentModeling.Definitions;
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

        Assert.Empty(values);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldPersistValues()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();
        var titleFieldId = Guid.NewGuid();
        var bodyFieldId = Guid.NewGuid();
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
    public async Task SaveFieldValuesAsync_ShouldReplaceExistingStoredSet()
    {
        var repository = new InMemoryContentRepository();

        var item = CreateItem();
        var initialValues = new[]
        {
            CreateValue(item.Id, Guid.NewGuid(), "title", "Home"),
            CreateValue(item.Id, Guid.NewGuid(), "body", "Old Body")
        };

        var replacementValues = new[]
        {
            CreateValue(item.Id, Guid.NewGuid(), "summary", "New Summary")
        };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, initialValues, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, replacementValues, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        var value = Assert.Single(stored);
        Assert.Equal("summary", value.FieldKey);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenItemIdMissing()
    {
        var repository = new InMemoryContentRepository();

        var values =
            new[]
            {
                CreateValue(Guid.NewGuid(), Guid.NewGuid(), "title", "Home")
            };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveFieldValuesAsync(
                Guid.Empty,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenValueDoesNotBelongToRequestedItem()
    {
        var repository = new InMemoryContentRepository();

        var itemId = Guid.NewGuid();
        var values =
            new[]
            {
                CreateValue(Guid.NewGuid(), Guid.NewGuid(), "title", "Home")
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
                CreateValue(item.Id, Guid.NewGuid(), "title", "Home")
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
        Guid? parentId = null,
        string name = "Home",
        string key = "home")
    {
        return new ContentItemDefinition(
            Guid.NewGuid(),
            name,
            key,
            Guid.NewGuid(),
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
}
