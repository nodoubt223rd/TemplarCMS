# Architecture Decision Record (ADR)

## ADR-0009: Target Role Catalog Contract and Boundary Validation

### Status
Accepted

### Date
2026-10-04

### Context
TemplarCMS is aligning its backend role architecture with the approved UX specification managed under the `TemplarCMS-VUE` layout repository (commit `c3a571f`). The original domain guidelines established in [ADR-0007: Strong Domain Typing and Pragmatic Patterns](ADR-0007-strong-domain-typing-and-pragmatic-patterns.md) remain fully active; this document serves as a new, distinct decision linked directly to the system workspace architecture defined in [ADR-0007: User Directory Foundation](../decisions/ADR-0007-user-directory-foundation.md).

The current project development baseline utilizes seven legacy role identifiers within `DirectoryUser.cs`. These seven legacy identifiers are completely retired; none of them match the five target identifiers specified in the agreed UX catalog. 

The deployment context for this application slice remains strictly restricted to transient, read-only review instances. Business stakeholders evaluate capabilities on temporary environments that are torn down immediately post-review. There is no active production database baseline, live client footprint, or deployed transaction volume. Consequently, this engineering slice explicitly excludes deployed data migrations, backward compatibility infrastructure for external business clients, backup rehearsal processes, or production rollout frameworks.

### Decision
We will establish an explicit application-layer catalog contract for user directory roles. This contract enforces strict entry mapping and validation constraints at the incoming directory request boundary, independent of downstream authentication or authorization subsystems.

#### 1. Catalog Entry vs. Assignment Segmentation
The system defines exactly five recognized catalog entries. Within that catalog, only four entries are classified as assignable roles.

| Role Identifier | Readable UX Label | Capability-Intent Description | Assignment Availability |
| :--- | :--- | :--- | :--- |
| `Author` | Author | Creates, updates, and manages structured content items within the lifecycle. | **Assignable** |
| `Developer` | Developer | Configures component schemas, headless layouts, and system-level integrations. | **Assignable** |
| `FormsEditor` | Forms Editor | Builds, structures, and designs layouts for dynamic data collection forms. | **Assignable** |
| `TemplarAdmin` | Templar Admin | Manages root template hierarchies, global settings items, and inheritance baselines. | **Assignable** |
| `MarketingAutomationEditors` | Marketing Automation Editors | Configures automated user journeys, triggers, and marketing logic engines. | **Recognized Only** *(Planned)* |

*Note: In accordance with the UX Handoff specification, exact application permissions, branch-level access boundaries, structural template safety overrides, and functional inheritance checks remain unresolved product decisions and are deferred to future security slices.*

#### 2. Marketing Automation Editors State Transition Rules
The `MarketingAutomationEditors` identifier is a recognized catalog entry but remains unassignable for new profiles or records. To prevent active drift while maintaining compatibility, profile update mutations handled at the directory layer must evaluate data state modifications against the stored record covered by the submitted revision:
* **Explicit POST Rejection:** Any incoming directory request matching a `POST /api/v1/security/users` payload that attempts to initialize a record with the `MarketingAutomationEditors` role will be blocked.
* **No New Assignments on PUT:** Any incoming profile change that attempts to add this identifier to an account record where it was not already present in the stored revision record must be rejected.
* **Preservation Allowed:** A profile save operation may preserve an existing `MarketingAutomationEditors` membership unchanged without triggering an error.
* **Removal Allowed:** A profile save operation is permitted to remove an existing `MarketingAutomationEditors` membership from an account record.
* **Revision-Safe Validation:** Membership evaluation is tied directly to revision-safe state. The incoming membership array is compared directly against the stored database record retrieved under the submitted revision token. Stale updates where the tokens do not match must return an HTTP `409 Conflict` response immediately, removing any requirement for historical database scans or audit log lookup sweeps.

#### 3. Boundary Request Validation Rules
* **Scoped Application Validation:** Validation checks are strictly scoped to incoming directory requests at the API layer (e.g., `POST /api/v1/security/users` and `PUT /api/v1/security/users/{id}`). While directory-user identity integration remains deferred, existing operator authentication policies and transactional token check routines remain explicitly required and preserved.
* **Rejection Criteria:** Any incoming directory request payload containing unknown strings, casing mismatches, duplicate role keys, or any of the seven retired legacy identifiers will be immediately rejected at the API boundary.
* **Error Response Format:** Validation failures must surface as an HTTP `400 Bad Request with ProblemDetails`, identifying the invalid key and preventing any persistence mutation from executing.
* **Membership Cardinality:** Accounts remain valid with multiple concurrent direct assignments or an entirely empty direct role array.
* **Environment Recovery Strategy:** Because the active database footprint relies on disposable test instances, all affected records must be cleanly reset before deploying this catalog change. The review directory structures will be purged and cleanly recreated via fresh seed scripts; no custom role-mapping arrays or database translation utilities will be introduced.

### Consequences

#### Positive
* Aligns the core application contracts precisely with the accepted capability-intent descriptions designated in the UX Handoff.
* Protects database persistence boundaries by introducing upfront validation filters for incoming directory requests.
* Elevates velocity within review cycles by executing clean seed data resets instead of maintaining complex legacy migration pathways.

#### Negative
* Application-layer validation code must explicitly check the current stored record during profile updates to differentiate between allowed preservation/removal and forbidden additions of the planned role.
* Directory memberships do not grant permissions; the existing operator authentication and ManageUserDirectory policy remain required.

### References
* [ADR-0007: Strong Domain Typing and Pragmatic Patterns](ADR-0007-strong-domain-typing-and-pragmatic-patterns.md)
* [ADR-0007: User Directory Foundation](../decisions/ADR-0007-user-directory-foundation.md)
* [TemplarCMS User Accounts: UX Handoff Reference Specification](../user-accounts-ux-handoff.md)
