# ADR-0004: Use Vue 3 for the Admin Experience

**Status:** Accepted

**Date:** 2026-05-29

## Context

TemplarCMS requires an administration and authoring experience for managing templates, content items, field values, languages, and future publishing workflows.

The admin experience must be capable of dynamically generating editing interfaces from template metadata and effective template definitions.

The UI should remain independent of backend implementation details and communicate exclusively through the public REST API.

Several frontend options were considered:

- Vue 3
- React
- Angular
- Blazor

The platform should favor simplicity, productivity, strong TypeScript support, and excellent support for metadata-driven UI generation.

## Decision

TemplarCMS will use Vue 3 as the primary administration and authoring framework.

The admin application will live in:

```text
admin/TemplarCMS.Admin
```

Recommended stack:

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router

The admin application will consume the same public REST API that external clients use.

No internal or privileged API endpoints should be required for basic authoring functionality.

## Architectural Principles

### API First

The Vue application should interact with TemplarCMS through documented REST endpoints.

This ensures:

- Consistent behavior.
- Easier testing.
- Better API quality.
- Easier future replacement of the admin application.

### Metadata Driven UI

Template metadata should drive the user interface.

The admin UI should be capable of generating forms dynamically based on field definitions.

Example:

```text
Article Template
 ├── Title (SingleLineText)
 ├── Body (RichText)
 └── PublishDate (DateTime)
```

The UI should render the correct editors based on field metadata.

### Component-Based Editors

Field types should map to reusable Vue components.

Examples:

```text
SingleLineTextFieldEditor
RichTextFieldEditor
CheckboxFieldEditor
DateTimeFieldEditor
DroplinkFieldEditor
MultilistFieldEditor
```

A registry-based approach should be used so new field types can be introduced without modifying the core editor shell.

## State Management

Pinia will be used for application state.

Expected stores include:

- AuthenticationStore (future).
- TemplateStore.
- ContentStore.
- NavigationStore.
- SettingsStore.

## Routing

Vue Router will be used.

Example routes:

```text
/templates
/templates/{id}
/content
/content/{id}
/content/{id}/versions
/settings
```

## Delivery Applications

The Vue admin application is separate from content delivery applications.

Future delivery SDKs may include:

- Nuxt.
- ASP.NET Core.
- Blazor.

The admin UI should not be coupled to delivery rendering concerns.

## Consequences

### Positive

- Excellent TypeScript support.
- Fast development experience.
- Strong ecosystem.
- Easy dynamic form generation.
- Clear separation between authoring and delivery.
- Works naturally with REST APIs.

### Negative

- Requires JavaScript/TypeScript expertise.
- Separate deployment pipeline from ASP.NET Core.
- Additional frontend build tooling.

## Future Work

- Dynamic template designer.
- Visual content editor.
- Workflow management UI.
- Publishing dashboard.
- Media library UI.
- Plugin architecture for custom field editors.
