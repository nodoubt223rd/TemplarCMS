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
    public async Task GetItemAsync_ByPath_ShouldReturnStoredNestedItem()
    {
        var repository = CreateRepository();
        var home = CreateItem(key: "home");
        var articles = CreateItem(home.Id, "Articles", "articles");
        var helloWorld = CreateItem(articles.Id, "Hello World", "hello-world");

        await repository.SaveItemAsync(home, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(articles, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(helloWorld, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetItemAsync(
                new ContentPath("/HOME/ARTICLES/HELLO-WORLD"),
                TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.Equal(helloWorld.Id, stored.Id);
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
    public async Task GetItemsByTemplateAsync_ShouldReturnItemsAssignedToTemplate()
    {
        var repository = CreateRepository();
        var templateId = new TemplateId(Guid.NewGuid());
        var matchingA = CreateItem(templateId: templateId, key: "home");
        var matchingB = CreateItem(templateId: templateId, key: "articles");
        var other = CreateItem(key: "other");

        await repository.SaveItemAsync(matchingA, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(matchingB, TestContext.Current.CancellationToken);
        await repository.SaveItemAsync(other, TestContext.Current.CancellationToken);

        var items =
            await repository.GetItemsByTemplateAsync(
                templateId,
                TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { matchingB.Id, matchingA.Id },
            items.Select(item => item.Id));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldMergeIntoExistingStoredSet()
    {
        var repository = CreateRepository();
        var item = CreateItem();
        var titleFieldId = new FieldId(Guid.NewGuid());
        var bodyFieldId = new FieldId(Guid.NewGuid());

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);

        var initialValues =
            new[]
            {
                CreateValue(item.Id, titleFieldId, "title", "Home", ContentVersion.Shared),
                CreateValue(item.Id, bodyFieldId, "body", "First")
            };

        var mergedValues =
            new[]
            {
                CreateValue(item.Id, bodyFieldId, "body", "Second"),
                CreateValue(item.Id, new FieldId(Guid.NewGuid()), "summary", "Third")
            };

        await repository.SaveFieldValuesAsync(item.Id, initialValues, TestContext.Current.CancellationToken);
        await repository.SaveFieldValuesAsync(item.Id, mergedValues, TestContext.Current.CancellationToken);

        var stored =
            await repository.GetFieldValuesAsync(
                item.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(3, stored.Count);
        Assert.Contains(stored, value => value.FieldKey == "title" && value.Value == "Home");
        Assert.Contains(stored, value => value.FieldKey == "body" && value.Value == "Second");
        Assert.Contains(stored, value => value.FieldKey == "summary" && value.Value == "Third");
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ShouldThrow_WhenValueDoesNotBelongToRequestedItem()
    {
        var repository = CreateRepository();
        var item = CreateItem();
        var values =
            new[]
            {
                CreateValue(new ContentItemId(Guid.NewGuid()), new FieldId(Guid.NewGuid()), "title", "Home")
            };

        await repository.SaveItemAsync(item, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveFieldValuesAsync(
                item.Id,
                values,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Database_ShouldRejectDuplicateFieldValueIdentity()
    {
        using var dbContext = CreateDbContext();
        var item = CreateItem();
        var fieldId = Guid.NewGuid();

        dbContext.ContentItems.Add(
            new PersistenceContentItem
            {
                Id = item.Id.Value,
                Name = item.Name,
                Key = item.Key.Value,
                TemplateId = item.TemplateId.Value,
                ParentId = item.ParentId?.Value
            });

        dbContext.ContentFieldValues.Add(
            new PersistenceContentFieldValue
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id.Value,
                FieldId = fieldId,
                FieldKey = "title",
                Language = "en",
                Version = 1,
                Value = "Home"
            });

        dbContext.ContentFieldValues.Add(
            new PersistenceContentFieldValue
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id.Value,
                FieldId = fieldId,
                FieldKey = "title",
                Language = "en",
                Version = 1,
                Value = "Duplicate"
            });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteItemAsync_ShouldDeleteItemAndCascadeFieldValues()
    {
        var repository = CreateRepository();
        var item = CreateItem();
        var values =
            new[]
            {
                CreateValue(item.Id, new FieldId(Guid.NewGuid()), "title", "Home", ContentVersion.Shared)
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
        string key = "home",
        TemplateId? templateId = null)
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            name,
            new ContentItemKey(key),
            templateId ?? new TemplateId(Guid.NewGuid()),
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
}
