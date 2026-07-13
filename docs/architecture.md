# TemplarCMS Architecture Document

## 1. Executive Summary

**TemplarCMS** is a template-driven, API-first headless CMS built on .NET. Its core purpose is to provide Sitecore-inspired content modeling capabilities—data templates, template inheritance, field sections, field types, content trees, language versions, and versioned field values—while exposing content and authoring operations through a HATEOAS-driven REST API.

The first version of TemplarCMS focuses on the foundation: content schema modeling, content item creation, field value persistence, and RESTful navigation using HAL-style responses. Later phases can add workflow, publishing, media management, search indexing, GraphQL, authorization, and a visual content authoring UI.

Current implementation note:

The repository is still in the foundation phase. The codebase currently
contains the domain, content modeling, application, abstractions, and
persistence projects. API, contracts, infrastructure, and admin UI
projects described in this document remain planned rather than
implemented.

## 2. Product Vision

TemplarCMS is intended for teams that want the modeling power of enterprise CMS platforms without the full weight of a monolithic CMS. It should feel familiar to developers who have worked with Sitecore templates, fields, inheritance, language versions, and content trees, while remaining modern, lightweight, and cloud-friendly.

### Vision Statement

TemplarCMS is an enterprise-grade, template-first headless CMS for .NET teams that need structured content modeling, clean APIs, and extensible delivery architecture.

## 3. Core Goals

TemplarCMS should provide:

- Sitecore-style data templates.
- Template sections and fields.
- Template inheritance.
- Shared, unversioned, and versioned field semantics.
- Hierarchical content items.
- Language-aware and version-aware field values.
- HATEOAS REST API responses.
- HAL-compatible `_links` and `_embedded` structures.
- EF Core persistence.
- Clean separation between Domain, Application, Persistence, API, and Tests.
- Future extensibility for publishing, workflow, media, search, GraphQL, and rendering SDKs.
- Vue.js as the primary admin/content authoring UI direction.

## 4. Non-Goals for MVP

The initial implementation will not include:

- Visual page editor.
- Experience Editor-style rendering composition.
- Full workflow engine.
- User/role management.
- Media library.
- Publishing targets.
- Search indexing.
- GraphQL.
- Personalization rules.
- Multi-site management.
- SaaS tenant provisioning.

These are candidates for future phases.

## 5. Solution Structure

```text
TemplarCMS/
├── TemplarCMS.sln
├── src/
│   ├── TemplarCMS.Abstractions/
│   ├── TemplarCMS.Domain/
│   ├── TemplarCMS.ContentModeling/
│   ├── TemplarCMS.Application/
│   └── TemplarCMS.Persistence/
└── tests/
    ├── TemplarCMS.Application.Tests/
    ├── TemplarCMS.ContentModeling.Tests/
    └── TemplarCMS.Integration.Tests/
```

Planned later:

```text
src/TemplarCMS.Api
src/TemplarCMS.Contracts
src/TemplarCMS.Infrastructure
admin/TemplarCMS.Admin
tests/TemplarCMS.Api.Tests
tests/TemplarCMS.Domain.Tests
```

### Project Responsibilities

#### TemplarCMS.Abstractions

Contains cross-layer contracts shared between application, persistence,
and other assemblies:

- `IContentRepository`
- future service and infrastructure contracts that span projects

This project may depend on the domain layer when contract signatures
need domain types, but it should not depend on concrete persistence or
API implementations.

#### TemplarCMS.Domain

Contains core entities and domain concepts:

- `ContentItemDefinition`
- `ContentItemId`
- `ContentItemKey`
- `ContentFieldValue`
- `FieldId`
- `ContentLanguage`
- `ContentVersion`
- `TemplateId`
- `FieldValueResolutionContext`
- `ResolvedContentItem`
- `FieldValueScope`
- `TypedFieldValue`
- `StringTypedFieldValue`
- `IntegerTypedFieldValue`
- `DecimalTypedFieldValue`
- `DateTimeTypedFieldValue`
- `BooleanTypedFieldValue`
- `NullTypedFieldValue`

This project should avoid infrastructure dependencies.

#### TemplarCMS.ContentModeling

Contains template-specific schema and template mechanics:

- public modeling contracts such as content resolution abstractions
- `TemplateDefinition`
- `TemplateSectionDefinition`
- `FieldDefinition`
- `InheritedTemplateDefinition`
- `EffectiveTemplateDefinition`
- `FieldType`
- typed field value conversion services
- template inheritance resolution
- effective template construction
- template validation
- template serialization and repository mapping

