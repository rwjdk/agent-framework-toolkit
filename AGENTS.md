# Conversation start rule (MANDATORY)
Whenever a **new conversation** begins, do **not** start coding immediately.

1. Investigate the codebase first. Read the relevant files and inspect existing patterns before proposing changes.
2. Identify what is unclear, risky, or ambiguous about the requested change.
3. Ask the user **3-10 clarifying questions in a numbered list** (not bullets).
4. Only after those questions are answered, start implementing. For follow-up turns in the same conversation, do not repeat this step unless a new ambiguity appears.

# Repo map (where things live)
- `src/AgentFrameworkToolkit/`: Core library, middleware, and shared helpers.
- `src/AgentFrameworkToolkit.*`: Provider packages, normally one package per provider.
- `src/AgentFrameworkToolkit.Tools/`: Tools integration.
- `src/AgentFrameworkToolkit.Tools.ModelContextProtocol/`: MCP tooling integration.
- `src/AgentSkillsDotNet/`: Skills integration for agents.
- `development/`: Sandbox, secrets utility, and tests. Treat this folder as development support code; do not run its tests unless the user explicitly asks.
- `Directory.Packages.props`: Central package version management. Do not put package versions directly in `.csproj` files.
- `Directory.Build.props` / `Directory.Build.targets`: Shared build, analyzer, packaging, and validation settings.
- `nuget-package.props`: Shared NuGet metadata and coordinated package versioning.

# New Provider Guidelines
Use the built-in skill `create-agent-provider` (see `.codex/skills/create-agent-provider/SKILL.md`).

# Model Name Updates
Use the `update-model-names` skill in `.agents/skills/update-model-names/SKILL.md` for model catalog refreshes.

# Prompting and execution principles
- Prefer short, explicit, high-signal instructions over long repetitive ones.
- Preserve the user's task intent and make the smallest change that fully solves the request.
- Separate rules, context, and deliverables clearly. Use Markdown sections and XML-style blocks when they improve instruction following.
- Ground decisions in repository evidence. Inspect files before assuming patterns, architecture, or naming.
- After the mandatory start-of-conversation questions are answered, continue proactively on reversible, low-risk steps instead of asking for unnecessary confirmation.
- If context can be retrieved from the repo, retrieve it before asking the user.
- When a task is blocked by a real ambiguity, ask only the minimum follow-up needed to unblock the work.

<instruction_priority>
- User instructions override default style, initiative, and formatting preferences.
- Safety, honesty, repository constraints, and permission constraints do not yield.
- If a newer instruction conflicts with an older one, follow the newer instruction and preserve all non-conflicting earlier instructions.
</instruction_priority>

<default_follow_through_policy>
# C# and .NET development rules
- Follow `.editorconfig` at the repo root and `src/.editorconfig` for all source changes.
- `src` uses CRLF line endings, braces, explicit types, implicit object creation with `new()`, collection expressions (`[]`), and namespaces that match folder structure.
- Nullable is enabled; analyzers are enabled; warnings are errors. Treat new warnings as build breaks.
- Prefer primary constructors where the surrounding code already uses them and they improve clarity.
- Public members require XML documentation.
- Use `[PublicAPI]` from JetBrains.Annotations where the existing patterns use it.
- Async methods must end with `Async`.
- Match the surrounding code style before introducing new abstractions, helpers, or patterns.
- Keep public A
- The mandatory conversation-start questions still apply to every new conversation.
- After those questions are answered, if the user's intent is clear and the next step is reversible and low-risk, proceed without asking permission again.
- Ask permission only when the next step is irreversible, has meaningful external side effects, requires secrets, or involves a choice that materially changes the outcome.
</default_follow_through_policy>

<missing_context_gating>
- Do not guess when required context is missing.
- Prefer repo inspection and tool-based lookup before asking the user.
- If you must proceed with an assumption, make the assumption explicit and keep the action reversible.
</missing_context_gating>

<verification_loop>
- Before finalizing, check that the output satisfies every requested requirement.
- Verify factual or behavioral claims against the files you inspected or the command output you obtained.
- Prefer lightweight verification such as a targeted `dotnet build` on the affected project or solution slice.
- Do **not** run tests under `development/` unless the user explicitly asks for test execution.
- If you could not verify something, say so clearly in the final response.
</verification_loop>

<terminal_tool_hygiene>
- This repository is typically worked on from Windows PowerShell.
- Do not attempt to use `rg`; it is not installed on this system.
- Prefer PowerShell-native commands such as `Get-ChildItem`, `Select-String`, and `Get-Content` for discovery.
- Use direct edit tools for file changes instead of trying to emulate patches in shell scripts.
- After edits, run an appropriate lightweight verification step before declaring the work done.
</terminal_tool_hygiene>
PI changes intentional and minimal; avoid broad refactors unless the user asks for them.

# Dependency and project file rules
- Keep package versions centralized in `Directory.Packages.props`.
- Add package references in project files without inline version numbers.
- If a new project or package is added, include it in `AgentFrameworkToolkit.slnx`.
- Reuse the shared props/targets files instead of copying build configuration into individual projects.

# Verification and workflow guidance
- Prefer inspecting and building only the affected area first.
- Use `dotnet build --configuration Release` for verification when a wider build is warranted.
- Avoid running `development/Sandbox`, `development/Secrets`, or `development/Tests` unless the user explicitly asks or the task specifically depends on them.
- Keep changes aligned with the existing public API shape, provider naming, and README examples.
- When the work changes user-facing behavior, update `CHANGELOG.md`.

## Repo update checklist (when relevant)
- If a new project or package is added: update `AgentFrameworkToolkit.slnx` and `Directory.Packages.props`.
- If a public API or user-facing behavior changes: update `CHANGELOG.md`. Update an affected README or provider README when usage instructions change.
- Omit the `Unreleased` heading from `CHANGELOG.md` when it has no entries; move its entries into the version being released.
- For routine model-name additions, update code and `CHANGELOG.md` only. Update READMEs only when usage instructions change or the user explicitly requests it.
- If documentation or samples change: keep `README.md` and provider documentation consistent with the code.
