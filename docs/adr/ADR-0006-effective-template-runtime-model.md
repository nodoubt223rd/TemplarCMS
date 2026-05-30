# ADR-0006: Effective Template Runtime Model

## Status

Accepted

## Date

2026-05-30

## Context

TemplarCMS supports template inheritance through `TemplateDefinition` and `TemplateInheritanceResolver`.

As the content modeling engine evolved, runtime consumers increasingly required a fully resolved view of a template rather than the authored inheritance graph.

Examples include:

- API serialization
- GraphQL schema generation
- Content editing experiences
- Content validation
- Runtime content creation
- Future code generation

Prior to this decision, consumers were expected to understand template inheritance and traverse the inheritance graph to determine the complete set of available sections and fields.

This approach introduced several problems:

- Runtime consumers became coupled to inheritance behavior.
- Multiple components duplicated inheritance traversal logic.
- Section and field override behavior was difficult to centralize.
- Future validation and schema generation would require repeated graph traversal.
- Front-end consumers had no consistent representation of the final template structure.

The system required a dedicated runtime model representing the fully resolved template.

## Decision

TemplarCMS will distinguish between authored templates and runtime templates.

### Authoring Model

The authoring model is represented by `TemplateDefinition`.

Responsibilities:

- Preserve authored template structure.
- Preserve inheritance relationships.
- Preserve local sections and fields.
- Serve as the source of truth for template authoring.

### Runtime Model

The runtime model is represented by `EffectiveTemplateDefinition`.

Responsibilities:

- Represent a fully resolved template.
- Preserve resolved section hierarchy.
- Expose the complete field set available to consumers.
- Remove the need for runtime inheritance traversal.

### Resolution Pipeline

TemplateDefinition
↓
TemplateInheritanceResolver
↓
EffectiveTemplateBuilder
↓
EffectiveTemplateDefinition

### Section Preservation

Effective templates preserve section hierarchy.

Sections are not flattened away during resolution.

This decision was made to:

- Improve discoverability for API consumers.
- Improve developer experience for front-end developers.
- Preserve author intent.
- Provide a richer representation for future editing experiences.

### Override Behavior

Inheritance is resolved using a base-first traversal strategy.

When duplicate section keys are encountered:

- The derived section definition overrides the base section definition.

When duplicate field keys are encountered:

- The derived field definition overrides the base field definition.

Override behavior is applied during effective template construction.

### Identity

Template identity is represented by a stable `Guid`.

Section identity is represented by a stable `Guid`.

Names and keys are not considered identity.

Keys are used for lookup, resolution, and serialization.

## Consequences

### Positive

- Runtime consumers no longer traverse inheritance graphs.
- Inheritance behavior is centralized.
- Validation can operate on authored and runtime models independently.
- API consumers receive a consistent template representation.
- Section hierarchy is preserved.
- Future GraphQL and code generation scenarios become simpler.
- Runtime behavior becomes easier to test.

### Negative

- An additional model type must be maintained.
- Effective template construction introduces a resolution step.
- Changes to inheritance behavior must be reflected in the builder.

## Future Work

The following capabilities will build upon this decision:

- Effective template validation.
- GraphQL schema generation.
- Content serialization.
- Content editing APIs.
- Code generation.
- Template caching.
- Runtime content creation services.

## References

- ADR-0005: Template Inheritance
- TemplateInheritanceResolver
- EffectiveTemplateBuilder
- EffectiveTemplateDefinition
