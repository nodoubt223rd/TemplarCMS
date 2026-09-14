#requires -Version 7.4
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Server = 'Salvatore',
    [ValidateSet('templarcms_authoring', 'templarcms_published')]
    [string]$SourceDatabase = 'templarcms_authoring',
    [Parameter(Mandatory)][string]$BackupDirectory,
    [Parameter(Mandatory)][string]$RestoreDataDirectory,
    [Parameter(Mandatory)][string]$RestoreLogDirectory,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../.tmp/sql-rehearsals'),
    [string]$MigrationScript = (Join-Path $PSScriptRoot '../database/sqlserver/006-preflight-1a.sql'),
    [ValidateRange(60, 86400)][int]$CommandTimeoutSeconds = 3600,
    [switch]$TrustServerCertificate,
    [switch]$Execute
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Sql-Literal([string]$Value) { "N'" + $Value.Replace("'", "''") + "'" }
function Sql-Identifier([string]$Value) { '[' + $Value.Replace(']', ']]') + ']' }

foreach ($path in @($BackupDirectory, $RestoreDataDirectory, $RestoreLogDirectory)) {
    if ($path -notmatch '^(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+\\)' -or $path -match '[\r\n]') {
        throw 'SQL file directories must be absolute Windows drive or UNC paths on the SQL host.'
    }
}
$migrationPath = (Resolve-Path -LiteralPath $MigrationScript).Path
$migration = Get-Content -LiteralPath $migrationPath -Raw
$expectedPattern = "@EXPECTED_DATABASE sysname = N'templarcms_authoring'"
$backupPattern = '@CONFIRM_BACKUP_TAKEN bit = 0'
foreach ($pattern in @($expectedPattern, $backupPattern)) {
    if ([regex]::Matches($migration, [regex]::Escape($pattern)).Count -ne 1) {
        throw "Migration contract changed or backup flag is already enabled: $pattern"
    }
}
if ($migration -match '(?im)^\s*GO\s*(?:--.*)?$') {
    throw 'This runner accepts the reviewed single-batch migration only; GO batches require review.'
}
$versionMatch = [regex]::Match($migration, "@MIGRATION_VERSION nvarchar\(100\) = N'([^']+)'")
if (-not $versionMatch.Success) { throw 'Migration version declaration is missing.' }
$version = $versionMatch.Groups[1].Value
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$stage = 'templarcms_rehearsal_' + $runId
$backupPath = $BackupDirectory.TrimEnd('\') + '\' + $SourceDatabase + '_' + $runId + '.bak'
$migrationHash = (Get-FileHash -LiteralPath $migrationPath -Algorithm SHA256).Hash

Write-Host "Source (backup only): $Server / $SourceDatabase"
Write-Host "New restore/migration target: $Server / $stage"
Write-Host "SQL-host backup path: $backupPath"
Write-Host "Migration SHA256: $migrationHash"
if (-not $Execute) {
    Write-Host 'Preview only. Add -Execute to back up, restore a NEW database, and migrate that copy.'
    return
}
if (-not $PSCmdlet.ShouldProcess("$Server / $stage", "Back up $SourceDatabase, restore a new copy, and migrate only that copy")) { return }

$runDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $runId
$null = New-Item -ItemType Directory -Path $runDirectory
$manifest = [ordered]@{
    RunId = $runId; Server = $Server; SourceDatabase = $SourceDatabase
    StagingDatabase = $stage; BackupPath = $backupPath; MigrationSHA256 = $migrationHash
    MigrationVersion = $version; StartedUtc = [DateTime]::UtcNow.ToString('o')
    Status = 'Running'; CompletedSteps = @(); ApplicationSmokeTest = 'Not performed'
}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runDirectory 'manifest.json') -Encoding utf8 }
Save-Manifest

function Invoke-Step([string]$Name, [string]$Database, [string]$Sql) {
    $Sql | Set-Content -LiteralPath (Join-Path $runDirectory "$Name.sql") -Encoding utf8
    $log = Join-Path $runDirectory "$Name.log"
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    # PowerShell treats this builder as a dictionary; use SQL keyword keys.
    $builder['Data Source'] = $Server
    $builder['Initial Catalog'] = $Database
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = [bool]$TrustServerCertificate
    $builder['Connect Timeout'] = 15
    $builder['Application Name'] = 'TemplarCMS migration rehearsal'
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    $handler = [System.Data.SqlClient.SqlInfoMessageEventHandler]{
        param($sender, $eventArgs)
        [IO.File]::AppendAllText($log, $eventArgs.Message + [Environment]::NewLine)
    }.GetNewClosure()
    $connection.add_InfoMessage($handler)
    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $CommandTimeoutSeconds
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $results = [System.Data.DataSet]::new()
    try {
        Write-Host "Running $Name on $Database"
        $connection.Open()
        $null = $adapter.Fill($results)
        for ($i = 0; $i -lt $results.Tables.Count; $i++) {
            $results.Tables[$i] | Export-Csv -LiteralPath (Join-Path $runDirectory "$Name-result-$i.csv") -NoTypeInformation
        }
        Add-Content -LiteralPath $log -Value 'Completed successfully.'
        $manifest.CompletedSteps += $Name
        Save-Manifest
        return ,$results
    } catch {
        Add-Content -LiteralPath $log -Value $_.Exception.ToString()
        throw
    } finally {
        $adapter.Dispose(); $command.Dispose(); $connection.Dispose()
    }
}

