# Initial Admin Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect authoring by default with one setup-provisioned TemplarAdmin, a unique temporary password, and an API-enforced first password change.

**Architecture:** ASP.NET Core Identity owns local credentials and cookie sessions; the existing directory owns profiles and catalog roles. Credentials and a singleton provisioning marker share the runtime database, with provisioning/password transitions committed atomically. The Vue SPA uses session state in App.vue and an authenticated same-origin proxy.

**Tech Stack:** .NET 8, EF Core 8.0.22, ASP.NET Core Identity EF 8.0.22, SQLite/SQL Server, Vue 3, strict TypeScript, xUnit v3, Vitest.

**Spec:** [Approved design](../specs/2026-10-10-initial-admin-design.md).

## Global Constraints

- SDK remains 8.0.400; no target-framework upgrade. Align Identity EF with existing EF 8.0.22.
- Login name is `TemplarAdmin`. Setup supplies the directory email; it does not send or verify email.
- Temporary passwords have at least 128 bits of cryptographic entropy and appear once after commit; store only the framework hash.
- Passwords are 15–128 characters, permit spaces/Unicode, and have no composition rules or periodic expiry.
- Lockout is five failures for 15 minutes; cookies are Secure, HttpOnly, SameSite, nonpersistent, with a 30-minute absolute lifetime.
- Every account-authenticated request revalidates account/status/stamp/current roles; restricted sessions cannot access CMS data.
- No automatic credential creation, reseeding, password reset, or account reactivation on startup.
- Identity passwords, cookies, hashes, and tokens never enter configuration, browser storage, bundles, or logs.
- Keep the pure Vue SPA, `<script setup lang="ts">`, explicit emits, scoped styling, and App.vue-owned state; no `any`, Pinia/Vuex, Razor UI, or component library.
- Keep stored content field values and runtime content modeling unchanged.
- Existing SQL Server deployments use reviewed scripts; application startup does not execute SQL Server DDL.
- No registration, invitation delivery, additional login-capable users, lifecycle UI, or expanded role permissions.

## Review Focus

- Setup process fails after commit but before showing the password: rerun cannot reveal/reset it; explicit recovery can (Task 2).
- Password reset races with login/password change: an old credential must not acquire a cookie carrying a newer valid security stamp (Tasks 2–3).
- A delayed response arrives after logout or session expiry: no private data repopulates cleared state and no mutation is automatically retried (Task 6).
- Multipart uploads and pooled proxy requests: antiforgery cannot be skipped and browser sessions cannot contaminate each other (Tasks 4–5).
- Legacy SQLite and incomplete SQL Server schemas: preserve existing rows and fail clearly without creating a partial account (Tasks 1–2).

## File structure and execution order

New neutral account contracts live under Domain/Security and Abstractions/Security.
Identity rows and provider schema live under Persistence/Security. Identity/HTTP
orchestration and command handling live under Api/Security and Api/Commands; no
framework types cross domain/application contracts. Frontend auth types, transport,
composable, and UI have separate files; App.vue owns the state they consume.

Execute Tasks 1–6 in order, then the [delivery-key plan](2026-10-10-delivery-api-keys.md),
then Task 7. Each task is a review/commit boundary. Do not publish a partially
integrated identity branch. Create an isolated worktree at execution time; do not
alter the approved design during implementation without flagging a material change.

For .NET red/green checks, first run `. .\scripts\templar-cms-bootstrap.ps1` and restore
the affected test project using `nuget.config`. Use `dotnet test <project> --no-restore
--filter FullyQualifiedName~<test-class>` for the named class below. The repo helper
is used for final full-project verification because it has no filter parameter.

## Shared contracts

Task 1 defines these neutral types in `src/TemplarCMS.Domain/Security/AdminIdentityContracts.cs`:

