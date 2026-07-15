. (Join-Path $PSScriptRoot 'templar-cms-bootstrap.ps1') | Out-Null

Write-Host 'TemplarCMS dev shell configured:'
Write-Host "  DOTNET_CLI_HOME=$env:DOTNET_CLI_HOME"
Write-Host "  NUGET_PACKAGES=$env:NUGET_PACKAGES"
Write-Host "  APPDATA=$env:APPDATA"
Write-Host "  DOTNET_SKIP_FIRST_TIME_EXPERIENCE=$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE"
Write-Host "  DOTNET_NOLOGO=$env:DOTNET_NOLOGO"
