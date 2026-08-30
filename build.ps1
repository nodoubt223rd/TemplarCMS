param(
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [string]$PublishDirectory,
    [string]$AdminPublishDirectory,
    [string]$InetpubDirectory = "C:\inetpub\wwwroot\TemplarCMS.Api",
    [string]$AdminInetpubDirectory = "C:\inetpub\wwwroot\TemplarCMS.Api\author-workspace",
    [string]$AppPoolName = "TemplarCMS.Api",
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
        '--inetpubDirectory', $InetpubDirectory,
        '--adminInetpubDirectory', $AdminInetpubDirectory
    )

    if ($PublishDirectory) {
        $cakeArguments += @('--publishDirectory', $PublishDirectory)
    }

    if ($AdminPublishDirectory) {
        $cakeArguments += @('--adminPublishDirectory', $AdminPublishDirectory)
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
