# TemplarCMS Part 6: Current State and Next Steps

## Purpose

This document captures the current state of the TemplarCMS content-modeling foundation after the single-inheritance refactor and identifies the next major areas of work.

The goal is to create a clear checkpoint before moving beyond template modeling into higher-level content, API, persistence, and runtime concerns.

## Current Status

The core template modeling pipeline is now stable, deterministic, and covered by tests.

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

The major architectural migration from multiple template inheritance to single template inheritance is complete.

## Major Architecture Decisions Completed

### Single Template Inheritance

TemplarCMS now supports a single base template per template.

Rejected model:

```text
ArticlePage
 ├─ BasePage
 └─ SeoPage
```

Accepted model:

```text
ArticlePage
    ↓
BasePage
    ↓
BaseContent
```

This decision simplifies:

- The authoring mental model.
- Inheritance resolution.
- Effective template generation.
- Conflict handling.
- Test coverage.
- Future API contracts.

It also avoids merge-order ambiguity that would have existed with multiple base templates.

### Ordered Inheritance Chain

`TemplateInheritanceResolver` now returns an `InheritedTemplateDefinition` rather than a raw collection of templates.

```csharp
public sealed class InheritedTemplateDefinition
{
    public TemplateDefinition Template { get; }

    public IReadOnlyList<TemplateDefinition> InheritanceChain { get; }
}
```

The inheritance chain includes the current template and is ordered from root base template to derived template.

Example:

```text
BaseContent
BasePage
ArticlePage
```

Ordering is part of the semantic contract, so `IReadOnlyList<T>` is used where ordering matters.

### Effective Template Generation

`EffectiveTemplateBuilder` consumes the resolved inheritance chain and produces a flattened `EffectiveTemplateDefinition`.

The builder applies inheritance in base-to-derived order.

Current behavior:

- Base sections are applied first.
- Derived sections are applied later.
- Sections with matching keys are merged case-insensitively.
- Derived section metadata overrides base section metadata.
- Fields with matching keys are replaced case-insensitively.
- Derived fields override base fields.
- Effective sections are ordered by `SortOrder`.
- Effective fields are flattened into the final effective template.

Once an `EffectiveTemplateDefinition` exists, consumers should not need to understand inheritance.

## Current Core Types

### TemplateDefinition

Represents an authored template.

Key responsibilities:

- Template identity.
- Template name and key.
- Optional single base template.
- Local template sections.

Conceptually:

```csharp
public sealed class TemplateDefinition
{
    public Guid Id { get; }

    public string Name { get; }

    public string Key { get; }

    public TemplateDefinition? BaseTemplate { get; }

    public IReadOnlyCollection<TemplateSectionDefinition> Sections { get; }
}
```

### TemplateSectionDefinition

Represents a logical grouping of fields within a template.

Key responsibilities:

- Section identity.
- Section name and key.
- Section ordering.
- Field collection.

### FieldDefinition

Represents a single field within a section.

Key responsibilities:

- Field identity.
- Field name and key.
- Field type.
- Shared, unversioned, and versioned semantics.
- Additional metadata.

### InheritedTemplateDefinition

Represents a template after inheritance resolution but before effective template construction.

Key responsibilities:

- Preserve the authored template being resolved.
- Expose the ordered inheritance chain.
- Separate inheritance resolution from effective template generation.

### EffectiveTemplateDefinition

Represents the final flattened template after inheritance and overrides have been applied.

Key responsibilities:

- Effective template identity.
- Ordered effective sections.
- Ordered flattened effective fields.
- Hide inheritance complexity from consumers.

Conceptually:

```csharp
public sealed class EffectiveTemplateDefinition
{
    public Guid Id { get; }

    public string Name { get; }

    public string Key { get; }

    public IReadOnlyList<TemplateSectionDefinition> Sections { get; }

    public IReadOnlyList<FieldDefinition> Fields { get; }
}
```

## Validation Coverage Completed

`TemplateValidator` currently covers important authoring-time validation scenarios.

Completed coverage includes:

- Duplicate section keys.
- Duplicate field keys within a section.
- Duplicate field keys across a template.
- Section and field key collisions.
- Case-insensitive key comparisons.
- Cancellation behavior.

## Inheritance and Builder Test Coverage Completed

The current tests document the intended behavior of inheritance resolution and effective template generation.

Covered scenarios include:

- Simple template effective generation.
- Single inheritance section inclusion.
- Deep inheritance chain resolution.
- Same-key section merging.
- Derived section metadata overrides.
- Derived field overrides.
- Deep inheritance field merging.
- Case-insensitive section and field override behavior.
- Error propagation from inheritance resolution.
- Effective template section ordering.
- Effective template field flattening.
- Effective template defensive copying.
- Empty effective template collections.

## Current Confidence Level

The content-modeling foundation is now in a strong state.

Current assessment:

```text
Inheritance model       Stable
Resolver behavior       Stable
Effective builder       Stable
Validation rules        Stable
Test coverage           Strong
Architecture direction  Clear
```

The project should avoid further architecture churn in this area unless a concrete new requirement appears.

## Known Documentation Drift

The broad architecture document still contains older language around multiple base templates and `BaseTemplates`-style examples.

This should be cleaned up separately so the high-level documentation matches the new single-inheritance model.

Recommended future doc cleanup:

