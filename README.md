# TemplarCMS

TemplarCMS is a template-driven, API-first headless CMS built on .NET.

## Status

Last CircleCI build: [![CircleCI](https://dl.circleci.com/status-badge/img/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/master)

Architecture and planning phase.

## Development

Use the SDK pinned in [global.json](/E:/Projects/TemplarCMS/global.json), then build from the solution container:

```powershell
dotnet build .\TemplarCMS.slnx
```

If you hit local NuGet or profile-path issues, the repo includes helper scripts:

```powershell
. .\scripts\templar-cms-bootstrap.ps1
.\scripts\dev-shell.ps1
.\scripts\dotnet-test.ps1 -Project .\tests\TemplarCMS.Application.Tests\TemplarCMS.Application.Tests.csproj
```

## Publishing For IIS Smoke Testing

The repo now includes a Cake-based publish wrapper for getting the API onto a
Windows IIS box quickly for Postman and live API testing.

Publish to a local artifact folder:

```powershell
.\build.ps1 -Target Publish-Api -PublishDirectory .\artifacts\publish\api
```

Publish directly to the default IIS content path:

```powershell
.\build.ps1 -Target Publish-Api-To-Inetpub
```

Publish to IIS and recycle an app pool after the files land:

```powershell
.\build.ps1 -Target Publish-To-IIS -AppPoolName TemplarCMS.Api
```

Notes:

- The default IIS target path is `C:\inetpub\wwwroot\TemplarCMS.Api`.
- Override it with `-InetpubDirectory` when the site uses a different root.
- The IIS site and app pool should already exist before publishing.
- Use `-SkipTests` when you want a faster inner-loop deploy from a known-good branch.

## API Discovery

The API now exposes an OpenAPI document at:

```text
/openapi/v1.json
```

There is also a browser UI at:

```text
/openapi
```

Both routes are enabled by the `OpenApi:Enabled` setting and default to `true`
in the API appsettings so the dev team can import the contract into Postman from
an IIS-hosted test instance or inspect the endpoints quickly in a browser.

## Goals

- Sitecore-inspired templates
- Template inheritance
- Content trees
- Shared, unversioned and versioned fields
- HATEOAS REST API
- .NET 8 runtime on a modern .NET SDK toolchain
- EF Core

See docs/architecture.md for full details.