```csharp
public sealed record AdminAccountSnapshot(Guid UserId, string LoginName, string Email,
    string Language, DirectoryUserStatus Status, IReadOnlyList<DirectoryRole> Roles,
    bool MustChangePassword);
public enum AdminProvisioningStatus { Created, AlreadyInitialized, DuplicateEmail,
    InvalidEmail, MissingAccount, InactiveAccount, Conflict }
public sealed record AdminProvisioningResult(AdminProvisioningStatus Status,
    Guid? UserId = null, string? TemporaryPassword = null);
public enum AdminSignInStatus { SignedIn, InvalidCredentials, LockedOut }
public sealed record AdminSignInResult(AdminSignInStatus Status,
    AdminAccountSnapshot? Account = null, string? VerifiedSecurityStamp = null);
public enum AdminPasswordChangeStatus { Changed, InvalidPassword, ValidationFailed,
    AccountUnavailable, Conflict }
public sealed record AdminPasswordChangeResult(AdminPasswordChangeStatus Status,
    AdminAccountSnapshot? Account = null, IReadOnlyList<string>? Errors = null);
```

`src/TemplarCMS.Abstractions/Security/IAdminProvisioningService.cs` declares:

```csharp
Task<AdminProvisioningResult> SetupAsync(string email, CancellationToken ct);
Task<AdminProvisioningResult> ResetAsync(CancellationToken ct);
```

`src/TemplarCMS.Abstractions/Security/IAdminIdentityService.cs` declares:

```csharp
Task<AdminSignInResult> CheckPasswordAsync(string loginName, string password, CancellationToken ct);
Task<AdminAccountSnapshot?> GetAccountAsync(Guid userId, CancellationToken ct);
Task<AdminPasswordChangeResult> ChangePasswordAsync(Guid userId, string currentPassword,
    string newPassword, CancellationToken ct);
```

Provisioning results are command-only: never serialize TemporaryPassword to HTTP.
VerifiedSecurityStamp is an internal login-race check, never a session-response field.

### Task 1: Persist identity and initialize equivalent provider schemas

**Files:** Create the three shared-contract files above; `src/TemplarCMS.Persistence/Security/CmsIdentityUser.cs`, `AdminProvisioningRow.cs`, `IdentitySchemaInitializer.cs`; `src/TemplarCMS.Api/Bootstrap/CmsRuntimeInitializer.cs`; `database/sqlserver/007-admin-identity.sql`; `tests/TemplarCMS.Integration.Tests/Persistence/AdminIdentitySchemaTests.cs`. Modify `src/TemplarCMS.Persistence/TemplarCmsDbContext.cs`, `TemplarCMS.Persistence.csproj`, `SqlServerSchemaVerifier.cs`; `src/TemplarCMS.Api/Bootstrap/DefaultContentBootstrapHostedService.cs`, `DefaultContentBootstrapServiceCollectionExtensions.cs`; `tests/TemplarCMS.Integration.Tests/Persistence/SqlPolicyTests.cs`, `SqlServerModelCompatibilityTests.cs`.

**Interfaces:** `CmsIdentityUser : IdentityUser<Guid>` adds `bool MustChangePassword`; its Id is the linked DirectoryUsers Id. `AdminProvisioningRow` has `int Id = 1`, `Guid UserId`, and application-owned UTC `DateTimeOffset CreatedAt`. `IdentitySchemaInitializer.EnsureAsync(TemplarCmsDbContext db, CancellationToken ct)` applies SQLite additions only. `CmsRuntimeInitializer.InitializeAsync(CancellationToken ct)` preserves the current schema/template/content initialization flow and is reused by startup and commands.

- [ ] Write `AdminIdentitySchemaTests`: `FreshSchemaStoresIdentityAndOneMarker`, `LegacySqlitePreservesDirectoryAndContent`, `MarkerCannotDuplicateOrReferenceMissingUser`, and `SqlServerModelRequiresIdentitySchema`. Assert a unique singleton marker, restricted profile deletion, credential/profile separation, unchanged preexisting rows, and missing schema errors naming `007-admin-identity.sql`.
  Pin the fresh-schema assertions in code:
  ```csharp
  Assert.Single(await db.Set<AdminProvisioningRow>().ToListAsync(ct));
  Assert.Equal(directoryUser.Id, identityUser.Id);
  Assert.True(identityUser.MustChangePassword);
  ```
