# TemplarCMS - Current State Summary

## Repository

Repository:

https://github.com/nodoubt223rd/TemplarCMS

Default branch:

```text
master
```

Summary scope:

```text
Current architecture state after the initial runtime conversion and
strong-typing slices.
```

This summary focuses on the current content-modeling architecture.
Later documentation-only branches may exist without changing the code
described below.

Recent implementation commits in this area:

```text
d676559
add initial typed field value conversion

a1bcf60
strengthen typed field value shape

f32215b
wire typed field conversion into runtime content

e8126e8
validate typed field values on save

0596ff4
add decimal and datetime typed field coverage
```

---

# Architecture Status

## Template Modeling

Completed.

Supports:

```text
Single inheritance only
```

Example:

```text
BaseContent
    ↓
BasePage
    ↓
ArticlePage
```

Rejected:

```text
Multiple inheritance
```

Reasoning:

```text
Simpler mental model
Deterministic inheritance
Simpler resolution
No merge-order ambiguity
```

---

## Content Modeling Pipeline

Implemented.

Full authored-template pipeline:

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

Runtime consumption pipeline:

```text
EffectiveTemplateDefinition
    ↓
ContentItemResolver
    ↓
ResolvedContentItem
```

### JsonTemplateRepository

Purpose:

```text
Load authored templates from JSON files and map them into domain definitions.
```

Important:

```text
It does not perform validation, inheritance resolution, caching, or catalog construction.
```

### TemplateValidator

Purpose:

```text
Validate authored templates before inheritance resolution and effective template generation.
```

Current coverage includes:

```text
Duplicate section keys
Duplicate field keys within a section
Duplicate field keys across a template
Section and field key collisions
Case-insensitive comparisons
```

### InheritedTemplateDefinition

Purpose:

```text
Which templates participate in inheritance resolution?
```

Contains:

```csharp
TemplateDefinition Template
IReadOnlyList<TemplateDefinition> InheritanceChain
```

Inheritance chain includes the current template.

Example:

```text
BaseContent
BasePage
ArticlePage
```

### EffectiveTemplateDefinition

Purpose:

```text
What is the final runtime shape after inheritance and override rules are applied?
```

Contains:

```csharp
IReadOnlyList<TemplateSectionDefinition> Sections

IReadOnlyList<FieldDefinition> Fields
```

Fields are flattened from all sections.

Important:

```text
ContentItemResolver consumes EffectiveTemplateDefinition.
```

Not:

```text
InheritedTemplateDefinition
```

---

# Content Modeling

Implemented.

## ContentLanguage

```csharp
public readonly record struct ContentLanguage
```

Characteristics:

```text
Opaque identifier
Not tied to CultureInfo
Normalized to lowercase
```

Examples:

```text
en
en-us
fr-ca
marketing-preview
```

## ContentVersion

```csharp
public readonly record struct ContentVersion
```

Includes:

```csharp
ContentVersion.Shared
ContentVersion.First
```

Validation:

```text
Cannot be negative
```

## ContentItemDefinition

```csharp
public sealed class ContentItemDefinition
```

Owned by:

```text
TemplarCMS.Domain.Content
```

Contains:

```csharp
ContentItemId Id
Name
Key
TemplateId TemplateId
ContentItemId? ParentId
```

Intentionally does NOT contain:

```text
Path
URL
Language
Version
Field values
```

## ContentFieldValue

```csharp
public sealed class ContentFieldValue
```

Owned by:

```text
TemplarCMS.Domain.Content
```

Contains:

```csharp
ContentItemId ItemId
FieldId FieldId
string FieldKey
ContentLanguage Language
ContentVersion Version
Value
```

Value storage:

```csharp
string? Value
```

Not:

```csharp
object?
JsonElement
```

Reason:

```text
Keep persistence simple
Field converters can be layered above storage
```

## Typed Field Value Conversion

Implemented for the currently supported field set.

Current implementation:

```csharp
ITypedFieldValueConverter
TypedFieldValueConverter
ConvertedFieldValue
TypedFieldValue
```

Current supported conversions:

```text
SingleLineText -> StringTypedFieldValue
MultiLineText -> StringTypedFieldValue
RichText -> StringTypedFieldValue
Integer -> IntegerTypedFieldValue
Decimal -> DecimalTypedFieldValue
DateTime -> DateTimeTypedFieldValue
Checkbox -> BooleanTypedFieldValue
Missing/null -> NullTypedFieldValue
```

Current behavior:

```text
Null stored values convert successfully to null
Invalid authored values return validation errors
Unsupported field types return validation errors
```

Runtime consumption note:

```text
Content item resolution now projects typed field values into the
resolved runtime item in addition to preserving the original stored
field values.
```

Write-path note:

```text
Content item field writes now validate typed conversion before
persistence so invalid authored values are rejected before they are
stored.
```

---

# Field Scope

Implemented.

## FieldValueScope

```csharp
public enum FieldValueScope
{
    Shared,
    Unversioned,
    Versioned
}
```

Owned by:

```csharp
FieldDefinition
```

Not:

```csharp
ContentFieldValue
```

FieldDefinition exposes:

```csharp
ValueScope
```

while retaining:

```csharp
IsShared
IsUnversioned
IsVersioned
```

---

# Field Value Resolution

Implemented.

