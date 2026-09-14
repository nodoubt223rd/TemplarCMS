using TemplarCMS.Abstractions.Media;
using TemplarCMS.Application.Media;
using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;
using Xunit;

namespace TemplarCMS.Application.Tests.Media;

public sealed class MediaAssetServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldStoreRasterAssetWithGeneratedFileName()
    {
        var repository = new InMemoryMediaAssetRepository();
        var fileStore = new InMemoryMediaFileStore();
        var service = new MediaAssetService(repository, fileStore);
        await using var content = new MemoryStream([1, 2, 3]);

        var asset = await service.CreateAsync(
            SystemSeedContentIds.Images,
            "Hero image.PNG",
            "image/png",
            content,
            content.Length,
            "A hero image",
            "Hero",
            TestContext.Current.CancellationToken);

        Assert.Equal("Hero image.PNG", asset.FileName);
        Assert.EndsWith(".png", asset.StoredFileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(asset, await repository.GetAsync(asset.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(await fileStore.OpenReadAsync(asset.StoredFileName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnsupportedContentType()
    {
        var service = new MediaAssetService(new InMemoryMediaAssetRepository(), new InMemoryMediaFileStore());
        await using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            SystemSeedContentIds.Images, "unsafe.svg", "image/svg+xml", content, content.Length, null, null, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(255, 500, 255, true)]
    [InlineData(256, 500, 255, false)]
    [InlineData(255, 501, 255, false)]
    [InlineData(255, 500, 256, false)]
    public async Task UploadEnforcesLengthsBeforeReadingTheStream(int filenameLength, int altLength, int titleLength, bool valid)
    {
        var repository = new InMemoryMediaAssetRepository();
        var service = new MediaAssetService(repository, new InMemoryMediaFileStore());
        await using var stream = new MemoryStream([1,2,3]);
        Task<MediaAsset> Create() => service.CreateAsync(SystemSeedContentIds.Images, new string('f',filenameLength), "image/png", stream, stream.Length, new string('a',altLength), new string('t',titleLength), TestContext.Current.CancellationToken);
        if (valid) Assert.Equal(filenameLength,(await Create()).FileName.Length);
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(Create);
            Assert.Equal(0,stream.Position);
            Assert.Empty(await repository.GetAllAsync(TestContext.Current.CancellationToken));
        }
    }

    private sealed class InMemoryMediaAssetRepository : IMediaAssetRepository
    {
        private readonly Dictionary<Guid, MediaAsset> _assets = [];
        public Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_assets.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<MediaAsset>>(_assets.Values.ToArray());
        public Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken = default) { _assets.Add(asset.Id, asset); return Task.CompletedTask; }
    }

    private sealed class InMemoryMediaFileStore : IMediaFileStore
    {
        private readonly Dictionary<string, byte[]> _files = [];
        public async Task SaveAsync(string storedFileName, Stream source, CancellationToken cancellationToken = default) { await using var buffer = new MemoryStream(); await source.CopyToAsync(buffer, cancellationToken); _files.Add(storedFileName, buffer.ToArray()); }
        public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default) { _files.Remove(storedFileName); return Task.CompletedTask; }
        public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(_files.TryGetValue(storedFileName, out var value) ? new MemoryStream(value) : null);
    }
}
