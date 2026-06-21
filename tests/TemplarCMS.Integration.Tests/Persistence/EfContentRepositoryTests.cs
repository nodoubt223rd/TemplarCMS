using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Domain.Content;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Content;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class EfContentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TemplarCmsDbContext> _options;

    public EfContentRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options =
            new DbContextOptionsBuilder<TemplarCmsDbContext>()
                .UseSqlite(_connection)
                .Options;

        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveItemAsync_ThenGetItemAsync_ShouldRoundTripItem()
    {
        var repository = CreateRepository();
        var item = CreateItem();

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Equal(item.Id, stored.Id);
        Assert.Equal(item.Name, stored.Name);
        Assert.Equal(item.Key, stored.Key);
        Assert.Equal(item.TemplateId, stored.TemplateId);
        Assert.Equal(item.ParentId, stored.ParentId);
    }

    [Fact]
    public async Task GetChildItemsAsync_ShouldReturnDirectChildrenInKeyOrder()
    {
        var repository = CreateRepository();
        var parent = CreateItem(key: "home");
        var childB = CreateItem(parent.Id, "Child B", "child-b");
        var childA = CreateItem(parent.Id, "Child A", "child-a");
        var grandChild = CreateItem(childA.Id, "Grand Child", "grand-child");

        await repository.SaveItemAsync(parent, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(childB, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(childA, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(grandChild, TestContext.Current.CancellationToken);

        var children =
            await repository.GetChildItemsAsync(
                parent.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { childA.Id, childB.Id },
            children.Select(item => item.Id));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldReplaceExistingStoredSet()
    {
        var repository = CreateRepository();
        var item = CreateItem();

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);

        var initialValues =
            new[]
            {
                CreateValue(item.Id, Guid.NewGuid(), "title", "Home", ContentVersion.Shared),
                CreateValue(item.Id, Guid.NewGuid(), "body", "First")
            };

        var replacementValues =
            new[]
            {
                CreateValue(item.Id, Guid.NewGuid(), "summary", "Second")
            };

        await repository.SaveFieldValuesAsync(item.Id, initialValues, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, replacementValues, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        var value = Assert.Single(stored);
        Assert.Equal("summary", value.FieldKey);
        Assert.Equal("Second", value.Value);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenValueDoesNotBelongToRequestedItem()
    {
        var repository = CreateRepository();
        var item = CreateItem();
        var values =
            new[]
            {
                CreateValue(new ContentItemId(Guid.NewGuid()), Guid.NewGuid(), "title", "Home")
            };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveFieldValuesAsync(
                item.Id,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldDeleteItemAndCascadeFieldValues()
    {
        var repository = CreateRepository();
        var item = CreateItem();
        var values =
            new[]
            {
                CreateValue(item.Id, Guid.NewGuid(), "title", "Home", ContentVersion.Shared)
            };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, values, TestContext.Current.CancellationToken);

        await repository.DeleteItemAsync(item.Id, TestContext.Current.CancellationToken);

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

    public void Dispose()
    {
        _connection.Dispose();
    }

    private TemplarCmsDbContext CreateDbContext()
    {
        return new TemplarCmsDbContext(_options);
    }

    private EfContentRepository CreateRepository()
    {
        return new EfContentRepository(CreateDbContext());
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
        Guid fieldId,
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
}
