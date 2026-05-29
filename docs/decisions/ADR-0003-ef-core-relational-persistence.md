# ADR-0003: Use EF Core and Relational Persistence for MVP

**Status:** Accepted

**Date:** 2026-05-29

## Context

TemplarCMS needs durable storage for templates, content items, field values, languages, versions, and future publishing state.

The platform should start with a persistence model that is easy to understand, easy to migrate, easy to test, and familiar to .NET teams.

Possible approaches include:

- EF Core with a relational database.
- Raw SQL with hand-written repositories.
- Document database storage.
- Event sourcing from the beginning.
- Hybrid read/write models.

Event sourcing and CQRS may become useful later for publishing, workflow, audit history, and distributed integrations. However, those patterns would add significant complexity before the core content model is proven.

## Decision

TemplarCMS will use EF Core with relational persistence for the MVP.

The default database will be SQL Server.

PostgreSQL should remain a future-supported option, but SQL Server will be the default for initial development because it is familiar to many enterprise .NET and Sitecore teams.

The persistence layer will live in:

```text
TemplarCMS.Persistence
```

The domain entities will live in:

```text
TemplarCMS.Domain
```

The persistence layer may depend on the domain layer, but the domain layer must not depend on EF Core.

## Persistence Principles

The MVP will use:

- EF Core 8.
- SQL Server as the default provider.
- Migrations for schema changes.
- Explicit entity configurations where needed.
- Relational constraints for uniqueness and integrity.
- Application-layer validation for business rules.

Initial persisted concepts include:

- Template.
- TemplateSection.
- TemplateField.
- TemplateBaseTemplate.
- ContentItem.
- FieldValue.

## Repository Pattern

TemplarCMS will not introduce a repository abstraction by default for simple CRUD access.

Application services may use `DbContext` directly where appropriate.

Dedicated repositories may be introduced later only when they provide clear value, such as:

- Complex query encapsulation.
- Published content read models.
- Search projections.
- Specialized template lookup behavior.

Avoiding premature repository abstractions keeps the early codebase simpler.

## Audit Strategy

The MVP should include basic audit metadata where useful:

- CreatedUtc.
- UpdatedUtc.

A full audit history is deferred.

Future audit support may include:

- CreatedBy.
- UpdatedBy.
- Change history.
- Field-level revision history.
- Publishing history.

## Soft Delete Strategy

Soft delete is deferred for the earliest implementation.

The first version should focus on reliable create/read/update behavior.

Soft delete can be added later with clear semantics for:

- Content items.
- Templates.
- Field definitions.
- Published content.

## Event Sourcing

Event sourcing is explicitly deferred.

The project may introduce domain events, an outbox pattern, or event sourcing in a later phase if publishing, workflow, audit, or integration requirements justify it.

For v1, relational entities are the source of truth.

## Consequences

### Positive

- Familiar stack for .NET developers.
- Straightforward local development.
- Good migration support.
- Strong relational integrity.
- Lower implementation complexity.
- Easier onboarding for Sitecore-oriented teams.

### Negative

- Less natural append-only audit history than event sourcing.
- Requires care when evolving field value storage.
- Requires mapping between persistence entities and content modeling definitions.

## Future Work

- Add PostgreSQL support.
- Add audit tables.
- Add soft delete strategy.
- Add outbox pattern for publishing and integrations.
- Add read-optimized published content model.
- Evaluate event sourcing after the MVP stabilizes.
