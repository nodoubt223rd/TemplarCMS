param()

$repoRoot = Split-Path -Parent $PSScriptRoot
$tmpRoot = Join-Path $repoRoot '.tmp'
$dotnetCliHome = Join-Path $tmpRoot 'dotnet-cli'
$nugetPackages = Join-Path $tmpRoot 'nuget-packages'
$appDataRoot = Join-Path $tmpRoot 'appdata'
$nugetAppData = Join-Path $appDataRoot 'NuGet'
$repoNugetConfig = Join-Path $repoRoot 'nuget.config'

New-Item -ItemType Directory -Force -Path $dotnetCliHome | Out-Null
New-Item -ItemType Directory -Force -Path $nugetPackages | Out-Null
New-Item -ItemType Directory -Force -Path $nugetAppData | Out-Null

if (Test-Path $repoNugetConfig) {
    Copy-Item $repoNugetConfig (Join-Path $nugetAppData 'NuGet.Config') -Force
}

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_HOME = $dotnetCliHome
$env:NUGET_PACKAGES = $nugetPackages
$env:DOTNET_NOLOGO = '1'
$env:APPDATA = $appDataRoot

[pscustomobject]@{
    RepoRoot = $repoRoot
    DotnetCliHome = $env:DOTNET_CLI_HOME
    NuGetPackages = $env:NUGET_PACKAGES
    AppData = $env:APPDATA
}
