# TEM-7 / TEM-8 implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver usable content-tree actions and persistent nested template folders, preserving template inheritance, authored data, and dynamic icon inheritance.

**Architecture:** Keep content in the relational repository and template definitions in their JSON repository. Store template organization in one revisioned JSON document. Add transactional content ordering and coordinated template mutations behind existing authoring policies; connect both trees to shared accessible menu/dialog components.

**Tech Stack:** Existing .NET 8, EF Core SQL Server/SQLite, JSON template storage, Vue 3, TypeScript, xUnit, Vitest. No framework or SDK upgrade.

**Spec:** [Approved design](../specs/2026-09-19-tem-7-tem-8-tree-actions.md), approved 2026-09-20 including UX/business icon clarification.

## Global constraints

- Placement is independent of inheritance.
- Every template definition remains an Item described by Template.
- Existing definitions retain their IDs, fields, defining template, and ordered base templates.
- System templates cannot be renamed, moved, or deleted.
- A null parent means that root.
- Keep absent overrides absent in persistence and API contracts.
- No automatic SQL schema migration is required.
- Deployment remains a separate operator action.
- Never run mutation smoke tests against live IIS or production databases.
- Existing templates without organization data must remain readable at the Templates root.
- Preserve unsaved editor values when operating on another node.

## Review focus

1. Two authors reorder or reorganize the same branch: reject stale revisions; never partially apply either operation (tasks 1–3).
2. A process stops between template-file and hierarchy-file updates: recover deterministically without losing a definition (task 2).
3. An explicit item icon equals its previous template icon: preserve the override when that template changes (task 5).
4. Right-click another node while editing unsaved content: act on the clicked identity and preserve the draft (task 6).
5. A folder is renamed/moved/deleted after a dialog opens: revalidate server state and retain the user's dialog input on conflict (tasks 2, 4, 6).

## Task 1 — Template hierarchy model and atomic JSON repository (TEM-8)

**Create:** `src/TemplarCMS.Domain/Content/TemplateFolderId.cs`; `src/TemplarCMS.ContentModeling/Organization/TemplateFolderDefinition.cs`, `TemplateOrganizationSnapshot.cs`, `TemplateOrganizationValidator.cs`, `ITemplateOrganizationRepository.cs`, `JsonTemplateOrganizationRepository.cs`.

**Modify:** `src/TemplarCMS.Domain/Content/BuiltInTemplateKeys.cs`, `SystemTemplateIds.cs`; `src/TemplarCMS.ContentModeling/Definitions/BuiltInTemplateProvider.cs`.

**Tests:** `tests/TemplarCMS.ContentModeling.Tests/Organization/TemplateOrganizationTests.cs` and `JsonTemplateOrganizationRepositoryTests.cs`; adjust built-in catalog assertions for Template Folder.

**Interfaces:** Define `TemplateFolderId` as a nonempty GUID value object following `ContentItemId`. `TemplateFolderDefinition : Item` has `Id`, `Name`, `Key`, and nullable `ParentId`; its defining template is the new protected Template Folder template. `TemplateOrganizationSnapshot` contains `Guid Revision`, `IReadOnlyList<TemplateFolderDefinition> Folders`, and `IReadOnlyDictionary<TemplateId, TemplateFolderId> Placements`. Missing placement means root.

```csharp
public interface ITemplateOrganizationRepository
{
    Task<TemplateOrganizationSnapshot> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TemplateOrganizationSnapshot snapshot, Guid expectedRevision,
        CancellationToken cancellationToken = default);
}
```

- [ ] Write tests for an absent organization file returning an empty snapshot, round-trip nested folders and placements, stale revisions, duplicate normalized sibling keys, nonexistent parents, self/descendant cycles, invalid GUIDs, and malformed files. Corruption must produce a diagnostic; it must not silently become an empty hierarchy.
- [ ] Run `dotnet test tests/TemplarCMS.ContentModeling.Tests --filter FullyQualifiedName~Organization` and verify the new behavior is missing before implementation.
- [ ] Implement validation using the existing validation-result pattern. Keep folders outside the inheritance graph and out of content-template selection lists.
- [ ] Store `Organization/tree.json` below the configured templates directory. Serialize writes using a cross-process file lock with bounded acquisition, reread the revision under that lock, write a same-directory temporary file, flush, and atomically replace. Each successful write generates a new revision. Cancellation and failed replacement leave the original file intact.
- [ ] Test two repository instances attempting writes from the same revision: one succeeds, one reports conflict. Inject a replacement failure and verify the prior document still parses and retains its original revision.
- [ ] Verify creating organization data does not change the results of the existing top-level JSON definition scan. Re-run the content-modeling suite; commit only this task's model/repository changes.

