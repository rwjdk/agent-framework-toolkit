# Codex

Experimental while the implementation stabilizes. APIs may change; usage reports compiler diagnostic `AFT999`.

Control a local Codex runtime through Microsoft's `AIAgent` API using the Codex app-server protocol. Install Codex CLI (target version: `0.163.0`) and run `codex login`, or set `CodexConnection.ApiKey`. On Windows, set `ExecutablePath` to the native `codex.exe` if automatic discovery fails.

```shell
dotnet add package AgentFrameworkToolkit.Codex
```

## Run and resume

```csharp
using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable AFT999 // Experimental Codex provider.

CodexAgentFactory factory = new();
await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
{
    WorkingDirectory = @"C:\projects\my-app",
    Instructions = "Complete the task and summarize the result."
});

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync("Create artifacts/report.md", session);
Console.WriteLine(response);

string threadId = session.GetThreadId()!;
AgentSession resumed = await agent.CreateSessionAsync(threadId);
await agent.RunAsync("Add an executive summary", resumed);
```

`WorkingDirectory` is required. Codex stores conversation history; send only new input. Resuming requires the original Codex storage and credentials.

Use `RunStreamingAsync` for incremental responses, `OnRunEvent` for typed activity, and `response.GetFileChanges()` for reported file edits. Shell-created files may not appear in that list.

## Approvals and questions

Defaults are `ApprovalPolicy.OnRequest` and `Permissions.WorkspaceWrite`, matching Codex's Auto preset. User questions are disabled unless `OnUserInputRequested` is supplied.

With `ApprovalPolicy.OnRequest`, handle approvals through standard Agent Framework content:

```csharp
while (response.Messages.SelectMany(message => message.Contents)
    .OfType<ToolApprovalRequestContent>().ToList() is { Count: > 0 } approvals)
{
    // Apply your application's approval decision to each request.
    ChatMessage decision = new(ChatRole.User,
        [.. approvals.Select(request => request.CreateResponse(approved: true))]);
    response = await agent.RunAsync(decision, session);
}
```

Pending approvals require the same agent and session instance and cannot be serialized.

To allow structured questions, supply `OnUserInputRequested`. It receives a `CodexUserInputRequest`; return `request.AnswerQuestions(...)` with answers keyed by question ID. Handlers should respect cancellation and provide their own interaction timeout. Without a handler, requests fail immediately; ordinary response text may still contain questions.

## Tools and configuration

Register C# functions in `Tools`; wrap them in `ApprovalRequiredAIFunction` to require approval. `ToolCallingMiddleware` and `RawToolCallDetails` apply to these functions. They execute in your .NET process, outside Codex permissions. Re-supply them when resuming with a new agent.

`CodexConnection` configures the runtime and authentication. Agent options configure threads; native overrides are available through `Config`, `ThreadStartParameters`, `ThreadResumeParameters`, `TurnStartParameters`, and `CodexAgentRunOptions.Parameters`.

Each factory-created agent owns its process. Cancelling or abandoning an active stream closes its client; create a new agent and resume the saved thread to continue.

Register with `services.AddCodexAgentFactory()`.
