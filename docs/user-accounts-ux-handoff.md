# TemplarCMS user accounts: UX handoff

Date: 2026-09-30  
Status: Ready for UX exploration; unresolved product decisions are listed below.  
Audience: UX, product, and engineering

## Objective

Design the user account screens so an administrator can find people, maintain
their profiles, understand role membership, and manage access with confidence.
Return annotated flows and screen designs that engineering can translate into
bounded implementation tickets with observable acceptance criteria.

The role names and descriptions below are supplied by the product owner.
Workflow recommendations and future safeguards are proposals for review, not
existing functionality or settled authorization policy.

## Users and primary jobs

- **Templar Admin:** maintain the directory, assign roles, and eventually manage
  invitations and account lifecycle.
- **Account holder:** eventually complete onboarding, sign in, recover access,
  and maintain permitted personal preferences. Self-service scope needs a
  product decision.
- **UX and engineering:** identify which controls are actionable now, which
  depend on future services, and which require a product decision.

## Current capability boundary

The implemented System workspace supports a persistent user directory:

- Search by name/email, role and status filters, and pagination.
- Create a pending user record with status `Invited`.
- Edit first name, last name, email, preferred interface language, and roles.
- Display account status, creation date, and last-login value.
- Record multiple assignments from the existing fixed role catalog.
- Detect conflicting profile writes using a revision token.

Creating a record sends no invitation and enables no login. Assignments do not
currently enforce permissions. Status is read-only. Credentials, lifecycle
actions, invitation delivery, and durable security audit events are deferred.
The current operator API-key connection is a transitional internal mechanism.

The backend currently has a different seven-role catalog. The five-role target
below needs an engineering mapping and migration decision; it has not yet been
implemented. The separate TemplarCMS-VUE prototype includes invitation and
status controls that exceed the backend's current capabilities.

Use [ADR-0007](decisions/ADR-0007-user-directory-foundation.md) as the source for
today's directory boundary. Approved future changes will need an accompanying
update or successor decision.

## Target role catalog

| Role | Product intent | UX treatment and boundaries |
| --- | --- | --- |
| Author | Access to the content tree, basic Content Editor features, and Media Library; reduced editing controls. | Describe the accessible applications and actions in plain language. Content scope and allowed media operations need definition. |
| Developer | Author capabilities plus design, advanced content manipulation, and development tools. | Show inherited Author access separately from direct assignments. Exact template, system-item, and tooling permissions need definition. |
| Forms Editor | Access to Forms, Campaign Creator, and Marketing Control Panel; edit, rename, move, and delete forms. | Confirm application availability and create-form permission. General content access and inheritance are unresolved. |
| Marketing Automation Editors | Future access to automation campaigns and related marketing definitions. The supplied Author description indicates inherited Author access. | Mark as planned. Proposed treatment: explain the role but prevent assignment until its capabilities are supported. |
| Templar Admin | Full access. | Include account administration in the target experience. Define what full access means for protected system operations and future capabilities. |

Sitecore application and ribbon references describe capability intent. UX should
translate them into TemplarCMS navigation and actions; they do not require a
Sitecore-style ribbon or identical application layout.

The supplied description references underlying Sitecore Client roles and List
Manager Editors. Neither is confirmed as a separate TemplarCMS role. Do not add
them to the five-role catalog without a product decision.

## Design scope and release separation

1. **Directory experience:** improve the existing list, pending-user creation,
   profile, membership, language, and error/recovery flows.
2. **Operational account management:** design invitations, lifecycle actions,
   permission enforcement, and audit history as a separate future package.
3. **Account-holder experience:** map onboarding, sign-in, sign-out, and recovery
   after identity-provider ownership is decided.

UX may explore the complete journey, but should label every control and flow by
release scope and dependency. Engineering should be able to implement the
directory package independently. Bulk actions, custom-role creation, marketing
application design, and identity-provider selection are outside this brief.

## Screen requirements

| Screen or flow | Required information and behavior | Acceptance criteria for the design |
| --- | --- | --- |
| User directory | Name, email, role memberships, status, and useful activity metadata; search, filters, pagination, clear profile entry point, and add-user action. | Show loading, empty directory, no matches, unavailable feature, access failure, and retry states. Deactivated records remain discoverable. Long names and several roles remain readable. |
| Add pending user | First name, last name, email, preferred interface language, and role selection. Required fields, limits, and duplicate-email errors. | Current flow says “Add pending user.” Confirmation explicitly says no invitation was sent and no sign-in access was granted. No success message before the server confirms creation. |
| Profile details | Editable identity details; read-only status and timestamps; Save and Cancel. Clear distinction between editable and server-owned information. | Validation identifies the field and recovery action. Failed saves preserve the draft. Closing a changed form offers discard or continue editing. Successful save refreshes displayed data. |
| Role membership | Multiple selections, role descriptions, selected memberships, and a clear no-roles state. Proposed future display of direct and inherited access. | Users can understand what they assigned and what access that implies. Inherited access is not presented as an independently removable direct assignment. Current release explains that assignments are recorded only. |
| Interface language | Preferred UI language with understandable choices and fallback explanation. | Distinguish UI language from content language. Do not promise translations that are unavailable. Define behavior for an existing unsupported preference. |
| Conflict recovery | Explain that another operator changed the record; provide a path to review current data and recover edits. | Never silently overwrite newer data. Make the consequence of reload/retry explicit. UX specifies how users retain or recover their draft. |
| Invitation management — future | Invite, delivery/pending status, resend, expiry, cancellation, and acceptance outcome. | Distinguish record creation, delivery, and accepted invitation. Failure and expired-link flows offer a clear next action. Sender success reflects actual service results. |
| Account lifecycle — future | Separate actions for suspend, deactivate, and restore with clear effects and confirmation. | Explain effects on current sessions and future sign-in once policy is defined. Include forbidden transitions, self-lockout, final-admin protection, and failed actions. Restore must not imply automatic credential reactivation. |
| Sign-in/onboarding/recovery — future | Entry, invitation acceptance, sign-in, sign-out, expired session, and recovery journeys. | Identify which steps TemplarCMS owns and which the identity provider owns. Include return destinations and failed/cancelled provider flows. |
| Security history — future | Relevant actor, action, target, timestamp, and result. | Show profile, role, and lifecycle changes according to approved audit policy. Do not display a shared operator key as an individually identified actor. |