## Task 2 — Coordinated template/folder authoring APIs (TEM-8)

**Create:** `src/TemplarCMS.Application/Templates/TemplateOrganizationService.cs`; `src/TemplarCMS.Api/Templates/TemplateOrganizationEndpoints.cs`, `TemplateOrganizationResponse.cs`; `tests/TemplarCMS.Api.Tests/Templates/TemplateOrganizationEndpointsTests.cs`.

**Modify:** `TemplateEndpoints.cs`, `TemplateResponse.cs`, API runtime registration and endpoint mapping, and the JSON repository's mutation coordination boundary.

**Consumes:** Task 1 repository/snapshot, `ITemplateRepository`, `IContentModelCatalog`, existing content/template dependency queries. **Produces:** revisioned organization read and authoring operations using the existing ProblemDetails and authorization conventions.

| Route | Operation |
| --- | --- |
| `GET /api/v1/template-organization` | Return revision, folder items, and template placement/action metadata |
| `POST /api/v1/template-folders` | Create folder with name, parent ID, expected revision |
| `POST /api/v1/template-folders/{id}/rename` | Change display name, preserving identity/key |
| `POST /api/v1/template-folders/{id}/move` | Change parent, checking cycles and sibling keys |
| `DELETE /api/v1/template-folders/{id}` | Delete only an empty folder using expected revision |
| `POST /api/v1/templates/{id}/move` | Change organization placement only |
| `POST /api/v1/templates/{id}/rename` | Change display name only, preserving key and field IDs |

- [ ] Add endpoint tests for missing authentication, permission denial, stale revision, missing destinations, protected system templates, cycles, nonempty folders, dependency-blocked template deletion, and successful restart/readback.
- [ ] Extend template creation with optional parent-folder ID and expected organization revision. Existing clients creating a root template without those fields remain supported. Return fresh organization revision and useful action links after mutations.
- [ ] Route all related definition create/update/delete operations through the same mutation coordinator. Do not introduce a lock that protects only the new folder endpoints while old endpoints can bypass it.
- [ ] Coordinate multi-file operations using a durable pending-operation journal under `Organization`, containing the operation ID, expected revision, and before/after documents. Write the journal before mutation; complete or restore under the lock before accepting the next mutation or publishing a catalog snapshot. On an exception, restore the before-state and refresh the catalog; retain the journal if recovery fails and report the failure.
- [ ] Inject failure after the definition write and before hierarchy replacement, then construct a fresh service and verify recovery. Ensure a failed create does not leave a hidden template and a failed delete does not lose its definition.
- [ ] Preserve the existing full-definition update semantics. The new rename action must clone existing sections/fields rather than reconstruct them from a reduced request. Validate template dependencies again at deletion time.
- [ ] Use `409` for stale/conflicting operations, `404` for missing identities, structured validation failures for invalid input, and the existing authoring policy for every mutation. Reflect each route and link in OpenAPI.
- [ ] Run API and model suites, verify old flat catalogs still expose every definition under the root, and commit the coordinator/API task.

## Task 3 — Persistent content ordering (TEM-7)

**Create:** `src/TemplarCMS.Domain/Content/ContentOrderDirection.cs`; `src/TemplarCMS.Application/Content/ContentOrderingService.cs`; `src/TemplarCMS.Abstractions/Content/IContentOrderingRepository.cs`; `src/TemplarCMS.Persistence/Content/EfContentOrderingRepository.cs`; ordering tests in Application.Tests and Integration.Tests.

**Modify:** `ContentItemService.cs`, `ContentLookupEndpoints.cs`, content response/link contracts, dependency registration, and `src/TemplarCMS.Admin/templarcms.admin.client/src/utils/content-tree.ts`.

**Interfaces:** `ContentOrderDirection` has Up, Down, First, Last. Add `POST /api/v1/content/{id}/reorder` accepting direction and expected sibling revision. Branch reads expose that revision; content summaries expose parsed nullable sort order and whether ordering is supported. The repository owns an atomic compare-and-write over sibling identity/order state, not a sequence of independent item saves.

