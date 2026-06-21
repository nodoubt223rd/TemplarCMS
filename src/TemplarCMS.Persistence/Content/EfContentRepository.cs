using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Persistence.Content;

/// <summary>
/// EF Core implementation of <see cref="IContentRepository"/>.
/// </summary>
public sealed class EfContentRepository : IContentRepository
{
    private readonly TemplarCmsDbContext _dbContext;

    public EfContentRepository(
        TemplarCmsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ContentItemDefinition?> GetItemAsync(
        ContentItemId itemId,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _dbContext.ContentItems
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    value => value.Id == itemId.Value,
                    cancellationToken);

        return item == null ? null : MapItem(item);
    }

    public async Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
        ContentItemId? parentId,
        CancellationToken cancellationToken = default)
    {
        var parentValue =
            parentId.HasValue
                ? parentId.Value.Value
                : (Guid?)null;

        var items =
            await _dbContext.ContentItems
                .AsNoTracking()
                .Where(value => value.ParentId == parentValue)
                .OrderBy(value => value.Key)
                .ToListAsync(cancellationToken);

        return items
            .Select(MapItem)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
        ContentItemId itemId,
        CancellationToken cancellationToken = default)
    {
        var values =
            await _dbContext.ContentFieldValues
                .AsNoTracking()
                .Where(value => value.ItemId == itemId.Value)
                .OrderBy(value => value.FieldKey)
                .ThenBy(value => value.Language)
                .ThenBy(value => value.Version)
                .ToListAsync(cancellationToken);

        return values
            .Select(MapFieldValue)
            .ToArray();
    }

    public async Task SaveItemAsync(
        ContentItemDefinition item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var existing =
            await _dbContext.ContentItems
                .FirstOrDefaultAsync(
                    value => value.Id == item.Id.Value,
                    cancellationToken);

        if (existing == null)
        {
            _dbContext.ContentItems.Add(
                new PersistenceContentItem
                {
                    Id = item.Id.Value,
                    Name = item.Name,
                    Key = item.Key.Value,
                    TemplateId = item.TemplateId.Value,
                    ParentId = item.ParentId?.Value
                });
        }
        else
        {
            existing.Name = item.Name;
            existing.Key = item.Key.Value;
            existing.TemplateId = item.TemplateId.Value;
            existing.ParentId = item.ParentId?.Value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFieldValuesAsync(
        ContentItemId itemId,
        IReadOnlyCollection<ContentFieldValue> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.ItemId != itemId)
            {
                throw new ArgumentException(
                    $"Field value '{value.FieldKey}' does not belong to content item '{itemId}'.",
                    nameof(values));
            }
        }

        var existingValues =
            await _dbContext.ContentFieldValues
                .Where(value => value.ItemId == itemId.Value)
                .ToListAsync(cancellationToken);

        _dbContext.ContentFieldValues.RemoveRange(existingValues);

        foreach (var value in values)
        {
            _dbContext.ContentFieldValues.Add(
                new PersistenceContentFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = value.ItemId.Value,
                    FieldId = value.FieldId,
                    FieldKey = value.FieldKey,
                    Language = value.Language.ToString(),
                    Version = value.Version.Value,
                    Value = value.Value
                });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteItemAsync(
        ContentItemId itemId,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _dbContext.ContentItems
                .FirstOrDefaultAsync(
                    value => value.Id == itemId.Value,
                    cancellationToken);

        if (item == null)
        {
            return;
        }

        _dbContext.ContentItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ContentItemDefinition MapItem(
        PersistenceContentItem item)
    {
        return new ContentItemDefinition(
            new ContentItemId(item.Id),
            item.Name,
            new ContentItemKey(item.Key),
            new TemplateId(item.TemplateId),
            item.ParentId == null ? null : new ContentItemId(item.ParentId.Value));
    }

    private static ContentFieldValue MapFieldValue(
        PersistenceContentFieldValue value)
    {
        return new ContentFieldValue(
            new ContentItemId(value.ItemId),
            value.FieldId,
            value.FieldKey,
            new ContentLanguage(value.Language),
            new ContentVersion(value.Version),
            value.Value);
    }
}
