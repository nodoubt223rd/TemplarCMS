# TemplarCMS Backlog

## API

### Authoring Endpoint Security

Status: Future

Problem:
The first public write endpoints can be shaped before authentication
and authorization are fully implemented, but authoring operations must
not remain unsecured once the API moves beyond early development.

Goals:
- Treat `POST`, `PUT`, `PATCH`, and `DELETE` endpoints as authoring
  operations
- Add authentication before the authoring API is considered
  production-ready
- Add authorization policies for content creation, update, and delete
  operations
- Add end-to-end HTTP tests that exercise `401`, `403`, and successful
  API-key authoring flows through the real ASP.NET Core pipeline
- Keep current endpoint contracts compatible with future `401` and
  `403` responses

### ProblemDetails Error Catalog

Status: In Progress

Problem:
The API now has an initial centralized `ProblemDetails` catalog for the
current content and template endpoints, but the broader error-contract
story is not complete yet.

Goals:
- Centralize reusable `ProblemDetails` titles and detail messages
- Keep HTTP error responses consistent across endpoints
- Avoid ad hoc hardcoded strings scattered through API handlers
- Leave room for stable error codes or typed factory helpers later

### Delete Semantics

Status: Future

Problem:
The current delete endpoints focus on basic lifecycle behavior, but CMS
authoring needs richer deletion semantics before those APIs are treated
as settled.

Goals:
- Decide whether delete means hard delete, soft delete, or recycle-bin
  by default
- Decide how parent-child delete impact is surfaced before destructive
  operations
- Decide whether cascading child deletion requires explicit client
  confirmation or a preflight step
- Define restore semantics for soft-deleted or recycled content
- Define retention and garbage-collection behavior for recycled content
- Keep current delete contracts compatible with a future recycle-bin or
  restore workflow

### System Seed Data Contract

Status: Future

Problem:
TemplarCMS now relies on stable out-of-box templates, fields, and root
items with fixed ids, but the long-term contract for where canonical
system data lives and how upgrades reconcile it with instance state is
not fully documented.

Goals:
- Keep built-in system templates, fields, and starter items defined in
  source-controlled code or seed artifacts rather than treating runtime
  `App_Data` contents as the product truth
- Preserve fixed ids for canonical system items so patches and support
  tooling can target the same objects across instances
- Separate mutable runtime storage from canonical built-in definitions
- Define how bootstrap or upgrade logic handles drift, missing items,
  and intentional local customization

## Content Modeling

### Standard Template Parity

Status: Future

Problem:
The seeded `standard` template is intentionally lightweight today, but
business has now provided a much fuller Sitecore-style target shape for
the long-term standard template and its system sections.

Goals:
- Evolve the default `standard` template toward a richer authored
  baseline rather than keeping it as a minimal placeholder
- Preserve the requested section layout under
  `templar/templates/systemTemplates/Sections`
- Treat the request as both a template-shape backlog item and a field
  type demand signal for future editor/runtime work
- Keep the current bootstrap-friendly default content structure until
  the richer standard template can be introduced intentionally

Requested standard-template sections:
- `Advanced`
- `Appearance`
- `Help`
- `Layout`
- `Lifetime`
- `Indexing`
- `Publishing`
- `Security`
- `Statistics`
- `Tagging`
- `Tasks`
- `Validators`
- `Workflow`
- `Version`

Field-type signal from this request:
- Already represented in the request: `Checkbox`, `Single-Line Text`,
  `Multi-Line Text`, `Droplink`, `Droptree`, `Treelist`, `TreelistEx`,
  `Checklist`, `DateTime`, `Layout`, `Datasource`, `Page Preview`,
  `General Link`, `Version Link`, `Icon`, `Thumbnail`, `Tracking`,
  `Security`, `Multilist with Search`, `Server File`, and plain `Text`
- Notably absent from this business-provided standard-template target:
  `Integer` and a decimal-oriented field type

Notes:
- This input is valuable because it reveals which field editors business
  expects to see in a realistic CMS authoring baseline.
- The current typed-value/runtime work should stay compatible with these
  future field types even if the admin editor support arrives later.

## Runtime

### Cookie Management

Status: Future

Problem:
Applications frequently implement cookie creation and management differently.

Goals:
- Provide ICookieService
- Provide ICookieSerializer
- Consistent cookie creation
- Consistent deletion
- Default security settings
- Strongly typed cookie definitions

### CLI Exploration

Status: Future

Problem:
TemplarCMS currently relies on API-first and admin-UI-first workflows,
but developer and operator scenarios may benefit from a dedicated CLI.

Goals:
- Explore a `templar` CLI for bootstrap, diagnostics, patching, and
  environment inspection
- Consider safe commands for system seed verification, content model
  refresh, health checks, and support workflows
- Keep any future CLI aligned with the stable built-in ids and
  source-controlled system data contract

### Multi-Database Strategy

Status: Future

Problem:
Sitecore-style CMS operations often separate authoring, delivery, and
core system concerns into distinct databases, while TemplarCMS
currently uses a single runtime persistence model.

Goals:
- Evaluate whether TemplarCMS should eventually distinguish authoring
  (`master`-like), published delivery (`web`-like), and admin/system
  (`core`-like) storage concerns
- Document which responsibilities belong in each store if TemplarCMS
  grows beyond a single database
- Keep current persistence choices compatible with a future publishing
  database split rather than assuming one database forever

Potential APIs:

public interface ICookieService
{
    void Set<T>(...);
    T? Get<T>(...);
    bool Exists(...);
    void Delete(...);
}

## Admin UI

### Frontend Unit Test Coverage

Status: Future

Problem:
The Vue admin experience now contains meaningful field-editor logic and
template-authoring behavior, but it still relies mostly on manual
verification and build-time type checks.

Goals:
- Add frontend unit tests around the admin field editor registry
- Cover structured `General Link` internal/external editing behavior
- Cover legacy value upgrade behavior in the `General Link` editor
- Add confidence around template designer field-type selection and
  editor rendering decisions
