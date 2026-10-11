# Initial administrator and secure first login

Status: Proposed for review. Product implementation has not started.

## Intent and agreed requirements

A new installation must protect the authoring workspace by default and provide
one initial account with the `TemplarAdmin` role. The user approved a unique,
random temporary password for each installation and a dedicated setup command
that provisions the account and displays that password once. First login must
require a password change before CMS access is granted.

The account is provisioned automatically by the setup command, rather than by
every application startup. This reconciles initial provisioning with deliberate,
one-time delivery of the secret. Application startup remains locked before setup.

The remaining details below are proposals for review, particularly local identity,
the retirement of the transitional operator key, and administrator recovery.

## Existing boundaries

- The API currently authenticates a shared operator API key. Authoring security
  and the directory default to disabled.
- `DirectoryUser` represents profiles, status, and memberships. It contains no
  credentials, and memberships currently grant no permissions.
- Directory profile creation makes an `Invited` record; it does not create a
  login or send an invitation.
- SQLite schema additions are applied by startup bootstrap. SQL Server schema
  additions use reviewed provisioning scripts and startup schema verification.
- The Admin client is a Vue/TypeScript SPA, with `/api` proxied during development.

See [ADR-0007](../../decisions/ADR-0007-user-directory-foundation.md),
[ADR-0009](../../adr/ADR-0009-target-role-catalog-contract-and-boundary-validation.md),
and [README](../../../README.md). This feature supersedes their directory-only
authentication and initial-account limitations when implemented.

## Approach and alternatives

Use ASP.NET Core Identity services and EF storage for local credentials, password
hashing, lockout, and security stamps, with cookie authentication and custom JSON
endpoints. Keep credential records separate from directory profile responses and
link each identity to one directory user. Reuse the existing role catalog rather
than introducing a second Identity role catalog. Do not scaffold Razor UI or
expose framework registration endpoints.

An external identity provider would introduce installation dependencies and would
not directly satisfy the requested local temporary-password flow. A hand-built
credential system would require recreating hashing, lockout, and revocation
behavior. Framework Identity services are the recommended middle ground.

Framework references:
[Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-8.0)
and [Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-8.0).

## Installation and provisioning

The proposed command is `setup-admin --email <operator-email>`, implemented as an
API executable command mode that exits without starting the HTTP server. It uses
the same environment, database connection, schema checks, and runtime paths as
the API. The login name is `TemplarAdmin`; the supplied email identifies the
directory profile, without implying email verification or email delivery.

Generate the temporary password using a cryptographically secure random source,
with at least 128 bits of entropy. Persist only its salted framework hash.
Create an Active directory profile, its credential record with
`MustChangePassword = true`, and a durable installation provisioning marker in
one transaction. The marker has a singleton database key so concurrent command
invocations cannot create multiple bootstrap accounts.

Display the login name and temporary password directly to the operator terminal
only after commit succeeds. Do not include the secret in application logs,
configuration, a generated file, or an HTTP response. If setup has already been
completed, report that fact without changing any account or returning a password.
An existing directory email collision fails clearly; do not silently adopt or
activate an invited profile. A marker/account mismatch fails closed and directs
the operator to recovery; it never triggers reseeding.

Regular startup may initialize schema and starter content but never provisions
credentials, generates a password, or reactivates an administrator. Restoring an
installation database preserves its provisioning marker and account state.

## Authentication and mandatory password change

Add JSON endpoints under `/api/v1/auth` for login, session inspection, password
change, logout, and obtaining an antiforgery request token. Successful temporary
password login establishes a restricted session and returns
`mustChangePassword: true`. The SPA then displays the change-password screen.

Every protected API request checks current account existence, Active directory
status, security stamp, password-change state, and current role membership.
Enforcement must not depend on a stale cookie role or a Vue route guard.
Restricted sessions may inspect their session, obtain an antiforgery token,
change their password, and log out; they cannot read directory information or
use authoring endpoints. Denials return `403` ProblemDetails with a stable
`password-change-required` code.

Password change requires the current password, validates the new password,
rejects reuse of the temporary password, and atomically updates the hash and
mandatory-change state. Rotate the security stamp and replace the session so
older sessions stop working. Database failure leaves the account restricted.
Use a minimum of 15 and a maximum of 128 characters, allow spaces and Unicode,
and avoid arbitrary character-composition rules or periodic expiry.

Use framework lockout with five failed attempts and a 15-minute lockout, plus
login rate limiting. Login errors must not reveal whether an account exists.
Initial temporary credentials do not expire automatically in this slice; access
remains restricted until changed, and local recovery can replace a lost secret.

## Sessions, client, and hosting

Use Secure, HttpOnly, SameSite cookies over HTTPS; require antiforgery validation
for login and all cookie-authenticated mutations, including password change and
logout. Return `401`/`403` JSON ProblemDetails instead of HTML redirects. Session
inspection is `no-store`; authentication responses and secrets are never cached.
Use nonpersistent sessions with a 30-minute absolute lifetime in this slice.

