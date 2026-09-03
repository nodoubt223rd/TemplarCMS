using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;

namespace TemplarCMS.Abstractions.Media;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken = default);
}
