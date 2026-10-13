using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;

namespace TemplarCMS.Api.Security;

public sealed class AdminIdentityService(TemplarCmsDbContext db, UserManager<CmsIdentityUser> users,
    SignInManager<CmsIdentityUser> signIn, IPasswordHasher<CmsIdentityUser> hasher,
    ILogger<AdminIdentityService> logger) : IAdminIdentityService
{
    public async Task<AdminSignInResult> CheckPasswordAsync(string loginName, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(loginName) || loginName.Length > 256 || string.IsNullOrEmpty(password) || password.Length > 128)
            return new(AdminSignInStatus.InvalidCredentials);
        var user = await users.FindByNameAsync(loginName.Trim());
        if (user is null) return new(AdminSignInStatus.InvalidCredentials);
        var snapshot = await GetAccountAsync(user.Id, ct);
        if (snapshot?.Status != DirectoryUserStatus.Active) return new(AdminSignInStatus.InvalidCredentials);
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        logger.LogInformation("Administrator login outcome {Succeeded} for account {AccountId}.", result.Succeeded, user.Id);
        if (!result.Succeeded) return new(result.IsLockedOut ? AdminSignInStatus.LockedOut : AdminSignInStatus.InvalidCredentials);
        // The verified stamp travels separately so a racing reset cannot stamp this login with newer credentials.
        var stamp = user.SecurityStamp;
        var current = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == user.Id, ct);
        if (current is null || current.SecurityStamp != stamp) return new(AdminSignInStatus.InvalidCredentials);
        return new(AdminSignInStatus.SignedIn, snapshot, stamp);
    }

    public async Task<AdminAccountSnapshot?> GetAccountAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId, ct);
        var profile = await db.DirectoryUsers.AsNoTracking().SingleOrDefaultAsync(value => value.Id == userId, ct);
        if (user is null || profile is null || !Enum.TryParse<DirectoryUserStatus>(profile.Status, out var status) || !Enum.IsDefined(status)) return null;
        try
        {
            var keys = JsonSerializer.Deserialize<string[]>(profile.RolesJson);
            if (keys is null) return null;
            var roles = new List<DirectoryRole>();
            foreach (var key in keys)
            {
                if (!Enum.TryParse<DirectoryRole>(key, out var role) || !Enum.IsDefined(role)) return null;
                roles.Add(role);
            }
            return new(userId, user.UserName!, profile.Email, profile.Language, status, roles, user.MustChangePassword);
        }
        catch (JsonException) { return null; }
    }

    public async Task<AdminPasswordChangeResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        if (newPassword is null || newPassword.Length is < 15 or > 128)
            return new(AdminPasswordChangeStatus.ValidationFailed, Errors: ["Choose a password between 15 and 128 characters."]);
        try
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var snapshot = await GetAccountAsync(userId, ct);
            var user = await users.FindByIdAsync(userId.ToString());
            if (user is null || snapshot?.Status != DirectoryUserStatus.Active) return new(AdminPasswordChangeStatus.AccountUnavailable);
            if (user.PasswordHash is not null && hasher.VerifyHashedPassword(user, user.PasswordHash, newPassword) != PasswordVerificationResult.Failed)
                return new(AdminPasswordChangeStatus.ValidationFailed, Errors: ["Choose a different password."]);
            user.MustChangePassword = false;
            var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return new(result.Errors.Any(error => error.Code == "ConcurrencyFailure") ? AdminPasswordChangeStatus.Conflict : AdminPasswordChangeStatus.InvalidPassword);
            }
            await transaction.CommitAsync(ct);
            logger.LogInformation("Administrator password changed for account {AccountId}.", userId);
            return new(AdminPasswordChangeStatus.Changed, await GetAccountAsync(userId, ct));
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException)
        {
            db.ChangeTracker.Clear();
            return new(AdminPasswordChangeStatus.Conflict);
        }
    }
}
