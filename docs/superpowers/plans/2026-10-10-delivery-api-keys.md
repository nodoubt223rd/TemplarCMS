# Client Site Delivery API Keys Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Require a read-only delivery API key for client-facing sites to read CMS content, while allowing unrestricted admin sessions on shared read endpoints.

**Architecture:** A dedicated DeliveryApiKey scheme checks named key digests in protected deployment configuration and produces read-only principals. ReadContent accepts a valid delivery principal or a current unrestricted TemplarAdmin account; authoring and directory policies never gain permissions from a delivery key. Keys live on the consuming site's server, with no new key database or management UI.

**Tech Stack:** .NET 8, ASP.NET Core authentication/authorization, SHA-256, cryptographic random generation, configuration options, xUnit v3, existing SQLite-backed API fixtures.

**Spec:** [Approved design](../specs/2026-10-10-initial-admin-design.md).

## Global Constraints

- Target .NET 8 with SDK 8.0.400; retain the current storage/content modeling boundaries.
- Site requests send the key in `X-Templar-Api-Key` over HTTPS; no keys in URL parameters.
- Keys have at least 256 bits of cryptographic randomness and are displayed once with their SHA-256 digest by a local command.
- Store only named digests in CMS deployment configuration outside source control; raw keys belong in each consuming site's server-side secret configuration.
- Compare digests in constant time. Missing/invalid keys return 401; valid delivery keys alone on authoring/directory routes return 403.
- No configured keys means no delivery access; never fall back to anonymous content reads.
- Account login and first password change secure authoring. Delivery keys grant neither AuthorContent nor ManageUserDirectory.
- Preserve delivery data shapes, ProblemDetails, HATEOAS policy alignment, and accurate OpenAPI requirements.
- No key-management UI/database, per-site content partitioning, browser bundle/storage secrets, or machine authoring credentials.

## Review Focus

- Duplicate headers, query-string keys, malformed config digests, and oversized keys fail closed without exceptions leaking values (Task 1).
- Mixed cookie/key requests are evaluated per credential: a valid key can read despite a restricted cookie, but cannot upgrade that cookie to authoring (Task 2).
- JSON metadata, content links, children, and media bytes cannot expose an anonymous delivery backdoor (Task 2).
- Downstream/shared caches must not serve authenticated data to a client missing credentials (Task 2).
- Removing a configured key must revoke it after the documented configuration reload/restart, without changing unrelated site keys (Task 3).

## Execution order and files

Execute after Tasks 1–6 in the [admin-identity plan](2026-10-10-admin-identity.md),
before its final integration/documentation gate. Task 1's validator and command
can be independently tested; Tasks 2–3 verify the shared account/key boundary.
An executor must read both plans and the approved spec. Use the same worktree.

New delivery options, key generation, validation, scheme, and read policy each
have focused files under Api/Security. Command dispatch extends the existing
AdminCommandRunner; do not introduce another entrypoint or a second key table.

For red/green checks, bootstrap/restore the API test project and run
`dotnet test .\tests\TemplarCMS.Api.Tests\TemplarCMS.Api.Tests.csproj --no-restore
--filter FullyQualifiedName~<test-class>`. Use actual generated keys in isolated
fixtures, never shared product credentials.

### Task 1: Generate, configure, and authenticate site delivery keys

**Files:** Create `src/TemplarCMS.Api/Security/DeliveryKeyOptions.cs`, `DeliveryKeyGenerator.cs`, `DeliveryKeyValidator.cs`, `DeliveryApiKeyAuthenticationHandler.cs`, `DeliveryApiKeyDefaults.cs`; `tests/TemplarCMS.Api.Tests/Security/DeliveryKeyTests.cs`. Modify `src/TemplarCMS.Api/Commands/AdminCommandRunner.cs`, `Security/AuthoringSecurityServiceCollectionExtensions.cs`, `Program.cs`, `appsettings.json`; command tests from the identity plan.

**Interfaces:** `DeliveryKeyOptions.SectionName = "DeliverySecurity"`, with `Dictionary<string, string> KeyDigests` and fixed header `X-Templar-Api-Key`. `GeneratedDeliveryKey(string Key, string Sha256Digest)` is command-only. `DeliveryKeyGenerator.Generate()` returns that record. `DeliveryKeyValidator.TryAuthenticate(string rawKey, out string? keyName)` returns bool. `DeliveryApiKeyDefaults.SchemeName = "DeliveryApiKey"`; authenticated principals contain NameIdentifier=keyName and claim `templar:delivery = read`, with no directory/catalog roles. The handler is a standard AuthenticationHandler<AuthenticationSchemeOptions>.

