---
name: upgrade-maf
description: Upgrade AgentFrameworkToolkit to a new Microsoft Agent Framework release.
---

# Upgrade Microsoft Agent Framework

Perform the upgrade in three phases. Hold after Phase 1 before committing the toolkit, and hold after Phase 2 before upgrading dependent repositories.

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
3. Never upgrade a package across a major-version boundary. "Latest" means the latest eligible version within the package's currently referenced major version. If the requested Agent Framework target crosses a major-version boundary, stop and report the conflict instead of upgrading.
4. Use the latest available version with these prerelease rules:
   - Allow prereleases for `Microsoft.Agents.AI.Anthropic`.
   - Allow prereleases for `Microsoft.Agents.AI.Foundry`.
   - Allow prereleases for `Azure.AI.OpenAI`.
   - Require stable releases for every other package, including `Microsoft.Agents.AI` and `Microsoft.Agents.AI.OpenAI`.
5. Keep versions centralized in `Directory.Packages.props`. Do not add inline versions to project files.
6. Make only the package-version edits required by the upgrade.

### Build

Run:

```powershell
dotnet build .\AgentFrameworkToolkit.slnx --configuration Release
```

Treat restore errors, compilation errors, or package incompatibilities as issues. Stop immediately, make no compatibility fixes, and report the exact result.

Do not mistake a command-wrapper timeout or a transient overlapping-build file-lock retry for a source issue when the build itself succeeds. Obtain an unambiguous final build result before deciding whether to stop.

### Changelog and tests

Only after a successful build:

1. Convert the top `## Unreleased` section into the new release entry: replace the `Unreleased` heading with the version heading, preserve every existing unreleased bullet, add the two upgrade bullets, and keep exactly one separator after the combined entry. If no `Unreleased` section exists, add the release entry at the top. Match the existing date and separator style:

```markdown
## Version <new-version> (<ordinal date>)
- Updated Agent Framework from <old-version> to <new-version>
- Updated all NuGet packages to the latest
- <all bullets preserved from the former Unreleased section, when present>

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

1. Use the authenticated GitHub REST API, for example through `gh api`; do not open a browser.
2. Read `repos/rwjdk/agent-framework-toolkit/releases` and inspect the immediately previous release to reproduce its tag, title, and body conventions. Check whether the target tag or release already exists before creating anything.
3. Prepare a local JSON payload for `POST repos/rwjdk/agent-framework-toolkit/releases`:
   - `tag_name`: `<version>`
   - `target_commitish`: the verified pushed commit SHA (normally on `main`)
   - `name`: the matching `CHANGELOG.md` heading, for example `Version <version> (<ordinal date>)`
   - `body`: copy the bullet content from the new changelog entry, excluding its heading and separator
   - `draft`: `false`
   - `prerelease`: whether the target version is a prerelease
   - `make_latest`: `"true"` for stable releases, `"false"` for prereleases
4. Verify the tag, target commit, title, description, and release label. Preserve actual newlines in the JSON body; pass the payload as a file with `gh api --input` rather than interpolating release notes into shell commands.
5. Do not create a remote release, draft, or tag until publication is explicitly authorized. Do not create a draft unless the user explicitly requests it.

GitHub creates a newly entered release tag when the release is published. State this clearly if the tag does not yet exist remotely.

## Hold 2: Publish confirmation

Report the commit, push, selected tag, title, release-note content, and label from the prepared API payload.

Wait for explicit publication approval unless the user has already authorized publishing this release in the current session. After approval, submit the prepared payload through the API and verify the returned release URL, tag, target commit, title, body, published status, and latest/prerelease status. If a request fails or its outcome is uncertain, check for an existing release and tag before retrying; do not overwrite an existing release or move a tag without explicit authorization.

## Phase 3: Upgrade dependent repositories

Proceed only after the user explicitly approves Phase 3. The user may approve it independently of publishing the prepared GitHub release.

### Discover repositories

1. Inspect immediate child directories of `X:\` that are Git repositories.
2. Exclude:
   - The current `agent-framework-toolkit` repository and its wiki repository.
   - The upstream `agent-framework` source repository, whose framework references are normally project references rather than NuGet consumption.
   - The `extensions` repository.
3. Read each candidate repository's applicable `AGENTS.md` or equivalent repository instructions.
4. Include only repositories with actual NuGet `PackageReference` or `PackageVersion` entries for at least one of:
   - `Microsoft.Agents.AI` or any package whose ID starts with `Microsoft.Agents.AI.`
   - `AgentFrameworkToolkit` or any package whose ID starts with `AgentFrameworkToolkit.`
   - `AgentSkillsDotNet`, which is part of AgentFrameworkToolkit.
5. Inspect every included repository's branch, upstream, remotes, working-tree status, package-version structure, and build entry point. Preserve unrelated changes, including changes that overlap a package file.

### Update scoped packages only

1. Query NuGet live for every matching package. Do not rely on cached knowledge.
2. Update only the Agent Framework and AgentFrameworkToolkit packages listed above. Ignore every other NuGet package, even when a newer version is available.
3. Never cross a major-version boundary.
4. For packages currently on a stable release, use the latest stable version in the current major. For packages currently on a prerelease, allow the latest prerelease in the current major.
5. Set all `AgentFrameworkToolkit.*` packages and `AgentSkillsDotNet` to the new stable toolkit version produced by Phase 1 when that version exists on NuGet.
6. Leave a matching package unchanged when NuGet has no newer eligible version.
7. Preserve central package management where a repository uses it. Do not move versions between files or change unrelated package declarations.

### Build, commit, and push each repository

Process every included repository independently:

1. Build using its repository-native instructions and widest practical solution or build entry point. When a repository contains multiple relevant solutions, build all of them.
2. If the build fails:
   - Do not make compatibility fixes.
   - Do not commit or push that repository.
   - Leave its package edits uncommitted and record the exact failure.
   - Continue processing the other repositories.
3. If the build succeeds:
   - Recheck the diff and status.
   - Stage only the scoped package-version edits. Do not stage unrelated user changes, even when they are in the same files.
   - Commit with a concise message such as `Upgrade Agent Framework packages to <version>`.
   - Push the current branch to its configured upstream.
   - Record the build result, commit hash, and push destination.
4. Confirm excluded repositories remain untouched and every successfully pushed repository is clean and synchronized with its upstream.

## Hold 3: Completion report

Report:

- Every repository discovered and whether it was included or excluded.
- Package upgrades performed in each included repository.
- Build result and warning/error counts for each included repository.
- Commit hash and push destination for each successfully built repository.
- Every failed or uncommitted repository with the exact reason.
- Matching packages intentionally left unchanged because no newer eligible version existed.
- Any unrelated pre-existing changes that remain uncommitted.
