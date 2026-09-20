# TEM-7 / TEM-8: Tree actions and template folders

Status: Approved by UX and business, 2026-09-20, with icon-inheritance clarification

## Intended outcome

Authors can right-click a content item to create a child item from a template,
create a folder, reorder siblings, move, rename, or delete. Template authors can
organize templates into nested folders and perform creation, moving, renaming,
and guarded deletion from that tree. Actions target the clicked node even when
another item is selected in the editor.

The user confirmed that templates have parent folders like content items.
Placement is independent of inheritance. Every template definition remains an
Item described by Template. Existing definitions retain their IDs, fields,
defining template, and ordered base templates. System definitions remain
protected. TEM-7 covers content actions; TEM-8 covers template organization.

## Approved icon inheritance behavior

- Content items with no explicit icon override display their effective template's
  current icon. Inheritance is resolved dynamically, not copied into the item
  when it is created.
- Changing a template icon updates every item inheriting that icon, including
  existing items. Items with explicit icon overrides retain their own icons.
- An explicit override remains an override even if it currently matches the
  template icon. Do not infer inheritance by comparing icon strings.
- Clearing an item's override (Use template icon) restores inheritance immediately.
- When a derived template has no local icon, its effective icon follows the
  existing ordered inheritance rules. A derived template's explicit icon stops
  icon changes from a base template propagating to its items.
- After saving a template icon, refresh the effective template/icon lookup and
  affected visible UI so the tree and selected-item displays agree without a
  manual page refresh. Preserve unsaved editor values while doing so.
- Keep absent overrides absent in persistence and API contracts. Moving an item
  or template between folders does not create or change an icon override.

## Existing behavior

- The active content tree emits selection and expansion only. An older menu
  component exists but is disconnected and uses a different node model.
- Content creation, rename, move, deletion, and dependency APIs already exist.
- Content children are sorted by key in persistence and again in the client.
  The Standard fields already include shared string `__sortorder`; menu ordering
  therefore requires backend work, not just wiring buttons.
- Template definitions use JSON storage and a flat catalog. Template create,
  update, delete, and dependency APIs exist; parent placement does not.

## Approach and alternatives

Recommended: keep template definitions in their existing JSON repository and
store template-folder items plus template-to-folder placement in one JSON
hierarchy document under `RuntimeData/Templates/Organization/tree.json`.
This is outside the current top-level definition scan. Folder changes can be
written atomically without changing schema-definition files or SQL tables.

Alternatives considered: placing definitions/folders into the relational content
tree would require a broader storage migration and synchronization policy;
embedding placement in every definition file would spread folder operations
across multiple documents. Neither is necessary for these tickets.

## Template organization model

- Add a strongly typed template-folder identity and a folder Item definition.
  A source-controlled Template Folder template inherits Standard and describes
  these folder items. Template definitions still use Template.
- The Templates root is a virtual, protected container, not a deletable folder.
  A null parent means that root. Existing definitions appear there automatically
  when no placement entry exists; no migration of definition IDs is needed.
- The hierarchy document stores folders, their parent IDs and display metadata,
  template placements, and a revision token for conflict detection.
- Only template folders can be parents of templates or other template folders.
  Templates themselves are leaves in this tree. This does not affect inheritance.
- Validate parent existence, cycles, identity uniqueness, normalized sibling
  folder keys, and name limits. Template keys remain globally unique because
  they identify inheritance references and existing storage files.
- Folder renaming changes display metadata, not identity. Template renaming
  through the menu changes the display name and preserves its key and field IDs.
- System templates cannot be renamed, moved, or deleted. Authored folders cannot
  be deleted while they contain templates or other folders. No cascade delete.
- Serialize organization mutations and use atomic file replacement plus revision
  checks. Coordinate definition create/delete with organization changes and
  explicit rollback/recovery so failed writes do not hide or lose definitions.
- Organization metadata is instance data: exclude it from publish artifacts and
  preserve it during IIS deployment along with other RuntimeData.

## Content ordering

