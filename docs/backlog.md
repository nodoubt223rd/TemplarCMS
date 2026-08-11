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

Status: In Progress

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

Completed so far:
- Promoted canonical built-in template keys and starter content ids into
  shared domain contract types
- Added bootstrap drift warnings for structural seed-item mismatches
  such as wrong runtime ids, wrong template assignments, and canonical
  ids found in unexpected locations
- Kept bootstrap reconciliation non-destructive by preserving existing
  runtime items when drift is detected instead of silently replacing or
  relocating them
- Changed default home-field seeding to backfill only missing values so
  intentional runtime customization is not overwritten on every startup

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

Requested implementation slice:
- Add a dedicated slice to structure the authored sections and default
  fields that belong to the built-in `standard` template so the OOTB
  baseline is intentional rather than implicit.

Wave 1 implemented so far:
- Added the built-in `Content`, `Appearance`, `Help`, `Lifetime`,
  `Publishing`, `Statistics`, and `Version` sections to the source-
  controlled `standard` template
- Reserved the `__*` field-key namespace for built-in system fields
- Made the new built-in field storage scopes explicit instead of
  relying on default versioning behavior

Wave 1 deferred fields:
- `Appearance`
  `__Icon` (`Icon`)
  `__Preview` (`Page Preview`)
  `__Thumbnail` (`Thumbnail`)
- These remain intentionally deferred until dedicated field types and
  authoring/runtime behavior exist for them.

Wave 1 temporary mappings:
- `Help`
  `__Help link` currently maps to `General Link`, reusing the existing
  structured link support for internal and external references.
- `Publishing`
  `__Publishing groups` currently maps to `Multilist` so the built-in
  baseline can carry the field before checklist-specific behavior
  exists.

Wave 1 explicit semantics:
- `Lifetime`
  `__Hide version` is treated as a shared system toggle, while
  `__Valid from` and `__Valid to` are unversioned so lifecycle windows
  can vary by language without duplicating per version.
- `Statistics`
  audit fields such as `__Created`, `__Created by`, `__Updated`, and
  `__Updated by` are shared system bookkeeping rather than per-version
  author content.

Requested standard-template sections:
- `Advanced`
- `Appearance`
- `Help`
- `Insert Options`
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

Removed from the current target:
- `Layout`
- `Item Buckets`

Candidate field inventory from the latest Sitecore reference review:
- `Advanced`
  `__Enable item fallback` (`Checkbox`)
  `__Enforce version presence` (`Checkbox`)
  `__Source Item` (`Version Link`)
  `__Source` (`Version Link`)
  `__Standard values` (`Droptree`)
  `__Tracking` (`Tracking`)
- `Appearance`
  `__Context Menu` (`Droptree`)
  `__Display name` (`Single-Line Text`)
  `__Editor` (`Server File`)
  `__Editors` (`TreelistEx`)
  `__Hidden` (`Checkbox`)
  `__Icon` (`Icon`)
  `__Originator` (`Droptree`)
  `__Preview` (`Page Preview`)
  `__Read Only` (`Checkbox`)
  `__Ribbon` (`Droptree`)
  `__Skin` (`Single-Line Text`)
  `__Sortorder` (`Single-Line Text`)
  `__Style` (`Single-Line Text`)
  `__Subitems Sorting` (`Droplink`)
  `__Thumbnail` (`Thumbnail`)
- `Help`
  `__Help link` (`General Link`)
  `__Long description` (`Multi-Line Text`)
  `__Short description` (`Single-Line Text`)
- `Layout`
  `__Content Test` (`Droptree`)
  `__Controller Action` (`Text`)
  `__Controller` (`Text`)
  `__Final Renderings` (`Layout`)
  `__Page Level Test Set Definition` (`Datasource`)
  `__Presets` (`Treelist`)
  `__Renderers` (`Multi-Line Text`)
  `__Renderings` (`Layout`)
- `Lifetime`
  `__Hide version` (`Checkbox`)
  `__Valid from` (`DateTime`)
  `__Valid to` (`DateTime`)
- `Indexing`
  `__Boost` (`Single-Line Text`)
  `__Boosting Rules` (`TreelistEx`)
  `__Facets` (`Treelist`)
- `Insert Options`
  `__Insert Rules` (`TreelistEx`)
  `__Masters` (`TreelistEx`)
- `Item Buckets`
  `__Bucket Parent Reference` (`Droptree`)
  `__Bucketable` (`Checkbox`)
  `__Default Bucket Query` (`Query Builder`)
  `__Default View` (`Droplist`)
  `__Enabled Views` (`Multilist`)
  `__Is Bucket` (`Checkbox`)
  `__Persistent Bucket Filter` (`Query Builder`)
  `__Quick Actions` (`Multilist`)
  `__Should Not Organize In Bucket` (`Checkbox`)
- `Publishing`
  `__Never publish` (`Checkbox`)
  `__Publish` (`DateTime`)
  `__Publishing groups` (`Checklist`)
  `__Unpublish` (`DateTime`)
- `Security`
  `__Owner` (`Single-Line Text`)
  `__Security` (`Security`)
- `Statistics`
  `__Created by` (`Single-Line Text`)
  `__Created` (`DateTime`)
  `__Revision` (`Single-Line Text`)
  `__Updated by` (`Single-Line Text`)
  `__Updated` (`DateTime`)
- `Tagging`
  `__Semantics` (`Multilist with Search`)
- `Tasks`
  `__Archive date` (`DateTime`)
  `__Archive Version date` (`DateTime`)
  `__Reminder date` (`DateTime`)
  `__Reminder recipients` (`Single-Line Text`)
  `__Reminder text` (`Multi-Line Text`)
- `Validators`
  `__Quick Action Bar Validation Rules` (`TreelistEx`)
  `__Suppressed Validation Rules` (`Multi-Line Text`)
  `__Validate Button Validation Rules` (`TreelistEx`)
  `__Validator Bar Validation Rules` (`TreelistEx`)
  `__Workflow Validation Rules` (`TreelistEx`)
- `Workflow`
  `__Default workflow` (`Droptree`)
  `__Lock` (`Multi-Line Text`)
  `__Workflow state` (`Droptree`)
  `__Workflow` (`Droptree`)
- `Version`
  `__Version Name` (`Single-Line Text`)

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
- Some `standard` template fields may remain intentionally hidden from
  lower-privilege authoring accounts even when they exist in the model.
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

### Template Designer Information Density

Status: Future

Problem:
The template designer is gaining more inherited structure, field-type
detail, and standard-template surface area, which makes the screen
busier and harder to scan during longer authoring sessions.

Goals:
- Make template sections collapsible so authors can focus on one area
  at a time
- Reduce visual clutter in the inherited preview and local section
  editor flows
- Keep important validation and inheritance context visible without
  forcing every section fully open all the time

### Frontend Unit Test Coverage

Status: Future

Problem:
The Vue admin experience now contains meaningful field-editor logic and
template-authoring behavior, but it still relies mostly on manual
verification and build-time type checks.

Goals:
- Add frontend unit tests around the admin field editor registry
- Expand component-level coverage beyond `General Link` and template
  designer interactions

Completed:
- Covered structured `General Link` internal/external editing behavior
- Covered legacy value upgrade behavior in the `General Link` editor
- Added confidence around template designer field-type selection and
  editor rendering decisions