## Interaction and content requirements

- Provide keyboard access, visible focus, meaningful labels, accessible errors,
  dialog focus management, and focus return after dismissal.
- Ensure forms, tables, and confirmations remain usable on smaller screens and
  with zoom. Status must be understandable without color alone.
- Show saving progress and prevent duplicate submissions. Preserve drafts on
  network and validation failures.
- Distinguish unavailable capabilities from insufficient permission. Annotate
  whether restricted actions are hidden, disabled with an explanation, or
  blocked at entry; server enforcement remains an engineering requirement.
- Define status wording. `Invited` currently means a pending directory record,
  not proof that an email was sent.
- Distinguish “Never signed in” from “Sign-in activity is not available.” A null
  timestamp alone should not make a claim the system cannot support.
- Present dates consistently and specify timezone behavior where time matters.
- Avoid implying that changing a directory email also changes a verified login
  identity until identity integration defines that behavior.
- Use specific confirmations: who is affected, what changes, and the next step.
  Keep implementation details such as revision tokens out of user-facing copy.

## Decisions required from product and engineering

| Decision | Current understanding or proposed assumption | Design dependency |
| --- | --- | --- |
| Role inheritance | Developer inherits Author. Marketing Automation Editors appears to inherit Author. Forms Editor inheritance is unresolved. | Membership presentation and effective-access summary. |
| Author scope | Basic content and media access is intended; allowed actions and content branches are unspecified. | Role descriptions and permission explanations. |
| Developer scope | Advanced design/development access is intended; protected system operations are unspecified. | Privileged-role descriptions and confirmation needs. |
| Forms scope | Edit, rename, move, delete are supplied; create-form access and application availability need confirmation. | Role descriptions and release availability. |
| Administration | Templar Admin has full access; delegation, self-edit restrictions, and final-admin policy need definition. | Who can manage accounts and which changes can be blocked. |
| Authentication | Identity provider and password/MFA/recovery ownership remain undecided. | Sign-in, onboarding, credential, and recovery screens. |
| Lifecycle | Allowed transitions and session/credential effects remain undecided. | Confirmations, status explanations, restoration behavior. |
| Existing role mapping | Backend catalog has seven roles; target catalog has five. | Migration, compatibility, and display of existing assignments. |
| Future role visibility | Proposed: show Marketing Automation Editors as planned and unassignable. | Role selector and explanatory copy. |
| Self-service | Permitted personal profile changes are unspecified. | Account-holder settings and verified-email handling. |
| UI languages | Supported translations and fallback rules need confirmation. | Language selector and unsupported-preference state. |
| Audit policy | Actor identity, recorded events, visibility, and retention need definition. | Security history scope and detail. |

UX should flag these decisions in annotations and avoid resolving authorization
policy solely through visual design.

## Requested UX deliverables

- A flow map for directory management, with future invitation and account-holder
  journeys clearly separated.
- Annotated wireframes or designs for each in-scope screen, including responsive
  behavior, keyboard/focus behavior, and role-based availability.
- A state inventory covering loading, empty, validation, success, forbidden,
  service failure, concurrent edits, unsaved changes, and future lifecycle cases.
- Proposed labels, helper text, errors, confirmations, and status descriptions.
- A role membership interaction showing multiple direct roles and proposed
  inherited access without duplicating or confusing assignments.
- An open-decision log with affected flows and the product/engineering answer
  required before implementation.
- A ticket-ready breakdown. Each proposed ticket should identify the user job,
  screens/states, release scope, dependencies, acceptance criteria, and design
  references.

## Review scenarios

Use these scenarios to review the designs with product and engineering:

1. Find a user by email, filter by role/status, and open the correct profile.
2. Create a pending user and understand that no invitation or login was enabled.
3. Submit a duplicate email, correct it, and retain the other entered values.
4. Assign multiple roles and explain direct membership versus inherited access.
5. Remove all direct roles and understand the resulting membership state.
6. Edit a profile while another administrator saves a change; recover without
   silently losing either operator's work.
7. Close a changed profile and choose whether to discard or continue editing.
8. Recover from a failed save without re-entering the draft.
9. Complete the directory tasks using only a keyboard and at increased zoom.
10. In the future package, resend an expired invitation, suspend a user, attempt
    to remove the final administrator, and restore an account with explicit
    credential/session consequences.

## Engineering references

- [User directory decision](decisions/ADR-0007-user-directory-foundation.md)
- [Runtime and deployment notes](../README.md#user-directory-preview)
- [Implemented directory component](../src/TemplarCMS.Admin/templarcms.admin.client/src/components/security/UserDirectory.vue)

Completion of the UX handoff means the directory flows are specified well enough
to estimate and implement, while future services and unsettled policies have
named dependencies and explicit decisions. It does not mean identity or access
control is already implemented.
