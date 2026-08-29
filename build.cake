#nullable enable

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var runTests = Argument("runTests", true);
var cleanOutput = Argument("cleanOutput", true);
var recycleAppPool = Argument("recycleAppPool", false);
var appPoolName = Argument("appPoolName", string.Empty);
var runtime = Argument("runtime", string.Empty);
var selfContained = Argument("selfContained", false);
var publishDirectoryArgument = Argument("publishDirectory", "./artifacts/publish/api");
var inetpubDirectoryArgument = Argument("inetpubDirectory", @"C:\inetpub\wwwroot\TemplarCMS.Api");

var apiProject = "./src/TemplarCMS.Api/TemplarCMS.Api.csproj";
var apiTestsProject = "./tests/TemplarCMS.Api.Tests/TemplarCMS.Api.Tests.csproj";
var artifactsDirectory = Directory("./artifacts");
var publishDirectory = MakeAbsolute(Directory(publishDirectoryArgument));
var inetpubDirectory = MakeAbsolute(Directory(inetpubDirectoryArgument));

Task("Clean")
    .Does(() =>
{
    EnsureDirectoryExists(artifactsDirectory);
    CleanDirectory(artifactsDirectory);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(apiProject);

    if (runTests)
    {
        DotNetRestore(apiTestsProject);
    }
});

Task("Test")
    .IsDependentOn("Restore")
    .Does(() =>
{
    if (!runTests)
    {
        Information("Skipping API tests because runTests=false.");
        return;
    }

    DotNetTest(
        apiTestsProject,
        new DotNetTestSettings
        {
            Configuration = configuration,
            NoRestore = true
        });
});

Task("Publish-Api")
    .IsDependentOn("Test")
    .Does(() =>
{
    PublishApi(publishDirectory);
});

Task("Publish-Api-To-Inetpub")
    .IsDependentOn("Test")
    .Does(() =>
{
    PublishApi(publishDirectory);
    DeployApiToInetpub(publishDirectory, inetpubDirectory);
});

Task("Recycle-IIS-AppPool")
    .Does(() =>
{
    if (!recycleAppPool)
    {
        Information("Skipping IIS app pool recycle because recycleAppPool=false.");
        return;
    }

    if (string.IsNullOrWhiteSpace(appPoolName))
    {
        throw new CakeException("Provide --appPoolName when recycleAppPool=true.");
    }

    StartProcess(
        "powershell",
        new ProcessSettings
        {
            Arguments =
                new ProcessArgumentBuilder()
                    .Append("-NoProfile")
                    .Append("-ExecutionPolicy")
                    .Append("Bypass")
                    .Append("-Command")
                    .AppendQuoted(
                        "Import-Module WebAdministration; " +
                        $"if (-not (Test-Path \"IIS:\\AppPools\\{appPoolName}\")) " +
                        "{ throw \"IIS app pool was not found.\" } " +
                        $"Restart-WebAppPool -Name \"{appPoolName}\"")
        });
});

Task("Default")
    .IsDependentOn("Publish-Api");

Task("Publish-To-IIS")
    .IsDependentOn("Publish-Api-To-Inetpub")
    .IsDependentOn("Recycle-IIS-AppPool");

RunTarget(target);

void PublishApi(DirectoryPath outputDirectory)
{
    EnsureDirectoryExists(outputDirectory);

    if (cleanOutput)
    {
        CleanDirectory(outputDirectory);
    }

    var settings =
        new DotNetPublishSettings
        {
            Configuration = configuration,
            OutputDirectory = outputDirectory,
            NoRestore = true,
            SelfContained = selfContained
        };

    if (!string.IsNullOrWhiteSpace(runtime))
    {
        settings.Runtime = runtime;
    }

    DotNetPublish(apiProject, settings);

    Information("Published API to {0}", outputDirectory.FullPath);
}

void DeployApiToInetpub(DirectoryPath sourceDirectory, DirectoryPath destinationDirectory)
{
    EnsureDirectoryExists(destinationDirectory);

    foreach (var sourceFile in System.IO.Directory.GetFiles(
                 sourceDirectory.FullPath,
                 "*",
                 System.IO.SearchOption.AllDirectories))
    {
        var relativePath = System.IO.Path.GetRelativePath(sourceDirectory.FullPath, sourceFile);

        if (IsRuntimeDataPath(relativePath))
        {
            continue;
        }

        var destinationFile = System.IO.Path.Combine(destinationDirectory.FullPath, relativePath);
        var destinationParent = System.IO.Path.GetDirectoryName(destinationFile);

        if (!string.IsNullOrWhiteSpace(destinationParent))
        {
            System.IO.Directory.CreateDirectory(destinationParent);
        }

        System.IO.File.Copy(sourceFile, destinationFile, overwrite: true);
    }

    Information(
        "Deployed API files to {0} while preserving RuntimeData and App_Data.",
        destinationDirectory.FullPath);
}

bool IsRuntimeDataPath(string relativePath)
{
    var firstPathSegment = relativePath.Split(
        new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar },
        StringSplitOptions.RemoveEmptyEntries)[0];

    return string.Equals(firstPathSegment, "RuntimeData", StringComparison.OrdinalIgnoreCase)
        || string.Equals(firstPathSegment, "App_Data", StringComparison.OrdinalIgnoreCase);
}
