# JsonTemplateMapper

## Purpose

The `JsonTemplateMapper` is responsible for translating JSON serialization models into domain content model definitions.

It acts as the boundary between the persistence layer and the domain model.

```text
JSON File
    ↓
JsonTemplateDefinition
    ↓
JsonTemplateMapper
    ↓
TemplateDefinition
```

## Responsibilities

The mapper is responsible for:

- Mapping JSON DTOs into domain models.
- Converting JSON field type identifiers into `FieldType` values.
- Mapping sections and fields.
- Ensuring required mapping values are present.

## Non-Responsibilities

The mapper does not perform:

- Template validation.
- Duplicate detection.
- Inheritance resolution.
- Effective template generation.
- Caching.
- Runtime catalog construction.

Those responsibilities belong to later stages in the content modeling pipeline.

```text
JsonTemplateMapper
        ↓
TemplateValidator
        ↓
TemplateInheritanceResolver
        ↓
EffectiveTemplateBuilder
        ↓
ContentModelCatalog
```

## Design Notes

Base template references are intentionally not resolved by the mapper.

The JSON format stores base template references as template keys:

```json
{
  "baseTemplates": [
    "base-page"
  ]
}
```

Resolving those references requires visibility into all templates loaded by the repository.

For that reason, base template resolution occurs after mapping.

## Repository Configuration

The JSON template repository is configured using
`JsonTemplateRepositoryOptions`.

Example:

```json
{
  "TemplatesPath": "content-models/templates"
}
```

The repository loads all template definition files
located in the configured directory.

## Testing Strategy

Every JSON template passes through this component.

Tests should verify:

- Template mapping.
- Section mapping.
- Field mapping.
- Field type conversion.
- Required property validation.
- Stable field identifier mapping.

Recommended test coverage:

```text
Map_ShouldMapTemplate()
Map_ShouldMapSections()
Map_ShouldMapFields()
Map_ShouldMapFieldId()
Map_ShouldMapSingleLineText()
Map_ShouldMapRichText()
Map_ShouldThrow_WhenFieldTypeMissing()
Map_ShouldThrow_WhenFieldTypeUnsupported()
Map_ShouldThrow_WhenNameMissing()
Map_ShouldThrow_WhenKeyMissing()
```
