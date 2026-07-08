---
name: templarcms-unit-tests
description: Create, update, review, or debug unit tests for TemplarCMS .NET code. Use when working on xUnit tests in `tests/TemplarCMS.Application.Tests`, `tests/TemplarCMS.ContentModeling.Tests`, `tests/TemplarCMS.Domain.Tests`, `tests/TemplarCMS.Api.Tests`, or related source under `src/`. Prefer this skill when adding regression coverage, tightening assertions, fixing failing test projects, or choosing the right targeted `dotnet test` command for this repository.
---

# TemplarCMS Unit Tests

## Workflow

1. Read the production file under test and the nearest existing test file before writing assertions.
2. Identify the behavior contract first: invariants, observable outputs, thrown exceptions, validation errors, or repository interactions.
3. Follow existing repo conventions:
   - `xUnit v3`
   - `Fact` and `Theory`
   - `NSubstitute` only where the project already uses it
   - direct assertions with `Assert.*`
4. Prefer narrow, behavior-focused tests over broad setup-heavy tests.
5. Do not weaken a regression test just to keep the suite green. If the production behavior is wrong, let the test expose it and report that clearly.
6. Avoid editing production code during test-only work unless the user explicitly asks for it.
7. Run the smallest relevant test project first, then expand only if the change crosses project boundaries.

## Test Project Map

- `tests/TemplarCMS.Application.Tests/TemplarCMS.Application.Tests.csproj`
  Use for application services and orchestration behavior.
- `tests/TemplarCMS.ContentModeling.Tests/TemplarCMS.ContentModeling.Tests.csproj`
  Use for template modeling, validators, builders, resolvers, and mapping.
- `tests/TemplarCMS.Domain.Tests/TemplarCMS.Domain.Tests.csproj`
  Use for domain value objects and domain-only behavior.
- `tests/TemplarCMS.Api.Tests/TemplarCMS.Api.Tests.csproj`
  Use for API contract and controller behavior.
- `tests/TemplarCMS.Integration.Tests/TemplarCMS.Integration.Tests.csproj`
  Use when persistence or cross-project wiring is the behavior under test.

## Conventions

- Match nearby naming and arrangement before introducing a new pattern.
- Prefer real value objects and lightweight in-memory collaborators over deep mocks.
- Use `NSubstitute` for interface seams that are already mocked elsewhere in the same test project.
- Assert specific exception types and meaningful message fragments when the behavior contract is failure-oriented.
- For validation-style results, assert error codes and targets, not just `IsValid == false`.
- For normalization or strong-type behavior, include edge cases that would catch accidental primitive drift.
- For repository and resolver behavior, assert both the selected result and the guard path that prevents invalid input from slipping through.

## Meaningful Assertions Only

- Do not write placeholder tests that only prove construction or non-null results unless that is the actual contract.
- Do not seed the expected output directly into mutable state and then assert it back out.
- Prefer assertions that would fail for a plausible regression in the code being covered.
- When confidence is shaky, ask what small bad code change should make the test fail. If the answer is “nothing obvious,” the test is probably weak.

## Restore And Test Commands

Use the repo-local NuGet config and workspace-local CLI caches so test runs do not depend on the user profile:

```powershell
$env:DOTNET_CLI_HOME='E:\Projects\TemplarCMS\.tmp\dotnet-cli'
$env:NUGET_PACKAGES='E:\Projects\TemplarCMS\.tmp\nuget-packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
```

Restore the target test project when needed:

```powershell
dotnet restore 'tests/TemplarCMS.ContentModeling.Tests/TemplarCMS.ContentModeling.Tests.csproj' --configfile 'E:\Projects\TemplarCMS\nuget.config'
```

Then run the targeted suite:

```powershell
dotnet test 'tests/TemplarCMS.ContentModeling.Tests/TemplarCMS.ContentModeling.Tests.csproj' --no-restore
```

Swap the project path for the relevant test project instead of defaulting to a broad solution-wide run.

## Completion Checklist

- New or changed tests match local conventions.
- Assertions protect meaningful behavior.
- The smallest relevant test project has been run.
- If restore was required, it used `nuget.config` from the repo root.
- Any temporary `.tmp` restore cache is removed before finishing when practical.
