using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Application.Tests.Content;

public sealed class ContentPathResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldReturnRootPath_ForRootItem()
    {
        var item =
            CreateItem(
                parentId: null,
                key: "home");

        var resolver =
            CreateResolver(
                item);

        var result =
            await resolver.ResolveAsync(
                item,
                TestContext.Current.CancellationToken);

        Assert.Equal("/home", result.ToString());
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNestedPath_ForDescendant()
    {
        var home =
            CreateItem(
                parentId: null,
                key: "home");
        var articles =
            CreateItem(
                parentId: home.Id,
                key: "articles");
        var helloWorld =
            CreateItem(
                parentId: articles.Id,
                key: "hello-world");

        var resolver =
            CreateResolver(
                home,
                articles,
                helloWorld);

        var result =
            await resolver.ResolveAsync(
                helloWorld,
                TestContext.Current.CancellationToken);

        Assert.Equal("/home/articles/hello-world", result.ToString());
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrow_WhenAncestorIsMissing()
    {
        var item =
            CreateItem(
                parentId: new ContentItemId(Guid.NewGuid()),
                key: "child");

        var resolver =
            CreateResolver(
                item);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            resolver.ResolveAsync(
                item,
                TestContext.Current.CancellationToken));
    }

    private static ContentPathResolver CreateResolver(
        params ContentItemDefinition[] items)
    {
        var repository =
            new InMemoryContentRepository();

        foreach (var item in items)
        {
            repository.SaveItemAsync(
                    item,
                    TestContext.Current.CancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        return new ContentPathResolver(repository);
    }

    private static ContentItemDefinition CreateItem(
        ContentItemId? parentId,
        string key)
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            key,
            new ContentItemKey(key),
            new TemplateId(Guid.NewGuid()),
            parentId);
    }
}