Note:

The typed runtime value objects themselves live in `TemplarCMS.Domain`.
`TemplarCMS.ContentModeling` currently owns the converter abstractions
and implementations that project stored string values into those domain
types.

#### TemplarCMS.Application

Contains use cases and application services:

- `ContentItemService`
- future template-oriented application services

Application services orchestrate domain concepts, content modeling
services, and shared contracts. They should not own persistence details
or schema storage concerns.

#### TemplarCMS.Persistence

Contains EF Core persistence:

- CmsDbContext
- Entity mappings
- Migrations
- Repository implementations, if needed

#### TemplarCMS.Api

Contains ASP.NET Core REST API:

- Controllers
- HAL/HATEOAS response models
- API versioning
- Swagger/OpenAPI configuration
- Error handling middleware

#### TemplarCMS.Contracts

Contains public DTOs and API contract models.

This keeps request/response contracts independent from the API implementation.

#### TemplarCMS.Infrastructure

Reserved for cross-cutting infrastructure:

- Caching
- File storage
- External search
- Publishing adapters
- Email
- Background jobs

#### TemplarCMS.Admin

Contains the Vue.js authoring and administration interface:

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router
- Component-driven field editors
- Template metadata-driven forms

## 6. Domain Model

### Template

A template defines the structure of a content item. It contains sections and can inherit from a single base template.

Key properties:

- Id
- Name
- Key
- Description
- Sections
- BaseTemplate
- CreatedUtc
- UpdatedUtc

### TemplateSection

A section groups fields within a template. This mirrors the familiar Sitecore template section concept.

Key properties:

- Id
- TemplateId
- Name
- SortOrder
- Fields

### TemplateField

A field defines a single editable piece of content.

Key properties:

- Id
- TemplateSectionId
- Name
- Key
- Type
- IsShared
- IsUnversioned
- Source
- SettingsJson
- SortOrder

### InheritedTemplateDefinition

Represents a template after inheritance resolution but before effective template generation.

Key properties:

- Template
- InheritanceChain

### EffectiveTemplateDefinition

Represents the flattened runtime view after base-to-derived inheritance has been applied.

Key properties:

- Id
- Name
- Key
- Sections
- Fields

### ContentItem

A content item is an instance of a template and can exist in a tree.

Key properties:

- Id (`ContentItemId`)
- TemplateId (`TemplateId`)
- ParentId (`ContentItemId?`)
- Name
- Key (`ContentItemKey`)

Current note:

The current domain model stores a normalized `ContentItemKey` rather
than separate slug and path properties. The key is normalized to
lowercase and whitespace is collapsed to hyphen-separated segments such
as `home-page`. Path is computed at runtime from the content tree rather
than stored on the content item record.

### FieldValue

Stores actual content values for an item and field.

Key properties:

- ItemId
- FieldId
- FieldKey
- Language
- Version
- Value

Storage note:

Stored field values remain `string?` at the persistence boundary. Typed
runtime projection is layered above storage rather than changing the
stored shape.

Write semantics note:

Persisting field values merges into the current stored set for the
content item. Writes upsert by content item, field, language, and
version identity while preserving unrelated stored values.

## 7. Field Semantics

TemplarCMS supports three field value modes.

### Shared Fields

A shared field has one value for all languages and versions.

Storage convention:

```text
Language = implementation-defined shared marker
Version = 0
```

### Unversioned Fields

An unversioned field varies by language but not by version.

Storage convention:

```text
Language = selected language
Version = 0
```

### Versioned Fields

A versioned field varies by language and version.

Storage convention:

```text
Language = selected language
Version = selected version
```

## 8. Template Inheritance

Template inheritance allows common fields to be defined once and reused.

Example:

```text
BaseContent
    ↓
BasePage
    ↓
ArticlePage
```

The effective field list for `ArticlePage` includes fields from each template in the chain, resolved from root base template to derived template.

Current content-modeling pipeline:

```text
JSON
    ↓
JsonTemplateRepository
    ↓
TemplateDefinition
    ↓
TemplateValidator
    ↓
TemplateInheritanceResolver
    ↓
InheritedTemplateDefinition
    ↓
EffectiveTemplateBuilder
    ↓
EffectiveTemplateDefinition
```

For example, if `BaseContent`, `BasePage`, and `ArticlePage` define fields, the effective field list for `ArticlePage` includes the merged result of that ordered chain.

