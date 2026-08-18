# TemplarCMS Agent Memory

Use this file as the quick-start memory for coding sessions in this repo. Keep it short, current, and opinionated. Link to deeper docs instead of duplicating them.

## Source Of Truth

- Start with [README.md](/E:/Projects/TemplarCMS/README.md) for build, test, IIS smoke-test, and OpenAPI details.
- Use [docs/current-state-summary.md](/E:/Projects/TemplarCMS/docs/current-state-summary.md) for the most accurate snapshot of what is already implemented.
- Use [docs/architecture.md](/E:/Projects/TemplarCMS/docs/architecture.md) for product vision, boundaries, and longer-term design intent.
- Use ADRs in [docs/adr](/E:/Projects/TemplarCMS/docs/adr) and [docs/decisions](/E:/Projects/TemplarCMS/docs/decisions) before changing core modeling behavior.
- Treat [docs/backlog.md](/E:/Projects/TemplarCMS/docs/backlog.md) as the place for deferred work, not this file.

## Project Snapshot

- TemplarCMS is a template-driven, API-first headless CMS on .NET 8.
- The solution currently includes `Abstractions`, `Domain`, `ContentModeling`, `Application`, `Persistence`, `Api`, and `Admin`, plus test projects for API, application, content modeling, domain, and integration coverage.
- Default branch is `master`.
- SDK is pinned in [global.json](/E:/Projects/TemplarCMS/global.json) to `.NET SDK 8.0.400`.

## Architecture Guardrails

- Prefer `EffectiveTemplateDefinition` for runtime template consumption. Inheritance resolution is an earlier pipeline step.
- `TemplarCMS.Domain` owns runtime content concepts such as content items, field values, language/version value objects, resolved content shapes, and typed field value objects.
- `TemplarCMS.ContentModeling` owns template definitions, validation, inheritance resolution, effective template building, JSON mapping, and typed field conversion services.
- `TemplarCMS.Abstractions` owns shared contracts like `IContentRepository`.
- Stored field values remain `string?` at the persistence boundary. Typed values are projected and validated above storage.
- Content paths are computed at runtime, not stored. Existing item key and parent changes are intentionally blocked until explicit rename/move semantics exist.
- Single template inheritance only. Child definitions override parent definitions by key.
- Built-in system templates and starter content should remain source-controlled bootstrap data, not drift into instance-local `App_Data` truth.

## Working Conventions

- Preserve strong typing at domain and application boundaries.
- Prefer validation result objects over exceptions when following existing modeling patterns.
- Keep key comparisons and path handling normalized and case-insensitive where the current architecture expects it.
- When changing authoring or delivery contracts, keep `ProblemDetails`, HATEOAS links, and OpenAPI behavior aligned.
- If a change affects content modeling rules, resolution semantics, or storage conventions, update the relevant doc or ADR in the same pass.

## Build And Test

- Standard build: `dotnet build .\TemplarCMS.slnx`
- Use the repo bootstrap when local NuGet or profile-path issues appear:
  - `. .\scripts\templar-cms-bootstrap.ps1`
  - `.\scripts\dev-shell.ps1`
- Preferred test helper:
  - `.\scripts\dotnet-test.ps1 -Project .\tests\TemplarCMS.Application.Tests\TemplarCMS.Application.Tests.csproj`
- The bootstrap script redirects `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, and `APPDATA` into repo-local `.tmp` paths to avoid machine-specific issues.

## Current Runtime Notes

- OpenAPI routes are enabled by default through the API app settings: `/openapi` and `/openapi/v1.json`.
- Authoring security is a lightweight API key gate for write endpoints and is controlled by the `AuthoringSecurity` configuration section.
- The Vue admin client consumes server field-type metadata for both content editing and template design.

## Memory Hygiene

- Add to this file only when the detail is stable, high-signal, and likely to unblock future sessions.
- Do not record temporary task state, one-off bugs, or details that already live clearly in code or tests.
