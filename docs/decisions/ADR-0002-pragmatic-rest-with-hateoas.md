# ADR-0002: Use Pragmatic REST with HATEOAS

**Status:** Accepted

**Date:** 2026-05-29

## Context

TemplarCMS needs a clean API surface for authoring, administration, and delivery. The API must support external consumers as well as the Vue-based admin interface.

The platform should expose predictable REST endpoints while still providing resource navigation through links. A pure hypermedia-only API would increase client complexity, while a plain CRUD API would lose useful discoverability and affordances.

GraphQL is useful for content delivery but is not required for the first implementation and should not delay the core platform.

## Decision

TemplarCMS will use pragmatic REST with HATEOAS affordances.

The API will use:

- Standard REST endpoints.
- HAL-inspired `_links`.
- `_embedded` for collections and nested resources when useful.
- OpenAPI/Swagger for developer documentation.
- API versioning from the beginning.
- `ProblemDetails` for error responses.

The API will not attempt to be a strict hypermedia-only API. Clients may use documented routes, but resource responses should still include useful links for navigation and follow-up actions.

## API Style

Initial routes will follow this pattern:

```text
/api/v1/templates
/api/v1/templates/{id}
/api/v1/templates/{id}/fields
/api/v1/content
/api/v1/content/{id}
/api/v1/content/{id}/values
/api/v1/content/{id}/children
```

Resource responses should include `_links`.

Example:

```json
{
  "id": "item-guid",
  "name": "Hello World",
  "path": "/home/articles/hello-world",
  "_links": {
    "self": { "href": "/api/v1/content/item-guid" },
    "template": { "href": "/api/v1/templates/template-guid" },
    "children": { "href": "/api/v1/content/item-guid/children" },
    "set-values": { "href": "/api/v1/content/item-guid/values" }
  }
}
```

## Link Relations

Initial link relations include:

| Relation | Purpose |
|---|---|
| self | Current resource |
| template | Template used by a content item |
| fields | Effective fields for a template |
| create-item | Create content from a template |
| set-values | Set content item field values |
| children | Child content items |
| parent | Parent content item |
| next | Next page in a collection |
| prev | Previous page in a collection |

## Error Handling

The API will use `ProblemDetails` for errors.

Example:

```json
{
  "type": "https://templarcms.dev/problems/template-key-conflict",
  "title": "Template key already exists",
  "status": 409,
  "detail": "Template key 'article' already exists."
}
```

## GraphQL

GraphQL is deferred to a later phase.

The REST API will be the first-class API for v1. GraphQL may be added later for delivery scenarios, especially after template definitions and effective templates are stable enough to support schema generation.

## Consequences

### Positive

- Familiar API style for most developers.
- Easier Vue admin integration.
- Supports discoverability through links.
- Keeps REST and OpenAPI as the primary contract.
- Avoids over-investing in GraphQL before the content model stabilizes.

### Negative

- Not a pure HATEOAS API.
- Clients may still hardcode some routes.
- HAL-inspired conventions require consistency across controllers.

## Future Work

- Define reusable HAL response models.
- Define API pagination conventions.
- Define relation names centrally.
- Add contract tests for `_links` and `ProblemDetails`.
- Revisit GraphQL after the content modeling engine stabilizes.