- [ ] Run `dotnet test .\tests\TemplarCMS.Integration.Tests\TemplarCMS.Integration.Tests.csproj --no-restore --filter FullyQualifiedName~AdminIdentitySchemaTests`; expect new assertions to fail before schema support exists.
- [ ] Add Identity EF 8.0.22 and derive the runtime context from `IdentityUserContext<CmsIdentityUser, Guid>`; call its base model configuration. Map identity user/claim/login/token tables with `CmsIdentity` prefixes, account Id FK to DirectoryUsers with Restrict delete, and the marker with a database check `Id = 1`. Explicitly map SQL types/lengths compatible with the verifier, including email max 254. Extract current runtime initialization into CmsRuntimeInitializer without seeding credentials. Add idempotent additive SQLite DDL, the reviewed SQL Server script/version marker, and verifier checks for its schema version. Ensure fresh and upgraded provider mappings match; do not rely on EnsureCreated to upgrade existing SQLite tables.
- [ ] Rerun the named class and existing bootstrap/schema tests; expect all pass. A live SQL Server schema/setup rehearsal is conditional on an explicitly configured test database, with unavailable provider checks recorded.
- [ ] Commit only the task's files with message `feat: persist local admin identity and provisioning state`.

### Task 2: Provision and recover one administrator through local commands

**Files:** Create `src/TemplarCMS.Api/Security/AdminProvisioningService.cs`, `TemporaryPasswordGenerator.cs`; `src/TemplarCMS.Api/Commands/AdminCommandRunner.cs`; `tests/TemplarCMS.Api.Tests/Security/AdminProvisioningTests.cs`, `Commands/AdminCommandTests.cs`. Modify `src/TemplarCMS.Api/Program.cs`, `Security/AuthoringSecurityServiceCollectionExtensions.cs`; reuse Task 1 runtime initialization.

**Interfaces:** `AdminProvisioningService : IAdminProvisioningService` implements SetupAsync/ResetAsync using scoped UserManager and the same context. `TemporaryPasswordGenerator.Generate()` returns base64url encoding of 16 cryptographically random bytes. `AdminCommandRunner.RunAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)` returns `Task<int>`; Program dispatches recognized command mode before starting the HTTP host. Invalid arguments return nonzero and never fall through into server mode. Normal HTTP configuration arguments remain supported. Commands resolve the provisioning interface; they do not depend on the later login service.

- [ ] Write `AdminProvisioningTests`: two separate installations return different working passwords and hashes; same/concurrent installation creates one Active profile, credential, and marker; rerun returns no secret; normalized email collisions and broken/inactive marker targets fail unchanged; missing schema/commit failure yields no account or secret. Reset preserves account ID/email/role, clears lockout, sets MustChangePassword, and changes stamp/hash. Write `AdminCommandTests`: valid setup writes a secret once after commit and exits without HTTP; invalid args/commit failure print no secret; simulated output failure after commit leaves setup initialized and recoverable by explicit reset. Use disposable SQLite databases and capturing writers/loggers only in tests.
  Pin repeat-provisioning assertions in code:
  ```csharp
  Assert.Equal(AdminProvisioningStatus.Created, first.Status);
  Assert.Equal(AdminProvisioningStatus.AlreadyInitialized, repeated.Status);
  Assert.Null(repeated.TemporaryPassword);
  Assert.NotEqual(first.TemporaryPassword, otherInstallation.TemporaryPassword);
  ```
- [ ] Run both test classes in `TemplarCMS.Api.Tests`; expect setup/reset behavior and command dispatch assertions to fail.
- [ ] Implement transaction-backed setup/reset with framework password hashing. Use the singleton constraint as the concurrency boundary and bounded SQLite busy retries; clear tracked state after rejected transactions. Setup creates first/last names `Templar`/`Administrator`, language `en`, role TemplarAdmin, and application-owned UTC timestamps. Normalize/validate supplied email using the existing profile rules. Register IdentityCore/UserManager/SignInManager/EF stores without framework registration or Razor UI. Program handles `setup-admin --email <email>` and `reset-admin-password`, initializes runtime once, writes success output only after commit, and does not reveal secrets through ILogger or exceptions. Capture and return typed expected failures instead of throwing validation exceptions.
- [ ] Rerun the tests; expect pass, including simultaneous setup on independent database contexts and simulated transaction/output failures. Verify normal startup leaves an empty identity store untouched.
- [ ] Commit with message `feat: add one-time admin setup and explicit local recovery`.

### Task 3: Login, restricted sessions, atomic password change, and revocation

