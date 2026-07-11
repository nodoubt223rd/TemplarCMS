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

    private sealed class FakeContentItemService : IContentItemService
    {
        private readonly ResolvedContentItem? _item;

        public FakeContentItemService(
            ResolvedContentItem? item)
        {
            _item = item;
        }

        public ContentItemId? LastRequestedItemId { get; private set; }

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
            throw new NotSupportedException();
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
        ContentItemId? parentId)
    {
        var templateId = new TemplateId(Guid.NewGuid());

        return new ResolvedContentItem(
            new ContentItemDefinition(
                itemId,
                "Hello World",
                new ContentItemKey("hello-world"),
                templateId,
                parentId),
            new ContentPath("/home/articles/hello-world"),
            new Dictionary<string, ContentFieldValue?>
            {
                ["title"] = new ContentFieldValue(
                    itemId,
                    new FieldId(Guid.NewGuid()),
                    "title",
                    new ContentLanguage("en"),
                    ContentVersion.First,
                    "Hello World")
            },
            new Dictionary<string, TypedFieldValue>
            {
                ["title"] = new StringTypedFieldValue("Hello World")
            });
    }
}
