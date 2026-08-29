using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests;

public sealed class ApiRootEndpointsTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnDiscoveryLinks_WhenOpenApiIsEnabled()
    {
        var result =
            await ApiRootEndpoints.GetAsync(openApiEnabled: true);

        var ok = Assert.IsType<Ok<ApiRootResponse>>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("TemplarCMS API", ok.Value.Name);
        Assert.Equal("v1", ok.Value.Version);
        Assert.Equal("/api/v1", ok.Value.Links.Self.Href);
        Assert.Equal("/api/v1/templates", ok.Value.Links.Templates.Href);
        Assert.Equal("/api/v1/field-types", ok.Value.Links.FieldTypes.Href);
        Assert.Equal("/api/v1/content/root/branch?lang=en&version=1", ok.Value.Links.ContentRoot.Href);
        Assert.Equal("/api/v1/content/root/children?lang=en&version=1", ok.Value.Links.ContentRootChildren.Href);
        Assert.Equal("/api/v1/templates", ok.Value.Links.CreateTemplate.Href);
        Assert.Equal("/api/v1/content", ok.Value.Links.CreateContent.Href);
        Assert.NotNull(ok.Value.Links.OpenApi);
        Assert.Equal("/openapi/v1.json", ok.Value.Links.OpenApi!.Href);
    }

    [Fact]
    public async Task GetAsync_ShouldOmitOpenApiLink_WhenOpenApiIsDisabled()
    {
        var result =
            await ApiRootEndpoints.GetAsync(openApiEnabled: false);

        var ok = Assert.IsType<Ok<ApiRootResponse>>(result);
        Assert.NotNull(ok.Value);
        Assert.Null(ok.Value.Links.OpenApi);
    }

    [Fact]
    public async Task GetContentPageAsync_ShouldRenderResolvedContent()
    {
        var item = CreateResolvedItem();
        var service = new FakeContentItemService(item);

        var result =
            await ApiRootEndpoints.GetContentPageAsync(
                "home/articles/hello-world",
                service,
                TestContext.Current.CancellationToken);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.ContentType);
        Assert.Equal("/home/articles/hello-world", service.LastRequestedPath?.ToString());
        Assert.Contains("Hello &lt;world&gt;", response.Body);
        Assert.Contains("<p>Authored body.</p>", response.Body);
    }

    [Fact]
    public async Task GetContentPageAsync_ShouldReturnHtmlNotFound_WhenContentIsMissing()
    {
        var result =
            await ApiRootEndpoints.GetContentPageAsync(
                "missing",
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.ContentType);
        Assert.Contains("Page not found", response.Body);
    }

    [Fact]
    public async Task GetContentPageAsync_ShouldRenderHomeChildrenAsNavigation()
    {
        var home = CreateResolvedItem("Home", "home", "/home");
        var article =
            CreateResolvedItem(
                "Article list",
                "articles",
                "/home/articles",
                navigationTitle: "Articles & updates");
        var service =
            new FakeContentItemService(
                item: article,
                home: home,
                homeChildren: new[] { article });

        var result =
            await ApiRootEndpoints.GetContentPageAsync(
                "home/articles",
                service,
                TestContext.Current.CancellationToken);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(home.Item.Id, service.LastRequestedChildrenParentId);
        Assert.Contains("<nav aria-label=\"Site navigation\">", response.Body);
        Assert.Contains("href=\"/home/articles\"", response.Body);
        Assert.Contains("Articles &amp; updates", response.Body);
    }

    private static ResolvedContentItem CreateResolvedItem(
        string name = "Hello world",
        string key = "hello-world",
        string path = "/home/articles/hello-world",
        string? navigationTitle = null)
    {
        var item =
            new ContentItemDefinition(
                new ContentItemId(Guid.NewGuid()),
                name,
                new ContentItemKey(key),
                new TemplateId(Guid.NewGuid()),
                parentId: null);

        var fields =
            new Dictionary<string, ContentFieldValue?>
            {
                ["title"] = CreateFieldValue(item.Id, "title", "Hello <world>"),
                ["body"] = CreateFieldValue(item.Id, "body", "<p>Authored body.</p>")
            };

        if (navigationTitle != null)
        {
            fields["navigationTitle"] =
                CreateFieldValue(item.Id, "navigationTitle", navigationTitle);
        }

        return new ResolvedContentItem(
            item,
            new ContentPath(path),
            fields,
            new Dictionary<string, TypedFieldValue>());
    }

    private static ContentFieldValue CreateFieldValue(
        ContentItemId itemId,
        string key,
        string value)
    {
        return new ContentFieldValue(
            itemId,
            new FieldId(Guid.NewGuid()),
            key,
            new ContentLanguage("en"),
            ContentVersion.First,
            value);
    }

    private static async Task<(int StatusCode, string? ContentType, string Body)> ExecuteResultAsync(
        IResult result)
    {
        var context = new DefaultHttpContext();
        await using var services =
            new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
        await using var responseBody = new MemoryStream();
        context.RequestServices = services;
        context.Response.Body = responseBody;

        await result.ExecuteAsync(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        return (context.Response.StatusCode, context.Response.ContentType, body);
    }

    private sealed class FakeContentItemService : IContentItemService
    {
        private readonly ResolvedContentItem? _item;
        private readonly ResolvedContentItem? _home;
        private readonly IReadOnlyCollection<ResolvedContentItem> _homeChildren;

        public FakeContentItemService(
            ResolvedContentItem? item,
            ResolvedContentItem? home = null,
            IReadOnlyCollection<ResolvedContentItem>? homeChildren = null)
        {
            _item = item;
            _home = home;
            _homeChildren = homeChildren ?? Array.Empty<ResolvedContentItem>();
        }

        public ContentPath? LastRequestedPath { get; private set; }

        public ContentItemId? LastRequestedChildrenParentId { get; private set; }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentItemId itemId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_home);
        }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentPath path,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedPath = path;
            return Task.FromResult(_item);
        }

        public Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
            ContentItemId? parentId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedChildrenParentId = parentId;
            return Task.FromResult(_homeChildren);
        }

        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RenameItemAsync(
            ContentItemId itemId,
            string name,
            ContentItemKey key,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task MoveItemAsync(
            ContentItemId itemId,
            ContentItemId? parentId,
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

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            FieldValueResolutionContext context,
            IReadOnlyDictionary<string, string?> values,
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
}
