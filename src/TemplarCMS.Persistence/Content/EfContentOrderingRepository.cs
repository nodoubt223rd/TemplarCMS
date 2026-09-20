using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Persistence.Content;

public sealed class EfContentOrderingRepository(TemplarCmsDbContext db, IContentModelCatalog catalog) : IContentOrderingRepository
{
    public async Task<ContentOrderSnapshot> ReadAsync(Guid? parentId, CancellationToken cancellationToken = default)
    {
        var siblings = await db.ContentItems.AsNoTracking().Where(i => i.ParentId == parentId).ToArrayAsync(cancellationToken);
        var ids = siblings.Select(i => i.Id).ToArray();
        var values = await db.ContentFieldValues.AsNoTracking().Where(v => ids.Contains(v.ItemId) && v.Language == "shared" && v.Version == 0).ToArrayAsync(cancellationToken);
        var entries = new List<ContentOrderEntry>();
        foreach (var sibling in siblings)
        {
            var template = await catalog.GetEffectiveTemplateAsync(new TemplateId(sibling.TemplateId), cancellationToken);
            var field = template?.Fields.FirstOrDefault(f => string.Equals(f.Key, "__sortorder", StringComparison.OrdinalIgnoreCase));
            var supported = field is { IsShared: true, FieldType: FieldType.SingleLineText };
            entries.Add(new(sibling.Id, sibling.Key, field?.Id.Value ?? Guid.Empty,
                values.FirstOrDefault(v => v.ItemId == sibling.Id && v.FieldId == field?.Id.Value)?.Value, supported));
        }
        return new(parentId, ContentSiblingOrdering.Revision(parentId, entries), ContentSiblingOrdering.Sort(entries));
    }

    public async Task<ContentOrderSnapshot> ReorderAsync(Guid itemId, ContentOrderDirection direction, string expectedRevision, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var item = await db.ContentItems.AsNoTracking().SingleOrDefaultAsync(i => i.Id == itemId, cancellationToken)
                ?? throw new KeyNotFoundException("Content item was not found.");
            var before = await ReadAsync(item.ParentId, cancellationToken);
            if (!string.Equals(before.Revision, expectedRevision, StringComparison.Ordinal))
                throw new ContentOrderConflictException("Sibling order changed. Refresh the branch and try again.");
            var ordered = ContentSiblingOrdering.Move(before.Items, itemId, direction);
            if (ordered.Select(e => e.Id).SequenceEqual(before.Items.Select(e => e.Id)))
            {
                await transaction.CommitAsync(cancellationToken);
                return before;
            }
            var ids = ordered.Select(e => e.Id).ToArray();
            var stored = await db.ContentFieldValues.Where(v => ids.Contains(v.ItemId) && v.Language == "shared" && v.Version == 0).ToListAsync(cancellationToken);
            for (var i = 0; i < ordered.Count; i++)
            {
                var entry = ordered[i];
                var row = stored.SingleOrDefault(v => v.ItemId == entry.Id && v.FieldId == entry.FieldId);
                if (row is null)
                {
                    row = new() { Id = Guid.NewGuid(), ItemId = entry.Id, FieldId = entry.FieldId, FieldKey = "__sortorder", Language = "shared", Version = 0 };
                    db.ContentFieldValues.Add(row);
                }
                row.Value = i.ToString(CultureInfo.InvariantCulture);
            }
            await db.SaveChangesAsync(cancellationToken);
            var after = await ReadAsync(item.ParentId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return after;
        }
        catch (Exception exception) when (exception is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 } ||
            exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 1222 })
        {
            db.ChangeTracker.Clear();
            throw new ContentOrderConflictException("Another author changed this branch. Refresh and try again.");
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