- [ ] Write tests for each direction, first/last boundary no-ops, malformed/missing values, duplicate order values, absent/incompatible sort-order fields, and deterministic key/ID tie-breaking.
- [ ] Build a sibling revision from ordered identities, parent identity, and the original stored shared-order values. In the transaction, reread and compare before computing/writing replacements; reject missing/moved/deleted siblings.
- [ ] Resolve each sibling's effective `__sortorder` field identity and shared scope through the catalog. Preserve the existing string field type and write invariant integer strings at shared scope. Refuse unsupported branches rather than partially ordering compatible siblings.
- [ ] For SQL Server use a serializable transaction; for SQLite use its appropriate write transaction. Roll back on failure. Retryable concurrency failures surface as a conflict, not a partially reordered branch.
- [ ] Normalize reordered sibling values to sequential integers and save all values together. Read the authoritative branch after commit. Ordinary reads retain deterministic behavior for historical invalid values.
- [ ] Replace client sorting by path/key with the server's order metadata and matching tie-breaks, including insert/update/delete branch reconciliation.
- [ ] Verify cross-language/version consistency and two concurrent reorder attempts. Inject a failure during sibling writes and assert all previous values remain. Run application/integration/API tests and commit.

## Task 4 — Shared accessible menus and dialogs (TEM-7 / TEM-8)

**Modify:** `components/tree/ContextMenu.vue`, `Treenode.vue`, `ContentTree.vue`.
**Create under the Vue client `src`:** `types/tree-actions.ts`, `components/tree/TreeActionDialog.vue`, `components/tree/ContextMenu.test.ts`, `components/tree/TreeActionDialog.test.ts`.

**Interfaces:** Menu rendering consumes typed action descriptors instead of the obsolete `TreeItem` model. All emitted actions carry a stable target identity.

```ts
export type TreeActionTarget =
  | { kind: 'content'; id: string }
  | { kind: 'template'; id: string }
  | { kind: 'template-folder'; id: string }
  | { kind: 'templates-root'; id: null }
export type TreeAction = 'new-item' | 'new-template' | 'new-folder'
  | 'move-up' | 'move-down' | 'move-first' | 'move-last'
  | 'move' | 'rename' | 'delete'
export type TreeActionRequest = { target: TreeActionTarget; action: TreeAction }
```

- [ ] Add mount tests opening the menu via right-click, keyboard ContextMenu/Shift+F10, and a visible actions button. Assert target identity, not the editor's selected ID.
- [ ] Implement menu/menuitem roles, initial focus, arrow/Home/End navigation, disabled-action skipping, Escape/outside dismissal, focus restoration, and viewport clamping. Register named listeners and remove them on unmount; non-Escape keys must not consume Escape handling.
- [ ] Supply dialogs for create/rename/destination selection and delete confirmation. Pass destination data and action callbacks from the workspace; the dialogs must not construct API URLs themselves.
- [ ] Preserve values and focus after validation or server conflicts. Disable duplicate submissions. Filter descendant/self destinations while retaining server-side checks.
- [ ] Test reopen/unmount behavior and narrow viewport placement. Run frontend lint/tests/build; commit the shared controls.

## Task 5 — Preserve dynamic icon inheritance (both tickets)

**Modify:** `TemplateResponse.cs`, `TemplateEndpoints.cs`, template request mapping, `types/admin-api.ts`, `App.vue`, `AuthorWorkspace.vue`, `components/templates/TemplateDesigner.vue`, `components/tree/Treenode.vue`, and selected-item/icon-picker components discovered through their imports.

**Tests:** Existing effective-template builder and template endpoint tests; `components/templates/TemplateDesigner.test.ts`; a new `utils/item-icon.test.ts` alongside `utils/item-icon.ts`.

**Interface:** A shared display-only resolver never writes its result back into an item's authored icon.

```ts
export function resolveItemIcon(
  override: string | null | undefined,
  effectiveTemplateIcon: string | null | undefined
): string {
  return override ?? effectiveTemplateIcon ?? 'file'
}
```

- [ ] Assert an absent item override follows icon A then icon B; an explicit A stays A after the template changes to B; clearing the override returns B. Test derived templates both with and without a local override.
- [ ] Distinguish authored nullable template icon from effective icon in template detail responses. Keep the existing display icon contract compatible by adding a separate authored-icon property where needed; serializers and update requests must preserve null.
- [ ] Verify ordinary template and item saves do not turn resolved icons into authored overrides. Provide Use template icon for items and inheritance-reset behavior for derived template icons.
- [ ] After successful template saves refresh the effective catalog lookup, including derived templates. Use that reactive lookup in both tree and selected-item displays; do not reload or replace unsaved content field drafts.
- [ ] Test both display surfaces during an icon change, then run API/model and frontend suites. Commit icon semantics separately for clear review.

