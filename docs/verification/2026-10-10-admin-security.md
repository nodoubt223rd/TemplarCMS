# Admin identity verification

Verified on 2026-10-12 on `pr/admin-identity`, starting from
`8f923557418c3b396d2d708aa0ac5f225239969e`.

## Scope

Admin identity Tasks 1–6 and the identity documentation/verification portions
of Task 7 are implemented. Delivery credentials, key-only content reads, and
the mixed cookie/key matrix belong to the separate delivery implementation.
Shared content API reads require an unrestricted admin cookie in this slice.

## Automated evidence

- Solution build: passed, zero warnings/errors, using the repository bootstrap,
  `dotnet build TemplarCMS.sln --no-restore -m:1 -p:UseSharedCompilation=false`.
- Solution tests: 523 passed, zero failed, three SQL Server integration tests
  skipped without a configured rehearsal database. Breakdown: Integration 21,
  ContentModeling 234, Application 60, API 208. Domain.Tests reports no tests.
- Vue: 159 tests passed across 32 files; strict TypeScript checks and production
  build passed. Vite reports its existing bundle-size warning.
- API coverage includes locked startup, provisioning uniqueness/idempotency,
  command output failure, recovery, cookie/CSRF attributes, mandatory password
  change, stale security stamps, login/reset race, lockout/rate limits,
  absolute expiry, protected reads/writes/directory operations, legacy-key
  rejection, current-profile validation, and bootstrap-role protection.
- Two actual HTTPS loopback proxy tests verify distinct browser cookies and
  CSRF headers, anonymous isolation, and redirect preservation.
- Frontend coverage includes gated startup, forced change, logout/expiry,
  JSON/multipart CSRF, absence of browser secret storage, and draft retention.

## Independent review and corrections

A fresh independent reviewer assessed the whole identity branch. No concrete
server bypass was identified. Two important findings were reproduced with
failing tests before corrections:

1. A successful template PUT followed by a pending refresh could repopulate
   private state after logout. Complete workflows now validate their session
   generation, and stale catches/finally blocks cannot affect the new session.
   Tree-action caches are also cleared on logout/account changes.
2. Expiry destroyed tree dialog input and prevented profile draft resumption.
   Dialogs now suspend while signed out and resume for the same account.
   Explicit logout or a different account clears the drafts. Actual tree and
   directory components have regression coverage.

Both findings went RED → GREEN; the full Vue suite passed after the changes.
One minor finding remains deferred: an expired restricted session that submits
the password-change form shows an error and requires Sign out or reload before
logging in again. Its protected API returns 401; it grants no authoring access.

## Isolated runtime smoke and limitations

A disposable SQLite installation under ignored `.tmp/identity-smoke` was used,
with separate templates and Data Protection paths. The configured SQL Server
database was not touched.

- The actual `setup-admin --email smoke@example.test` command exited 0 and
  issued a temporary password. The secret was omitted from this evidence.
- Repeated setup exited 0, reported completion, and emitted no password.
- Actual `reset-admin-password` exited 0 and issued a different password.
- API and Vite started on isolated HTTPS localhost ports; temporary servers
  were stopped after the check.
- Interactive browser verification could not proceed because the in-app
  browser rejected the local certificate with `ERR_CERT_AUTHORITY_INVALID`.
  No browser security warning was bypassed. Keyboard-only login/change,
  visual validation, and deployed IIS/restart checks remain manual evidence
  gaps; automated endpoint, component, and HTTPS proxy tests passed.
- Live SQL Server schema/application validation was unavailable and is not
  claimed. Apply `007-admin-identity.sql` to a rehearsal database before an
  operator deployment.

No deployment, remote push, merge, issue update, or delivery-key implementation
was performed.