## FieldValueResolutionContext

Purpose:

```text
Represents a field value resolution request.
```

Owned by:

```text
TemplarCMS.Domain.Content
```

Contains:

```csharp
ContentLanguage Language
ContentVersion Version
```

Important architectural decision:

Use:

```csharp
FieldValueResolutionContext
```

instead of passing:

```csharp
ContentLanguage
ContentVersion
```

throughout the resolution pipeline.

## IFieldValueResolutionPolicy

```csharp
public interface IFieldValueResolutionPolicy
```

Owned by:

```text
TemplarCMS.ContentModeling.Abstractions
```

Current implementation:

```csharp
ExactMatchFieldValueResolutionPolicy
```

Current matching behavior:

### Shared

```text
Requested language is ignored
Requested version is ignored

Current policy matches:
Version = Shared
```

Note:

```text
This is the current matching behavior.
If stricter storage invariants are desired for shared values, they should be enforced separately.
```

### Unversioned

```text
Match requested language
Ignore requested version

Current policy matches:
Language = requested language
Version = Shared
```

### Versioned

```text
Match requested language
Match requested version
```

## IFieldValueResolver

Implemented.

Owned by:

```text
TemplarCMS.ContentModeling.Abstractions
```

Current implementation:

```csharp
FieldValueResolver
```

Responsibility:

```text
Delegate resolution to policy
```

Contains no additional business logic.

---

# Content Item Resolution

Implemented.

## ResolvedContentItem

```csharp
public sealed class ResolvedContentItem
```

Owned by:

```text
TemplarCMS.Domain.Content
```

Contains:

```csharp
ContentItemDefinition Item

IReadOnlyDictionary<string, ContentFieldValue?> Fields

IReadOnlyDictionary<string, TypedFieldValue> ConvertedFields
```

Dictionary key:

```text
Field key
```

Not:

```text
Field id
```

Reason:

```text
Consumers think in field keys such as:
title
body
metaDescription
```

Converted field dictionary values:

```text
StringTypedFieldValue
IntegerTypedFieldValue
DecimalTypedFieldValue
DateTimeTypedFieldValue
BooleanTypedFieldValue
NullTypedFieldValue
```

## IContentItemResolver

Implemented.

Owned by:

```text
TemplarCMS.ContentModeling.Abstractions
```

Current implementation:

```csharp
ContentItemResolver
```

Consumes:

```csharp
EffectiveTemplateDefinition
```

Important:

Does NOT consume:

```csharp
InheritedTemplateDefinition
```

Reason:

```text
Effective template building is already complete.
```

## ContentItemResolver Responsibilities

```text
For each effective field:

    Filter candidate values by field id

    Call IFieldValueResolver

    Convert the resolved stored value into a typed runtime value

    Store resolved value by field key

    Store typed runtime value by field key

Return ResolvedContentItem
```

Does NOT perform:

```text
Template inheritance
Effective template building
Persistence
Caching
Fallback selection
```

---

# Important Architectural Decisions

## Candidate Value Filtering

Chosen:

```text
Option A
```

Meaning:

```text
ContentItemResolver
    supplies candidate values

Policy
    resolves values
```

Rejected:

```text
Policy filters values itself
```

Reason:

```text
Cleaner separation
Less work per resolution
More scalable
```

## Fallbacks

Not implemented.

Future plans may include:

```text
LatestVersionFieldValueResolutionPolicy
LanguageFallbackPolicy
Composite policies
```

Current behavior:

```text
Exact match only
```

Intentional.

---

# Test Status

Reported green in the current implementation snapshot.

Coverage includes:

```text
ContentLanguageTests
ContentVersionTests
ContentItemDefinitionTests
ContentFieldValueTests
FieldDefinitionTests

FieldValueResolutionContextTests

ExactMatchFieldValueResolutionPolicyTests

FieldValueResolverTests

ResolvedContentItemTests

ContentItemResolverTests
```

---

# Backlog

Document exists:

```text
docs/backlog.md
```

Notable future item:

```text
Cookie Management
```

Potential location:

```text
TemplarCMS.Web
```

Possible abstractions:

```csharp
ICookieService
ICookieSerializer
```

---

# Current Architecture Snapshot

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
    ↓
ContentItemResolver
    ↓
ResolvedContentItem

                ↑

        FieldValueResolver
                ↑

ExactMatchFieldValueResolutionPolicy
```

---

# Suggested Next Discussion

Potential next topics:

```text
Abstractions and persistence

IContentRepository

Persistence implementation for content storage

Typed field value conversion
Rendering pipeline

Publishing pipeline

Fallback policies
```

Current recommendation:

```text
Continue typed field coverage and runtime consumption before fallback
policies.
```

## Current Boundary Note

The runtime content concepts now live outside
`TemplarCMS.ContentModeling`.

- `TemplarCMS.Domain.Content` owns content items, field values,
  language/version value objects, field value resolution context, and
  resolved content shapes.
- `TemplarCMS.Abstractions.Content` owns `IContentRepository`.
- `TemplarCMS.ContentModeling.Abstractions` owns content modeling
  contracts such as content and field value resolver interfaces.
- `TemplarCMS.ContentModeling` remains focused on template definitions,
  inheritance, validation, effective template building, JSON template
  mapping, and the current resolver implementations.