## Task 6 — Wire both active workspaces (TEM-7 / TEM-8)

**Create:** `composables/useTreeActions.ts`, `components/templates/TemplateTree.vue`, `utils/template-tree.ts`, and corresponding Vitest tests under the Vue client `src`.
**Modify:** `App.vue`, `AuthorWorkspace.vue`, `types/admin-api.ts`, content-tree event wiring, and template designer selection/creation wiring.

**Consumes:** Tasks 2–3 API responses and revision tokens, task 4 requests/dialogs, task 5 icon resolver. **Produces:** working actions from both active trees, with stable selection/draft state.

- [ ] Add workspace tests with two distinct items: selected A has unsaved values; right-click B, create/move/rename/delete under B; assert requests use B and A's values remain unchanged.
- [ ] Implement the content menu using existing create, rename, move, dependency, and delete links plus the reorder link. New Folder chooses Folder; New Item opens the template picker and preselects the clicked parent. Use a fresh lookup for the target, never a selected-item-only helper.
- [ ] Render nested template folders from the organization response, with all unmapped definitions under the root. Add New Template/New Folder to root/folder menus and Rename/Move/Delete to supported authored nodes. Templates cannot be parent destinations.
- [ ] Separate menu target, editor selection, and dialog state. Confirm before discarding a draft when a successful operation must clear the selected editor. Reconcile only affected branches and keep unrelated expansion state.
- [ ] Refresh dependency/parent/revision data when a dialog opens and handle conflicts at submit time without losing input. Show actionable dependency details for blocked deletions.
- [ ] Test existing-flat-catalog rendering, nested folders, empty-folder deletion, blocked nonempty deletion, unsupported/protected actions, stale destination, reorder reconciliation, and failed requests preserving drafts.
- [ ] Run complete frontend checks and commit active-workspace wiring.

## Task 7 — Documentation, isolated acceptance, and review

**Modify:** `docs/current-state-summary.md`, API documentation where routes are documented, and `docs/decisions/ADR-0014-template-containment-and-content-ordering.md` (create).

- [ ] Record containment versus inheritance, revision conflicts, shared sort-order interpretation, JSON recovery, icon override semantics, and backup/deployment preservation rules. Correct the current-state document's stale default-branch paragraph while editing it.
- [ ] Run the backend acceptance sequence from the repository root:

```powershell
. ./scripts/templar-cms-bootstrap.ps1
dotnet build ./TemplarCMS.sln --configuration Release
dotnet test ./TemplarCMS.sln --no-build --no-restore --configuration Release
```

- [ ] Run the frontend acceptance sequence from `src/TemplarCMS.Admin/templarcms.admin.client`:

```powershell
npm run lint
npm test
npm run build
```

- [ ] With temporary SQLite/JSON storage, exercise create child/folder/template, restart, move/rename/reorder, inherited/overridden icon changes, and successful/blocked deletions. Inspect the active UI, not only API responses. Ensure no production connection string is inherited by the isolated host.
- [ ] Verify publish output excludes organization/runtime data and deployment preservation still covers it. Report optional SQL tests separately if they were skipped; do not claim SQL concurrency was verified by SQLite tests.
- [ ] Review the whole branch against the approved spec. Resolve correctness findings and rerun affected checks. Commit documentation and package the work with separate TEM-7/TEM-8 validation notes; do not deploy or merge automatically.

## Execution and checkpoints

Recommended: native execution in this session, followed by independent whole-branch review. The repository coordination, API contracts, and workspace state depend closely on one another, so a single implementer can keep those interfaces consistent. Each task still ends with targeted verification and a separate commit. Subagent-driven execution is also available if per-task independent reviews are preferred.

Before product-code changes, create or select an isolated feature checkout from up-to-date trunk and carry this approved spec/plan into it. Preserve unrelated edits. The parent placement and icon rules in the approved spec take precedence if any implementation detail here conflicts with them.

Self-review: every approved behavior maps to a task above; each review-focus failure has explicit verification in its owning task. No live-data changes are part of this plan.
