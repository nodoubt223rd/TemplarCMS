# ADR-0015: Initial administrator identity and separate site delivery credentials

Status: Accepted for the admin identity implementation. Delivery API keys are approved as a separate follow-up slice.

## Context

The directory foundation stored profiles and role memberships, while a shared
operator API key authenticated authoring. It provided no user login and defaulted
to disabled authoring security. New installations need a real administrator,
security enabled by default, and a password the operator chooses at first login.

## Decision

Use ASP.NET Core Identity for credentials, password hashing, lockout, stamps, and
cookie authentication. Identity rows remain separate from DirectoryUsers and
share the profile's GUID. Directory roles remain the single catalog; no duplicate
Identity role catalog or public registration endpoints are introduced.

`setup-admin --email <operator-email>` initializes the configured runtime and
atomically creates one Active profile with TemplarAdmin, its hashed temporary
credential, and a singleton installation marker. The login name is TemplarAdmin.
The cryptographically generated temporary password has 128 bits of entropy and
is displayed once after commit. Repeated setup and ordinary startup cannot reset,
reveal, reactivate, or recreate the administrator. Missing/inconsistent marker
targets fail closed. Setup never silently adopts an invited profile.

Temporary-password sessions can inspect their session, change the password, and
log out. Every protected account request validates the current profile status,
roles, password-change flag, and security stamp. Authoring requires an Active,
unrestricted TemplarAdmin identity. The API, rather than a Vue guard, enforces
the mandatory change. Password changes are atomic, require the current password,
reject reuse, rotate the stamp, and replace the cookie. Older sessions stop
working on their next request. Local recovery via `reset-admin-password` issues
a replacement temporary password, clears lockout, and revokes old sessions; it
does not create or reactivate an account.

Passwords permit spaces and Unicode and must be 15–128 characters. Five failed
attempts lock the account for 15 minutes; login also has an IP rate limit of ten
requests per minute. Sessions are nonpersistent, with a 30-minute absolute
lifetime and no sliding renewal. Cookies use Secure, HttpOnly, SameSite=Strict,
Path=/, and the __Host- prefix. API cookie mutations require antiforgery tokens;
authentication failures are JSON ProblemDetails, never HTML login redirects.

The SPA and `/api` run behind one HTTPS origin. The pooled Admin proxy never
stores browser cookies or automatically follows API redirects. Each browser's
Cookie/Set-Cookie values and antiforgery headers pass through independently.
Data Protection keys persist under the configurable
`IdentitySecurity:DataProtectionKeysPath`, defaulting to ignored
`RuntimeData/IdentityKeys`. Deployments must restrict access to that directory and
use durable storage; shared instances need the same key ring/application name.

Security and the directory default to enabled. An explicit
`AuthoringSecurity:Enabled=false` prevents startup. A secure fallback policy covers
authoring and content API routes; session operations have their own restricted
policy, and health, content-free discovery, token/login entrypoints, and rendered
sample HTML pages have explicit anonymous allowances. Missing accounts or
credentials never enable anonymous authoring. Disabling the directory does not
disable authentication.

Directory profile creation still produces Invited records with no credentials,
email, or access. The initial administrator cannot lose TemplarAdmin membership
through profile edits. Identity email updates share the profile write transaction,
with the stable login name preserved. Last-login updates use UTC and preserve
profile revision concurrency semantics. Other catalog roles grant no permissions
in this slice.

## Delivery boundary

The transitional operator key no longer grants authoring access. Client-facing
sites will use separate read-only delivery API keys under the approved
[delivery plan](../superpowers/plans/2026-10-10-delivery-api-keys.md). That scheme is
not implemented in the admin-first slice: shared content reads currently require
an unrestricted administrator cookie. Implement the delivery scheme before
enabling site consumers against this new authentication contract. Site keys must
remain on the consuming server, never in public browser JavaScript or storage.

## Consequences

SQLite startup adds credential and provisioning tables without rewriting existing
content or profiles. SQL Server operators apply `007-admin-identity.sql` after
the approved baseline and directory scripts; startup verifies rather than migrates
SQL Server schema. None of these schema steps provision credentials.

The Vue workspace loads private data only after unrestricted login. Expired
sessions hide the workspace while preserving in-memory drafts for the same user.
Explicit logout and account changes clear workspace state, and generation checks
discard late request results. Secrets/tokens are never stored in localStorage or
sessionStorage. The System directory reuses the shared session rather than asking
for an operator key.

This supersedes the operator-authentication, disabled-default, and credentials-
deferred boundaries of ADR-0007/ADR-0009. It does not introduce email invitations,
public registration, additional login-capable users, delegated lifecycle controls,
MFA, external identity, full role enforcement, or a durable audit subsystem.
