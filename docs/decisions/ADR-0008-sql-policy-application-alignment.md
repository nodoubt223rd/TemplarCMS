# SQL policy application alignment

Status: Implemented for testing; no application cutover authorized by this ADR.

## Contracts

The approved ContentItems, ContentFieldValues, and MediaAssets string bounds are
product limits. Shared domain constants, domain/media validation, EF metadata,
and a SaveChanges validation backstop reject oversized strings without truncation.
Lengths count UTF-16 units, including trailing whitespace before normalization.
The existing content-name and template-field-key editors enforce their bounds;
the media picker currently selects assets, not uploads them. Future upload UI
must apply the same filename/title/alternative-text bounds.

## Timestamps and deletion

Content CreatedUtc is application-generated UTC on insert and immutable on update.
ModifiedUtc advances for item metadata changes and field saves, including field-only
saves. Field values and the owning item's timestamp share the SaveChanges
transaction. SQL datetimeoffset columns remain unchanged. IsDeleted is reserved;
hard-delete behavior and foreign-key cascades are intentionally unchanged.

SQLite bootstrap adds missing timestamp columns to existing installations. Legacy
timestamps use DateTimeOffset.MinValue to mean unknown rather than inventing
historical dates. New writes receive actual UTC times. These columns are not a
security audit trail or an API concurrency token.

## Schema ownership

SQL startup performs read-only verification before seeding and never calls
EnsureCreated or Migrate. It requires the reviewed migration version, approved
database/column collation, mapped column types/lengths/nullability, timestamp
precision, primary keys, and expected indexes. Extra policy columns are allowed.
The SQL model includes the root unique index and the nonunique FieldKey index.
Failure gives a deployment error, not an attempt to grant rights or create schema.

SQLite retains local bootstrap behavior. SQL inspection is an early compatibility
check, not a general migration engine or a proof of every constraint/default.

## Comparison and verification

Preserve accents and the SQL_Latin1_General_CP1_CI_AS baseline. No accent stripping
or collation changes are introduced. SQL linguistic comparisons and .NET ordinal
comparisons are not identical for every Unicode input; tests cover the approved
case/accent examples without claiming universal equivalence.

Integration tests opt into a rehearsal database through the process-only variable
TEMPLARCMS_SQL_REHEARSAL_CONNECTION. The database name must start with
templarcms_rehearsal_. Schema/comparison checks are read-only; repository write
tests use explicit transactions and roll back. Never point these tests at a source
database or change the checked-in SQLite configuration for test execution.

Application HTTP/UI smoke testing against isolated SQL-backed staging remains a
separate release checkpoint. Backups do not migrate media files or JSON templates.

## Verification record

September 13, 2026: 138 API, 54 application, 222 content-modeling, and 16
integration tests passed. The integration suite passed with each of the two
migrated rehearsal databases; its SQL write checks were rolled back. Frontend
tests passed (107), and the production frontend build passed with the existing
bundle-size warning. The Domain.Tests project currently contains no tests and
reported no tests discovered; domain limit checks are covered in integration.
No source database migration, IIS deployment, or SQLite configuration change was
performed in this application-alignment pass.
