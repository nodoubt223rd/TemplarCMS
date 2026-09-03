using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Media;
using TemplarCMS.Application.Media;
using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;
using Xunit;

namespace TemplarCMS.Api.Tests.Media;

public sealed class MediaEndpointsTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnCatalogAssets()
    {
        var asset = CreateAsset();
        var result = await MediaEndpoints.GetAllAsync(new FakeMediaAssetService(asset), TestContext.Current.CancellationToken);
        var context = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Contains(asset.Id.ToString(), body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/api/v1/media/assets/{asset.Id}/content", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAsync_ShouldRejectMissingFileOrFolder()
    {
        var result = await MediaEndpoints.UploadAsync(null, null, null, null, new FakeMediaAssetService(), TestContext.Current.CancellationToken);
        var context = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task GetContentAsync_ShouldReturnNotFound_WhenAssetIsMissing()
    {
        var result = await MediaEndpoints.GetContentAsync(Guid.NewGuid(), new FakeMediaAssetService(), TestContext.Current.CancellationToken);
        var context = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateRasterAsset()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        var file = new FormFile(content, 0, content.Length, "file", "hero.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var service = new FakeMediaAssetService();

        var result = await MediaEndpoints.UploadAsync(file, SystemSeedContentIds.Images.Value, "Hero", "Hero", service, TestContext.Current.CancellationToken);
        var context = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.True(service.Created);
    }

    [Fact]
    public async Task UploadAsync_ShouldRejectSvg()
    {
        await using var content = new MemoryStream([1]);
        var file = new FormFile(content, 0, content.Length, "file", "unsafe.svg") { Headers = new HeaderDictionary(), ContentType = "image/svg+xml" };

        var result = await MediaEndpoints.UploadAsync(file, SystemSeedContentIds.Images.Value, null, null, new FakeMediaAssetService(rejectSvg: true), TestContext.Current.CancellationToken);
        var context = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private static async Task<DefaultHttpContext> ExecuteAsync(IResult result)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        await result.ExecuteAsync(context);
        return context;
    }

    private static MediaAsset CreateAsset() => new(Guid.NewGuid(), SystemSeedContentIds.Images, "hero.png", "stored.png", "image/png", 3, "Hero", "Hero", DateTimeOffset.UtcNow);

    private sealed class FakeMediaAssetService : IMediaAssetService
    {
        private readonly MediaAsset? _asset;
        private readonly bool _rejectSvg;
        public bool Created { get; private set; }
        public FakeMediaAssetService(MediaAsset? asset = null, bool rejectSvg = false) { _asset = asset; _rejectSvg = rejectSvg; }
        public Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<MediaAsset>>(_asset == null ? [] : [_asset]);
        public Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_asset?.Id == id ? _asset : null);
        public Task<MediaAsset> CreateAsync(ContentItemId folderId, string fileName, string contentType, Stream content, long length, string? altText, string? title, CancellationToken cancellationToken = default)
        {
            if (_rejectSvg && contentType == "image/svg+xml") throw new ArgumentException("Only JPEG, PNG, GIF, and WebP images are supported.");
            Created = true;
            return Task.FromResult(_asset ?? CreateAsset());
        }
        public Task<Stream?> OpenReadAsync(MediaAsset asset, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream([1, 2, 3]));
    }
}