**Files:** Create `src/TemplarCMS.Api/Security/AdminIdentityService.cs`, `AccountSessionValidator.cs`, `AuthEndpoints.cs`, `AuthContracts.cs`; `tests/TemplarCMS.Api.Tests/Security/AdminAuthenticationTests.cs`; `tests/TemplarCMS.Api.Tests/Support/AdminIdentityApiFactory.cs`. Modify security registration, Program.cs, `Security/ApiAuthorizationPolicies.cs`, `AuthoringAccessAuthorizationHandler.cs`, `AuthoringSecurityOperationFilter.cs`, and `src/TemplarCMS.Api/ApiProblems.cs` to extend the existing shared catalog.

**Interfaces:** `AdminIdentityService : IAdminIdentityService` implements Task 1's identity methods. `AccountSessionValidator.ValidateAsync(CookieValidatePrincipalContext context)` rejects missing/inactive/stamp-invalid accounts and regenerates current role/change-state claims. `ApiAuthorizationPolicies.AccountSession = "AccountSession"` allows current Active account sessions, including restricted ones, for session/change/logout. AuthorContent and ManageUserDirectory now require unrestricted Active TemplarAdmin. `AuthEndpoints.MapAuthEndpoints(this IEndpointRouteBuilder endpoints)` maps GET `/api/v1/auth/antiforgery`, GET `/api/v1/auth/session`, POST `/api/v1/auth/login`, POST `/api/v1/auth/change-password`, POST `/api/v1/auth/logout`. Session DTO: `{ userId, loginName, email, language, roles, mustChangePassword, _links }`, with roles serialized as catalog string keys; request DTOs are `{ loginName, password }` and `{ currentPassword, newPassword }`, rejecting unmapped JSON members. The antiforgery DTO is `{ requestToken, headerName }`, with header `X-Templar-CSRF`. Use stable ProblemDetails codes including `password-change-required`, `invalid-credentials`, `antiforgery-failed`, and `password-change-conflict`.

- [ ] Write `AdminAuthenticationTests`: temporary login returns a restricted session; 14/129-character and unchanged passwords fail, 15/128-character passwords and spaces/Unicode work; change clears restriction and old password/cookies fail immediately; injected save/concurrency failure remains restricted; reset racing with credential verification never stamps an old login with the reset account's newer stamp. Assert five failures lock for 15 minutes, unknown/wrong/inactive login outcomes do not enumerate users, expired cookies fail at 30 minutes, and logout does not invalidate unrelated sessions. Test session/log serialization contains no secrets or hashes.
  Pin restricted-login and changed-password assertions in code:
  ```csharp
  Assert.True(loginJson.GetProperty("mustChangePassword").GetBoolean());
  Assert.Equal(HttpStatusCode.Forbidden, restrictedAuthoringWrite.StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized, oldSessionRead.StatusCode);
  ```
- [ ] Run `dotnet test .\tests\TemplarCMS.Api.Tests\TemplarCMS.Api.Tests.csproj --no-restore --filter FullyQualifiedName~AdminAuthenticationTests`; expect the new auth routes/transitions to fail before implementation.
- [ ] Implement credential checks via framework lockout and UserManager, same-password rejection, and atomic hash/change-state/stamp updates; map concurrency failures to 409 and validation to 400. Sign in only if the current security stamp still equals the stamp captured during password verification; never reread a newer stamp to mint a valid cookie for an old password. Use Identity.Application cookies, SameSite=Strict, SecurePolicy=Always, HttpOnly, IsPersistent=false, ExpireTimeSpan=30 minutes, SlidingExpiration=false, and per-request stamp validation. Rotate/reissue the cookie after password change. Login is limited to ten requests per minute per remote IP, no queue, returning 429 ProblemDetails. Register antiforgery and durable Data Protection keys through a configurable external path; missing/unusable key storage fails clearly in non-test deployments. Use injectable TimeProvider for timing tests. Log event/outcome IDs only; apply no-store to every auth response and JSON challenge/forbid handlers.
- [ ] Validate antiforgery on login/change-password/logout explicitly, including anonymous-login tokens; missing/invalid tokens return 400 `antiforgery-failed`. Include these cases in the failing HTTP tests before implementation. Set cookie names `__Host-TemplarAdmin` and `__Host-TemplarCSRF`, Path=/, no Domain, Secure=Always; set all Identity password composition requirements false and enforce the maximum of 128. Implement AccountSession plus the core unrestricted-admin policies now, replacing the disabled-security success branch and unregistering the operator API-key scheme, so restricted login is independently testable against an existing protected authoring mutation. Task 4 adds route-wide reads/antiforgery/profile safeguards. Add `AdminSession` to OpenAPI using the actual session-cookie name and change existing authoring declarations to that scheme. Public login/token routes are exempt from account authorization only, never from mutation antiforgery checks.
- [ ] Rerun the named tests; expect pass through real HTTPS-base-address cookie clients. `AdminIdentityApiFactory` explicitly initializes/provisions users and uses the actual antiforgery/login flow; it exposes `CreateAnonymousClient()` and `Task<HttpClient> CreateAdminClientAsync(bool changePassword, CancellationToken ct)` for later tests, plus isolated database/key-directory configuration. Add no product fixture credentials.
- [ ] Commit with message `feat: enforce admin login and mandatory first password change`.

