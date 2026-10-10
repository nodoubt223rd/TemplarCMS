# TemplarCMS Agent Memory

Use this file as the quick-start memory for coding sessions in this repo. Keep it short, current, and opinionated. Link to deeper docs instead of duplicating them.

## Source Of Truth

- Start with [README.md](./README.md) for build, test, IIS smoke-test, and OpenAPI details.
- Use [docs/current-state-summary.md](./docs/current-state-summary.md) for the most accurate snapshot of what is already implemented.
- Use [docs/architecture.md](./docs/architecture.md) for product vision, boundaries, and longer-term design intent.
- Use ADRs in [docs/adr](./docs/adr) and [docs/decisions](./docs/decisions) before changing core modeling behavior.
- Treat [docs/backlog.md](./docs/backlog.md) as the place for deferred work, not this file.

## Project Snapshot

- TemplarCMS is a template-driven, API-first headless CMS on .NET 8.
- The solution includes `.NET 8` backend projects (`Abstractions`, `Domain`, `ContentModeling`, `Application`, `Persistence`, `Api`) and a **pure Vue.js & TypeScript SPA** for the `Admin` workspace (`./src/TemplarCMS.Admin/templarcms.admin.client`).
- Default branch is `trunk`.
- SDK is pinned in [global.json](./global.json) to `.NET SDK 8.0.400`.

## Architecture Guardrails

### Backend (.NET Core)
- Prefer `EffectiveTemplateDefinition` for runtime template consumption. Inheritance resolution is an earlier pipeline step.
- `TemplarCMS.Domain` owns runtime content concepts such as content items, field values, language/version value objects, resolved content shapes, and typed field value objects.
- `TemplarCMS.ContentModeling` owns template definitions, validation, inheritance resolution, effective template building, JSON mapping, and typed field conversion services.
- `TemplarCMS.Abstractions` owns shared contracts like `IContentRepository`.
- Stored field values remain `string?` at the persistence boundary. Typed values are projected and validated above storage.
- Content paths are computed at runtime, not stored. Existing item key and parent changes are intentionally blocked until explicit rename/move semantics exist.
- Ordered multiple template inheritance. Base templates are applied left-to-right, later bases override earlier bases by key, and local definitions override every inherited definition.
- Built-in system templates and starter content should remain source-controlled bootstrap data, not drift into instance-local `App_Data` truth.

### Frontend (Vue.js Admin SPA)
- **Architecture:** Purely decoupled single-page application. No Razor views or server-rendered templates.
- **Type Safety:** Enforce strict TypeScript types across all Vue components, composables, and API clients. Avoid using `any`.
- **State Management:** Uses Vue's built-in Composition API (`ref`, `reactive`, `computed`) and localized composables. **Do not introduce Pinia or Vuex.** Global application state lives in `./src/TemplarCMS.Admin/templarcms.admin.client/src/App.vue`. Reusable composables (e.g., `useTreeActions`, `useToast`) manage shared state patterns.
- **Styling:** Powered by Tailwind CSS v4 plus custom CSS wired via the Tailwind Vite plugin. The active master stylesheet is `./src/TemplarCMS.Admin/templarcms.admin.client/src/assets/author-workspace.css`. Keep all local styling scoped (`<style scoped>`) to avoid leaking design patterns.
- **UI Components:** Built entirely with custom Vue components. **Do not install Vuetify or other general-purpose component libraries.** Tiptap is explicitly used for rich-text editing workflows.
- **Dynamic Metadata:** The Vue client dynamically consumes server field-type metadata for both content editing and template design workflows. Ensure UI component state reactively scales with changes to backend layout or schema definitions.

### Vue 3 Style Guide & Best Practices
- **Composition API:** Always use `<script setup lang="ts">`. Do not use the Options API (`data`, `methods`, etc.).
- **Component Structure:** Follow a uniform internal structure: store/composable imports first, reactive refs/computed properties next, lifecycle hooks, and finally regular functions.
- **Reactivity Rules:** Use `ref()` for primitive values and `reactive()` strictly for objects/collections where structural mutation is required. Always destructure props using `toRefs()` or `defineProps` utilities to preserve reactivity.
- **Explicit Emits:** Declare all component events explicitly using `defineEmits()`.

## Working Conventions

- Preserve strong typing at domain, application, and frontend TypeScript boundaries.
- Prefer validation result objects over exceptions when following existing modeling patterns.
- Keep key comparisons and path handling normalized and case-insensitive where the current architecture expects it.
- When changing authoring or delivery contracts, keep `ProblemDetails`, HATEOAS links, and OpenAPI behavior aligned.
- If a change affects content modeling rules, resolution semantics, or storage conventions, update the relevant doc or ADR in the same pass.

## Build And Test

- Standard build: `dotnet build .\TemplarCMS.sln`
- Use the repo bootstrap when local NuGet or profile-path issues appear:
  - `. .\scripts\templar-cms-bootstrap.ps1`
  - `.\scripts\dev-shell.ps1`
- Preferred test helper:
  - `.\scripts\dotnet-test.ps1 -Project .\tests\TemplarCMS.Application.Tests\TemplarCMS.Application.Tests.csproj`
- The bootstrap script redirects `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, and `APPDATA` into repo-local `.tmp` paths to avoid machine-specific issues.

## Current Runtime Notes

- OpenAPI routes are enabled by default through the API app settings: `/openapi` and `/openapi/v1.json`.
- Authoring security is a lightweight API key gate for write endpoints and is controlled by the `AuthoringSecurity` configuration section.
- Persistence defaults to SQLite; set `Persistence:Provider` to `SqlServer` with an external `TemplarCms` connection string after applying `database/sqlserver/001-initial-schema.sql`.

## Memory Hygiene

- Add to this file only when the detail is stable, high-signal, and likely to unblock future sessions.
- Do not record temporary task state, one-off bugs, or details that already live clearly in code or tests.
