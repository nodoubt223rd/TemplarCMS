-- Apply after 002-user-directory.sql and the approved 006-preflight-1a.sql baseline.
-- Adds credential storage only; this script does not provision an account.
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
    THROW 51000, 'Apply the reviewed schema baseline before 007-admin-identity.sql.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionKey = N'2026-09-tighten-columns-and-indexes-v1')
    THROW 51000, 'The required schema baseline version is missing.', 1;
IF OBJECT_ID(N'dbo.DirectoryUsers', N'U') IS NULL
    THROW 51000, 'Apply 002-user-directory.sql before 007-admin-identity.sql.', 1;

BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.CmsIdentityUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CmsIdentityUsers (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CmsIdentityUsers PRIMARY KEY,
        UserName nvarchar(256) NULL, NormalizedUserName nvarchar(256) NULL,
        Email nvarchar(254) NULL, NormalizedEmail nvarchar(254) NULL,
        EmailConfirmed bit NOT NULL, PasswordHash nvarchar(max) NULL,
        SecurityStamp nvarchar(max) NULL, ConcurrencyStamp nvarchar(max) NULL,
        PhoneNumber nvarchar(max) NULL, PhoneNumberConfirmed bit NOT NULL,
        TwoFactorEnabled bit NOT NULL, LockoutEnd datetimeoffset(7) NULL,
        LockoutEnabled bit NOT NULL, AccessFailedCount int NOT NULL,
        MustChangePassword bit NOT NULL,
        CONSTRAINT FK_CmsIdentityUsers_DirectoryUsers_Id FOREIGN KEY (Id) REFERENCES dbo.DirectoryUsers(Id)
    );
END;
IF OBJECT_ID(N'dbo.CmsIdentityUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CmsIdentityUserClaims (
        Id int IDENTITY NOT NULL CONSTRAINT PK_CmsIdentityUserClaims PRIMARY KEY,
        UserId uniqueidentifier NOT NULL, ClaimType nvarchar(max) NULL, ClaimValue nvarchar(max) NULL,
        CONSTRAINT FK_CmsIdentityUserClaims_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.CmsIdentityUsers(Id) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'dbo.CmsIdentityUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CmsIdentityUserLogins (
        LoginProvider nvarchar(128) NOT NULL, ProviderKey nvarchar(128) NOT NULL,
        ProviderDisplayName nvarchar(max) NULL, UserId uniqueidentifier NOT NULL,
        CONSTRAINT PK_CmsIdentityUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_CmsIdentityUserLogins_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.CmsIdentityUsers(Id) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'dbo.CmsIdentityUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CmsIdentityUserTokens (
        UserId uniqueidentifier NOT NULL, LoginProvider nvarchar(128) NOT NULL,
        Name nvarchar(128) NOT NULL, Value nvarchar(max) NULL,
        CONSTRAINT PK_CmsIdentityUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_CmsIdentityUserTokens_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.CmsIdentityUsers(Id) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'dbo.AdminProvisioning', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminProvisioning (
        Id int NOT NULL CONSTRAINT PK_AdminProvisioning PRIMARY KEY,
        UserId uniqueidentifier NOT NULL, CreatedAt datetimeoffset(7) NOT NULL,
        CONSTRAINT CK_AdminProvisioning_Singleton CHECK (Id = 1),
        CONSTRAINT FK_AdminProvisioning_CmsIdentityUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.CmsIdentityUsers(Id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CmsIdentityUsers') AND name = N'UserNameIndex')
    CREATE UNIQUE INDEX UserNameIndex ON dbo.CmsIdentityUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CmsIdentityUsers') AND name = N'EmailIndex')
    CREATE INDEX EmailIndex ON dbo.CmsIdentityUsers(NormalizedEmail);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CmsIdentityUserClaims') AND name = N'IX_CmsIdentityUserClaims_UserId')
    CREATE INDEX IX_CmsIdentityUserClaims_UserId ON dbo.CmsIdentityUserClaims(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CmsIdentityUserLogins') AND name = N'IX_CmsIdentityUserLogins_UserId')
    CREATE INDEX IX_CmsIdentityUserLogins_UserId ON dbo.CmsIdentityUserLogins(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AdminProvisioning') AND name = N'IX_AdminProvisioning_UserId')
    CREATE UNIQUE INDEX IX_AdminProvisioning_UserId ON dbo.AdminProvisioning(UserId);
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionKey = N'2026-10-admin-identity-v1')
    INSERT INTO dbo.SchemaVersions(VersionKey, ScriptName, Notes)
    VALUES(N'2026-10-admin-identity-v1', N'007-admin-identity.sql', N'Local identity storage and singleton admin provisioning marker; no account seeded');
COMMIT TRANSACTION;
