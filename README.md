# TemplarCMS

TemplarCMS is a template-driven, API-first headless CMS built on .NET.

## Status

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/master)

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

## Goals

- Sitecore-inspired templates
- Template inheritance
- Content trees
- Shared, unversioned and versioned fields
- HATEOAS REST API
- .NET 8 runtime on a modern .NET SDK toolchain
- EF Core

See docs/architecture.md for full details.
