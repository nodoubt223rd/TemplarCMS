# ADR-0001: Introduce a Dedicated Content Modeling Engine

**Status:** Accepted

**Date:** 2026-05-29

## Context

TemplarCMS is intended to provide Sitecore-inspired content modeling capabilities including templates, template inheritance, sections, field definitions, shared fields, unversioned fields, versioned fields, and effective field resolution.

As the platform evolves, TemplarCMS is expected to support template import/export, serialization, package deployment, content migrations, GraphQL schema generation, admin UI generation from template metadata, and future YAML-based template definitions.

These concerns are content-modeling concerns, not persistence, API, or infrastructure concerns.

If template logic is embedded directly into EF Core entities, controllers, or application services, the system will become harder to evolve and test independently.

## Decision

Create a dedicated project:

```text
TemplarCMS.ContentModeling
```

This project will contain logic related to content schema definition and resolution.

The ContentModeling project will be responsible for:

- Template definitions.
- Section definitions.
- Field definitions.
- Template inheritance.
- Effective template generation.
- Template validation.
- Field type registration.
- Schema metadata.

The project must not depend on EF Core, ASP.NET Core, infrastructure services, or database implementations.

## Architecture

`TemplarCMS.Domain` represents stable runtime content concepts such as
`ContentItemDefinition`, `ContentFieldValue`, `ContentLanguage`,
`ContentVersion`, and related value objects used across application and
repository boundaries.

`TemplarCMS.ContentModeling` represents logical template definitions and
template mechanics such as `TemplateDefinition`,
`TemplateSectionDefinition`, `FieldDefinition`, inheritance resolution,
template validation, and `EffectiveTemplateDefinition`.

`TemplarCMS.Abstractions` holds cross-layer contracts such as
`IContentRepository` when those contracts need to be shared without
living in persistence or application assemblies.

The template definition model remains independent of persistence even as
runtime content concepts and repository contracts evolve in parallel.

## Consequences

### Positive

- Cleaner architecture.
- Better testability.
- Easier future expansion.
- Reduced coupling between API, persistence, and schema logic.
- Supports future template serialization and package management.
- Supports future GraphQL and Vue admin UI generation.

### Negative

- Additional project complexity.
- Mapping is required between domain entities and content model definitions.

This tradeoff is acceptable because TemplarCMS is intended to be a long-lived platform rather than a simple CRUD application.

## Future Work

Planned additions to `TemplarCMS.ContentModeling` include:

- FieldTypeRegistry.
- TemplateSerializer.
- TemplateImporter.
- TemplateExporter.
- SchemaGenerator.
- GraphQLSchemaBuilder.
- MigrationEngine.