- [ ] Write `DeliveryKeyTests`: generated keys decode to 32 random bytes (43 unpadded base64url characters), two keys differ, and UTF-8 SHA-256 matches a 64-character hexadecimal digest. Assert valid keys authenticate with the configured name, invalid/absent keys do not, and empty config fails closed. Test duplicated headers, query-only keys, wrong format/length, oversized keys, invalid digest lengths/hex, and duplicate digest entries. Capture command output to prove only `generate-delivery-key` emits a raw key/digest and no logger emits either.
  Pin generation and configuration assertions in code:
  ```csharp
  Assert.Equal(43, generated.Key.Length);
  Assert.Equal(64, generated.Sha256Digest.Length);
  Assert.True(validator.TryAuthenticate(generated.Key, out var keyName));
  Assert.Equal("site-A", keyName);
  ```
- [ ] Run the named class and command tests; expect new validator/generator/dispatch assertions to fail.
- [ ] Generate 32 bytes via RandomNumberGenerator and base64url encode without padding; hash the exact UTF-8 key text with SHA256.HashData. Accept one header value only, exactly the generated key format; never trim/merge multiple values or read query strings. Validate configured digest syntax and unique digests/names at startup, naming the invalid entry without echoing its value. Decode configured digests and use CryptographicOperations.FixedTimeEquals for matching. Register the scheme with typed 401/403 ProblemDetails and no sensitive failure detail. Use startup-snapshotted key configuration in this slice so revocation explicitly requires a restart. Add an empty KeyDigests map to shipped settings. Dispatch `generate-delivery-key` locally without needing runtime database initialization or running HTTP; the command returns the raw key and digest to the operator terminal only.
- [ ] Rerun tests; expect pass and fail-closed configuration/format behavior. Confirm the retired AuthoringSecurity:ApiKey is ignored for both key provisioning and authentication.
- [ ] Commit with message `feat: add read-only site delivery API keys`.

### Task 2: Apply separate read and authoring policies to every route

**Files:** Create `src/TemplarCMS.Api/Security/ReadContentAuthorizationHandler.cs`, `ReadContentRequirement.cs`, `ApiAuthorizationResultHandler.cs`; `tests/TemplarCMS.Api.Tests/Security/DeliveryAuthorizationTests.cs`. Modify `Security/ApiAuthorizationPolicies.cs`, `AuthoringSecurityServiceCollectionExtensions.cs`, `AuthoringAccessAuthorizationHandler.cs`, `UserDirectoryEndpoints.cs`; `Content/ContentLookupEndpoints.cs`, `Content/ContentItemResponse.cs`, `Media/MediaEndpoints.cs`, `Templates/TemplateEndpoints.cs`, `TemplateOrganizationEndpoints.cs`, `FieldTypeEndpoints.cs`, Program.cs; API content/media/security tests affected by new read requirements.

**Interfaces:** `ApiAuthorizationPolicies.ReadContent = "ReadContent"`; `ReadContentRequirement : IAuthorizationRequirement`; `ReadContentAuthorizationHandler : AuthorizationHandler<ReadContentRequirement>`. ReadContent, AuthorContent, and ManageUserDirectory authenticate both Identity.Application and DeliveryApiKey to distinguish authenticated-but-forbidden delivery requests from anonymous ones. Only ReadContent can succeed because of a delivery principal. Account eligibility consumes current validated account claims from the identity plan, not merely a generic IsAuthenticated or merged-principal role.

- [ ] Write `DeliveryAuthorizationTests` as a route/credential matrix: anonymous and wrong key read=401, valid key read=200, unrestricted admin cookie read=200, restricted cookie alone read=403, valid key+restricted cookie read=200, key-only authoring/directory=403, restricted cookie+key authoring=403, unrestricted admin cookie+invalid key authoring=allowed. Include disabled/no-key config and retired operator keys. Assert raw content-by-ID/path, children/root children, and media binary URLs require the read policy; workspace branches, dependencies, template metadata, field types, and media listing remain admin-only. Assert range requests do not bypass media authentication, delivery principals have no admin role, and anonymous alternate/raw routes cannot be discovered from endpoint metadata. Verify delivery response links do not promise forbidden mutations/workspace access.
  Pin mixed-credential and forbidden-write assertions in code:
  ```csharp
  Assert.Equal(HttpStatusCode.OK, keyWithRestrictedCookieRead.StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden, keyOnlyWrite.StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden, keyWithRestrictedCookieWrite.StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized, anonymousMediaRead.StatusCode);
  ```
