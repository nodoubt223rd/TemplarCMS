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
var adminPublishDirectoryArgument = Argument("adminPublishDirectory", "./artifacts/publish/admin");
var inetpubDirectoryArgument = Argument("inetpubDirectory", @"C:\inetpub\wwwroot\TemplarCMS.Api");
var adminInetpubDirectoryArgument = Argument("adminInetpubDirectory", @"C:\inetpub\wwwroot\TemplarCMS.Api\author-workspace");

var apiProject = "./src/TemplarCMS.Api/TemplarCMS.Api.csproj";
var adminProject = "./src/TemplarCMS.Admin/TemplarCMS.Admin.Server/TemplarCMS.Admin.Server.csproj";
var apiTestsProject = "./tests/TemplarCMS.Api.Tests/TemplarCMS.Api.Tests.csproj";
var artifactsDirectory = Directory("./artifacts");
var publishDirectory = MakeAbsolute(Directory(publishDirectoryArgument));
var adminPublishDirectory = MakeAbsolute(Directory(adminPublishDirectoryArgument));
var inetpubDirectory = MakeAbsolute(Directory(inetpubDirectoryArgument));
var adminInetpubDirectory = MakeAbsolute(Directory(adminInetpubDirectoryArgument));

Task("Clean")
    .Does(() =>
{
    EnsureDirectoryExists(artifactsDirectory);
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

Task("Publish-Admin")
    .IsDependentOn("Test")
    .Does(() =>
{
    PublishProject(adminProject, adminPublishDirectory);
});

Task("Publish-Admin-To-Inetpub")
    .IsDependentOn("Test")
    .Does(() =>
{
    PublishProject(adminProject, adminPublishDirectory);
    DeployApiToInetpub(adminPublishDirectory, adminInetpubDirectory);
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
    PublishProject(apiProject, outputDirectory);

    Information("Published API to {0}", outputDirectory.FullPath);
}

void PublishProject(string project, DirectoryPath outputDirectory)
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

    DotNetPublish(project, settings);
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

        if (ShouldPreserveDestinationFile(relativePath, destinationDirectory))
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
        "Deployed publish files to {0} while preserving RuntimeData and populated App_Data\\Templates.",
        destinationDirectory.FullPath);
}

bool ShouldPreserveDestinationFile(string relativePath, DirectoryPath destinationDirectory)
{
    var firstPathSegment = relativePath.Split(
        new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar },
        StringSplitOptions.RemoveEmptyEntries)[0];

    if (string.Equals(firstPathSegment, "RuntimeData", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!IsAppDataTemplatePath(relativePath))
    {
        return false;
    }

    var destinationTemplatesDirectory =
        System.IO.Path.Combine(destinationDirectory.FullPath, "App_Data", "Templates");

    return System.IO.Directory.Exists(destinationTemplatesDirectory)
        && System.IO.Directory.EnumerateFiles(
                destinationTemplatesDirectory,
                "*",
                System.IO.SearchOption.AllDirectories)
            .Any();
}

bool IsAppDataTemplatePath(string relativePath)
{
    var pathSegments = relativePath.Split(
        new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar },
        StringSplitOptions.RemoveEmptyEntries);

    return pathSegments.Length >= 2
        && string.Equals(pathSegments[0], "App_Data", StringComparison.OrdinalIgnoreCase)
        && string.Equals(pathSegments[1], "Templates", StringComparison.OrdinalIgnoreCase);
}
