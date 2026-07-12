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
