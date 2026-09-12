# User directory foundation

Status: Accepted for the directory slice; identity integration remains deferred.

## Scope

The System workspace presents the user directory using the UX reference at
TemplarCMS-VUE commit c3a571f. The directory stores profiles and the seven fixed
role identifiers. It does not authenticate these users or use their assignments
to authorize authoring. No example users are seeded into production data.

Users have stable GUIDs, a normalized unique email, preferred interface language,
created/last-login timestamps, status, roles, and a revision token. Name, email,
language, and membership edits require the original revision; conflicting edits
return 409. Email normalization trims surrounding whitespace and compares
invariant uppercase values. Deactivated records remain visible by default.
Empty role membership is valid and represents no assigned roles.

## API boundary

`UserDirectory:Enabled` defaults to false. All `/api/v1/security` reads and writes
require the `ManageUserDirectory` policy: an authenticated configured API-key
operator with the existing AuthorContent claim. Disabling AuthoringSecurity
does not grant anonymous directory access. This shared operator credential is
an internal transitional mechanism; it is not a Security Administrator identity.
The directory must remain on a controlled internal deployment until identity,
delegation, and audit requirements are implemented.

- GET `/api/v1/security/roles` returns the fixed role catalog.
- GET `/api/v1/security/users` supports search, role, status, offset, and limit.
  Limit defaults to 50, maximum 100; the UI uses pages of 25. This initial small
  directory filters in memory after loading records; larger deployments need
  database-side filtering and counting.
- GET `/api/v1/security/users/{id}` returns a profile and its revision.
- POST `/api/v1/security/users` creates a pending directory record with status
  Invited. It sends no email and grants no sign-in access.
- PUT `/api/v1/security/users/{id}` replaces profile details and roles, requiring
  revision. Unknown JSON properties, including status and credentials, fail 400.

Profile writes are limited to firstName, lastName, email, language, roles, and
revision. Status and timestamps are server-owned. There are no lifecycle, delete,
password, credential, or invitation-delivery endpoints. API errors use
ProblemDetails; record responses include self and collection links.

## Storage and rollout

Domain types and the repository contract are independent of EF. EF maps directory
rows in the existing runtime database. Fixed role keys are stored as a JSON array
of names, not numeric enum values. These are assignments to a source-controlled
catalog, not custom role definitions or effective permissions.

SQLite bootstrap adds DirectoryUsers and its unique email index to existing
databases. SQL Server operators apply `database/sqlserver/002-user-directory.sql`
after the initial schema and before enabling the feature. Fresh EF-created
databases include the table. No existing content or media records are rewritten.

## Follow-up decisions

Identity provider, invitation completion/delivery, credential issuance and
revocation, account transition rules, administrator delegation and final-admin
protection, scoped authorization, custom roles, and durable security audit events
remain future slices. Before assignments affect live permissions, those safeguards
must be in place. Retained credentials must never reactivate automatically.

The UI stores the transitional operator key only in component memory and clears
it on disconnect or workspace exit. It does not store the secret in browser
storage or bundles. Profile status is read-only until lifecycle dialogs arrive.
