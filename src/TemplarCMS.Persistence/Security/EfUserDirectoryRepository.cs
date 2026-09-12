using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Persistence.Security;

public sealed class EfUserDirectoryRepository(TemplarCmsDbContext db) : IUserDirectoryRepository
{
    public async Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken) =>
        (await db.DirectoryUsers.AsNoTracking().OrderBy(u => u.NormalizedEmail).ToListAsync(cancellationToken))
        .Select(Map).ToArray();

    public async Task<DirectoryUser?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await db.DirectoryUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken) is { } row ? Map(row) : null;

    public async Task<DirectoryWriteResult> CreateAsync(DirectoryUserProfile profile, CancellationToken cancellationToken)
    {
        var row = new DirectoryUserRow { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        Apply(row, profile);
        db.DirectoryUsers.Add(row);
        return await SaveAsync(row, cancellationToken);
    }

    public async Task<DirectoryWriteResult> UpdateAsync(Guid id, Guid revision, DirectoryUserProfile profile, CancellationToken cancellationToken)
    {
        var row = await db.DirectoryUsers.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (row is null) return new(DirectoryWriteStatus.NotFound);
        if (row.Revision != revision) return new(DirectoryWriteStatus.Conflict);
        Apply(row, profile);
        return await SaveAsync(row, cancellationToken);
    }

    private async Task<DirectoryWriteResult> SaveAsync(DirectoryUserRow row, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new(DirectoryWriteStatus.Saved, Map(row));
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(row).State = EntityState.Detached;
            return new(DirectoryWriteStatus.Conflict);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 } ||
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(row).State = EntityState.Detached;
            return new(DirectoryWriteStatus.DuplicateEmail);
        }
    }

    private static void Apply(DirectoryUserRow row, DirectoryUserProfile profile)
    {
        row.FirstName = profile.FirstName;
        row.LastName = profile.LastName;
        row.Email = profile.Email;
        row.NormalizedEmail = profile.Email.ToUpperInvariant();
        row.Language = profile.Language;
        row.RolesJson = JsonSerializer.Serialize(profile.Roles.Select(role => role.ToString()));
        row.Revision = Guid.NewGuid();
    }

    private static DirectoryUser Map(DirectoryUserRow row) => new(row.Id, row.FirstName, row.LastName,
        row.Email, row.Language, Enum.Parse<DirectoryUserStatus>(row.Status),
        JsonSerializer.Deserialize<string[]>(row.RolesJson)!.Select(Enum.Parse<DirectoryRole>).ToArray(),
        row.CreatedAt, row.LastLogin, row.Revision);
}
