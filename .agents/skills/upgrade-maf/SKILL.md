---
name: upgrade-maf
description: Upgrade AgentFrameworkToolkit to a new Microsoft Agent Framework release.
---

# Upgrade Microsoft Agent Framework

Perform the upgrade in two phases with a mandatory user-confirmation hold after each phase.

## Initial inspection

1. Read `AGENTS.md` and follow its repository rules but ignore its conversation-start questions requirement.
2. Inspect:
   - `Directory.Packages.props`
   - `nuget-package.props`
   - `AgentFrameworkToolkit.slnx`
   - The newest entries in `CHANGELOG.md`
   - Package references in every project, including `development`
   - `development/Tests/Tests.csproj`
   - `git status`, the current branch, and configured remotes
3. Treat invocation with a target Agent Framework version as confirmation of the target version and acceptance of this skill's documented prerelease policy, build scope, test scope, and release expectations. Do not ask routine clarifying questions for those items. Ask only when the target version is missing or ambiguous, or when the user's instructions conflict with this workflow.
4. Preserve unrelated user changes.

## Phase 1: Upgrade and verify

### Update packages

1. Query NuGet for the current latest versions; do not rely on cached knowledge.
2. Update every centrally declared or directly referenced NuGet package, including packages used only by `development`.
3. Use the latest available version with these prerelease rules:
   - Allow prereleases for `Microsoft.Agents.AI.Anthropic`.
   - Allow prereleases for `Microsoft.Agents.AI.Foundry`.
   - Allow prereleases for `Azure.AI.OpenAI`.
   - Require stable releases for every other package, including `Microsoft.Agents.AI` and `Microsoft.Agents.AI.OpenAI`.
4. Keep versions centralized in `Directory.Packages.props`. Do not add inline versions to project files.
5. Make only the package-version edits required by the upgrade.

### Build

Run:

```powershell
dotnet build .\AgentFrameworkToolkit.slnx --configuration Release
```

Treat restore errors, compilation errors, or package incompatibilities as issues. Stop immediately, make no compatibility fixes, and report the exact result.

Do not mistake a command-wrapper timeout or a transient overlapping-build file-lock retry for a source issue when the build itself succeeds. Obtain an unambiguous final build result before deciding whether to stop.

### Changelog and tests

Only after a successful build:

1. Add a new entry at the top of `CHANGELOG.md`, matching the existing date and separator style:

```markdown
## Version <new-version> (<ordinal date>)
- Updated Agent Framework from <old-version> to <new-version>
- Updated all NuGet packages to the latest

---
```

2. Run every test in the test project:

```powershell
dotnet test .\development\Tests\Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=phase1-<version>.trx" --results-directory .\TestResults
```

Use a 5 min timeout because the suite may take several minutes. Do not filter or ignore tests unless the user explicitly changes the scope.

3. If any test fails, stop without changing the toolkit package version. Report failures and do not attempt fixes.
4. If every test passes, set `<PackageVersion>` in `nuget-package.props` to the exact stable version of `Microsoft.Agents.AI`.
5. Check the final diff, line endings, and working-tree status. Confirm that only intended files changed.

## Hold 1: Phase 1 confirmation

Stop and report:

- Package upgrades performed (List out all in details)
- Build result
- Test counts
- New toolkit package version
- Changed files
- Any unverified detail

Do not commit, push, tag, or open the GitHub release workflow. Wait for the user to explicitly approve Phase 2.

## Phase 2: Commit, push, and prepare release

Proceed only after the user approves Phase 2.

### Commit and push

1. Recheck `git status` and the staged diff.
2. Stage only the Phase 1 files.
3. Commit with a concise message such as:

```text
Upgrade Microsoft Agent Framework to <version>
```

4. Push the current branch to its configured upstream.
5. Report the commit hash and push destination.

### Prepare the GitHub release

1. Use the Chrome browser skill and the user's existing signed-in Chrome session.
2. Open `https://github.com/rwjdk/AgentFrameworkToolkit/releases`.
3. Inspect the immediately previous release and reproduce its tag, title, and body conventions.
4. Open the new-release form and prepare:
   - Tag: `<version>`, targeting the pushed branch (normally `main`)
   - Title: the matching `CHANGELOG.md` heading, for example `Version <version> (<ordinal date>)`
   - Description: copy the bullet content from the new changelog entry, excluding its heading and separator
   - Release label: `Latest`, unless the target version is a prerelease
5. Verify the selected tag, target, title, description, and release label.
6. Do not click **Publish release**.
7. Do not click **Save draft** unless the user explicitly requests it.
8. Leave the completed live form open in Chrome as a handoff.

GitHub creates a newly entered release tag when the release is published. State this clearly if the tag does not yet exist remotely.

## Hold 2: Publish confirmation

Stop with the prepared form open. Report the commit, push, selected tag, title, release-note content, and label.

The user must review and press **Publish release**. Never publish on their behalf unless they issue a new, explicit instruction to do so.