### Task 4: Protect authoring and preserve directory invariants

**Files:** Modify `src/TemplarCMS.Api/Security/AuthoringAccessAuthorizationHandler.cs`, `AuthoringSecurityServiceCollectionExtensions.cs`, `AuthoringSecurityOptions.cs`, `UserDirectoryEndpoints.cs`; `src/TemplarCMS.Persistence/Security/EfUserDirectoryRepository.cs`; `src/TemplarCMS.Domain/Security/DirectoryUser.cs`; `src/TemplarCMS.Api/Media/MediaEndpoints.cs`; template/field-type/template-organization/content-ordering endpoints; appsettings in API and test projects. Create `src/TemplarCMS.Api/Security/AuthoringAntiforgeryFilter.cs`, `tests/TemplarCMS.Api.Tests/Security/AdminAuthorizationTests.cs`. Modify existing authoring/directory/persistence tests and API-key handler/defaults files.

**Interfaces:** `AuthoringAntiforgeryFilter : IEndpointFilter` calls IAntiforgery.ValidateRequestAsync before protected mutations. Existing AuthorContent and ManageUserDirectory policies now require a current unrestricted Active TemplarAdmin cookie. Extend `DirectoryWriteStatus` with `ProtectedAdministrator` and map it to 409 `bootstrap-admin-protected`. Repository profile updates synchronize linked identity Email/NormalizedEmail and preserve Username within the same SaveChanges transaction; use the framework ILookupNormalizer. Existing profile request/response shapes otherwise remain unchanged.

- [ ] Write `AdminAuthorizationTests`: anonymous, restricted, inactive/missing, and non-admin access is denied on every authoring read/write; disabled directory returns 503 only after authentication and does not disable login. Direct requests with legacy operator keys cannot author. Missing/invalid antiforgery rejects JSON and multipart changes; valid tokens allow both. Assert profile edits cannot remove the bootstrap admin role, stale revisions return 409, linked identity email stays consistent while login name is unchanged, and invited records remain credential-free. Enumerate endpoint metadata to catch future unmapped authoring routes.
  Pin authorization and antiforgery assertions in code:
  ```csharp
  Assert.Equal(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden, restrictedWrite.StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest, missingCsrfWrite.StatusCode);
  Assert.Equal(HttpStatusCode.Conflict, removeAdminRole.StatusCode);
  ```
- [ ] Run the named class plus current `AuthoringSecurityIntegrationTests`, `UserDirectoryIntegrationTests`, and directory repository tests; expect the new requirements to fail.
- [ ] Extend Task 3's mandatory policies across the route inventory; default security and directory to enabled. An explicitly configured AuthoringSecurity:Enabled=false must stop startup with an actionable configuration error, never grant anonymity. Require AuthorContent for authoring branches, dependencies, template design/organization, field metadata, media asset management, and shared content reads until the delivery plan adds ReadContent. Remove `.DisableAntiforgery()` from upload and validate the request token for every protected mutation. Keep session/password-change/logout available to restricted sessions. Preserve current-role and last-login/revision consistency; update LastLogin on successful login without disabling profile optimistic concurrency. Protect the marked admin in repository updates, synchronize identity email, and reject lifecycle mutation fields as before. Update tests to use explicit authenticated fixtures, never an enabled=false shortcut.
- [ ] Rerun those tests; expect pass. Shared content reads stay protected by unrestricted admin identity at this stage; the delivery plan adds the read-only API-key alternative without an anonymous interval.
- [ ] Commit with message `feat: enforce admin authoring and directory policies`.

