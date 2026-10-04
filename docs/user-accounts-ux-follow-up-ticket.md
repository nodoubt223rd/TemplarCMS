# User accounts UX: finish role selection and accessible directory flows

Suggested team: UX  
Related work: TEM-9 and TEM-9 follow-up (UX repo PRs #4 and #5)  
Repository: `TemplarCMS-VUE`  
Scope: Directory prototype polish and an annotated handoff for production integration

## Problem

The latest UX update uses the agreed five-role catalog and respects the current
directory-only boundary. Role membership still reuses a content-item picker:
it displays raw identifiers, fabricated content paths, and ordering controls
that do not explain role access. Profile entry and modal interactions also
need keyboard and focus support. Pending-user creation can discard entered
data without confirmation.

Complete these interactions so the prototype is a usable reference for the
production authoring client.

## Requested work

### 1. Make role selection specific to user membership

- Show readable labels: Author, Developer, Forms Editor, and Templar Admin.
- Include concise role descriptions based on the
  [account UX brief](user-accounts-ux-handoff.md#target-role-catalog).
- Support finding, assigning, and removing multiple direct memberships,
  including an empty selection.
- Remove content paths, folder/language/version metadata, and ordering controls
  from the membership experience. Membership is an unordered set.
- Preserve the shared content picker behavior for its content-editing consumers;
  use a role-specific component or configurable presentation.
- Keep Marketing Automation Editors visibly described as planned and unavailable
  for assignment, including through Select all.
- Continue explaining that assignments are recorded only and do not enforce
  permissions in the current release.
- Annotate how direct versus inherited access would be shown. Inheritance policy
  is unresolved: do not implement automatic assignments or claim effective
  permissions until product confirms the rules.

### 2. Complete keyboard and dialog behavior

- Provide a semantic, keyboard-operable control to open each user profile, with
  a clear accessible name and visible focus.
- Move focus into the Add pending user and profile dialogs when they open;
  contain focus while modal; return focus to the initiating control on close.
- Make Escape follow the same close/discard rules as Cancel, Close, and backdrop
  dismissal.
- Make the discard confirmation accessible as a modal confirmation. Move focus
  into it, prevent interaction with the underlying form, and return focus to
  that form when Continue editing is selected. Escape should continue editing.
- Associate form labels with controls and validation/helper text with its field.
  Identify invalid fields and focus the first invalid field after submission,
  opening the appropriate profile section when necessary.
- Give role-picker selection and transfer controls meaningful accessible names
  and expose selected state to assistive technology.
- Verify usable layouts at narrow widths and increased zoom, with visible focus
  and readable text/status contrast. Do not rely on color alone for status.

### 3. Make creation and editing behavior consistent

- Apply the unsaved-change guard to pending-user creation as well as profile
  editing. Cancel, Close, backdrop, and Escape must not silently discard data.
- Preserve existing trim/case-insensitive duplicate-email checks in both flows.
  Trim names and email before committing a new prototype record.
- Use consistent unavailable sign-in activity wording in the directory and
  profile, rather than an unexplained dash or a claim that the user never signed in.
- Keep status read-only, invitations/credentials/lifecycle actions deferred, and
  confirmations explicit that creation sends no invitation and grants no login.

## Acceptance criteria

1. An administrator can open a profile, edit its fields and role memberships,
   and save or cancel using only the keyboard. Focus returns predictably.
2. Membership shows readable role names and descriptions, with no fabricated
   content metadata or role-ordering controls. Select all excludes the planned role.
3. Empty membership is valid and says “No roles assigned,” without asserting
   future authorization behavior.
4. Every dismissal path on a changed profile or pending-user form offers Discard
   changes or Continue editing. Continuing preserves the draft.
5. Validation identifies the affected field and provides a clear correction
   path. Duplicate emails differing only in whitespace/case are rejected.
6. Missing sign-in timestamps use consistent, accurate wording in both views.
7. The prototype includes review examples for multiple roles, no roles, long
   names/emails, and invited/suspended/deactivated records.
8. `npm run build` passes. The handoff records keyboard, zoom, and responsive
   verification, plus any limitations that still require engineering work.

## Deliverables

- A PR to the UX repository containing the updated interactions.
- Screenshots or a short walkthrough showing role selection, validation, and
  discard/continue flows, including a narrow viewport.
- An annotated state/interaction handoff for production integration: loading,
  saving, failed save with draft retained, and concurrent-edit recovery.
- An explicit decision note for role inheritance; distinguish confirmed policy
  from a proposed visual treatment.

## Outside this ticket

Production API persistence, revision/conflict enforcement, actual asynchronous
save handling, stored-role migration, authentication, permission enforcement,
invitation delivery, account lifecycle, and audit storage belong to engineering
follow-up work. This prototype may illustrate those states but must not claim
they are implemented. Do not introduce credentials or seed production users.

## Review evidence

Reviewed UX revision: `9f205d1` (PR #5), implementation commit `ca655ed`.
With `.figma/make/site.json` present, `npm run build` passed on 2026-10-02.
The previous missing-configuration blocker is resolved; remaining Vite warnings
concern compatibility with a future native configuration loader.

Relevant source files:

- `src/components/security/UserManager.vue`
- `src/components/security/UserProfile.vue`
- `src/components/fields/MultilistWithSearchField.vue`
- `src/composables/useUsersState.ts`
- `src/data/users.ts`
