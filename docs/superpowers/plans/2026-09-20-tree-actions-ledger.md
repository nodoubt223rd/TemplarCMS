# Native execution ledger

Spec: `../specs/2026-09-19-tem-7-tem-8-tree-actions.md`
Plan: `2026-09-20-tem-7-tem-8-implementation.md`

- Execution approved: native, 2026-09-20.
- Ruling: use the existing workspace on a dedicated feature branch, consistent with the preceding tickets; no second checkout is needed to protect unrelated work because only the approved documents were untracked. This preserves the user's normal local build/deployment paths.
- Ruling: serialize placements as a list of typed template/parent pairs rather than GUID-keyed JSON properties; this avoids a custom dictionary-key converter while retaining uniqueness validation. Cost if wrong: adjust the new organization contract before release.
- Task 1: nested folders/placements, missing parents, cycles, duplicate sibling keys, stale/concurrent writes, malformed files, failed replacement, cancellation, and definition-scan isolation covered. Round-trip test exposed TemplateId constructor deserialization; explicit JsonConstructor fixed it.
- Task 1 verification: content-modeling suite passed 232 tests, including six organization tests. No live data used.
- Task 1 complete: commit fb201bc.
- Task 2 in progress: folder API, template rename/move, shared endpoint mutation filter, startup recovery, and journal tests implemented.
- Ruling: journal keeps the complete before-state and always rolls interrupted operations back, rather than replaying after-state. Commit is removal of the journal after catalog validation. Cost if wrong: an interrupted unacknowledged operation must be retried; authored data is preserved.
- Ruling: transaction coordination is in ContentModeling beside file storage, consumed by the Application organization service and API endpoint filter. This avoids coupling persistence recovery to HTTP. Existing mutation handlers retain their tested signatures.
- Task 2 checkpoint: 147 API tests and 234 model tests passed; subsequent template link checks passed 36 tests. Remaining acceptance expansion: permission-denied cases, endpoint restart/readback, and additional failure injection.
- Task 3 checkpoint: shared ordering repository, service, API, and client comparison implemented. Tests: application 60 passed, integration 15 passed / 2 optional SQL skips; injected SQLite trigger failure preserves revision; frontend tree utilities 10 passed. Remaining: simultaneous writer test and API revision/link acceptance.
- Ruling: expose sibling revision and supported metadata through GET /api/v1/content-order?parentId=...; existing branch response gains parsed sortOrder per item. Menus fetch the fresh order snapshot when opened. Cost if wrong: one additional read per menu and future branch-contract consolidation.
