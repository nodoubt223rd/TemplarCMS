# ADR-0014: Template containment and content ordering

Status: Accepted — 2026-09-25

## Context

TEM-7 and TEM-8 add actions on the clicked content/template tree node. Authors need nested template folders without changing inheritance, and persistent sibling ordering without a new SQL column.

## Decision

Template folders are Items defined by the built-in Template Folder template. Containment is independent of defining-template identity and ordered template inheritance. Moving a definition changes its placement, not its bases, fields, key, or identity. Unmapped definitions appear at the Templates root. Folders contain folders and definitions; definitions cannot be parent destinations. Protected system definitions cannot be renamed, moved, or deleted. Nonempty folders and templates with dependencies cannot be deleted.

The configured template directory contains top-level definition JSON and an `Organization` subdirectory. Its `tree.json` stores a revision, folder records, and typed placement pairs. Definition scans exclude the subdirectory. Missing parents, cycles, duplicate sibling keys, and malformed organization data are rejected. Organization mutations require the current revision; stale requests return conflicts.

Template mutation coordination serializes writers with a filesystem lock and records a complete before-state journal before writing. Catalog validation precedes commit; removing the journal commits the operation. Failure or startup recovery restores the before-state. An interrupted, unacknowledged operation may need to be retried. Definition and organization files must be backed up together during a write pause; do not remove a recovery journal manually.

Content sibling ordering uses the effective shared string field `__sortorder`. Reads parse invariant integers, put invalid/missing values last, and break ties by normalized key and ID. Reorder writes sequential invariant integer strings for the sibling group in one transaction. A revision derived from parent, identities, and original values detects stale requests. Unsupported effective fields reject the operation. SQLite and SQL Server use transactional compare-and-write; SQL Server uses serializable isolation. No SQL schema migration is needed.

The client keeps clicked target, selection, and dialog state separate. Mutations refresh affected contextual branches and preserve unrelated expansion and drafts. Reordering also updates the selected sibling's draft sort-order field so a later content save cannot restore an old value. Icon-only updates preserve field drafts.

Display icons resolve as item override, effective template icon, then `file`. Nullable authored template icons remain separate from effective display icons in detail responses. Template changes therefore flow to inheriting items and derived templates; explicit overrides remain unchanged. Clearing an override resumes inheritance. Tree and editor header share the resolver and icon renderer.

## HTTP surfaces

- `GET /api/v1/template-organization`: revisioned folders and definition placements/protection metadata.
- `POST /api/v1/template-folders`: create a folder; folder rename/move/dependencies/delete endpoints use its identity.
- Template create accepts an optional parent folder and organization revision; template rename/move use dedicated endpoints. Existing dependency/delete guards remain authoritative.
- `GET /api/v1/content-order?parentId=...`: current sibling revision and ordering support.
- `POST /api/v1/content/{id}/reorder`: direction plus expected revision. Normal branch/item responses include parsed sort order.

Authoring authorization applies to mutation routes. API validation and conflict responses remain authoritative even when the menu prechecks a target.

## Deployment and validation

Runtime data is excluded from API publish output. Existing deployment preservation covers `RuntimeData` and populated legacy `App_Data/Templates`, including nested organization data. If templates are configured outside these paths, operators must preserve and back up that configured location. Never replace instance organization data with another environment's data or publish artifacts.

Acceptance uses isolated SQLite and temporary JSON storage. Tests cover recovery, revision conflicts, concurrent SQLite reorder writers, permission denial, application restart, dependency guards, and icon inheritance. Browser checks cover nested creation/move/rename/reload and draft preservation. Optional SQL Server rehearsal tests require explicit configuration; SQLite evidence does not establish SQL Server concurrency behavior.
