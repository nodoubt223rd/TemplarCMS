#nullable enable

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var runTests = Argument("runTests", true);
var cleanOutput = Argument("cleanOutput", true);
var recycleAppPool = Argument("recycleAppPool", false);
var appPoolName = Argument("appPoolName", "TemplarCMS.Api");
var adminAppPoolName = Argument("adminAppPoolName", "TemplarCMS.Admin");
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
    DeployProjectToIis(publishDirectory, inetpubDirectory, appPoolName);
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
    DeployProjectToIis(adminPublishDirectory, adminInetpubDirectory, adminAppPoolName);
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

    StopIisAppPool(appPoolName);
    StartIisAppPool(appPoolName);
});

Task("Default")
    .IsDependentOn("Publish-Api");

Task("Publish-To-IIS")
    .IsDependentOn("Publish-Api-To-Inetpub");

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

void DeployProjectToIis(
    DirectoryPath sourceDirectory,
    DirectoryPath destinationDirectory,
    string targetAppPoolName)
{
    EnsureIisAppPoolName(targetAppPoolName);
    Information("Stopping IIS app pool '{0}' before deployment.", targetAppPoolName);
    StopIisAppPool(targetAppPoolName);

    try
    {
        Information("Deploying API files to {0}.", destinationDirectory.FullPath);
        DeployApiToInetpub(sourceDirectory, destinationDirectory);
    }
    finally
    {
        Information("Starting IIS app pool '{0}' after deployment.", targetAppPoolName);
        StartIisAppPool(targetAppPoolName);
    }
}

void EnsureIisAppPoolName(string targetAppPoolName)
{
    if (string.IsNullOrWhiteSpace(targetAppPoolName))
    {
        throw new CakeException("Provide --appPoolName for IIS API deployment.");
    }
}

void StopIisAppPool(string targetAppPoolName)
{
    RunIisAppPoolCommand(
        targetAppPoolName,
        "stop",
        allowFailure: true);
}

void StartIisAppPool(string targetAppPoolName)
{
    RunIisAppPoolCommand(
        targetAppPoolName,
        "start",
        allowFailure: false);
}

void RunIisAppPoolCommand(
    string targetAppPoolName,
    string operation,
    bool allowFailure)
{
    var exitCode =
        StartProcess(
            @"C:\Windows\System32\inetsrv\appcmd.exe",
            new ProcessSettings
            {
                Arguments =
                    new ProcessArgumentBuilder()
                        .Append(operation)
                        .Append("apppool")
                        .Append($"/apppool.name:{targetAppPoolName}")
            });

    if (exitCode != 0)
    {
        if (allowFailure)
        {
            Warning(
                "IIS app pool {0} command returned exit code {1} for '{2}'. Continuing because the pool may already be stopped.",
                operation,
                exitCode,
                targetAppPoolName);
            return;
        }

        throw new CakeException(
            $"IIS app pool {operation} command failed for '{targetAppPoolName}'.");
    }
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