Example effective field list:

- Meta Title
- Meta Description
- Title
- Body

Inheritance rules:

- Single inheritance only.
- Base template fields are resolved first.
- Child template fields are resolved after base fields.
- Child definitions override parent definitions by key.
- Sections with matching keys are merged case-insensitively.
- If a child field has the same key as a base field, the child definition wins.
- Cycles are not allowed.

## 9. Content Tree

Content items can be arranged hierarchically.

Example:

```text
/content
  /home
    /articles
      /hello-world
```

Each content item has:

- ParentId
- Key
- Computed Path

The current implementation computes path from the parent path and item
key.

Example:

```text
Parent path: /home/articles
Key: hello-world
Generated path: /home/articles/hello-world
```

Current runtime rules:

- Root item `home` resolves to `/home`.
- Child item `articles` under `/home` resolves to `/home/articles`.
- Path is projected in runtime read models rather than stored in persistence.
- Renaming or moving an existing item is currently blocked until explicit
  move/rename semantics are designed.

### Path Lookup Contract

Current implementation contract:

- Path lookup uses canonical absolute content paths.
- Path normalization is lowercase and slash-delimited.
- Route or client casing differences normalize to the same `ContentPath`.
- Sibling keys must remain unique so a parent path plus key identifies at
  most one child.
- Path lookup is currently a read concern; item ids remain the stable write
  identity.
- Missing ancestors are treated as invalid stored state rather than as a
  partial match.

Examples:

```text
/home
/home/articles
/home/articles/hello-world
```

## 10. REST API Design

The REST API should use standard REST endpoints with pragmatic HATEOAS affordances.

Every resource response should include `_links`.

Collections can include `_embedded`.

Example:

```json
{
  "Id": "item-guid",
  "Name": "Hello World",
  "Path": "/home/articles/hello-world",
  "_links": {
    "self": {
      "href": "/api/v1/content/item-guid"
    },
    "template": {
      "href": "/api/v1/templates/template-guid"
    },
    "children": {
      "href": "/api/v1/content/item-guid/children"
    },
    "set-values": {
      "href": "/api/v1/content/item-guid/values"
    }
  }
}
```

## 11. Link Relations

The initial API should support these link relations:

| Relation | Purpose |
|---|---|
| self | Current resource |
| template | Template used by a content item |
| fields | Effective fields for a template |
| dependencies | Delete-impact dependencies for a template |
| create-item | Create content from a template |
| set-values | Set content item field values |
| children | Child content items |
| parent | Parent content item |
| next | Next page in a collection |
| prev | Previous page in a collection |

## 12. API Endpoints

### Templates

```http
POST /api/v1/templates
GET /api/v1/templates/{id}
GET /api/v1/templates/{id}/fields
GET /api/v1/templates/{id}/dependencies
```

### Content

```http
POST /api/v1/content
GET /api/v1/content/{id}?lang=en&version=1
GET /api/v1/content/by-path/{**path}?lang=en&version=1
GET /api/v1/content/root/children?lang=en&version=1
POST /api/v1/content/{id}/values
GET /api/v1/content/{id}/children?page=1&pageSize=20
GET /api/v1/content/{id}/dependencies?lang=en&version=1
```

Authoring security note:

- `POST`, `PUT`, `PATCH`, and `DELETE` routes are authoring operations.
- Early development slices may shape these contracts before full auth is
  in place.
- Authentication and authorization must be added before write endpoints
  are treated as production-ready API surface.

Path lookup route note:

- The public route accepts a slash-delimited relative route segment such as
  `home/articles/hello-world`.
- The API normalizes that segment into the canonical absolute
  `ContentPath` value `/home/articles/hello-world` before application-layer
  lookup.
- Responses should return the canonical absolute path so clients can store
  one stable representation.

## 13. Example Template Creation Request

```json
{
  "Name": "Article",
  "Key": "article",
  "BaseTemplate": null,
  "Sections": [
    {
      "Name": "Content",
      "SortOrder": 100,
      "Fields": [
        {
          "Name": "Title",
          "Key": "title",
          "Type": "SingleLineText",
          "IsUnversioned": true
        },
        {
          "Name": "Body",
          "Key": "body",
          "Type": "RichText"
        }
      ]
    }
  ]
}
```

The authored JSON repository format is documented separately. It uses
`baseTemplates` in JSON, but the repository resolves that into the single
`BaseTemplate` domain relationship. Zero or one base template key is supported;
multiple entries are rejected.

