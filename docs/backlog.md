# TemplarCMS Backlog

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