### Task 5: Forward browser cookies safely through the existing Admin host

**Files:** Create `src/TemplarCMS.Admin/TemplarCMS.Admin.Server/TemplarApiProxyHandler.cs`; modify its Program.cs and `templarcms.admin.client/vite.config.ts`. Create `tests/TemplarCMS.Api.Tests/Security/AdminProxyIsolationTests.cs`; add the Admin.Server project reference to that test project if required.

**Interfaces:** `TemplarApiProxyHandler : HttpClientHandler` sets UseCookies=false; its development-only certificate exception preserves current behavior. Register it for the existing TemplarApiProxy client. Tests use `WebApplicationFactory<TemplarApiProxyHandler>` to select the Admin.Server assembly without introducing a second public Program type. Do not follow backend redirects automatically; preserve each Set-Cookie separately and browser-origin Cookie headers.

- [ ] Write `AdminProxyIsolationTests`: two independent browser clients use different authenticated cookie state through the same pooled handler; an anonymous client never inherits an admin cookie; multiple Set-Cookie values and antiforgery header survive forwarding. Assert Secure/HttpOnly/SameSite attributes and no shared cookie container. Use a disposable HTTPS loopback backend and the actual proxy host, not only property inspection.
  Pin proxy isolation assertions in code:
  ```csharp
  Assert.False(handler.UseCookies);
  Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
  Assert.NotEqual(firstBrowserCookie, secondBrowserCookie);
  ```
- [ ] Run the named class; expect cross-request isolation/forwarding assertions to fail with current pooled cookie handling.
- [ ] Configure the dedicated proxy handler and verify Vite forwards browser cookies without rewriting away Secure or using another user's state. Keep origin/host handling consistent with same-origin antiforgery; do not enable wildcard credentialed CORS. Do not add a browser-facing delivery key.
- [ ] Rerun tests; expect pass with two browser sessions and an anonymous request on reused connections.
- [ ] Commit with message `fix: isolate admin proxy cookies across browser sessions`.

### Task 6: Connect the Vue workspace to login and password change

**Files:** Create `src/TemplarCMS.Admin/templarcms.admin.client/src/types/auth.ts`, `composables/useAuthSession.ts`, `utils/auth-request-context.ts`, `utils/request-helpers.test.ts`, `components/security/LoginForm.vue`, `ChangePasswordForm.vue`, their `.test.ts` files, `composables/useAuthSession.test.ts`, and `src/App.auth.test.ts`. Modify `src/utils/request-helpers.ts`, `App.vue`, `components/security/UserDirectory.vue` and its tests, `components/layout/TopBar.vue` within the same client directory.

**Interfaces:** `AuthSession` matches Task 3's session DTO and has typed links; `AuthState` is `'checking' | 'anonymous' | 'change-password' | 'authenticated'`. `useAuthSession()` returns refs `state`, `session`, `error` and async `initialize()`, `login(loginName: string, password: string)`, `changePassword(currentPassword: string, newPassword: string)`, `logout()`. `configureAuthRequestContext({ getRequestToken, onUnauthorized, onPasswordChangeRequired })` configures memory-only transport hooks owned by App.vue; `clearAuthRequestContext()` resets them. `ApiRequestError extends Error` carries `status: number`, `code: string | null`, and `fieldErrors: Record<string, string[]>`. Existing fetchJson/sendMutation signatures remain compatible.

- [ ] Write tests for state transitions and failure handling; assert zero workspace requests before unrestricted login and an immediate forced-change screen after temporary login. Spy on both storage APIs to assert no secret/token writes. Test JSON/DELETE/FormData requests include CSRF without replacing multipart Content-Type. Test 401 during a draft preserves it, same-user login resumes without resetting it, explicit logout clears it, different-user login cannot reuse it, and delayed responses after logout are ignored. Test errors retain status/code/field validation and failed requests are never automatically replayed. Form tests cover password clearing, confirmation mismatch, focus/error announcements, and disabled duplicate submit.
  Pin state/secret assertions in code:
  ```typescript
  expect(auth.state.value).toBe('change-password')
  expect(workspaceFetch).not.toHaveBeenCalled()
  expect(localStorageSpy).not.toHaveBeenCalled()
  expect(sessionStorageSpy).not.toHaveBeenCalled()
  ```