### Template Dependency Response

`GET /api/v1/templates/{id}/dependencies` exposes the current blockers for a
safe template delete attempt.

Current behavior:

- `dependentTemplates` includes authored descendant templates that inherit
  directly or indirectly from the requested template
- `contentItems` includes stored content items assigned directly to the
  requested template
- `canDelete` is `true` only when both dependency collections are empty
- This endpoint is a read-only preflight contract and does not imply cascade,
  recycle-bin, or restore semantics

### Content Dependency Response

`GET /api/v1/content/{id}/dependencies?lang=en&version=1` exposes the current
blockers for a safe content delete attempt.

Current behavior:

- `children` includes direct child content items only
- `canDelete` is `true` only when the direct child collection is empty
- Child responses include canonical resolved paths and item links in the
  requested language/version context
- This endpoint is a read-only preflight contract and does not imply cascade,
  soft-delete, recycle-bin, or restore semantics

## 14. Example Template Response

```json
{
  "Id": "template-guid",
  "Name": "Article",
  "Key": "article",
  "_links": {
    "self": {
      "href": "/api/v1/templates/template-guid"
    },
    "fields": {
      "href": "/api/v1/templates/template-guid/fields"
    },
    "create-item": {
      "href": "/api/v1/content"
    }
  }
}
```

## 15. Example Content Creation Request

```json
{
  "TemplateId": "template-guid",
  "Name": "Hello World",
  "ParentId": null,
  "Key": "hello-world"
}
```

## 16. Example Field Value Request

```json
{
  "Language": "en",
  "Version": 1,
  "Values": {
    "title": "Hello World",
    "body": "<p>This is the first article.</p>"
  }
}
```

## 17. Persistence Design

TemplarCMS will use EF Core for persistence.

Recommended starting database:

- SQL Server for local enterprise-style development.
- PostgreSQL as an optional alternative for cloud-native hosting.

Initial indexes:

- Template.Key unique.
- ContentItem.ParentId + ContentItem.Key unique.
- FieldValue.ItemId + FieldValue.FieldId + FieldValue.Language + FieldValue.Version indexed.
- Template field keys unique within their template definition rules.

## 18. Validation Strategy

Validation should happen at the application service layer.

Initial validations:

- Template key must be unique.
- Field keys must be normalized.
- Field keys must be unique within a template section.
- Duplicate field keys across a template are rejected unless they are valid inheritance overrides.
- Section and field key collisions are rejected.
- Key comparisons are case-insensitive.
- Template inheritance cycles are rejected.
- Content item template must exist.
- Content item keys must be normalized and unique among siblings.
- Content item paths are computed from the current parent chain and normalized item keys.
- Content item key changes and parent changes are currently rejected for existing items until move/rename semantics are explicitly implemented.
- Field values can only be set for fields defined by the item’s effective template.
- Field values should match their declared field type before persistence.
- Field value writes should merge by item, field, language, and version identity rather than replacing unrelated stored values.
- Delete semantics remain intentionally open for a later product decision.
- Future work should decide whether author-facing delete uses hard delete,
  soft delete, or a recycle-bin workflow with retention and restore.
- Future work should also define how parent-child delete impact is
  surfaced before cascading operations are allowed.

## 19. Error Handling

The API should use `ProblemDetails` for errors.

Example:

```json
{
  "type": "https://templarcms.dev/problems/template-key-conflict",
  "title": "Template key already exists",
  "status": 409,
  "detail": "Template key 'article' already exists."
}
```

## 20. API Versioning

The API should be versioned from the beginning.

Initial route format:

```text
/api/v1/...
```

Future versions:

```text
/api/v2/...
```

Versioning gives freedom to evolve the contract without breaking existing consumers.

## 21. Frontend Strategy

TemplarCMS will use Vue.js as the primary frontend technology for the authoring/admin experience.

Recommended frontend stack:

- Vue 3
- TypeScript
- Vite
- Pinia for state management
- Vue Router
- Component-driven field editors
- Admin UI generated from template metadata

The admin UI should consume the REST API using the same HATEOAS links exposed to external clients. This keeps the UI aligned with the API contract and avoids route duplication where practical.

Future delivery starters may include Nuxt for public website rendering and Blazor for .NET-centric teams.

## 22. Testing Strategy

### Unit Tests

Unit tests should cover:

- Template creation.
- Template inheritance resolution.
- Inheritance cycle detection.
- Field key normalization.
- Shared field storage behavior.
- Unversioned field storage behavior.
- Versioned field storage behavior.
- Content path generation.