- Interpret the existing shared `__sortorder` value as an invariant integer for
  tree order. Missing, invalid, or tied values use a deterministic key/ID fallback.
  Keep the existing field ID, string storage, and scope unchanged.
- Add a reorder operation with up/down/first/last directions. It reads the actual
  siblings on the server, computes the new order, and persists sibling values as
  one transaction. The service must reject stale/conflicting sibling state and
  must not partly reorder a branch.
- Return the refreshed branch and authoritative ordering metadata. Remove the
  client-side key sort that would otherwise undo the server order. No-op boundary
  actions are disabled in the menu and handled safely by the server.
- Ordering applies across languages and versions because the field is shared.
  Items whose effective schema lacks the compatible shared field cannot expose
  reorder actions. New or moved items retain deterministic placement; authors
  can then explicitly reorder them.
- No automatic SQL schema migration is required. Any necessary transactional
  repository contract is added above the existing field-value tables.

## API behavior

- Reuse content mutation and dependency endpoints. Add reorder and template
  organization endpoints with the existing authoring policy, ProblemDetails,
  HATEOAS links, and OpenAPI conventions.
- Expose an organization tree/read response containing folders and template
  summaries with parent placement and available actions. Keep inheritance
  responses distinct and compatible with existing consumers.
- Template creation accepts a target folder through the coordinated authoring
  operation. Move/rename are metadata operations that preserve definition data.
- Recheck dependency and parent state on the server at mutation time; a menu
  being enabled is not authorization or proof that deletion remains safe.
- Template deletion remains blocked by existing content usage and inheritance
  dependencies. Folder deletion reports contained items rather than removing them.

## Workspace interactions

- Right-click or keyboard menu activation opens a shared accessible menu for the
  clicked node. A visible actions button supplies access without right-click.
  Menu focus, arrow navigation, Escape, outside click, viewport placement, and
  listener cleanup are covered.
- Content menu: New Item from Template, New Folder, Move Up/Down/First/Last,
  Move to, Rename, Delete. Creation preselects the clicked item as parent;
  New Folder selects the Folder template.
- Templates root/folder menu: New Template and New Folder. Authored folders also
  offer Rename, Move to, and Delete. Authored templates offer Rename, Move to,
  and Delete; a template is not a parent container.
- Use dialogs with names, template/destination selection, validation, and clear
  delete confirmation. Filter invalid destinations, including descendants.
- Preserve unsaved editor values when operating on another node. If an action
  requires discarding the current draft, ask before proceeding.
- Apply mutations to affected branches, preserving expansion and selection when
  possible. Remove a deleted selection and show a useful empty state. Failed
  requests leave the tree and drafts intact and show actionable error feedback.
- Template folder UI appears in the template workspace; it does not duplicate
  template definitions into the content tree or change content paths.

## Verification and delivery

Tests cover clicked-versus-selected targets; child creation; keyboard dismissal;
ordering persistence, boundaries and concurrency; folder nesting and restart;
invalid parents/cycles; renames preserving field IDs; protected system definitions;
nonempty-folder and dependency-blocked deletion; write-failure recovery; and old
flat catalogs loading under the root with unchanged effective fields.

Icon tests cover existing and newly created items inheriting a template icon,
template-icon changes propagating after save, explicit overrides remaining
unchanged (including an override equal to the previous template icon), clearing
an override, and derived-template icon inheritance. Verify both the tree and
selected-item displays, and ensure ordinary saves do not materialize inherited
icons as overrides.

Run relevant domain/model/application/API/integration suites, full frontend lint,
tests and production build, and solution build with the pinned .NET 8 SDK.
Exercise create, move, rename, reorder and delete through a local isolated runtime
with temporary storage, not the live IIS installation or production databases.

Implement backend organization and ordering first, then shared menu/dialog
components, then active workspace wiring. Keep TEM-7 and TEM-8 changes identifiable
in commits and PR notes. Update an ADR and current-state documentation alongside
the modeling/API changes. Deployment remains a separate operator action.
