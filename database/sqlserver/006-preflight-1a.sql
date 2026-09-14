/*
  Template: preflight + safe migration checks (updated)
  - Must set @CONFIRM_BACKUP_TAKEN = 1 after taking/validating backups to proceed.
  - Verifies target DB, runs prechecks, validates indexes, checks column dependencies,
    enforces UTC defaults for datetimeoffset columns, then performs safe ALTERs and records version.
  Usage:
    1) Run the audit scripts and resolve findings.
    2) Take and verify backups/restores.
    3) Edit variables below, set @CONFIRM_BACKUP_TAKEN = 1, reconnect to target DB, then execute.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;
-- Required by XML metadata queries and filtered-index creation, including under sqlcmd.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF @@TRANCOUNT <> 0
    THROW 60003, N'Run this migration in a dedicated session without an existing transaction.', 1;

-- ===== CONFIGURE =====
DECLARE
    @EXPECTED_DATABASE sysname = N'templarcms_authoring', -- set to templarcms_authoring or templarcms_published
    @MIGRATION_VERSION nvarchar(100) = N'2026-09-tighten-columns-and-indexes-v1',
    @MIGRATION_SCRIPT_NAME nvarchar(260) = N'006-preflight-1a.sql',
    @CONFIRM_BACKUP_TAKEN bit = 0;  -- MUST be set to 1 after backup/restore verification to proceed

-- ===== GUARD: correct DB CONTEXT and not re-applied =====
IF DB_NAME() <> @EXPECTED_DATABASE
    THROW 60001, N'Connected database does not match @EXPECTED_DATABASE. Reconnect to the target DB and re-run.', 1;

-- ===== PREFLIGHT: data length checks & duplicate checks =====
BEGIN TRY
    BEGIN TRANSACTION;

    -- Length prechecks
    IF EXISTS (SELECT 1 FROM dbo.ContentItems WHERE DATALENGTH([Name]) / 2 > 255)
        THROW 61001, N'Preflight failure: Some ContentItems.Name exceed 255 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.ContentItems WHERE [Icon] IS NOT NULL AND DATALENGTH([Icon]) / 2 > 255)
        THROW 61002, N'Preflight failure: Some ContentItems.Icon exceed 255 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.ContentFieldValues WHERE DATALENGTH([FieldKey]) / 2 > 450)
        THROW 61003, N'Preflight failure: Some ContentFieldValues.FieldKey exceed 450 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.MediaAssets WHERE DATALENGTH([FileName]) / 2 > 255)
        THROW 61004, N'Preflight failure: Some MediaAssets.FileName exceed 255 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.MediaAssets WHERE DATALENGTH([StoredFileName]) / 2 > 500)
        THROW 61005, N'Preflight failure: Some MediaAssets.StoredFileName exceed 500 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.MediaAssets WHERE DATALENGTH([ContentType]) / 2 > 255)
        THROW 61006, N'Preflight failure: Some MediaAssets.ContentType exceed 255 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.MediaAssets WHERE [AltText] IS NOT NULL AND DATALENGTH([AltText]) / 2 > 500)
        THROW 61007, N'Preflight failure: Some MediaAssets.AltText exceed 500 chars.', 1;
    IF EXISTS (SELECT 1 FROM dbo.MediaAssets WHERE [Title] IS NOT NULL AND DATALENGTH([Title]) / 2 > 255)
        THROW 61008, N'Preflight failure: Some MediaAssets.Title exceed 255 chars.', 1;

    -- Duplicate prechecks
    IF EXISTS (SELECT NormalizedEmail FROM dbo.DirectoryUsers GROUP BY NormalizedEmail HAVING COUNT(*) > 1)
        THROW 62001, N'Preflight failure: Duplicate DirectoryUsers.NormalizedEmail found.', 1;
    IF EXISTS (SELECT [Key] FROM dbo.ContentItems WHERE ParentId IS NULL GROUP BY [Key] HAVING COUNT(*) > 1)
        THROW 62002, N'Preflight failure: Duplicate root ContentItems.[Key] found.', 1;
    IF EXISTS (SELECT ParentId, [Key] FROM dbo.ContentItems WHERE ParentId IS NOT NULL GROUP BY ParentId, [Key] HAVING COUNT(*) > 1)
        THROW 62003, N'Preflight failure: Duplicate sibling ContentItems.[Key] found.', 1;
    IF EXISTS (SELECT ItemId, FieldId, Language, Version FROM dbo.ContentFieldValues GROUP BY ItemId, FieldId, Language, Version HAVING COUNT(*) > 1)
        THROW 62004, N'Preflight failure: Duplicate ContentFieldValues (ItemId,FieldId,Language,Version) found.', 1;

    -- ===== BACKUP PAUSE: explicit human confirmation REQUIRED =====
    PRINT N'PRECHECKS PASSED. TAKE A FULL BACKUP AND PERFORM A RESTORE TEST BEFORE PROCEEDING.';
    IF @CONFIRM_BACKUP_TAKEN = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 63000, N'Backup confirmation required. Set @CONFIRM_BACKUP_TAKEN = 1 after you have verified backup/restore, then re-run.', 1;
    END;

    -- ===== INDEX VERIFICATION =====
    IF OBJECT_ID('tempdb..#IndexVerification') IS NOT NULL DROP TABLE #IndexVerification;
    IF OBJECT_ID('tempdb..#NormalizedIndexes') IS NOT NULL DROP TABLE #NormalizedIndexes;
    IF OBJECT_ID('tempdb..#ExpectedIndexes') IS NOT NULL DROP TABLE #ExpectedIndexes;
    CREATE TABLE #ExpectedIndexes
    (
        TableName sysname NOT NULL,
        IndexName sysname NOT NULL,
        IsUnique bit NOT NULL,
        ColumnList nvarchar(1000) NOT NULL, -- comma-separated key columns in order (no brackets)
        IncludedColumnList nvarchar(1000) NULL,
        FilterDefinition nvarchar(1000) NULL
    );

    INSERT INTO #ExpectedIndexes (TableName, IndexName, IsUnique, ColumnList, IncludedColumnList, FilterDefinition)
    VALUES
      ('ContentItems', 'IX_ContentItems_ParentId_Key', 1, 'ParentId,Key', NULL, 'ParentId IS NOT NULL'),
      ('ContentItems', 'IX_ContentItems_Root_Key', 1, 'Key', NULL, 'ParentId IS NULL'),
      ('ContentItems', 'IX_ContentItems_TemplateId', 0, 'TemplateId', NULL, NULL),
      ('ContentFieldValues', 'IX_ContentFieldValues_ItemId_FieldId_Language_Version', 1, 'ItemId,FieldId,Language,Version', NULL, NULL),
      ('ContentFieldValues', 'IX_ContentFieldValues_FieldKey', 0, 'FieldKey', NULL, NULL),
      ('MediaAssets', 'IX_MediaAssets_FolderId', 0, 'FolderId', NULL, NULL),
      ('DirectoryUsers', 'IX_DirectoryUsers_NormalizedEmail', 1, 'NormalizedEmail', NULL, NULL);

    ;WITH idx AS
    (
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            i.index_id,
            i.name AS IndexName,
            i.is_unique,
            i.is_disabled,
            i.filter_definition,
            STUFF((
                SELECT ',' + c2.name
                FROM sys.index_columns ic2
                JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id
                WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0
                ORDER BY ic2.key_ordinal
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'') AS KeyCols,
            STUFF((
                SELECT ',' + c3.name
                FROM sys.index_columns ic3
                JOIN sys.columns c3 ON c3.object_id = ic3.object_id AND c3.column_id = ic3.column_id
                WHERE ic3.object_id = i.object_id AND ic3.index_id = i.index_id AND ic3.is_included_column = 1
                ORDER BY ic3.index_column_id
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'),1,1,'') AS IncludedCols
        FROM sys.indexes i
        JOIN sys.objects t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N'dbo' AND t.name IN ('ContentItems','ContentFieldValues','MediaAssets','DirectoryUsers')
    )
    SELECT e.TableName, e.IndexName, e.IsUnique, e.ColumnList, e.IncludedColumnList, e.FilterDefinition,
           ix.IndexName AS ExistingIndexName,
           ix.is_unique AS ExistingIsUnique,
           ix.is_disabled AS ExistingIsDisabled,
           ix.filter_definition AS ExistingFilter,
           ix.KeyCols AS ExistingKeyCols,
           ix.IncludedCols AS ExistingIncludedCols
    INTO #IndexVerification
    FROM #ExpectedIndexes e
    LEFT JOIN idx ix ON ix.TableName = e.TableName AND ix.IndexName = e.IndexName;

    -- If missing: report but DO NOT abort (will create missing indexes later)
    IF EXISTS (SELECT 1 FROM #IndexVerification WHERE ExistingIndexName IS NULL)
    BEGIN
        SELECT 'MissingIndex' AS Issue, * FROM #IndexVerification WHERE ExistingIndexName IS NULL;
    END;

    -- Normalize comparison: remove brackets/parentheses and whitespace, compare case-insensitive
    SELECT *,
            LOWER(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(ExistingKeyCols,''),'[',''),']',''),' ',''),CHAR(9),'')) AS NormExistingKeyCols,
            LOWER(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(ColumnList,''),'[',''),']',''),' ',''),CHAR(9),'')) AS NormExpectedKeyCols,
            LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(ExistingFilter,''),'(',''),')',''),'[',''),']',''),' ',''),CHAR(9),''),CHAR(13),''),CHAR(10),'')) AS NormExistingFilter,
            LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(FilterDefinition,''),'(',''),')',''),'[',''),']',''),' ',''),CHAR(9),''),CHAR(13),''),CHAR(10),'')) AS NormExpectedFilter
    INTO #NormalizedIndexes
    FROM #IndexVerification;
    -- Abort if mismatched definitions or disabled or uniqueness mismatch
    IF EXISTS (
        SELECT 1 FROM #NormalizedIndexes
        WHERE ExistingIndexName IS NOT NULL
          AND (ExistingIsUnique <> IsUnique OR ExistingIsDisabled = 1
               OR NormExistingKeyCols <> NormExpectedKeyCols
               OR (ISNULL(NormExistingFilter,'') <> ISNULL(NormExpectedFilter,'')))
    )
    BEGIN
        SELECT 'IndexMismatch' AS Issue, * FROM #NormalizedIndexes
        WHERE ExistingIndexName IS NOT NULL
          AND (ExistingIsUnique <> IsUnique OR ExistingIsDisabled = 1
               OR NormExistingKeyCols <> NormExpectedKeyCols
               OR (ISNULL(NormExistingFilter,'') <> ISNULL(NormExpectedFilter,'')));
        ROLLBACK TRANSACTION;
        THROW 64002, N'Index definition mismatches detected. Fix indexes (recreate/enable) before proceeding.', 1;
    END;

    -- ===== COLUMN DEPENDENCY CHECKS =====
    -- Only changed columns require dependency review; an already compliant indexed column is safe.
    IF OBJECT_ID('tempdb..#NarrowedColumns') IS NOT NULL DROP TABLE #NarrowedColumns;
    SELECT c.object_id, c.column_id, t.name AS TableName, c.name AS ColumnName
    INTO #NarrowedColumns
    FROM (VALUES
        (N'ContentItems', N'Name', 255), (N'ContentItems', N'Icon', 255),
        (N'ContentFieldValues', N'FieldKey', 450),
        (N'MediaAssets', N'FileName', 255), (N'MediaAssets', N'StoredFileName', 500),
        (N'MediaAssets', N'ContentType', 255), (N'MediaAssets', N'AltText', 500),
        (N'MediaAssets', N'Title', 255)
    ) targets(TableName, ColumnName, TargetLength)
    JOIN sys.tables t ON t.name = targets.TableName AND t.schema_id = SCHEMA_ID(N'dbo')
    JOIN sys.columns c ON c.object_id = t.object_id AND c.name = targets.ColumnName
    WHERE c.max_length = -1 OR c.max_length > targets.TargetLength * 2;

    IF EXISTS (SELECT 1 FROM #NarrowedColumns c
        JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id)
    BEGIN
        SELECT 'ColumnDependency' AS Issue, N'dbo' AS SchemaName, c.TableName, i.name AS IndexName, c.ColumnName, ic.is_included_column
        FROM #NarrowedColumns c
        JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id;

        ROLLBACK TRANSACTION;
        THROW 65001, N'An indexed column requires narrowing. Review a transactional index recreation plan before proceeding; do not drop indexes outside that plan.', 1;
    END;

    -- ===== CREATE SchemaVersions table (inside the confirmed transaction) =====
    IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchemaVersions
        (
            VersionKey nvarchar(100) NOT NULL PRIMARY KEY,
            AppliedOn datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
            AppliedBy sysname NOT NULL DEFAULT SUSER_SNAME(),
            ScriptName nvarchar(260) NOT NULL,
            Notes nvarchar(max) NULL
        );
    END;

    -- Prevent re-application (double-check inside transaction)
    IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionKey = @MIGRATION_VERSION)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 60002, N'This migration version has already been applied to this database. Aborting.', 1;
    END;

    -- ===== SCHEMA CHANGES: safe ALTERs (executed only after all checks passed) =====

    -- ALTER: ContentItems.Name -> nvarchar(255)
    IF EXISTS (
        SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'Name'
          AND (max_length = -1 OR max_length > 255 * 2)
    )
    BEGIN
        ALTER TABLE dbo.ContentItems ALTER COLUMN [Name] nvarchar(255) NOT NULL;
    END;

    -- ALTER: ContentItems.Icon -> nvarchar(255) NULL
    IF EXISTS (
        SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'Icon'
          AND (max_length = -1 OR max_length > 255 * 2)
    )
    BEGIN
        ALTER TABLE dbo.ContentItems ALTER COLUMN [Icon] nvarchar(255) NULL;
    END;

    -- ALTER: ContentFieldValues.FieldKey -> nvarchar(450)
    IF EXISTS (
        SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ContentFieldValues') AND name = N'FieldKey'
          AND (max_length = -1 OR max_length > 450 * 2)
    )
    BEGIN
        ALTER TABLE dbo.ContentFieldValues ALTER COLUMN [FieldKey] nvarchar(450) NOT NULL;
    END;

    -- ALTER: MediaAssets FileName/StoredFileName/ContentType/AltText/Title
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'FileName' AND (max_length = -1 OR max_length > 255 * 2))
    BEGIN
        ALTER TABLE dbo.MediaAssets ALTER COLUMN [FileName] nvarchar(255) NOT NULL;
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'StoredFileName' AND (max_length = -1 OR max_length > 500 * 2))
    BEGIN
        ALTER TABLE dbo.MediaAssets ALTER COLUMN [StoredFileName] nvarchar(500) NOT NULL;
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'ContentType' AND (max_length = -1 OR max_length > 255 * 2))
    BEGIN
        ALTER TABLE dbo.MediaAssets ALTER COLUMN [ContentType] nvarchar(255) NOT NULL;
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'AltText' AND (max_length = -1 OR max_length > 500 * 2))
    BEGIN
        ALTER TABLE dbo.MediaAssets ALTER COLUMN [AltText] nvarchar(500) NULL;
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'Title' AND (max_length = -1 OR max_length > 255 * 2))
    BEGIN
        ALTER TABLE dbo.MediaAssets ALTER COLUMN [Title] nvarchar(255) NULL;
    END;

    -- ===== TIMESTAMP DEFAULTS: keep datetimeoffset columns; ensure UTC default =====
    -- CreatedUtc
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'CreatedUtc')
    BEGIN
        -- Ensure column is datetimeoffset; if not, fail and instruct manual conversion
        IF NOT EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'CreatedUtc' AND TYPE_NAME(user_type_id) LIKE '%datetimeoffset%'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 67001, N'dbo.ContentItems.CreatedUtc is not datetimeoffset. Manual review/conversion required before running this migration.', 1;
        END;

        -- Drop existing default constraint if present, then add UTC datetimeoffset default
        DECLARE @dcname_created sysname = NULL;
        SELECT @dcname_created = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID(N'dbo.ContentItems') AND c.name = 'CreatedUtc';

        IF @dcname_created IS NOT NULL
        BEGIN
            DECLARE @drop_created nvarchar(max) = N'ALTER TABLE dbo.ContentItems DROP CONSTRAINT ' + QUOTENAME(@dcname_created);
            EXEC sys.sp_executesql @drop_created;
        END;

        IF NOT EXISTS (
            SELECT 1 FROM sys.default_constraints dc
            JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
            WHERE c.object_id = OBJECT_ID(N'dbo.ContentItems') AND c.name = 'CreatedUtc'
        )
        BEGIN
            ALTER TABLE dbo.ContentItems ADD CONSTRAINT DF_ContentItems_CreatedUtc DEFAULT (SYSUTCDATETIME() AT TIME ZONE 'UTC') FOR CreatedUtc;
        END;
    END;

    -- ModifiedUtc: similar handling, keep as datetimeoffset and add UTC default if missing
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'ModifiedUtc')
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'ModifiedUtc' AND TYPE_NAME(user_type_id) LIKE '%datetimeoffset%'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 67002, N'dbo.ContentItems.ModifiedUtc is not datetimeoffset. Manual review/conversion required before running this migration.', 1;
        END;

        DECLARE @dcname_modified sysname = NULL;
        SELECT @dcname_modified = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID(N'dbo.ContentItems') AND c.name = 'ModifiedUtc';

        IF @dcname_modified IS NOT NULL
        BEGIN
            DECLARE @drop_modified nvarchar(max) = N'ALTER TABLE dbo.ContentItems DROP CONSTRAINT ' + QUOTENAME(@dcname_modified);
            EXEC sys.sp_executesql @drop_modified;
        END;

        IF NOT EXISTS (
            SELECT 1 FROM sys.default_constraints dc
            JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
            WHERE c.object_id = OBJECT_ID(N'dbo.ContentItems') AND c.name = 'ModifiedUtc'
        )
        BEGIN
            ALTER TABLE dbo.ContentItems ADD CONSTRAINT DF_ContentItems_ModifiedUtc DEFAULT (SYSUTCDATETIME() AT TIME ZONE 'UTC') FOR ModifiedUtc;
        END;
    END;

    -- ===== INDEX CREATION (missing indexes will be created now) =====
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'IX_ContentItems_TemplateId')
        CREATE INDEX IX_ContentItems_TemplateId ON dbo.ContentItems (TemplateId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'IX_ContentItems_ParentId_Key')
        CREATE UNIQUE INDEX IX_ContentItems_ParentId_Key ON dbo.ContentItems (ParentId, [Key]) WHERE ParentId IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ContentItems') AND name = N'IX_ContentItems_Root_Key')
        CREATE UNIQUE INDEX IX_ContentItems_Root_Key ON dbo.ContentItems ([Key]) WHERE ParentId IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ContentFieldValues') AND name = N'IX_ContentFieldValues_ItemId_FieldId_Language_Version')
        CREATE UNIQUE INDEX IX_ContentFieldValues_ItemId_FieldId_Language_Version ON dbo.ContentFieldValues (ItemId, FieldId, Language, Version);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ContentFieldValues') AND name = N'IX_ContentFieldValues_FieldKey')
        CREATE INDEX IX_ContentFieldValues_FieldKey ON dbo.ContentFieldValues (FieldKey);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MediaAssets') AND name = N'IX_MediaAssets_FolderId')
        CREATE INDEX IX_MediaAssets_FolderId ON dbo.MediaAssets (FolderId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DirectoryUsers') AND name = N'IX_DirectoryUsers_NormalizedEmail')
        CREATE UNIQUE INDEX IX_DirectoryUsers_NormalizedEmail ON dbo.DirectoryUsers (NormalizedEmail);

    -- ===== RECORD VERSION & COMMIT =====
    INSERT INTO dbo.SchemaVersions (VersionKey, ScriptName, Notes)
    VALUES (@MIGRATION_VERSION, @MIGRATION_SCRIPT_NAME, N'Tightened columns, created/verified indexes, ensured UTC datetimeoffset defaults');

    COMMIT TRANSACTION;
    PRINT N'Migration applied and version recorded: ' + @MIGRATION_VERSION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
