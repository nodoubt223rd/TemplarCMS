# TemplarCMS

TemplarCMS is a template-driven, API-first headless CMS built on .NET.

## Status

Last CircleCI build: [![CircleCI](https://dl.circleci.com/status-badge/img/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/trunk.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/PCTm5tLRZgivY3sG1VYhQm/BaNsGx2kPXGTG2sCfe233x/tree/trunk)

Architecture and planning phase.

## Development

The default branch is `trunk`. Use the .NET 8 SDK selected by [global.json](/E:/Projects/TemplarCMS/global.json), then build all backend, test, and admin-host projects:

```powershell
dotnet build .\TemplarCMS.sln
```

Use the explicit `.sln` path: the pinned .NET 8 SDK cannot build the `.slnx` file retained for newer IDEs. `dotnet build` compiles the test projects; run `dotnet test .\TemplarCMS.sln --no-build` to execute them. The Vue client is built during Admin publishing (see below), or separately with `npm ci` and `npm run build` in its directory.

If you hit local NuGet or profile-path issues, the repo includes helper scripts:

```powershell
. .\scripts\templar-cms-bootstrap.ps1
.\scripts\dev-shell.ps1
.\scripts\dotnet-test.ps1 -Project .\tests\TemplarCMS.Application.Tests\TemplarCMS.Application.Tests.csproj
```

### Admin Frontend

The Vue authoring client lives in
[src/TemplarCMS.Admin/templarcms.admin.client](src/TemplarCMS.Admin/templarcms.admin.client).
Its application entry point is [src/main.ts](src/TemplarCMS.Admin/templarcms.admin.client/src/main.ts),
with the root component in [App.vue](src/TemplarCMS.Admin/templarcms.admin.client/src/App.vue).
The ASP.NET Core host for the client is
[TemplarCMS.Admin.Server](src/TemplarCMS.Admin/TemplarCMS.Admin.Server).

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

Deploy the API to IIS using the default app pool. The target stops the pool
before copying files and starts it after deployment so updated assemblies can
never be held open by the active worker:

```powershell
.\build.ps1 -Target Publish-Api-To-Inetpub
```

`Publish-To-IIS` remains an alias for the same safe API deployment flow:

```powershell
.\build.ps1 -Target Publish-To-IIS
```

Notes:

- The default IIS target path is `C:\inetpub\wwwroot\TemplarCMS.Api`.
- Override it with `-InetpubDirectory` when the site uses a different root.
- The IIS site and app pool should already exist before publishing.
- IIS API deployment defaults to the `TemplarCMS.Api` app pool. Override it
  with `-AppPoolName` for another site.
- Run IIS deployment targets from an elevated PowerShell session. They write
  under `C:\inetpub` and can otherwise fail with `Access to the path
  '...\\appsettings.json' is denied`, even when the source publish succeeds.
- IIS deployment stages the publish output under `artifacts` and copies application
  files into the IIS root. It does not clean the IIS root.
- `RuntimeData` is explicitly preserved so local SQLite data is never deleted
  or overwritten during deployment. When legacy `App_Data\Templates` already
  contains one or more files, its template files are also preserved; an absent
  or empty directory can still receive the published bootstrap templates.
- API deployment stops the selected pool before copying and starts it after the
  copy, even when deployment fails, so locked assemblies do not leave the app
  unavailable.
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

Browse to `https://templarcms.api/author-workspace/`. Complete the local
administrator setup below, sign in, and change the temporary password before
using authoring tools. Preserve the configured Data Protection key directory
across deployments and give the API app-pool identity access to it.

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

For localhost development, run this once in PowerShell as your normal Windows
user and accept the Windows certificate trust prompt:

```powershell
dotnet dev-certs https --trust
dotnet dev-certs https --check --trust
```

Restart the browser (including Codex if using its embedded browser) after
changing certificate trust. Then restart `npm run dev`. Vite checks for a trusted
.NET development certificate and refreshes its ignored `.certs` PEM/key export
on every start, so a cached certificate cannot survive certificate renewal or
replacement. Production builds do not use this local certificate setup. Never
commit the exported private key.

If Windows reports trust but a browser still reports `ERR_CERT_AUTHORITY_INVALID`,
check the certificate actually served by that hostname and the browser's trust
store. A browser running under another user or in a separate environment may
need trust configured there as well. Do not disable certificate validation or
click through security warnings as a permanent fix. See Microsoft's
[development certificate guidance](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs).

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
Public content items are rendered relative to the starter home item. For
example, a CMS item stored at the canonical path
`/templar/content/home/articles/hello-world` is available publicly at
`/articles/hello-world`. Unknown public paths return an HTML `404` page. The API
discovery document is available at `/api/v1`.

The public shell also renders a navigation menu from the direct children of the
starter home item. Each link uses a path relative to the public home root and
its authored `navigationTitle`, falling back to `title` and then the item name.
New instances include an `About` page beneath the starter home item, making
`/about` the first navigable public page in the out-of-box site.

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
- `https://templarcms.api/api/v1/field-types` (401 anonymously; 200 after unrestricted admin login)
- `https://templarcms.api/health`

### Runtime Diagnostics

Every API response includes an `X-Correlation-ID` header. Supply a safe value
containing only letters, digits, `.`, `_`, or `-` to trace a request through
the structured request-completion logs; the API generates a new value for
missing or unsafe headers. Request logs intentionally record the method, path,
status code, duration, and correlation ID, never request bodies or query data.

The API writes JSON logs to standard output. For normal IIS operation, leave
`stdoutLogEnabled="false"` in the deployed `web.config`. When diagnosing an
IIS startup failure, temporarily set it to `true`, make sure the app-pool
identity can write to the deployed `logs` directory, recycle the pool, and
inspect `logs\stdout*`. Turn it back off after capturing the failure; stdout
files do not rotate automatically. Also check Windows Event Viewer > Windows
Logs > Application for ASP.NET Core Module and application-hosting errors.

`/health` is an anonymous liveness endpoint. It confirms that the API process
is running; the startup bootstrap performs the SQLite and template-directory
initialization before the application begins serving requests.

### SQL Server Deployment

SQLite remains the default local runtime store. Before starting this version
against SQL Server, provision the schema with
[001-initial-schema.sql](database/sqlserver/001-initial-schema.sql) and
[002-user-directory.sql](database/sqlserver/002-user-directory.sql), then have
the deployment operator apply the reviewed
[006-preflight-1a.sql](database/sqlserver/006-preflight-1a.sql) migration after
the required audits and backup/restore verification. Follow the
[migration rehearsal runbook](docs/sqlserver-backup-restore-runbook.md);
do not execute the SQL folder as an indiscriminate sequence.

SQL startup verifies the required schema version, columns, indexes, and collation
before seeding. It fails with a deployment error for incompatible schemas and
does not create or migrate them. Runtime accounts do not need DDL permissions.
See [ADR-0008](docs/decisions/ADR-0008-sql-policy-application-alignment.md) for
string limits and application-owned UTC timestamps.

Set the provider and connection string outside source control:

```json
"Persistence": {
  "Provider": "SqlServer"
},
"ConnectionStrings": {
  "TemplarCms": "Server=YOUR_SERVER;Database=TemplarCMS;Integrated Security=True;TrustServerCertificate=True"
}
```

The startup bootstrap remains responsible for source-controlled templates and
starter content after the schema exists. It does not migrate an existing
SQLite `RuntimeData` database into SQL Server; schedule that data migration as
a separate, reviewed cutover.

## Authoring Security

Authoring uses ASP.NET Core Identity account sessions. Security and the System
user directory are enabled by default. Before setup, authoring is locked. An
explicit `AuthoringSecurity:Enabled=false` stops startup; the transitional
operator API key no longer grants authoring access.

### First installation and recovery

Configure the intended database and runtime paths first. For SQLite, schema is
added during runtime initialization. For SQL Server, apply the approved baseline,
`002-user-directory.sql`, and [007-admin-identity.sql](database/sqlserver/007-admin-identity.sql)
before starting the application or running setup. SQL Server startup does not run DDL.

From the repository root, provision the initial account:

```powershell
dotnet run --project .\src\TemplarCMS.Api\TemplarCMS.Api.csproj --no-launch-profile -- setup-admin --email operator@example.com
```

The command uses the application's configured provider, connection string,
environment, and runtime paths. It exits without starting HTTP and displays the
`TemplarAdmin` login and a unique temporary password once, after the profile,
credential hash, and installation marker commit together. Do not redirect that
output to routine logs. Re-running setup reports that setup already completed
without changing or revealing the password. Normal startup never seeds credentials.

Sign in through the Admin workspace over HTTPS. The API allows the temporary
session to change its password and log out, and denies authoring until the change
succeeds. Choose 15–128 characters; spaces and Unicode are permitted. Changes
require the current password and invalidate earlier sessions. Five failed attempts
lock login for 15 minutes; IP login rate limiting also applies.

If the one-time output is lost or the account is locked out, a trusted local
operator can issue a replacement temporary password:

```powershell
dotnet run --project .\src\TemplarCMS.Api\TemplarCMS.Api.csproj --no-launch-profile -- reset-admin-password
```

Recovery preserves account identity, revokes existing sessions, clears lockout,
and requires another password change. It never creates a second account or
reactivates a suspended/deactivated/missing account. There is no anonymous reset
or registration endpoint.

### Sessions and hosting

Serve the SPA and `/api` behind one HTTPS origin, using the Admin.Server proxy or
Vite development proxy. Set `TemplarApi:BaseUrl` for Admin.Server and
`TEMPLAR_API_BASE_URL` for Vite to the intended API instance. Secure, HttpOnly,
SameSite=Strict cookies identify the browser session. Sessions are nonpersistent
and expire after 30 minutes without sliding renewal. The proxy forwards browser
cookies independently and does not cache them in its pooled HTTP handler.

Configure `IdentitySecurity:DataProtectionKeysPath` outside source control to a
durable, access-restricted directory. The default is `RuntimeData/IdentityKeys`.
Keep this directory outside public web assets and preserve it across deployment;
multiple API instances must share the same key ring/application name. Protect
runtime database and key storage with deployment permissions.

JSON API clients use GET `/api/v1/auth/antiforgery` to obtain a request token, then
send `X-Templar-CSRF` with login and every cookie-authenticated mutation. Use a
cookie jar, and refresh the request token after login, password change, and logout.
GET `/api/v1/auth/session` describes the current session and required password
change. OpenAPI describes the `AdminSession` cookie scheme. API errors use
401/403 ProblemDetails rather than HTML redirects. Passwords, hashes, cookies,
and tokens never belong in browser storage, bundles, URLs, or routine logs.

The SPA preserves unsaved drafts in memory during expiry so the same account can
sign in again. Explicit logout and account changes clear workspace state. Failed
mutations are not automatically replayed.

### User directory

The System workspace uses the shared account session. `Add pending user` stores
an Invited profile, sends no email, and grants no login access. The initial
administrator's TemplarAdmin membership cannot be removed through profile edits.
Its login name remains stable when its profile email changes. Active,
unrestricted TemplarAdmin identities can author and manage the directory; other
catalog memberships grant no permissions yet. Status/lifecycle controls and
additional credential issuance remain deferred.

The catalog still contains `Author`, `Developer`, `FormsEditor`, `TemplarAdmin`,
and planned `MarketingAutomationEditors`. Unknown/retired/differently cased/
duplicate role keys fail validation. Existing planned memberships may be
preserved or removed, but cannot be newly assigned. Empty and multiple
memberships remain valid for ordinary pending profiles; stale revisions return
409. No UX demo accounts are seeded. See [ADR-0009](docs/adr/ADR-0009-target-role-catalog-contract-and-boundary-validation.md)
for the catalog and [ADR-0015](docs/decisions/ADR-0015-admin-identity-and-delivery-authentication.md)
for the current identity boundary.

### Client-facing content consumers

Admin identity is the first implementation slice. Content API reads currently
require an unrestricted administrator session. Separate read-only delivery API
keys for site servers are the approved next slice; they are not implemented here.
Do not use the retired operator key or expose a private key in a public website's
JavaScript. Health, content-free API discovery, and rendered sample HTML remain
public; authoring and raw content API data do not.

## Goals

- Sitecore-inspired templates
- Template inheritance
- Content trees
- Shared, unversioned and versioned fields
- HATEOAS REST API
- .NET 8 runtime on a modern .NET SDK toolchain
- EF Core

See docs/architecture.md for full details.
