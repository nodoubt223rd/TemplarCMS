# Content Persistence Review

## Purpose

This note reviews the current content persistence shape now that
TemplarCMS supports:

- content tree reads
- content metadata updates
- explicit rename and move operations
- tree-aware mutation responses
- an admin client that consumes branch refresh contracts

The goal is not to redesign persistence prematurely.

The goal is to identify which database decisions are now important
enough to make intentionally before more authoring behavior builds on
top of the current shape.

## Current Shape

Current EF Core persistence lives in:

- `TemplarCmsDbContext`
- `PersistenceContentItem`
- `PersistenceContentFieldValue`
- `EfContentRepository`

Current relational tables:

### ContentItems

- `Id`
- `Name`
- `Key`
- `TemplateId`
- `ParentId`

Current constraints and indexes:

- primary key on `Id`
- unique index on `(ParentId, Key)`

### ContentFieldValues

- `Id`
- `ItemId`
- `FieldId`
- `FieldKey`
- `Language`
- `Version`
- `Value`

Current constraints and indexes:

- primary key on `Id`
- non-unique index on `(ItemId, FieldId, Language, Version)`
- cascade delete from `ContentItems`

## What Is Working Well

Several current decisions still fit the project well.

### 1. Adjacency List Tree Storage Is Still Reasonable

Using `ParentId` on `ContentItems` is still a sensible MVP shape.

Why it still works:

- rename and move semantics are now explicit
- sibling uniqueness is already enforced
- branch-level reads only require parent/child traversal
- the current admin flow refreshes direct branches, not arbitrary deep subtrees

This keeps writes simple and matches the domain model cleanly.

### 2. Sibling Uniqueness Is Correctly Enforced In The Database

The unique index on `(ParentId, Key)` is high value.

It aligns with:

- computed path composition
- current route semantics
- current authoring rules
- the move and rename collision checks already enforced in the application layer

This should be preserved.

### 3. Field Values Are Stored At A Practical Boundary

Storing authored values as `string?` at persistence and projecting typed
values in higher layers still fits the current architecture.

That avoids coupling the database too early to field-type-specific
storage strategies while the content model is still evolving.

## Findings

The following areas are now worth deliberate attention.

### Finding 1: Field Value Identity Is Not Fully Enforced

Current database behavior:

- the repository upserts field values as though `(ItemId, FieldId, Language, Version)` is the logical identity
- the database only has a non-unique index for that combination

Risk:

- duplicate rows could exist if data is inserted outside the current repository logic
- future bulk operations or parallel writes could create ambiguous stored state
- reads assume there is only one stored value per logical slot

Recommendation:

- promote the `(ItemId, FieldId, Language, Version)` index to a unique constraint

Priority:

- high

### Finding 2: Path Lookup Does Not Scale Well In The Current Repository

Current behavior in `EfContentRepository.GetItemAsync(ContentPath)`:

- load all content items
- map them into domain items
- compute every path in memory
- search for the matching item

This is acceptable for tiny datasets and early tests.

It will degrade quickly as content volume grows.

Recommendation options:

#### Option A: Keep Adjacency Storage, Accept Current Lookup For MVP

This is the cheapest option and may still be fine for small early slices.

Tradeoff:

- simplest schema
- weakest path-lookup scalability

#### Option B: Add A Cached Canonical Path Column

Persist a normalized path column, update it on create/rename/move, and
index it uniquely.

Benefits:

- efficient path lookup
- easier uniqueness enforcement for full canonical paths
- simpler branch/query features later

Tradeoffs:

- move and rename now affect descendants
- path update logic becomes a persistence concern
- current domain/persistence separation becomes more complex

Recommendation:

- do not add a persisted path yet
- explicitly note it as the most likely future scaling change

Priority:

- medium

Reasoning:

- current branch-oriented UI flow does not require this yet
- current mutation contracts are branch-based, not path-heavy
- this is the first major schema choice that meaningfully increases write complexity

### Finding 3: There Is No Optimistic Concurrency Boundary

Current shape has no row-version or concurrency token on content items or
field values.

Risk:

- concurrent rename, move, or metadata updates can last-write-win silently
- branch-aware admin interactions make concurrent authoring more likely over time

Recommendation:

- add a concurrency token when authoring becomes multi-user or when publish/workflow semantics start to matter

Possible shapes:

- SQL rowversion/timestamp where supported
- GUID/etag-style version token
- integer revision column

Priority:

- medium

Reasoning:

- it is important, but not yet the most urgent persistence gap
- current single-user development slices can proceed without it

### Finding 4: Template Referential Integrity Is Application-Enforced Only

`TemplateId` on `ContentItems` is just a GUID value in persistence.

That is currently consistent with the project because effective template
resolution and authored templates are not stored in the same relational
shape here.

Risk:

- orphaned template references are possible if persistence and model
  catalog drift

Recommendation:

- keep this application-enforced for now
- revisit only when template persistence settles into a more explicit
  relational boundary

Priority:

- low to medium

### Finding 5: Additional Query Indexes Will Eventually Matter

Current content-item indexes:

- `(ParentId, Key)` unique

Current field-value index:

- `(ItemId, FieldId, Language, Version)` non-unique

Likely future query pressure:

- branch reads by `ParentId`
- template dependency checks by `TemplateId`
- field-value retrieval by `ItemId`

Recommendations:

- add a non-unique index on `TemplateId`
- consider an explicit non-unique index on `ParentId` alone if query plans need it
- keep the current field-value identity index, but make it unique

Priority:

- medium

## Recommended Near-Term Changes

The safest near-term persistence improvements are:

1. Make field-value identity unique at the database level.
2. Add an index on `ContentItems.TemplateId`.
3. Add integration tests that prove those constraints exist behaviorally.

These changes strengthen correctness and lookup performance without
committing the project to a more complex tree-storage strategy yet.

## Recommended Non-Changes For Now

The following should probably wait:

1. Persisted canonical content path column.
2. Nested-set, closure-table, or hierarchyid-style tree redesign.
3. Full concurrency token support.
4. Broader template relational enforcement.

Those may become correct later, but current authoring slices do not yet
justify the added complexity.

## Suggested Next Persistence Slice

If we want to act on this review immediately, the next implementation
slice should be:

### Persistence Hardening For Content MVP

Scope:

- make field-value identity unique
- add `TemplateId` index
- add constraint-oriented integration tests
- document why adjacency-list storage remains the chosen tree strategy for now

This would be a clean, low-risk persistence upgrade that supports the
API and admin workflows already in place.
