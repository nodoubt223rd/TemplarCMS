# ADR-0008: Pragmatic Logging Baseline

## Status

Accepted

## Date

2026-07-12

## Context

TemplarCMS has reached the point where logging is beginning to spread
across multiple areas:

- content model catalog refresh and validation
- public API request handling
- future authoring operations
- future publishing and background processing

The codebase already uses `Microsoft.Extensions.Logging`, and
`ContentModelCatalog` has established the first meaningful logging
behavior. However, there has not yet been an explicit architectural
decision about how logging should be used across the system.

Without a baseline, the codebase risks drifting into inconsistent
patterns such as:

- logging every method entry and exit
- mixing diagnostic noise with business-significant events
- logging the same failure at multiple layers
- leaking request payloads or sensitive content into logs
- introducing sink-specific assumptions into application code

TemplarCMS needs a lightweight logging decision now so future work can
follow consistent rules without overdesigning observability before the
platform is ready.

## Decision

TemplarCMS will use `Microsoft.Extensions.Logging` as its logging
abstraction and will apply a pragmatic, structured logging baseline.

### Baseline Rules

- Use structured logs with named properties instead of string-built log
  messages where contextual values matter.
- Log meaningful state changes, failures, and lifecycle events.
- Do not log routine method entry, routine method exit, or other
  low-signal tracing noise by default.
- Prefer logging once at the boundary that owns the operational meaning
  of the event.
- Do not bake provider-specific behavior into application, domain, or
  API code.

### Layer Guidance

#### Domain

- Domain types should not depend on logging.
- Domain invariants should continue to fail through exceptions or return
  values rather than internal logging.

#### Application

- Application services may log orchestration-level events only when
  those events have operational value.
- Validation and business-rule failures should not automatically be
  logged at the application layer if they are expected to surface as
  normal API responses.

#### API

- API endpoints should avoid duplicating logs for failures already
  represented clearly in HTTP responses unless the event has operational
  importance.
- Write endpoints are higher-value logging candidates than simple reads.
- API logging should favor request identity, content item id, template
  id, and similar metadata over raw payload contents.

#### Infrastructure and Background Work

- Infrastructure components and future background processes may log
  lifecycle, retries, refreshes, integration boundaries, and unexpected
  failures.
- Catalog refresh, publishing, and import/export flows are especially
  good candidates for `Information`, `Warning`, and `Error` logs.

### Level Guidance

- `Information`: successful lifecycle events with operational value such
  as refresh completion, invalidation, or a significant write
  operation.
- `Warning`: recoverable or expected-but-important abnormal situations
  such as validation failures that stop a refresh or a blocked delete.
- `Error`: unexpected failures, unavailable dependencies, corrupted
  state, or unhandled exceptions.
- `Debug` and `Trace`: reserved for temporary diagnostics or future
  targeted troubleshooting, not as the default style.

### Sensitive Data Guidance

- Do not log raw field values, request bodies, secrets, tokens,
  connection details, or personally sensitive content by default.
- Prefer ids, keys, counts, and high-level state over content payloads.

## Consequences

### Positive

- Logging behavior can grow consistently without a large refactor later.
- The current `ContentModelCatalog` logging approach remains aligned
  with the architecture.
- Future API and authoring work has a clear default posture.
- The system stays compatible with different providers and deployment
  environments.

### Negative

- Some teams may want more detailed tracing than this baseline allows.
- Additional decisions will still be needed later for sinks,
  correlation, distributed tracing, audit logs, and production log
  retention.

## Future Work

- Decide whether API request correlation ids should become part of the
  standard logging contract.
- Decide how authentication and authorization events should be logged
  once secured authoring endpoints are introduced.
- Revisit audit logging separately from operational logging.
- Add provider and environment guidance when deployment architecture is
  more mature.
