using TemplarCMS.Abstractions.Media;
using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;

namespace TemplarCMS.Application.Media;

public sealed class MediaAssetService : IMediaAssetService
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/gif"] = ".gif", ["image/webp"] = ".webp"
    };
    private readonly IMediaAssetRepository _repository;
    private readonly IMediaFileStore _fileStore;

    public MediaAssetService(IMediaAssetRepository repository, IMediaFileStore fileStore)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    public Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default) => _repository.GetAllAsync(cancellationToken);
    public Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);

    public async Task<MediaAsset> CreateAsync(ContentItemId folderId, string fileName, string contentType, Stream content, long length, string? altText, string? title, CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.TryGetValue(contentType, out var extension)) throw new ArgumentException("Only JPEG, PNG, GIF, and WebP images are supported.", nameof(contentType));
        if (length <= 0) throw new ArgumentException("The uploaded file is empty.", nameof(length));
        var id = Guid.NewGuid();
        var asset = new MediaAsset(id, folderId, Path.GetFileName(fileName), id + extension, contentType, length, altText?.Trim(), title?.Trim(), DateTimeOffset.UtcNow);
        await _fileStore.SaveAsync(asset.StoredFileName, content, cancellationToken);
        await _repository.SaveAsync(asset, cancellationToken);
        return asset;
    }

    public Task<Stream?> OpenReadAsync(MediaAsset asset, CancellationToken cancellationToken = default) => _fileStore.OpenReadAsync(asset.StoredFileName, cancellationToken);
}
