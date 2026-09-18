# Template composition implementation plan

**Goal:** Remove Item as a template; compose Standard from reusable section templates and distinguish an item's defining template from template inheritance.

**Architecture:** Item is a domain base type. Template definitions use Template as their defining template; Template inherits Standard. Standard inherits the eight existing system section templates. Page is an editable starter definition inheriting Standard, not a protected system template. Preserve field IDs and values.

**Tech stack:** Existing .NET 8, JSON template storage, SQL Server/SQLite content storage, Vue admin.

**Spec:** The approved model in this conversation; recorded in docs/decisions/ADR-0013-items-and-template-composition.md with this change.

## Constraints

- Preserve ordered multiple inheritance and effective-template runtime consumption.
- Preserve existing uncommitted deployment and editor fixes.
- Do not deploy, rewrite live JSON, or migrate live SQL during implementation.
- Existing installations require a backup and explicit conversion of legacy Item references before deployment.

## Tasks

- [x] Add graph regression tests proving Standard inherits section templates, Template inherits Standard, and Item is absent; run to observe the old model failing.
- [x] Implement Item base type and separate defining-template identity; expose the identity through template responses.
- [x] Extract existing system sections into source-controlled templates without changing their field IDs, metadata, or scopes. Keep Content on Standard for compatibility.
- [x] Seed an editable Page only when absent, retaining the old starter ID on new installs. Seed template tree entries using Template rather than Folder.
- [x] Replace frontend Item-specific protection with the system template catalog keys.
- [x] Add an offline migration script that backs up JSON, changes legacy base references to Standard while preserving Body, and emits transactional SQL to reassign legacy content to the existing Page ID. Test using disposable fixtures.
- [x] Verify content-modeling, domain, application, API, and frontend suites. Update architecture and current-state documentation with migration instructions and limitations.

## Verification completed 2026-09-17

Content-modeling: 226 passed; application: 55 passed; API: 140 passed; frontend: 124 passed and production build succeeded. The domain test project has no discoverable tests. The isolated SQLite API smoke test returned Healthy and exposed Page's SEO and inherited administrator fields. Migration fixtures passed after review fixes, including field-ID mapping and refusal of incompatible scope/inheritance. Generated SQL was reviewed but has not been executed against SQL Server. No live deployment or database conversion was performed.
