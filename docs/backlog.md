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
- Keep current endpoint contracts compatible with future `401` and
  `403` responses

### ProblemDetails Error Catalog

Status: Future

Problem:
The first API slice currently uses inline `ProblemDetails` titles and
details directly in endpoint handlers. That is acceptable for the
initial path lookup endpoint, but it risks inconsistent wording and
duplicated literals as the public API grows.

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

Potential APIs:

public interface ICookieService
{
    void Set<T>(...);
    T? Get<T>(...);
    bool Exists(...);
    void Delete(...);
}