The supported browser deployment places the SPA and `/api` behind one origin,
including the Vite development proxy and the documented IIS setup. Update proxy
cookie forwarding and hosting documentation as needed. The Admin server's pooled
HTTP proxy must disable its handler's cookie container (`UseCookies = false`)
and forward each browser's Cookie/Set-Cookie headers independently, so operator
sessions are never shared between proxy requests. Do not introduce permissive
credentialed CORS. Configure durable Data Protection keys outside source control
for application restarts and shared keys for multiple instances.

`App.vue` owns session state through Composition API, with a typed reusable auth
composable and custom login/change-password components. Load authoring data only
after unrestricted authentication succeeds. Session expiry returns the client to
login; an expired session during editing preserves drafts in memory while the
user signs in again. Explicit logout clears account-specific state. Password
fields are cleared after submission or dismissal, with no password or session
token in localStorage/sessionStorage or bundles. Antiforgery request tokens remain
in memory. No Pinia, Vuex, Razor views, or new general-purpose UI library.

## Authorization and directory integration

Enable security and the System directory by default. An incomplete installation
denies authoring access; absence of credentials must never enable anonymous
access. Disabling directory UI must not disable authentication.

After the mandatory password change, an Active account with `TemplarAdmin` may
use the existing authoring and directory-management policies. Other catalog roles
gain no permissions in this slice. Public content delivery and health remain
anonymous. Inventory endpoints during implementation and explicitly protect
workspace trees, editing metadata, template design, media management, and all
authoring mutations, while preserving the documented delivery read contract.

Remove the transitional operator API key as an authoring authentication path for
this flow; it must not bypass setup or mandatory password change. This is a
documented breaking change for existing operator tooling. Machine authentication
is separate follow-up work. Tests provision identities explicitly rather than
relying on disabled security or shipped fixture credentials.

Pending profiles remain unable to log in: assigning a role does not issue
credentials or activate them. Protect the bootstrap account from removal of its
`TemplarAdmin` membership through existing profile updates. Its login name remains
stable when its directory email changes; keep associated identity email data
consistent transactionally. Bootstrap status changes and deletion remain
unavailable. Broader delegated administration and final-admin rules are deferred.

## Recovery, storage, and failures

Provide an explicit local `reset-admin-password` command for the marked bootstrap
account when one-time output is lost or the administrator is locked out. It
generates and displays a new temporary password once, clears lockout, requires
password change again, and revokes previous sessions transactionally. It must
never create a second account or reactivate an inactive/missing account. Local
command access requires the same trusted deployment/database permissions as setup;
there is no anonymous browser reset endpoint. No secrets are command arguments.

Add credential and provisioning tables to SQLite bootstrap and a versioned SQL
Server provisioning script. Preserve existing directory/content rows. Fresh and
existing database initialization must use equivalent constraints. Credential
records and directory responses remain separate; timestamps are application-owned
UTC values. Directory revisions continue to protect profile edits.

Validation and expected conflicts return typed results and ProblemDetails, with
HATEOAS links and OpenAPI security requirements aligned. Missing schema causes a
clear startup/setup failure. Emit security event logs for setup, reset, login
outcome, password change, and logout without passwords, hashes, cookies, or tokens.
Durable audit services remain follow-up work.

## Verification and documentation

- Fresh SQLite setup creates one admin and displays a unique password; a second
  installation receives a different password. Repeat/concurrent setup and restarts
  neither duplicate the account nor change its credentials.
- Existing SQLite additions preserve data. SQL Server scripts and the equivalent
  setup path are verified when a test server is available, with limitations recorded.
- Anonymous authoring is denied before and after setup. Temporary-password sessions
  cannot call protected reads or writes directly, including using the old operator key.
- Password change enables admin access, invalidates the temporary password and old
  sessions, persists across restart, and remains restricted on failed/conflicting saves.
- Invited, suspended, deactivated, missing, and non-admin profiles cannot obtain admin
  access. Profile edits cannot remove the bootstrap admin membership.
- Lockout, rate limits, cookie attributes, antiforgery, logout, expiry, recovery,
  and secret-free output/logging receive focused integration checks.
- Vue tests cover login, forced change, failed changes, expiry with drafts, and logout.
  Keyboard/focus checks and an HTTPS browser smoke test cover the complete flow.
- Run backend build and relevant domain/application/persistence/API tests, frontend
  type checking/build, and targeted component tests using README helpers.
- Update README, current-state summary, and an identity ADR during implementation.
  Treat this as a separate identity feature from TEM-16's directory verification;
  retain the prohibitions on demo users and operator secrets in browser storage.

## Scope exclusions

No public registration, email invitations, self-service email reset, external
identity providers, MFA, permission inheritance, general account lifecycle UI,
additional login-capable users, machine credentials, or durable audit subsystem.
These exclusions do not relax bootstrap protection or first-login enforcement.