try {
    $src = Sql-Identifier $SourceDatabase
    $dst = Sql-Identifier $stage
    $bak = Sql-Literal $backupPath
    $stageLiteral = Sql-Literal $stage
    $null = Invoke-Step '01-target-check' 'master' "
IF DB_ID($(Sql-Literal $SourceDatabase)) IS NULL THROW 60010, 'Source database not found.', 1;
IF DB_ID($stageLiteral) IS NOT NULL THROW 60011, 'Rehearsal database already exists; refusing overwrite.', 1;
SELECT @@SERVERNAME AS ServerName, ORIGINAL_LOGIN() AS OperatorName;"

    # COPY_ONLY preserves the regular differential backup base; the unique path avoids reuse.
    $null = Invoke-Step '02-backup' 'master' "BACKUP DATABASE $src TO DISK = $bak WITH COPY_ONLY, CHECKSUM, STOP_ON_ERROR, STATS = 10;"
    $null = Invoke-Step '03-verify-backup' 'master' "RESTORE VERIFYONLY FROM DISK = $bak WITH FILE = 1, CHECKSUM, STOP_ON_ERROR;"
    $files = Invoke-Step '04-file-list' 'master' "RESTORE FILELISTONLY FROM DISK = $bak WITH FILE = 1;"
    $moves = @()
    foreach ($file in $files.Tables[0].Rows) {
        if ($file.Type -notin @('D', 'L')) { throw 'Only ordinary data/log files are supported. FILESTREAM or other file types need a DB-reviewed restore plan.' }
        $directory = if ($file.Type -eq 'L') { $RestoreLogDirectory } else { $RestoreDataDirectory }
        $extension = if ($file.Type -eq 'L') { '.ldf' } else { '.mdf' }
        $destination = $directory.TrimEnd('\') + '\' + $stage + '_' + $file.FileId + $extension
        $moves += "MOVE $(Sql-Literal $file.LogicalName) TO $(Sql-Literal $destination)"
    }
    if ($moves.Count -eq 0) { throw 'Backup contains no restorable files.' }
    $moveSql = $moves -join ",`n"
    $null = Invoke-Step '05-verify-relocation' 'master' "RESTORE VERIFYONLY FROM DISK = $bak WITH FILE = 1, CHECKSUM, $moveSql;"
    # No REPLACE, DROP DATABASE, SINGLE_USER, or forced disconnections are permitted.
    $null = Invoke-Step '06-restore' 'master' "
IF DB_ID($stageLiteral) IS NOT NULL THROW 60011, 'Rehearsal database already exists; refusing overwrite.', 1;
RESTORE DATABASE $dst FROM DISK = $bak WITH FILE = 1, CHECKSUM, STOP_ON_ERROR, RECOVERY, $moveSql, STATS = 10;"

    foreach ($phase in @('07-before-migration', '09-after-migration')) {
        $check = Invoke-Step $phase $stage "DBCC CHECKDB ($dst) WITH NO_INFOMSGS, ALL_ERRORMSGS, TABLERESULTS;"
        foreach ($table in $check.Tables) {
            if ($table.Rows.Count -gt 0) { throw "CHECKDB returned diagnostics at $phase; review logs before proceeding." }
        }
        if ($phase -eq '07-before-migration') {
            $prepared = $migration.Replace($expectedPattern, "@EXPECTED_DATABASE sysname = $stageLiteral").Replace($backupPattern, '@CONFIRM_BACKUP_TAKEN bit = 1')
            $null = Invoke-Step '08-migration' $stage $prepared
        }
    }
    $null = Invoke-Step '10-version-check' $stage "
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionKey = $(Sql-Literal $version))
    THROW 60012, 'Expected migration version was not recorded.', 1;
SELECT VersionKey, AppliedOn, AppliedBy, ScriptName FROM dbo.SchemaVersions WHERE VersionKey = $(Sql-Literal $version);"
    $manifest.Status = 'DatabaseRehearsalPassed'
    Write-Host "Database rehearsal passed. Application smoke testing is still required against $stage."
} catch {
    $manifest.Status = 'Failed'
    $manifest.Error = $_.Exception.Message
    throw
} finally {
    $manifest.CompletedUtc = [DateTime]::UtcNow.ToString('o')
    Save-Manifest
    Write-Host "Evidence: $runDirectory"
    Write-Host 'Backup and rehearsal database are retained. No automatic cleanup or production migration is performed.'
}
