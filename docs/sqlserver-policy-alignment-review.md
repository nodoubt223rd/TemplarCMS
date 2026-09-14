# SQL Server policy alignment: DB team review

## Purpose and verification scope

This is the historical DB-team review. The implemented application decisions
and current validation boundary are recorded in
[ADR-0008](decisions/ADR-0008-sql-policy-application-alignment.md).

Treat the DB team's schema updates as intentional company-policy requirements,
not changes to revert. This list identifies application alignment work and asks
for decisions where the intended behavior is not yet defined.

Read-only verification on September 12, 2026 compared the checked-in SQL scripts,
the EF model, and live metadata in `Salvatore` / `templarcms_authoring`.
The working server name is `Salvatore`, not `Salvatore\raide`.
No database data, schema, permissions, or application configuration was changed.
The published database was not inspected.

## Confirmed and resolved

- The authoring database contains ContentItems, ContentFieldValues, MediaAssets,
  and DirectoryUsers. Inspected column definitions match the current scripts.
- The expected indexes are present, including root-key uniqueness and normalized
  directory-email uniqueness. The content-field foreign key uses DELETE CASCADE.
- `IIS APPPOOL\TemplarCMS.api` now belongs only to `db_datareader` and
  `db_datawriter` among the inspected database-role memberships.
- Session impersonation of that database user confirms SELECT, INSERT, UPDATE,
  and DELETE permissions on sampled application objects, with database ALTER
  and CREATE TABLE denied. This is not an end-to-end IIS connection test or a
  complete server-level permission audit.
- Directory-user column lengths already align with the EF model. No exception
  to those limits is requested.

## 1. Approve bounded strings as application contracts

The following limits exist in the live database and scripts but are not declared
as maximum lengths in the corresponding EF properties:

| Table | Column | SQL limit |
| --- | --- | --- |
| ContentItems | Name | nvarchar(255) |
| ContentItems | Icon | nvarchar(255) |
| ContentFieldValues | FieldKey | nvarchar(450) |
| MediaAssets | FileName | nvarchar(255) |
| MediaAssets | StoredFileName | nvarchar(500) |
| MediaAssets | ContentType | nvarchar(255) |
| MediaAssets | AltText | nvarchar(500) |
| MediaAssets | Title | nvarchar(255) |

**Requested decision:** Confirm these as the supported product limits, especially
the author-facing name, title, and alternative-text limits. We propose adapting
the application rather than requesting unlimited SQL columns.

**Application work:** Add matching EF mappings and boundary validation, expose
useful validation errors, and test exact-limit and over-limit input. EF length
metadata alone does not provide request validation. Audit existing source data
before migration; reject/report oversized values rather than silently truncate.
For nvarchar, account for UTF-16 storage units, including supplementary characters.

## 2. Define deletion policy before treating IsDeleted as functional

SQL adds `ContentItems.IsDeleted NOT NULL DEFAULT 0`. The persistence entity does
not map it, reads do not filter it, and `EfContentRepository.DeleteItemAsync`
physically removes rows. The field-value foreign key cascades physical deletion.

**Requested decision:** Is the column reserved for future use, or must this release
implement soft deletion? If mandatory, approve retention/purge rules, restore
behavior, descendant handling, and whether deleted keys can be reused.

**Application work:** Implement those semantics explicitly if required. Adding a
column alone is not soft deletion. Existing unique indexes include deleted rows;
do not change their filters until key-reuse and restore rules are agreed.

## 3. Assign timestamp ownership and UTC semantics

SQL adds CreatedUtc and ModifiedUtc with `sysdatetimeoffset()` defaults. Neither
property is mapped by the content-item EF entity. No ContentItems trigger was
found. The defaults populate inserts; they do not advance ModifiedUtc on updates.
The expression preserves the server's offset rather than guaranteeing +00:00.

**Requested decision:** Should the application or database own modification
timestamps? Does policy require a UTC-normalized +00:00 value or an offset-aware
instant? Define whether field-only saves also update the parent item's timestamp.

**Recommendation:** Use an explicit UTC timestamp contract and one authoritative
writer. If DB-managed triggers are required, coordinate trigger-aware EF save
configuration and integration tests before deployment. Do not present these
columns as a complete security audit trail.

## 4. Keep indexes, align the model, and define comparison behavior

SQL adds a unique root Key index filtered to ParentId IS NULL, alongside the
sibling index filtered to ParentId IS NOT NULL. EF currently declares only the
composite sibling index. SQL also adds a nonunique FieldKey index absent from EF.

**Requested decision:** Confirm these indexes as the SQL baseline. We propose
retaining them and aligning provider-specific EF metadata and schema tests.
Root-key uniqueness is consistent with preventing duplicate root keys.

The live database collation is `SQL_Latin1_General_CP1_CI_AS`. Confirm it as the
approved baseline; test key/email comparisons against application normalization,
including case, accents, and whitespace. No collation change is requested now.

## 5. Approve a versioned, DB-owned schema upgrade process

The initial script creates bounded columns for fresh tables but does not narrow
existing columns. Its index guards check names, not definitions, so rerunning it
does not replace an older same-named index. Fresh and upgraded databases can differ.
The database-creation script also hardcodes this machine's SQL data-file paths.

**Requested decision:** Use reviewed incremental upgrade scripts and a schema
version/preflight check; parameterize deployment-specific data paths. Require
data-length and duplicate checks before tightening constraints, with an agreed
backup and rollback plan. Do not grant the runtime account DDL permissions.

**Application work:** For SQL Server, replace the unconditional startup
EnsureCreated call with verification of the externally provisioned schema.
EnsureCreated is not a schema-upgrade mechanism; missing tables must produce a
clear deployment error rather than an attempted runtime schema creation.

## Cutover acceptance

- Resolve whether soft deletion and maintained timestamps are required now or
  explicitly deferred; approve the bounded-string contracts.
- Align application mappings/validation and add SQL Server regression coverage.
- Verify the schema under the actual IIS identity and run approved write smoke
  tests. Permission checks alone do not prove successful application saves.
- Keep the published database outside this cutover until publishing storage and
  access boundaries are agreed. The initial script's instruction to run on both
  databases does not itself establish that architecture.

## Evidence in this repository

- `database/sqlserver/000-initial-database-setup.sql`
- `database/sqlserver/001-initial-schema.sql`
- `database/sqlserver/002-user-directory.sql`
- `src/TemplarCMS.Persistence/TemplarCmsDbContext.cs`
- `src/TemplarCMS.Persistence/Content/PersistenceContentItem.cs`
- `src/TemplarCMS.Persistence/Content/EfContentRepository.cs`
- `src/TemplarCMS.Api/Bootstrap/DefaultContentBootstrapHostedService.cs`
