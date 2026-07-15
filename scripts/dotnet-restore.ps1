param(
    [Parameter(Mandatory = $true)]
    [string]$Project
)

$bootstrap = . (Join-Path $PSScriptRoot 'templar-cms-bootstrap.ps1')
$nugetConfig = Join-Path $bootstrap.RepoRoot 'nuget.config'

dotnet restore $Project --configfile $nugetConfig
exit $LASTEXITCODE
