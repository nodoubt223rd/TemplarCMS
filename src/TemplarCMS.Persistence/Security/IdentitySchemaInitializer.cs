using Microsoft.EntityFrameworkCore;

namespace TemplarCMS.Persistence.Security;

/// <summary>Additive SQLite provisioning. SQL Server remains operator-provisioned.</summary>
public static class IdentitySchemaInitializer
{
    public static async Task EnsureAsync(TemplarCmsDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsSqlite()) throw new ArgumentException("Identity schema initialization requires SQLite.", nameof(db));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS CmsIdentityUsers (
                Id TEXT NOT NULL CONSTRAINT PK_CmsIdentityUsers PRIMARY KEY,
                UserName TEXT NULL, NormalizedUserName TEXT NULL,
                Email TEXT NULL, NormalizedEmail TEXT NULL, EmailConfirmed INTEGER NOT NULL,
                PasswordHash TEXT NULL, SecurityStamp TEXT NULL, ConcurrencyStamp TEXT NULL,
                PhoneNumber TEXT NULL, PhoneNumberConfirmed INTEGER NOT NULL,
                TwoFactorEnabled INTEGER NOT NULL, LockoutEnd TEXT NULL,
                LockoutEnabled INTEGER NOT NULL, AccessFailedCount INTEGER NOT NULL,
                MustChangePassword INTEGER NOT NULL,
                CONSTRAINT FK_CmsIdentityUsers_DirectoryUsers_Id FOREIGN KEY (Id) REFERENCES DirectoryUsers (Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS UserNameIndex ON CmsIdentityUsers (NormalizedUserName);
            CREATE INDEX IF NOT EXISTS EmailIndex ON CmsIdentityUsers (NormalizedEmail);
            CREATE TABLE IF NOT EXISTS CmsIdentityUserClaims (
                Id INTEGER NOT NULL CONSTRAINT PK_CmsIdentityUserClaims PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL, ClaimType TEXT NULL, ClaimValue TEXT NULL,
                CONSTRAINT FK_CmsIdentityUserClaims_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES CmsIdentityUsers (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_CmsIdentityUserClaims_UserId ON CmsIdentityUserClaims (UserId);
            CREATE TABLE IF NOT EXISTS CmsIdentityUserLogins (
                LoginProvider TEXT NOT NULL, ProviderKey TEXT NOT NULL, ProviderDisplayName TEXT NULL, UserId TEXT NOT NULL,
                CONSTRAINT PK_CmsIdentityUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
                CONSTRAINT FK_CmsIdentityUserLogins_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES CmsIdentityUsers (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_CmsIdentityUserLogins_UserId ON CmsIdentityUserLogins (UserId);
            CREATE TABLE IF NOT EXISTS CmsIdentityUserTokens (
                UserId TEXT NOT NULL, LoginProvider TEXT NOT NULL, Name TEXT NOT NULL, Value TEXT NULL,
                CONSTRAINT PK_CmsIdentityUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
                CONSTRAINT FK_CmsIdentityUserTokens_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES CmsIdentityUsers (Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS AdminProvisioning (
                Id INTEGER NOT NULL CONSTRAINT PK_AdminProvisioning PRIMARY KEY,
                UserId TEXT NOT NULL, CreatedAt TEXT NOT NULL,
                CONSTRAINT CK_AdminProvisioning_Singleton CHECK (Id = 1),
                CONSTRAINT FK_AdminProvisioning_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES CmsIdentityUsers (Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_AdminProvisioning_UserId ON AdminProvisioning (UserId);
            """, ct);
        await transaction.CommitAsync(ct);
    }
}
