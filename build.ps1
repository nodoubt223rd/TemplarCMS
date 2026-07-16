param(
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [string]$PublishDirectory,
    [string]$InetpubDirectory = "C:\inetpub\wwwroot\TemplarCMS.Api",
    [string]$AppPoolName,
    [string]$Runtime,
    [switch]$RecycleAppPool,
    [switch]$SkipTests,
    [switch]$SelfContained,
    [switch]$NoCleanOutput
)

$bootstrap = . (Join-Path $PSScriptRoot 'scripts\templar-cms-bootstrap.ps1')

Push-Location $bootstrap.RepoRoot

try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $cakeArguments = @(
        'cake',
        'build.cake',
        '--target', $Target,
        '--configuration', $Configuration,
        '--runTests', (-not $SkipTests.IsPresent).ToString().ToLowerInvariant(),
        '--recycleAppPool', $RecycleAppPool.IsPresent.ToString().ToLowerInvariant(),
        '--selfContained', $SelfContained.IsPresent.ToString().ToLowerInvariant(),
        '--cleanOutput', (-not $NoCleanOutput.IsPresent).ToString().ToLowerInvariant(),
        '--inetpubDirectory', $InetpubDirectory
    )

    if ($PublishDirectory) {
        $cakeArguments += @('--publishDirectory', $PublishDirectory)
    }

    if ($AppPoolName) {
        $cakeArguments += @('--appPoolName', $AppPoolName)
    }

    if ($Runtime) {
        $cakeArguments += @('--runtime', $Runtime)
    }

    & dotnet @cakeArguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