- Replace `BaseTemplates` examples with `BaseTemplate`.
- Remove references to multiple base templates being allowed.
- Update inheritance examples to show a single linear chain.
- Update JSON examples to reflect the current template model.

## What Is Still Missing

The template modeling foundation is solid, but TemplarCMS still needs the next layer of domain behavior.

The next major work should move above templates into content, runtime behavior, or public contracts.

## Candidate Part 6 Workstreams

### Option A: Content Item Modeling

Introduce the domain model for content items that are instances of templates.

Possible types:

```csharp
ContentItemDefinition
ContentItem
ContentFieldValue
ContentVersion
ContentLanguage
```

Questions to answer:

- How does a content item reference its template?
- Does it reference `TemplateDefinition` or only the effective template at runtime?
- How are paths represented?
- How are parent-child relationships represented?
- What is the minimum viable content tree?

This is likely the most natural next step.

### Option B: Field Value Semantics

Define how shared, unversioned, and versioned field values are stored and resolved.

Questions to answer:

- What is the storage shape for shared values?
- What is the storage shape for unversioned values?
- What is the storage shape for versioned values?
- How does value resolution work for a requested language and version?
- How are missing values represented?

This work is closely related to content item modeling.

### Option C: Template Repository and Serialization Contracts

Harden the JSON repository and define the public serialization shape.

Questions to answer:

- What does the canonical template JSON format look like?
- Should JSON use `baseTemplate` as a key reference?
- How are base templates resolved when loading from files?
- Should templates be loadable in any order?
- How should repository errors be reported?

This would make the template system easier to use outside tests.

### Option D: Public API Contracts

Start designing the external API contract for template and content operations.

Questions to answer:

- What should template responses expose?
- Should effective templates have their own endpoint?
- How should validation errors be represented over HTTP?
- What HATEOAS links are required for MVP?

This should probably wait until content item modeling is clearer.

### Option E: Schema Generation

Generate downstream schema artifacts from effective templates.

Possible outputs:

- JSON Schema.
- OpenAPI fragments.
- TypeScript types.
- C# models.
- GraphQL types.

This is valuable, but it depends on the effective template model remaining stable.

## Recommended Next Step

The recommended next major focus is content item modeling.

Reasoning:

- Templates are now stable enough to consume.
- Effective templates provide the runtime shape needed to validate content.
- Field value semantics cannot be fully designed without content items.
- API contracts will be more grounded once content behavior exists.

Recommended Part 6 focus:

```text
TemplateDefinition
    ↓
EffectiveTemplateDefinition
    ↓
ContentItem
    ↓
FieldValue
```

## Suggested Part 6 Starting Scope

Start small and keep the first content item model intentionally minimal.

Initial goals:

1. Define a content item identity model.
2. Define parent-child relationships.
3. Define template association.
4. Define field value storage objects.
5. Define basic field value validation against an effective template.
6. Add tests before introducing persistence.

Avoid starting with EF Core or API endpoints before the domain model is clear.

## Proposed Initial Types

Potential starting point:

```csharp
public sealed class ContentItemDefinition
{
    public ContentItemId Id { get; }

    public string Name { get; }

    public ContentItemKey Key { get; }

    public TemplateId TemplateId { get; }

    public ContentItemId? ParentId { get; }
}
```

```csharp
public sealed class ContentFieldValue
{
    public FieldId FieldId { get; }

    public string FieldKey { get; }

    public ContentLanguage Language { get; }

    public ContentVersion Version { get; }

    public string? Value { get; }
}
```

These are only starting sketches. They should be refined before implementation.

Current implementation note:

- `ContentItemId` and `TemplateId` are now first-class strong types in
  the runtime content model and content service/repository boundaries.
- `FieldId` is now a first-class strong type shared between template
  field definitions and stored content field values.
- Content path is now computed at runtime via `ContentPathResolver`
  rather than stored on `ContentItemDefinition`.
- Root path composition is `/key`; child path composition is
  `parent-path/key`.
- Path lookup now uses canonical absolute `ContentPath` values for
  read scenarios, with normalization to lowercase and sibling-key
  uniqueness as the current disambiguation rule.
- Existing item key changes and parent changes are currently rejected
  until explicit rename/move semantics are designed.

## Design Questions for the Next Session

Before implementing content items, decide:

1. Should content items store `TemplateId`, `TemplateKey`, or both?
2. Should field values be addressed by field id, field key, or both?
3. Should language be a string, value object, or dedicated type?
4. Should version be represented as an integer or a richer value object?
5. How should move and rename semantics evolve now that paths are computed?
6. Should item keys/slugs be unique only among siblings?
7. Should field values be immutable snapshots?
8. Should validation return `ValidationResult<T>` or a dedicated content validation result?

## Current Recommendation

Use the next session to design content item and field value models before writing persistence or API code.

The template subsystem is now strong enough to serve as the foundation for runtime content behavior.

## Boundary Update

The runtime content concepts discussed in this document now belong in
`TemplarCMS.Domain.Content`, while `TemplarCMS.ContentModeling` remains
focused on template definitions, inheritance, validation, effective
template building, and related schema concerns.

Cross-layer contracts such as `IContentRepository` now belong in
`TemplarCMS.Abstractions.Content` so application and persistence code
can share them without pushing those contracts back into the modeling
assembly.

## Current Mood

```text
Architecture = stable
Compiler = happy
Tests = passing
Next frontier = content items
```
