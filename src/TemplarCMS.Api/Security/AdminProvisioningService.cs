using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Application.Security;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;

namespace TemplarCMS.Api.Security;

public sealed class AdminProvisioningService(TemplarCmsDbContext db, UserManager<CmsIdentityUser> users,
    ILogger<AdminProvisioningService> logger) : IAdminProvisioningService
{
    public const string LoginName = "TemplarAdmin";

    public Task<AdminProvisioningResult> SetupAsync(string email, CancellationToken ct)
    {
        var profile = new DirectoryUserProfile("Templar", "Administrator", email, "en", [DirectoryRole.TemplarAdmin]);
        if (DirectoryProfileValidation.Validate(profile).Count != 0)
            return Task.FromResult(new AdminProvisioningResult(AdminProvisioningStatus.InvalidEmail));
        profile = DirectoryProfileValidation.Normalize(profile);
        return ExecuteAsync(async () =>
        {
            var marker = await db.AdminProvisioning.SingleOrDefaultAsync(ct);
            if (marker is not null) return await ExistingAsync(marker, ct);
            if (await db.DirectoryUsers.AnyAsync(row => row.NormalizedEmail == profile.Email.ToUpperInvariant(), ct))
                return new(AdminProvisioningStatus.DuplicateEmail);
            if (await db.Users.AnyAsync(row => row.NormalizedUserName == "TEMPLARADMIN", ct))
                return new(AdminProvisioningStatus.MissingAccount);
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            db.DirectoryUsers.Add(new DirectoryUserRow { Id = id, FirstName = profile.FirstName, LastName = profile.LastName,
                Email = profile.Email, NormalizedEmail = profile.Email.ToUpperInvariant(), Language = profile.Language,
                Status = nameof(DirectoryUserStatus.Active), RolesJson = JsonSerializer.Serialize(new[] { nameof(DirectoryRole.TemplarAdmin) }),
                CreatedAt = now, Revision = Guid.NewGuid() });
            await db.SaveChangesAsync(ct);
            var password = TemporaryPasswordGenerator.Generate();
            var account = new CmsIdentityUser { Id = id, UserName = LoginName, Email = profile.Email,
                MustChangePassword = true, LockoutEnabled = true };
            var created = await users.CreateAsync(account, password);
            if (!created.Succeeded) return new(AdminProvisioningStatus.Conflict);
            db.AdminProvisioning.Add(new() { UserId = id, CreatedAt = now });
            await db.SaveChangesAsync(ct);
            return new(AdminProvisioningStatus.Created, id, password);
        }, ct);
    }

    public Task<AdminProvisioningResult> ResetAsync(CancellationToken ct) => ExecuteAsync(async () =>
    {
        var marker = await db.AdminProvisioning.SingleOrDefaultAsync(ct);
        if (marker is null) return new(AdminProvisioningStatus.MissingAccount);
        var existing = await ExistingAsync(marker, ct);
        if (existing.Status != AdminProvisioningStatus.AlreadyInitialized) return existing;
        var account = await users.FindByIdAsync(marker.UserId.ToString());
        if (account is null) return new(AdminProvisioningStatus.MissingAccount);
        var password = TemporaryPasswordGenerator.Generate();
        account.MustChangePassword = true;
        account.AccessFailedCount = 0;
        account.LockoutEnd = null;
        var token = await users.GeneratePasswordResetTokenAsync(account);
        var reset = await users.ResetPasswordAsync(account, token, password);
        if (!reset.Succeeded) return new(AdminProvisioningStatus.Conflict);
        return new(AdminProvisioningStatus.Created, account.Id, password);
    }, ct);

    private async Task<AdminProvisioningResult> ExistingAsync(AdminProvisioningRow marker, CancellationToken ct)
    {
        var profile = await db.DirectoryUsers.AsNoTracking().SingleOrDefaultAsync(row => row.Id == marker.UserId, ct);
        if (profile is null || !await db.Users.AnyAsync(row => row.Id == marker.UserId, ct))
            return new(AdminProvisioningStatus.MissingAccount);
        if (profile.Status != nameof(DirectoryUserStatus.Active) ||
            !JsonSerializer.Deserialize<string[]>(profile.RolesJson)!.Contains(nameof(DirectoryRole.TemplarAdmin), StringComparer.Ordinal))
            return new(AdminProvisioningStatus.InactiveAccount);
        return new(AdminProvisioningStatus.AlreadyInitialized, marker.UserId);
    }

    private async Task<AdminProvisioningResult> ExecuteAsync(Func<Task<AdminProvisioningResult>> operation, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var result = await operation();
                if (result.Status == AdminProvisioningStatus.Created) await transaction.CommitAsync(ct);
                else { await transaction.RollbackAsync(ct); db.ChangeTracker.Clear(); }
                logger.LogInformation("Administrator provisioning outcome {Outcome} for account {AccountId}.", result.Status, result.UserId);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                return new(AdminProvisioningStatus.Conflict);
            }
            catch (Exception exception) when (Retryable(exception))
            {
                db.ChangeTracker.Clear();
                if (attempt == 2) return new(AdminProvisioningStatus.Conflict);
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)), ct);
            }
        }
        return new(AdminProvisioningStatus.Conflict);
    }

    private static bool Retryable(Exception exception) => exception is SqliteException { SqliteErrorCode: 5 or 6 } or
        SqlException { Number: 1205 or 2601 or 2627 } || exception is DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 2067 } } or
        DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } };
}