- [ ] Run `npm test -- src/composables/useAuthSession.test.ts src/utils/request-helpers.test.ts src/components/security/LoginForm.test.ts src/components/security/ChangePasswordForm.test.ts src/App.auth.test.ts src/components/security/UserDirectory.test.ts` from the client directory; expect the new auth flow assertions to fail.
- [ ] Implement typed auth transport/composable and custom forms. Fetch/refresh antiforgery tokens after login, password change, and logout because their identity changes. Keep the workspace mounted but hidden/disabled on expiry to preserve in-memory drafts; use an auth-generation counter/AbortController so delayed results cannot restore cleared data. Gate mounted hooks/watchers and mutations by authenticated state, including useTreeActions refreshes. Replace directory key/header/connect controls with the shared account session and load on mount. Add an explicit logout emit/action to TopBar. Keep local styling scoped and do not alter the established Vue architecture despite the stale nested React scaffold instructions.
- [ ] Rerun targeted tests, `npm run typecheck`, and `npm run build`; expect pass. Check that all callers of raw fetch/fetchJson pass through CSRF/status handling and preserve cancellation.
- [ ] Commit with message `feat: connect admin workspace to secure account sessions`.

### Task 7: Integrate both authentication paths and document deployment

**Files:** Modify README.md, docs/current-state-summary.md, docs/decisions/ADR-0007-user-directory-foundation.md, docs/adr/ADR-0009-target-role-catalog-contract-and-boundary-validation.md; create `docs/decisions/ADR-0015-admin-identity-and-delivery-authentication.md`, `docs/verification/2026-10-10-admin-security.md`. Update the README's existing IIS/API smoke instructions that assume anonymous content or operator-key writes. Depends on all tasks in the delivery plan.

**Interfaces:** No new runtime interface. This gate exercises the actual commands, proxy, browser session, and ReadContent policy together.

- [ ] Add/finish integrated tests for startup without setup, setup/login/forced change, admin content reads, key-only content reads, forbidden key writes, revocation/reset/restart, and the one-key/one-cookie mixed-credential matrix from the delivery plan. Assert named protected routes expose no anonymous alternate raw-content path and the sample HTML page/health still work.
  Pin the integrated credential boundary in code:
  ```csharp
  Assert.Equal(HttpStatusCode.OK, siteKeyContentRead.StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden, siteKeyDirectoryRead.StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized, anonymousContentRead.StatusCode);
  ```
- [ ] Run the integrated test classes before finishing the gate; a missing transition/policy must fail with its expected 401/403/409 assertion, rather than weakening authentication to retain old tests.
- [ ] Update docs with command usage, schema script order, Data Protection key storage and permissions, HTTPS/same-origin IIS and Vite setup, startup locked state, lost-password recovery, session expiry, and the operator-key breaking change. Document server-side delivery-key placement and rotation; retain demo-fixture exclusions and note future identity/directory limitations. Record redacted evidence only. Do not change Linear issues or publish/merge without a separate user instruction.
- [ ] Run `. .\scripts\templar-cms-bootstrap.ps1`, `dotnet build .\TemplarCMS.sln`, and `scripts\dotnet-test.ps1 -Project <project>` for Application.Tests, Domain.Tests, Integration.Tests, and Api.Tests. In the client run `npm run typecheck`, `npm run build`, and targeted auth/directory/request tests. Rehearse an isolated HTTPS installation through Admin.Server and Vite: keyboard-only login/change, visible validation, logout, expiry with an unsaved draft, two browsers, reset, and restart. Test a site-server request with a delivery key. Record any unavailable live SQL Server/browser validation rather than claiming it passed. Use the verification and review skills before declaring completion.
- [ ] Commit the documentation/evidence and remaining integration fixes with message `docs: document admin identity and site delivery authentication`. Present the completed branch with test evidence for integration review.
