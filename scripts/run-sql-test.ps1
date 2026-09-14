$run = @{
    Server = 'Salvatore'
    SourceDatabase = 'templarcms_authoring'
    BackupDirectory = 'E:\SqlBackups'
    RestoreDataDirectory = 'E:\SqlRehearsal\Data'
    RestoreLogDirectory = 'E:\SqlRehearsal\Log'
    OutputDirectory = 'E:\SqlDeploymentEvidence'
}
.\scripts\sqlserver-migration-rehearsal.ps1 @run
.\scripts\sqlserver-migration-rehearsal.ps1 @run -Execute -WhatIf