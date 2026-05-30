# ADR-0005: Define the Template Inheritance Model

**Status:** Accepted

**Date:** 2026-05-29

## Context

Template inheritance is a core capability of TemplarCMS and one of the primary features inspired by Sitecore.

The platform must support reusable field definitions, composable schemas, and consistent field resolution across content items.

Without a formal inheritance model, template definitions become duplicated and difficult to maintain.

The inheritance model must support:

* Reusable field definitions.
* Multiple base templates.
* Override behavior.
* Effective template generation.
* Validation.
* Cycle detection.

## Decision

TemplarCMS will support multiple base templates.

Templates may inherit from zero or more base templates.

Inheritance relationships will be represented through template references.

Example:

```text
_Base SEO
 ├── MetaTitle
 └── MetaDescription

_Base Auditing
 ├── CreatedBy
 └── LastModifiedBy

Article
 ├── inherits _Base SEO
 ├── inherits _Base Auditing
 ├── Title
 └── Body
```

The effective template for Article contains:

```text
MetaTitle
MetaDescription
CreatedBy
LastModifiedBy
Title
Body
```

## Effective Template

Consumers should work primarily with Effective Templates.

An Effective Template is a fully resolved representation of a template after inheritance has been applied.

Effective Templates should:

* Contain all inherited fields.
* Contain all local fields.
* Resolve conflicts.
* Preserve field metadata.
* Be immutable after creation.

### TemplateDefinition vs EffectiveTemplateDefinition

TemplarCMS distinguishes between authored schema definitions and resolved schema definitions.

```text
TemplateDefinition
    ↓
EffectiveTemplateDefinition
```

#### TemplateDefinition

Represents the authored schema definition.

A `TemplateDefinition` may contain:

* Base template references.
* Local sections.
* Local field definitions.
* Incomplete inheritance information.

It is the source model used by content architects and administrators when defining schemas.

#### EffectiveTemplateDefinition

Represents the fully resolved schema produced by the content modeling engine.

An `EffectiveTemplateDefinition` contains:

* All inherited fields.
* All local fields.
* Applied overrides.
* Resolved field metadata.
* Deterministic field ordering.

An Effective Template should not require additional inheritance resolution.

Consumers should prefer `EffectiveTemplateDefinition` whenever possible.

Examples include:

* REST API responses.
* Template validation.
* Vue Admin form generation.
* GraphQL schema generation.
* Package deployment.
* Content serialization.

#### Example

```text
_Base SEO
 ├── MetaTitle
 └── MetaDescription

Article
 ├── inherits _Base SEO
 ├── Title
 └── Body
```

TemplateDefinition (Article):

```text
BaseTemplates:
 └── _Base SEO

Fields:
 ├── Title
 └── Body
```

EffectiveTemplateDefinition (Article):

```text
Fields:
 ├── MetaTitle
 ├── MetaDescription
 ├── Title
 └── Body
```

#### Architectural Rule

The Content Modeling Engine is responsible for producing Effective Templates.

The following services participate in this process:

```text
ITemplateInheritanceResolver
TemplateInheritanceResolver

IEffectiveTemplateBuilder
EffectiveTemplateBuilder

ITemplateValidator
TemplateValidator
```

Controllers, API contracts, and persistence entities must not implement inheritance resolution logic directly.

## Resolution Order

Inheritance resolution follows this order:

1. Resolve base templates.
2. Resolve inherited fields.
3. Resolve local template fields.
4. Apply overrides.
5. Produce Effective Template.

This guarantees deterministic results.

## Override Rules

Local template fields take precedence over inherited fields.

Example:

```text
_Base SEO
 └── MetaTitle

Article
 └── MetaTitle
```

The Article definition overrides the inherited MetaTitle definition.

The child template definition wins.

## Field Identity

Field identity is determined by field key.

Example:

```text
MetaTitle
metaTitle
```
#### TemplateDefinition

Represents the authored schema definition.

A `TemplateDefinition` may contain:

- Base template references.
- Local sections.
- Local field definitions.
- Incomplete inheritance information.

It is the source model used by content architects and administrators when defining schemas.

#### EffectiveTemplateDefinition

Represents the fully resolved schema produced by the content modeling engine.

An `EffectiveTemplateDefinition` contains:

- All inherited fields.
- All local fields.
- Applied overrides.
- Resolved field metadata.
- Deterministic field ordering.

An Effective Template should not require additional inheritance resolution.

Consumers should prefer `EffectiveTemplateDefinition` whenever possible.

Examples include:

- REST API responses.
- Template validation.
- Vue Admin form generation.
- GraphQL schema generation.
- Package deployment.
- Content serialization.

TemplarCMS distinguishes between authored `TemplateDefinition` models and resolved `EffectiveTemplateDefinition` models.

Field keys should be normalized during validation.

Normalized field keys must be unique within the Effective Template.

## Conflict Handling

Conflicts are resolved using override precedence.

Priority:

1. Local template fields.
2. Direct base templates.
3. Indirect base templates.

When multiple inherited templates introduce the same normalized field key, validation should report the conflict unless a clear override exists.

## Cycle Detection

Inheritance cycles are not allowed.

Invalid example:

```text
TemplateA -> TemplateB
TemplateB -> TemplateC
TemplateC -> TemplateA
```

The inheritance resolver must detect cycles and return validation errors.

Cycle detection must occur before Effective Template generation.

## Validation Rules

The inheritance model must validate:

* Duplicate field keys.
* Circular inheritance.
* Invalid template references.
* Duplicate base template references.
* Invalid field metadata.

Validation should produce structured validation results rather than throwing exceptions whenever practical.

## Content Modeling Responsibilities

Inheritance logic belongs in:

```text
TemplarCMS.ContentModeling
```

Key services include:

```text
ITemplateInheritanceResolver
TemplateInheritanceResolver

IEffectiveTemplateBuilder
EffectiveTemplateBuilder

ITemplateValidator
TemplateValidator
```

Inheritance resolution should not live in controllers, persistence entities, or API contracts.

## Consequences

### Positive

* Strong template reuse.
* Reduced duplication.
* Deterministic schema generation.
* Easier GraphQL generation.
* Easier dynamic Vue form generation.
* Familiar behavior for Sitecore developers.

### Negative

* Additional complexity during validation.
* More expensive schema resolution compared to flat templates.
* Requires caching for large template hierarchies.

## Future Work

Potential future enhancements:

* Effective template caching.
* Template dependency graphs.
* Template visualization tools.
* Template serialization.
* Package deployment support.
* Schema comparison tools.

## Decision Summary

TemplarCMS will support multiple base templates, deterministic inheritance resolution, effective template generation, override precedence, and cycle detection.

TemplarCMS distinguishes between authored `TemplateDefinition` models and resolved `EffectiveTemplateDefinition` models.

Consumers should interact with Effective Templates whenever possible.
