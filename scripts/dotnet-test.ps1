param(
    [Parameter(Mandatory = $true)]
    [string]$Project,

    [switch]$NoRestore
)

$bootstrap = . (Join-Path $PSScriptRoot 'templar-cms-bootstrap.ps1')
$nugetConfig = Join-Path $bootstrap.RepoRoot 'nuget.config'

if (-not $NoRestore) {
    dotnet restore $Project --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

dotnet test $Project --no-restore
exit $LASTEXITCODE