### Integration Tests

Integration tests should cover:

- Creating templates through the API.
- Creating content items through the API.
- Setting field values through the API.
- Reading content items through the API.
- Validating HAL links.
- Validating API version routes.

### API Contract Tests

Contract tests should verify:

- `_links.self` exists on resource responses.
- `_embedded` exists on collection responses where applicable.
- Pagination links appear correctly.
- Error responses use `ProblemDetails`.

## 23. Roadmap

### Phase 1: Core CMS Foundation

- Domain model.
- EF Core persistence.
- Template creation.
- Template inheritance.
- Content item creation.
- Field value storage.
- HATEOAS REST API.
- Swagger/OpenAPI.
- Basic tests.

### Phase 2: Authoring Improvements

- Vue.js admin UI.
- Template builder UI.
- Content editor UI.
- Field type-specific editors.
- Content validation messages.
- Draft states.

### Phase 3: Publishing

- Publishing states.
- Publish queue.
- Published read model.
- Cache invalidation.
- CDN-friendly delivery APIs.

### Phase 4: Media and Assets

- Media library.
- File upload.
- Image metadata.
- Asset references.
- CDN asset URLs.

### Phase 5: Search

- Search indexing.
- Content search API.
- Field-level indexing configuration.
- Search provider abstractions.

### Phase 6: GraphQL

- GraphQL delivery API.
- Template-generated GraphQL types.
- Strongly typed content queries.

### Phase 7: Rendering SDKs

- Nuxt delivery starter.
- Blazor starter.
- ASP.NET Core rendering starter.
- Component mapping.

### Phase 8: Advanced Enterprise Features

- Workflow.
- Approval chains.
- Role-based access control.
- Multi-site support.
- Personalization rules.
- Audit history.

## 24. Initial Repository Deliverables

The first repository commit should include:

```text
README.md
docs/architecture.md
src/
tests/
.gitignore
.editorconfig
global.json
TemplarCMS.sln
```

The first implementation commit should include:

- Domain entities.
- EF Core DbContext.
- TemplateService.
- ItemService.
- HAL response models.
- TemplatesController.
- ContentController.
- Initial smoke tests.

## 25. README Summary

The repository README should describe TemplarCMS as:

> TemplarCMS is a template-driven, API-first headless CMS built on .NET. It provides Sitecore-inspired content modeling features such as data templates, template inheritance, field sections, language versions, and versioned field values, exposed through a pragmatic HATEOAS REST API.

## 26. Open Design Questions

Before implementation, the following should be confirmed:

1. Target framework: .NET 8 LTS or .NET 9.
2. Primary database: SQL Server or PostgreSQL.
3. Whether REST should use pure HAL or a custom HAL-inspired format.
4. Whether the current single-inheritance model is sufficient for foreseeable authoring scenarios.
5. Whether content paths should be stored or computed once path semantics are introduced.
6. Whether field value history should be retained after edits.
7. Whether publishing should be part of the first release or deferred.

Already decided in code and ADRs:

- Strong domain typing is preferred at domain and application
  boundaries.
- Stored field values remain string-based at the persistence boundary.
- Typed runtime values are projected during resolution and validated on
  writes for supported field types.
- `ContentItemKey` is normalized to lowercase hyphenated form.
- Content paths are computed at runtime rather than stored.
- `Microsoft.Extensions.Logging` is the logging abstraction, with a
  pragmatic structured logging baseline rather than verbose method-level
  tracing.

## 27. Recommended MVP Defaults

Recommended choices for the first implementation:

- .NET 8 LTS.
- SQL Server.
- Pragmatic REST + HATEOAS.
- HAL-inspired JSON.
- Vue.js admin UI.
- String/JSON field value storage.
- Typed runtime field projection layered above string storage.
- Single base template inheritance.
- Normalized content item keys.
- Computed runtime path strategy.
- No publishing in MVP.
- No auth in MVP.
- REST authoring and delivery API first.
- GraphQL deferred.

## 28. Conclusion

TemplarCMS should begin as a clean, focused content modeling engine with a HATEOAS-driven REST API. By keeping the MVP centered on templates, inheritance, content items, field values, and navigation links, the project establishes a strong foundation without overcommitting to advanced CMS features too early.

The architecture should remain modular so that publishing, workflow, search, media, GraphQL, Vue-based authoring, and rendering SDKs can be added incrementally.
