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

Publish the author workspace to a local artifact folder:

```powershell
.\build.ps1 -Target Publish-Admin -AdminPublishDirectory .\artifacts\publish\author-workspace
```

Deploy to the default IIS content path:

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
- IIS deployment stages the publish output under `artifacts` and copies application
  files into the IIS root. It does not clean the IIS root.
- `RuntimeData` is explicitly preserved so local SQLite data is never deleted
  or overwritten during deployment. When legacy `App_Data\Templates` already
  contains one or more files, its template files are also preserved; an absent
  or empty directory can still receive the published bootstrap templates.
- The `Publish-To-IIS` target recycles the selected app pool after the file copy.
- Use `-SkipTests` when you want a faster inner-loop deploy from a known-good branch.

### Author Workspace

The Vue authoring client is hosted by `TemplarCMS.Admin.Server` and is intended
to run as the `/author-workspace/` IIS application under the API site. Its
production build uses that base path for static assets and continues to call
the API through same-origin `/api` routes.

Publish its files to the IIS application directory:

```powershell
.\build.ps1 -Target Publish-Admin-To-Inetpub
```

Create the IIS application once, after the API site and a `TemplarCMS.Admin`
app pool exist:

```powershell
New-WebApplication -Site 'TemplarCMS.api' -Name 'author-workspace' `
  -PhysicalPath 'C:\inetpub\wwwroot\TemplarCMS.Api\author-workspace' `
  -ApplicationPool 'TemplarCMS.Admin'
```

Browse to `https://templarcms.api/author-workspace/`. Authentication is
intentionally deferred for local, internal testing; do not expose this route
to an untrusted network until authoring authentication and authorization are
implemented.

### IIS Prerequisites

Before expecting the published API to start behind IIS, make sure the host
machine has the following in place:

- IIS installed with HTTPS bindings configured for the chosen host name.
- The ASP.NET Core Hosting Bundle installed so `AspNetCoreModuleV2` is
  available to IIS.
- A .NET runtime compatible with the target framework in
  [TemplarCMS.Api.csproj](/E:/Projects/TemplarCMS/src/TemplarCMS.Api/TemplarCMS.Api.csproj).
- Host-name resolution configured locally when using a custom dev host such as
  `templarcms.api`.

If IIS returns `500.19` with error code `0x8007000d` while loading
`web.config`, check for a missing Hosting Bundle first.

### Local HTTPS For Custom Dev Hosts

`dotnet dev-certs` is usually enough for `localhost`, but a custom IIS host
name such as `https://templarcms.api` needs its own trusted certificate.

One workable local path is `mkcert`:

```powershell
mkcert -install
mkcert -pkcs12 -p12-file .\.certs\templarcms.api.pfx templarcms.api
```

Then:

- Import the generated `.pfx` into `Certificates (Local Computer) > Personal`.
- Bind that certificate to the IIS site using the `templarcms.api` HTTPS host
  name.
- Add a local hosts-file entry such as `127.0.0.1 templarcms.api`.

After installing the Hosting Bundle or changing IIS certificate bindings,
restart IIS:

```powershell
iisreset
```

## API Discovery

The API now exposes an OpenAPI document at:

```text
/openapi/v1.json
```

The site root renders the source-controlled sample home content. Use it to
verify the bootstrap, runtime resolution, and public rendering path end-to-end.
Public content items can also be rendered from their normalized CMS paths; for
example, an item at `/home/articles/hello-world` is available at that public
URL. Unknown public paths return an HTML `404` page. The API discovery document
is available at `/api/v1`.

The public shell also renders a navigation menu from the direct children of the
starter home item. Each link uses the child item's canonical CMS path and its
authored `navigationTitle`, falling back to `title` and then the item name.

There is also a browser UI at:

```text
/openapi
```

Both routes are enabled by the `OpenApi:Enabled` setting and default to `true`
in the API appsettings so the dev team can import the contract into Postman from
an IIS-hosted test instance or inspect the endpoints quickly in a browser.

Quick IIS-hosted smoke-test targets:

- `https://templarcms.api/openapi`
- `https://templarcms.api/openapi/v1.json`
- `https://templarcms.api/api/v1/field-types`

## Authoring Security

Write endpoints now support a first-pass authoring security model that can be
enabled for Postman and shared test instances.

Configuration:

```json
"AuthoringSecurity": {
  "Enabled": false,
  "ApiKeyHeaderName": "X-Templar-Api-Key",
  "ApiKey": ""
}
```

When enabled:

- `POST`, `PUT`, and `DELETE` authoring routes require the configured API key header.
- Read routes stay anonymous for now.
- The API responds with standard `401` and `403` endpoint metadata so the contract stays compatible with stricter future auth flows.
- The OpenAPI document marks protected authoring routes with the API key requirement so Postman imports and Swagger UI are easier to use.

## Goals

- Sitecore-inspired templates
- Template inheritance
- Content trees
- Shared, unversioned and versioned fields
- HATEOAS REST API
- .NET 8 runtime on a modern .NET SDK toolchain
- EF Core

See docs/architecture.md for full details.
