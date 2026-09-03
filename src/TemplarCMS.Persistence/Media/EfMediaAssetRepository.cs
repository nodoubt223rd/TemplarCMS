using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Media;
using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;

namespace TemplarCMS.Persistence.Media;

public sealed class EfMediaAssetRepository : IMediaAssetRepository
{
    private readonly TemplarCmsDbContext _dbContext;
    public EfMediaAssetRepository(TemplarCmsDbContext dbContext) => _dbContext = dbContext;
    public async Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) => (await _dbContext.MediaAssets.AsNoTracking().FirstOrDefaultAsync(value => value.Id == id, cancellationToken)) is { } asset ? Map(asset) : null;
    public async Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default) => (await _dbContext.MediaAssets.AsNoTracking().OrderBy(value => value.FileName).ToListAsync(cancellationToken)).Select(Map).ToArray();
    public async Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        _dbContext.MediaAssets.Add(new PersistenceMediaAsset { Id = asset.Id, FolderId = asset.FolderId.Value, FileName = asset.FileName, StoredFileName = asset.StoredFileName, ContentType = asset.ContentType, Length = asset.Length, AltText = asset.AltText, Title = asset.Title, CreatedUtc = asset.CreatedUtc });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    private static MediaAsset Map(PersistenceMediaAsset asset) => new(asset.Id, new ContentItemId(asset.FolderId), asset.FileName, asset.StoredFileName, asset.ContentType, asset.Length, asset.AltText, asset.Title, asset.CreatedUtc);
}
