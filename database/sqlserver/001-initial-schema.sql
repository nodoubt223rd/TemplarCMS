/*
   TemplarCMS SQL Server bootstrap schema.

   This script is idempotent and is intended for a new SQL Server deployment.
   It creates only runtime persistence tables; source-controlled templates and
   starter content are initialized by the application after startup.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[dbo].[ContentItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContentItems]
    (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Key] nvarchar(450) NOT NULL,
        [TemplateId] uniqueidentifier NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [Icon] nvarchar(max) NULL,
        CONSTRAINT [PK_ContentItems] PRIMARY KEY ([Id])
    );
END;

IF COL_LENGTH(N'[dbo].[ContentItems]', N'Icon') IS NULL
BEGIN
    ALTER TABLE [dbo].[ContentItems] ADD [Icon] nvarchar(max) NULL;
END;

IF OBJECT_ID(N'[dbo].[ContentFieldValues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContentFieldValues]
    (
        [Id] uniqueidentifier NOT NULL,
        [ItemId] uniqueidentifier NOT NULL,
        [FieldId] uniqueidentifier NOT NULL,
        [FieldKey] nvarchar(max) NOT NULL,
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
        [FileName] nvarchar(max) NOT NULL,
        [StoredFileName] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NOT NULL,
        [Length] bigint NOT NULL,
        [AltText] nvarchar(max) NULL,
        [Title] nvarchar(max) NULL,
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

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ContentItems]')
      AND [name] = N'IX_ContentItems_ParentId_Key'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContentItems_ParentId_Key] ON [dbo].[ContentItems] ([ParentId], [Key]);
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
    WHERE [object_id] = OBJECT_ID(N'[dbo].[MediaAssets]')
      AND [name] = N'IX_MediaAssets_FolderId'
)
BEGIN
    CREATE INDEX [IX_MediaAssets_FolderId] ON [dbo].[MediaAssets] ([FolderId]);
END;

COMMIT TRANSACTION;
