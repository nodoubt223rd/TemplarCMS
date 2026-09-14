# SQL Server backup and migration rehearsal

## Scope and approval

Use `scripts/sqlserver-migration-rehearsal.ps1` to back up one current database,
restore a separate copy on the same SQL instance, migrate only that copy, and
capture evidence. Run authoring and published separately. The script NEVER
migrates or restores over the source database. Target deployment is a separate
approval after application tests pass.

Prerequisites:

- PowerShell 7.4 or later on Windows and System.Data.SqlClient available.
- A DB-team Windows identity permitted to back up, restore/create databases,
  run CHECKDB, and execute schema changes. Do not grant these rights to IIS.
- Approved maintenance/resource window: backups and CHECKDB consume I/O, CPU,
  and disk space even though source schema/data are not modified.
- Existing backup/data/log directories accessible to the SQL Server service
  account, with capacity for a full backup, restored files, and CHECKDB work.
  These paths are on the SQL host, not necessarily the PowerShell host.
- An encrypted connection with a trusted SQL certificate. For the approved local
  test instance only, `-TrustServerCertificate` explicitly bypasses certificate
  validation while keeping encryption enabled; do not use it by default.
- Restrict access to backups, restored databases, and log directories: they
  contain production information. Restores retain source database users and
  permissions. Use an isolated instance instead if company policy requires it;
  this same-instance helper does not provide isolation or anonymization.
- Review the migration and rerun audits before proceeding. TDE/encrypted backups,
  FILESTREAM, and cross-instance transfer require a separate DBA plan.

## Preview and execute

Replace the example directory paths with DB-team-approved locations. Preview
validates arguments and the migration contract without connecting to SQL.

```powershell
$run = @{
    Server = 'Salvatore'
    SourceDatabase = 'templarcms_authoring'
    BackupDirectory = 'D:\SqlBackups'
    RestoreDataDirectory = 'D:\SqlRehearsal\Data'
    RestoreLogDirectory = 'D:\SqlRehearsal\Log'
    OutputDirectory = 'E:\SqlDeploymentEvidence'
}
.\scripts\sqlserver-migration-rehearsal.ps1 @run
.\scripts\sqlserver-migration-rehearsal.ps1 @run -Execute -WhatIf

# Execute only after DB-team approval of paths, capacity, and maintenance window.
.\scripts\sqlserver-migration-rehearsal.ps1 @run -Execute -Confirm
```

For published, change SourceDatabase to `templarcms_published` and repeat after
reviewing the authoring results. Every execution creates a uniquely named
`templarcms_rehearsal_*` database and unique backup/file paths. Existing staging
databases are never reused or overwritten. A failed run leaves artifacts for
diagnosis; use a new run after remediation. Cleanup is a separate DBA action.

## Sequence and evidence

1. Validate source existence and absence of the generated restore target.
2. Make a COPY_ONLY full backup with CHECKSUM and stop-on-error behavior.
3. Run VERIFYONLY, enumerate every data/log file, and verify relocation paths.
4. Restore to fresh file paths without WITH REPLACE, then run full CHECKDB.
5. Prepare a run-local migration copy targeting the restored database. Only now
   set its backup-confirmation flag; the checked-in migration remains unchanged.
6. Run the migration, CHECKDB again, and verify the expected SchemaVersions row.
7. Retain SQL batches, informational/error logs, result CSVs, and manifest.json,
   including the migration SHA256, completed steps, server, and database names.

SQL errors/timeouts stop the workflow. A timeout during restore may leave an
incomplete database; it is NOT automatically dropped or retried. The manifest
distinguishes database rehearsal success from application smoke-test approval.
Do not publish backup files or evidence containing sensitive information to Git.

## Application acceptance and target deployment

Point a separate approved staging application at the restored database. Keep its
media storage and mutable template files isolated from live files; SQL backup
does not include those files. Disable outbound integrations. Test exact-limit and
over-limit validation, root/sibling uniqueness, key/email comparisons, directory
edits, and CreatedUtc/ModifiedUtc behavior. Application alignment is implemented
in [ADR-0008](decisions/ADR-0008-sql-policy-application-alignment.md); staging
HTTP/UI acceptance is still required before target deployment.

After signed-off staging tests, the DB team schedules target deployment, stops
writers, reruns audits, takes a fresh verified backup, and follows its approved
restore-tested recovery procedure. Review the exact migration hash, target name,
and backup evidence before enabling its confirmation flag for target execution.
Never point this rehearsal helper's generated migration copy at production.

Rollback of a committed migration requires an explicit DBA recovery decision;
restoring a backup can discard newer writes. Record the recovery point, coordinate
application/file-store recovery, and keep writers stopped until validation ends.
This helper does not perform production rollback or approve data loss.

## Validation status

PowerShell parsing, preview, and Execute/WhatIf paths passed on September 13,
2026 without connecting to SQL. Invalid source databases, relative SQL-host
directories, and unexpected migration contracts were rejected. This records the
initial runner-validation pass. Later integration checks against two migrated
rehearsal databases are recorded in
[ADR-0008](decisions/ADR-0008-sql-policy-application-alignment.md). Those checks
do not replace operator backup/restore evidence or staging HTTP/UI acceptance.

## References

- [SQL Server RESTORE documentation](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-for-restoring-recovering-and-managing-backups-transact-sql)
- [Restore files to new locations](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/restore-files-to-a-new-location-sql-server)

VERIFYONLY is an additional backup check, not a replacement for an actual restore,
CHECKDB, or application tests. The helper performs the restore and CHECKDB steps;
application validation remains manual.
