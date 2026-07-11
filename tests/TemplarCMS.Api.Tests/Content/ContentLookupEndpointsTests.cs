using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Content;

public sealed class ContentLookupEndpointsTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenItemExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var resolvedItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: new ContentItemId(Guid.NewGuid()));
        var service =
            new FakeContentItemService(
                resolvedItem);

        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                itemId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastContext);

        Assert.Equal(itemId.Value.ToString(), ok.Value.Id);
        Assert.Equal("/home/articles/hello-world", ok.Value.Path);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(itemId, service.LastRequestedItemId);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var id = Guid.NewGuid();

        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                id,
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                Guid.NewGuid(),
                "en",
                -1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnOk_WhenItemExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var resolvedItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: new ContentItemId(Guid.NewGuid()));
        var service =
            new FakeContentItemService(
                resolvedItem);

        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "HOME/ARTICLES/HELLO-WORLD",
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(service.LastContext);
        Assert.NotNull(ok.Value);

        Assert.Equal("/home/articles/hello-world", ok.Value.Path);
        Assert.Equal("en", ok.Value.Language);
        Assert.Equal(1, ok.Value.Version);
        Assert.Equal("Hello World", ok.Value.Fields["title"]);
        Assert.Equal(
            "/api/v1/content/by-path/home/articles/hello-world?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            new ContentPath("/home/articles/hello-world"),
            service.LastRequestedPath);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenPathIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                null,
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "home/missing",
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "home/articles/hello-world",
                "en",
                -1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnOk_WhenParentHasChildren()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var parent =
            CreateResolvedItem(
                itemId: parentId,
                parentId: null,
                path: "/home");
        var childA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-a",
                name: "Child A",
                key: "child-a",
                title: "A");
        var childB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-b",
                name: "Child B",
                key: "child-b",
                title: "B");
        var service =
            new FakeContentItemService(
                parent,
                [childA, childB]);

        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                parentId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastContext);

        Assert.Equal(
            $"/api/v1/content/{parentId.Value}/children?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{parentId.Value}?lang=en&version=1",
            ok.Value.Links.Parent.Href);
        Assert.Equal(2, ok.Value.Embedded.Items.Count);
        Assert.Equal(
            new[] { "/home/child-a", "/home/child-b" },
            ok.Value.Embedded.Items.Select(item => item.Path).ToArray());
        Assert.Equal(
            new[] { "A", "B" },
            ok.Value.Embedded.Items.Select(item => item.Fields["title"]).ToArray());
        Assert.Equal(parentId, service.LastRequestedChildParentId);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnOk_WhenParentHasNoChildren()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var parent =
            CreateResolvedItem(
                itemId: parentId,
                parentId: null,
                path: "/home");
        var service =
            new FakeContentItemService(
                parent,
                []);

        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                parentId.Value,
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Empty(ok.Value.Embedded.Items);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnProblem_WhenParentIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                Guid.NewGuid(),
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                Guid.NewGuid(),
                "en",
                -1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    private sealed class FakeContentItemService : IContentItemService
    {
        private readonly ResolvedContentItem? _item;
        private readonly IReadOnlyCollection<ResolvedContentItem> _children;

        public FakeContentItemService(
            ResolvedContentItem? item,
            IReadOnlyCollection<ResolvedContentItem>? children = null)
        {
            _item = item;
            _children = children ?? [];
        }

        public ContentItemId? LastRequestedItemId { get; private set; }

        public ContentItemId? LastRequestedChildParentId { get; private set; }

        public ContentPath? LastRequestedPath { get; private set; }

        public FieldValueResolutionContext? LastContext { get; private set; }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentItemId itemId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedItemId = itemId;
            LastContext = context;
            return Task.FromResult(_item);
        }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentPath path,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedPath = path;
            LastContext = context;
            return Task.FromResult(_item);
        }

        public Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
            ContentItemId? parentId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedChildParentId = parentId;
            LastContext = context;
            return Task.FromResult(_children);
        }

        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private static ResolvedContentItem CreateResolvedItem(
        ContentItemId itemId,
        ContentItemId? parentId,
        string path = "/home/articles/hello-world",
        string name = "Hello World",
        string key = "hello-world",
        string title = "Hello World")
    {
        var templateId = new TemplateId(Guid.NewGuid());

        return new ResolvedContentItem(
            new ContentItemDefinition(
                itemId,
                name,
                new ContentItemKey(key),
                templateId,
                parentId),
            new ContentPath(path),
            new Dictionary<string, ContentFieldValue?>
            {
                ["title"] = new ContentFieldValue(
                    itemId,
                    new FieldId(Guid.NewGuid()),
                    "title",
                    new ContentLanguage("en"),
                    ContentVersion.First,
                    title)
            },
            new Dictionary<string, TypedFieldValue>
            {
                ["title"] = new StringTypedFieldValue(title)
            });
    }
}
