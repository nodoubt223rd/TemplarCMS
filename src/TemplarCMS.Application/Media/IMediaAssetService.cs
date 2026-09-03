using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;

namespace TemplarCMS.Application.Media;

public interface IMediaAssetService
{
    Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaAsset> CreateAsync(ContentItemId folderId, string fileName, string contentType, Stream content, long length, string? altText, string? title, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(MediaAsset asset, CancellationToken cancellationToken = default);
}
