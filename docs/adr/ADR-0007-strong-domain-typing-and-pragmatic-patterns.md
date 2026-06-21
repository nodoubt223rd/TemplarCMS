# ADR-0007: Strong Domain Typing and Pragmatic Patterns

## Status

Accepted

## Date

2026-06-20

## Context

TemplarCMS is building a domain model around templates, content items,
field values, language/version resolution, and hierarchical content
behavior.

Several parts of the codebase already lean toward explicit modeling:

- `ContentItemId`
- `TemplateId`
- `ContentItemKey`
- `ContentLanguage`
- `ContentVersion`
- dedicated application services
- dedicated repositories
- dedicated resolver and builder pipelines

As the system grows, there is a risk of drifting back toward
primitive-heavy APIs such as `Guid`, `string`, and `int` values being
passed through every layer without clearly expressing intent or
invariants.

There is a second, related risk of overcorrecting by introducing design
patterns and wrapper types everywhere, even when they add little value.

TemplarCMS needs a clear rule for when to introduce strong types and
when to apply design patterns so the codebase remains expressive without
becoming ceremonial.

## Decision

TemplarCMS will prefer strong domain types and purposeful design
patterns where they improve correctness, readability, and boundary
clarity.

### Strong Type Guidance

Strong types should be introduced for concepts that meet one or more of
these conditions:

- the value has domain-specific invariants
- the value is easy to confuse with another value of the same primitive type
- the value appears across multiple layers or boundaries
- the value benefits from behavior or normalization that should live with the type

High-value candidates include:

- identifiers such as content item ids, template ids, and field ids
- lookup values such as item keys, slugs, and paths
- bounded value concepts such as language, version, and field scope

Low-value candidates should remain primitives when they do not carry
meaningful behavior or constraints beyond simple storage.

Strong typing is a tool for protecting the model, not a blanket rule to
wrap every primitive.

### Pattern Guidance

TemplarCMS will use design patterns when they reflect a real boundary or
variation in the domain.

Patterns already aligned with the architecture include:

- application service for orchestration and validation
- repository for persistence boundaries
- builder for effective template construction
- resolver and policy for content and field value resolution
- value object for constrained domain concepts

Patterns should not be introduced only because they are familiar or
fashionable. New abstractions should earn their place by reducing
duplication, clarifying responsibilities, or protecting invariants.

### Boundary Rule

Strong types should be favored at domain and application boundaries
first.

Persistence and transport models may use simpler representations when
necessary, as long as mapping is explicit and the domain model remains
protected from infrastructure concerns.

### Incremental Adoption

TemplarCMS will adopt stronger typing incrementally.

The team does not need to halt feature work for a broad primitive
replacement pass. Instead, new types should be introduced when:

- adding new domain capabilities
- touching ambiguous or error-prone APIs
- strengthening existing invariants

## Consequences

### Positive

- Domain intent becomes easier to read.
- Invalid states are pushed closer to construction boundaries.
- APIs become harder to misuse accidentally.
- Application and domain code gain clearer separation from persistence shapes.
- Existing architectural patterns gain a more explicit rationale.

### Negative

- More mapping code may be required between layers.
- Some simple operations may feel heavier during early implementation.
- Overuse remains a risk if the decision is applied mechanically.

## Future Work

The following follow-up work is encouraged:

- identify the next high-value primitive wrappers that still remain in content APIs
- document naming guidance for ids, keys, slugs, and paths
- keep repository and persistence models explicit when mapping to domain types
- revisit existing APIs when ambiguity or misuse appears in tests or implementation work

## References

- ADR-0001: Content Modeling Engine
- ADR-0003: EF Core and Relational Persistence for MVP
- ADR-0006: Effective Template Runtime Model
