# ADR-0013: Items, defining templates, and system section composition

Status: Accepted, 2026-09-16

## Decision

`Item` is a C# domain abstraction, not a template. Content items and template
definitions derive from it. Every template definition has the system Template
ID as its `TemplateId`. This is its defining template, independent of its
ordered `BaseTemplates` inheritance references. Template itself is described by
Template; that identity reference is not an inheritance cycle.

Standard inherits Advanced, Appearance, Help, Lifetime, Publishing, Statistics,
Tasks, and Version. Each is a source-controlled template containing its existing
section. Template and Folder inherit Standard. Page is an editable authored
template inheriting Standard. The existing Content section remains local to
Standard to preserve Title, Navigation Title, and Meta Description behavior.

System section and field IDs, scopes, visibility metadata, and existing
resolution rules remain unchanged. Effective-template consumers therefore
receive Page's local Content/SEO fields and inherited admin sections together.
Section templates do not inherit Standard: doing so would create a cycle.

The JSON repository persists `templateId`; legacy documents without that property
default to Template. Other defining-template IDs are rejected. REST template
detail responses expose `templateId` independently of `baseTemplates`.
This preserves the existing separate storage boundaries: template definitions
are JSON; content values are relational. It does not move definitions into SQL
or introduce a second editable copy of each definition in the content tree.

## Bootstrap and compatibility

The protected built-in Item template is removed. On a new instance, bootstrap
creates an editable Page only when no Page exists. Its starter ID reuses the
retired Item ID; existing authored Pages keep their own IDs and fields. Standard's
starter content-tree entry uses Template as its defining template.

Existing installations need an explicit data conversion before running this
version. An authored Page may still inherit `item`, and stored content may still
use Item's old ID. The conversion must preserve authored Page identity and
fields, change legacy base references to Standard, preserve inherited Body as a
local field where necessary, and reassign stored Item instances to Page.

## Upgrade procedure (SQL Server)

1. Back up both the target database and its runtime template directory. Pause
   authoring and stop the application before taking the final conversion copy.
2. Run the offline preparation helper for the appropriate runtime directory and
   database. It only reads that directory and writes a new output directory:

   ```powershell
   ./scripts/prepare-template-composition-upgrade.ps1 `
     -TemplatesPath C:/inetpub/wwwroot/TemplarCMS.Api/RuntimeData/Templates `
     -OutputDirectory .tmp/template-upgrade-authoring `
     -DatabaseName templarcms_authoring
   ```

3. Review `original` versus `templates`, especially Page's Content and SEO
   fields. The helper refuses authored keys that collide with system templates,
   additional bases on templates requiring conversion, Page system-field
   overrides, and changes to the legacy fields' types or scopes. Those cases
   require a separate effective-model and stored-value review.
4. Review `reassign-template-items.sql`. Its default is a transaction that rolls
   back. It targets the named database, converts legacy Item IDs to the Page ID
   from the matching JSON, remaps legacy field IDs to Page's corresponding field
   IDs, and corrects the seeded Standard item's defining template. It stops if
   destination field values already exist. It never deletes content or field
   values or changes their language/version scopes. Set `@Apply = 1` only for
   the approved conversion. The generated SQL is not executed by the helper.
5. With the application stopped, apply the reviewed SQL and matching JSON, then
   deploy the updated API/admin together. Do not start the new API against JSON
   that still inherits `item`. Check health, Page's inherited fields, Home/About,
   and saving a Page section after restart.
6. Evaluate the published database separately using the same template identities
   as its deployed model. Do not assume authoring's database conversion upgrades
   published. Rollback requires the old binaries, original JSON, and database
   backup as one consistent set.

No SQL schema change is needed. Existing SQLite installations require equivalent
operator-reviewed data reassignment; the helper emits SQL Server syntax only.
