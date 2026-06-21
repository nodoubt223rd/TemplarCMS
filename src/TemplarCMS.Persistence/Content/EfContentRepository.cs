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
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _dbContext.ContentItems
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    value => value.Id == itemId,
                    cancellationToken);

        return item == null ? null : MapItem(item);
    }

    public async Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        var items =
            await _dbContext.ContentItems
                .AsNoTracking()
                .Where(value => value.ParentId == parentId)
                .OrderBy(value => value.Key)
                .ToListAsync(cancellationToken);

        return items
            .Select(MapItem)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var values =
            await _dbContext.ContentFieldValues
                .AsNoTracking()
                .Where(value => value.ItemId == itemId)
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
                    value => value.Id == item.Id,
                    cancellationToken);

        if (existing == null)
        {
            _dbContext.ContentItems.Add(
                new PersistenceContentItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Key = item.Key.Value,
                    TemplateId = item.TemplateId,
                    ParentId = item.ParentId
                });
        }
        else
        {
            existing.Name = item.Name;
            existing.Key = item.Key.Value;
            existing.TemplateId = item.TemplateId;
            existing.ParentId = item.ParentId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFieldValuesAsync(
        Guid itemId,
        IReadOnlyCollection<ContentFieldValue> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (itemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Content item id is required.",
                nameof(itemId));
        }

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
                .Where(value => value.ItemId == itemId)
                .ToListAsync(cancellationToken);

        _dbContext.ContentFieldValues.RemoveRange(existingValues);

        foreach (var value in values)
        {
            _dbContext.ContentFieldValues.Add(
                new PersistenceContentFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = value.ItemId,
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
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _dbContext.ContentItems
                .FirstOrDefaultAsync(
                    value => value.Id == itemId,
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
            item.Id,
            item.Name,
            new ContentItemKey(item.Key),
            item.TemplateId,
            item.ParentId);
    }

    private static ContentFieldValue MapFieldValue(
        PersistenceContentFieldValue value)
    {
        return new ContentFieldValue(
            value.ItemId,
            value.FieldId,
            value.FieldKey,
            new ContentLanguage(value.Language),
            new ContentVersion(value.Version),
            value.Value);
    }
}
