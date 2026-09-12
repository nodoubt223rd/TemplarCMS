/*
   TemplarCMS SQL Server bootstrap schema (improved).

   This script is idempotent and is intended for a new SQL Server deployment.
   It creates only runtime persistence tables; source-controlled templates and
   starter content are initialized by the application after startup.

   Run against: templarcms_authoring and templarcms_published
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[dbo].[ContentItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContentItems]
    (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(255) NOT NULL,
        [Key] nvarchar(450) NOT NULL,
        [TemplateId] uniqueidentifier NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [Icon] nvarchar(255) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_ContentItems_IsDeleted] DEFAULT (0),
        [CreatedUtc] datetimeoffset NOT NULL CONSTRAINT [DF_ContentItems_CreatedUtc] DEFAULT (sysdatetimeoffset()),
        [ModifiedUtc] datetimeoffset NOT NULL CONSTRAINT [DF_ContentItems_ModifiedUtc] DEFAULT (sysdatetimeoffset()),
        CONSTRAINT [PK_ContentItems] PRIMARY KEY ([Id])
    );
END;

-- Idempotent column additions for existing deployments
IF COL_LENGTH(N'[dbo].[ContentItems]', N'Icon') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContentItems] ADD [Icon] nvarchar(255) NULL;
END;

IF COL_LENGTH(N'[dbo].[ContentItems]', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContentItems] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_ContentItems_IsDeleted] DEFAULT (0);
END;

IF COL_LENGTH(N'[dbo].[ContentItems]', N'CreatedUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContentItems] ADD [CreatedUtc] datetimeoffset NOT NULL CONSTRAINT [DF_ContentItems_CreatedUtc] DEFAULT (sysdatetimeoffset());
END;

IF COL_LENGTH(N'[dbo].[ContentItems]', N'ModifiedUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContentItems] ADD [ModifiedUtc] datetimeoffset NOT NULL CONSTRAINT [DF_ContentItems_ModifiedUtc] DEFAULT (sysdatetimeoffset());
END;

IF OBJECT_ID(N'[dbo].[ContentFieldValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContentFieldValues]
    (
        [Id] uniqueidentifier NOT NULL,
        [ItemId] uniqueidentifier NOT NULL,
        [FieldId] uniqueidentifier NOT NULL,
        [FieldKey] nvarchar(450) NOT NULL,
        [Language] nvarchar(450) NOT NULL,
        [Version] int NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_ContentFieldValues] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ContentFieldValues_ContentItems_ItemId'
)
BEGIN
    ALTER TABLE [dbo].[ContentFieldValues]
        ADD CONSTRAINT [FK_ContentFieldValues_ContentItems_ItemId]
        FOREIGN KEY ([ItemId]) REFERENCES [dbo].[ContentItems] ([Id]) ON DELETE CASCADE;
END;

IF OBJECT_ID(N'[dbo].[MediaAssets]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MediaAssets]
    (
        [Id] uniqueidentifier NOT NULL,
        [FolderId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [StoredFileName] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(255) NOT NULL,
        [Length] bigint NOT NULL,
        [AltText] nvarchar(500) NULL,
        [Title] nvarchar(255) NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentItems]')
      AND [name] = N'IX_ContentItems_TemplateId'
)
BEGIN
    CREATE INDEX [IX_ContentItems_TemplateId] ON [dbo].[ContentItems] ([TemplateId]);
END;

-- Unique Key among siblings (items sharing the same parent)
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentItems]')
      AND [name] = N'IX_ContentItems_ParentId_Key'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContentItems_ParentId_Key]
        ON [dbo].[ContentItems] ([ParentId], [Key])
        WHERE [ParentId] IS NOT NULL;
END;

-- Unique Key among root-level items (no parent)
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentItems]')
      AND [name] = N'IX_ContentItems_Root_Key'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContentItems_Root_Key]
        ON [dbo].[ContentItems] ([Key])
        WHERE [ParentId] IS NULL;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentFieldValues]')
      AND [name] = N'IX_ContentFieldValues_ItemId_FieldId_Language_Version'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContentFieldValues_ItemId_FieldId_Language_Version]
        ON [dbo].[ContentFieldValues] ([ItemId], [FieldId], [Language], [Version]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentFieldValues]')
      AND [name] = N'IX_ContentFieldValues_FieldKey'
)
BEGIN
    CREATE INDEX [IX_ContentFieldValues_FieldKey] ON [dbo].[ContentFieldValues] ([FieldKey]);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[MediaAssets]')
      AND [name] = N'IX_MediaAssets_FolderId'
)
BEGIN
    CREATE INDEX [IX_MediaAssets_FolderId] ON [dbo].[MediaAssets] ([FolderId]);
END;

COMMIT TRANSACTION;
