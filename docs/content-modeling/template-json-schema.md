# Template JSON Schema Design

## Purpose

This document defines the initial JSON authoring format for TemplarCMS template definitions.

The JSON format is intended to be human-readable, source-control friendly, and easy to review in pull requests. It represents the authored template model, not the resolved runtime model.

```text
JSON Template Files
        ↓
JSON DTOs
        ↓
Mapper
        ↓
TemplateDefinition
        ↓
TemplateValidator
        ↓
TemplateInheritanceResolver
        ↓
EffectiveTemplateBuilder
        ↓
EffectiveTemplateDefinition
```

## Recommended Storage Layout

Use one JSON file per template.

```text
content-models/
  templates/
    base-page.json
    article-page.json
    landing-page.json
```

Each file should contain one authored template definition.

The file name should usually match the template key, but the `key` property inside the JSON remains authoritative.

## Template JSON Shape

```json
{
  "id": "8f3d9f8a-89c5-4c3f-94b2-8df68a7a6c01",
  "name": "Article Page",
  "key": "article-page",
  "baseTemplates": [
    "base-page"
  ],
  "sections": [
    {
      "id": "5f0f0c36-8df3-44f3-9382-c45a1d78a6f8",
      "name": "Content",
      "key": "content",
      "sortOrder": 100,
      "fields": [
        {
          "name": "Title",
          "key": "title",
          "fieldType": "singleLineText"
        },
        {
          "name": "Body",
          "key": "body",
          "fieldType": "richText"
        }
      ]
    }
  ]
}
```

## Template Properties

| Property | Required | Description |
|---|---:|---|
| `id` | Yes | Stable template identity. Used by the runtime model and inheritance system. |
| `name` | Yes | Human-readable display name. |
| `key` | Yes | Stable lookup and serialization key. |
| `baseTemplates` | No | Ordered template keys for inherited base templates. Later entries override earlier entries when keys collide. |
| `sections` | No | Authored template sections. |

## Section Properties

| Property | Required | Description |
|---|---:|---|
| `id` | Yes | Stable section identity. Used for section merging and future-safe resolution. |
| `name` | Yes | Human-readable display name. |
| `key` | Yes | Stable section key. Sections with matching keys merge during effective template generation. |
| `sortOrder` | Yes | Sort position for display and runtime ordering. |
| `fields` | No | Authored fields in the section. |

## Field Properties

| Property | Required | Description |
|---|---:|---|
| `name` | Yes | Human-readable display name. |
| `key` | Yes | Stable field key. Derived fields with matching keys override inherited fields. |
| `fieldType` | Yes | Field type identifier. |

Fields do not currently require stable GUID identity. Field identity is key-based inside the resolved effective template model.

## Base Template References

Base templates should be referenced by template key.

The runtime model supports ordered multiple inheritance. Base templates are
resolved left-to-right; later entries override earlier entries when sections or
fields share a key, and local definitions override all inherited definitions.
Duplicate keys are invalid.

```json
"baseTemplates": [
  "base-page",
  "metadata"
]
```

Keys are preferred over GUIDs in authored JSON because they are easier to read, review, and maintain.

The repository layer is responsible for resolving these keys into ordered
`TemplateDefinition` references before inheritance resolution.

## Field Type Values

Field type values should use camel-case strings.

Initial values:

```text
singleLineText
richText
number
date
boolean
media
reference
```

These values should map into the domain `FieldType` enum.

The JSON format should not expose enum casing or implementation details directly. This keeps authored content stable if C# enum naming changes later.

## Design Decisions

### Use Stable IDs for Templates and Sections

Templates and sections use stable GUID identities.

This supports:

- Reliable inheritance resolution.
- Stable section merging.
- Future migrations.
- Source-controlled changes where display names may change.

### Use Keys for Authoring References

Keys are used for base template references and general lookup.

This keeps JSON readable and avoids forcing authors to copy GUIDs for common template relationships.

### Use Names Only for Display

Names are display values only.

They should not be used for identity, lookup, merging, or inheritance resolution.

### Keep JSON Separate from Domain Models

The repository should not deserialize JSON directly into `TemplateDefinition`.

Preferred flow:

```text
JsonTemplateDefinition
JsonTemplateSectionDefinition
JsonFieldDefinition
        ↓
JsonTemplateMapper
        ↓
TemplateDefinition
```

This keeps the domain model clean and allows the JSON format to evolve independently.

## Future Considerations

The initial schema intentionally avoids advanced field settings.

Future versions may add:

- Field descriptions.
- Field default values.
- Validation rules.
- Field editor hints.
- Source values for reference fields.
- Shared, unversioned, and versioned field semantics.
- Template icons or display metadata.
- Schema versioning.

If schema versioning is introduced, prefer adding an explicit property such as:

```json
"schemaVersion": 1
```

Do not add it until there is an actual migration or compatibility need.

## Summary

The initial JSON template format should optimize for readability, stability, and clean mapping into the existing content modeling pipeline.

The important boundaries are:

```text
JSON is the authoring format.
TemplateDefinition is the domain model.
EffectiveTemplateDefinition is the runtime model.
```

Keeping those boundaries clear prevents persistence details from leaking into runtime consumers.
