# SQL migration validation

Reviewed script: `database/sqlserver/006-preflight-1a.sql`.
Validation date: September 13, 2026.

## Corrections

- Materialize normalized index metadata so validation and diagnostic queries can
  both use it; normalize both brackets and filter whitespace.
- Limit index discovery to dbo. Allow missing indexes to reach creation.
- Check index dependencies only for columns that actually require narrowing,
  covering all eight approved bounded-string columns.
- Build quoted constraint-removal commands in variables before execution.
- Set the SQL session options required for XML queries and filtered indexes.
- Reject ambient transactions and preserve SQL error numbers with THROW.
- Retain the existing backup gate, transactional version record, datetimeoffset
  columns, and UTC defaults. Backup confirmation remains disabled in the file.

## Verification performed

The full script passed SQL Server PARSEONLY validation. An extracted preflight
prefix passed against Salvatore / templarcms_authoring. The prefix excluded all
persistent schema/default/version changes and rolled back its temporary work.
In-memory test variants correctly rejected:

- Missing backup confirmation: 63000.
- Wrong target database: 60001.
- Incorrect index uniqueness: 64002.
- An indexed column requiring narrowing: 65001.

No migration DDL, data updates, or version records were executed. The published
database was not tested in this pass. Parse-only and preflight checks do not
prove that migration execution or application integration will succeed.

## Before deployment

Use the corrected `006-preflight-1a.sql`, not the obsolete
`006-preflight-plus-migration-template.sql` still present in this workspace.
Do not execute this folder as an indiscriminate sequence of SQL files.

The DB team must rehearse the full migration against a restored staging database,
including error rollback and repeat-version rejection, before approving target
execution. Confirm backup/restore evidence, stop application writes during the
maintenance window, and validate the resulting schema and application behavior.
An indexed column requiring narrowing intentionally stops for a reviewed,
transactional dependency-recreation plan; indexes are not dropped automatically.

Application length validation, timestamp maintenance, and startup schema
verification are now implemented in
[ADR-0008](decisions/ADR-0008-sql-policy-application-alignment.md), which also
records later rehearsal integration results. The verification above describes
the earlier SQL-script-only pass. File-path parameterization in the database
creation script remains outside this corrective patch.
