# Native execution ledger

Spec: `../specs/2026-09-19-tem-7-tem-8-tree-actions.md`
Plan: `2026-09-20-tem-7-tem-8-implementation.md`

- Execution approved: native, 2026-09-20.
- Ruling: use the existing workspace on a dedicated feature branch, consistent with the preceding tickets; no second checkout is needed to protect unrelated work because only the approved documents were untracked. This preserves the user's normal local build/deployment paths.
- Ruling: serialize placements as a list of typed template/parent pairs rather than GUID-keyed JSON properties; this avoids a custom dictionary-key converter while retaining uniqueness validation. Cost if wrong: adjust the new organization contract before release.
- Task 1: nested folders/placements, missing parents, cycles, duplicate sibling keys, stale/concurrent writes, malformed files, failed replacement, cancellation, and definition-scan isolation covered. Round-trip test exposed TemplateId constructor deserialization; explicit JsonConstructor fixed it.
- Task 1 verification: content-modeling suite passed 232 tests, including six organization tests. No live data used.