- [ ] Run the named matrix; expect key authorization/shared read behavior to fail before the new policy exists.
- [ ] Implement per-identity eligibility without combining claims into elevated privileges. Protect the four content lookup/children routes with ReadContent, plus media binary reads. Protect workspace/root/item branch views, authoring metadata/dependencies, media listing, and every mutation with AuthorContent; protect the entire directory group with ManageUserDirectory. Keep `/health`, content-free `/api/v1` discovery, and existing rendered HTML pages public. Require authenticated responses to use private/no-store cache headers, including media range/conditional responses, and preserve authorization before any file/cache response. Filter mutation/workspace links for delivery callers while keeping content field payloads stable. Add 401/403 ProblemDetails declarations and align allowed HATEOAS targets. Document that public-site image URLs need a site-server media proxy that sends the key; direct visitor browser requests cannot carry a private site key.
- [ ] Register `ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler` to emit one consistent ProblemDetails body for multi-scheme failures; individual scheme challenges must not append competing JSON responses. Assert one valid JSON body and expected 401/403 codes in the route matrix. Make admin-only link properties nullable and omit nulls for delivery responses with JsonIgnoreCondition.WhenWritingNull; retain every existing link for eligible admin callers. Do not weaken the Admin client's typed link assumptions because its authenticated responses still contain those links.
- [ ] Rerun the matrix plus existing content/media/authoring/directory tests using explicit fixtures; expect pass. Test real HTTPS header/cookie requests, not only direct handler calls. Keep the sample rendered page and health regression checks passing.
- [ ] Commit with message `feat: separate site content delivery from admin authoring`.

### Task 3: Verify rotation, OpenAPI, and consuming-site setup

**Files:** Create `tests/TemplarCMS.Api.Tests/Security/DeliveryKeyRotationTests.cs`, `DeliveryOpenApiTests.cs`; modify `src/TemplarCMS.Api/Security/AuthoringSecurityOperationFilter.cs`, `Program.cs`, and README.md. Add delivery evidence to `docs/verification/2026-10-10-admin-security.md` from the identity plan's final gate.

**Interfaces:** OpenAPI scheme IDs are `DeliveryApiKey` (header apiKey) and `AdminSession` (cookie apiKey metadata for the Identity.Application cookie). Read endpoints list separate OR security requirements for delivery key and admin session; authoring/directory list AdminSession only. Login/antiforgery are explicitly marked public; change/session/logout require AdminSession. Document CSRF header requirements for cookie mutations, without embedding real values.

- [ ] Write `DeliveryKeyRotationTests`: start with site-A and site-B named digests, replace A and restart against the same database, assert old-A=401, new-A=200, B=200, empty-key config=401. Assert configuration changes without restart do not silently imply immediate revocation. Write `DeliveryOpenApiTests` asserting read schemes are OR, authoring never advertises a delivery key, restricted-session failures are documented, and no key/hash/password values are present in generated OpenAPI.
  Pin rotation assertions in code:
  ```csharp
  Assert.Equal(HttpStatusCode.Unauthorized, oldKeyResponse.StatusCode);
  Assert.Equal(HttpStatusCode.OK, replacementKeyResponse.StatusCode);
  Assert.Equal(HttpStatusCode.OK, unrelatedSiteResponse.StatusCode);
  ```
- [ ] Run both classes; expect rotation/documentation security assertions to fail before wiring.
- [ ] Extend the identity plan's security definitions/filter with the delivery scheme and ReadContent OR alternatives. Document `generate-delivery-key`, setting `DeliverySecurity:KeyDigests:<site-name>` externally, placing the raw key on the site server, using the request header, and explicit restart for rotation/revocation. Explain that site names label credentials and do not partition content permissions in this slice. Update README smoke examples to accept externally supplied delivery keys through server-side secret configuration or process memory without logging headers or command-line secrets; operator mutations now use the account flow.
- [ ] Rerun both classes and the full API test project; expect pass. Make a real server-side HTTP content request with a generated test key, then confirm an anonymous request returns 401. Record only redacted results. Return to Task 7 of the identity plan for the combined build/browser/provider verification gate.
- [ ] Commit with message `docs: verify and document site delivery key rotation`.
