# Codex

Experimental while we stabilize the implementation: APIs and behavior may change. Usage reports compiler diagnostic `AFT999`; the examples below explicitly suppress it.

Control a local Codex runtime from C# through Microsoft's `AIAgent` API. This package implements the documented Codex app-server protocol directly; it does not depend on a TypeScript/Python SDK or claim to be an official OpenAI .NET SDK.

Install the Codex CLI separately and authenticate with `codex login`, or supply an API key through `CodexConnection.ApiKey`. The protocol implementation targets Codex CLI `0.163.0`. On Windows, set `connection.ExecutablePath` to the native `codex.exe` if the package cannot discover it; PowerShell and command wrappers are not launched.

```shell
dotnet add package AgentFrameworkToolkit.Codex
```

```csharp
using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;

#pragma warning disable AFT999 // Codex support is experimental while the implementation stabilizes.

CodexConnection connection = new();
CodexAgentFactory factory = new(connection);
await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
{
    WorkingDirectory = @"C:\projects\my-app",
    Instructions = "Complete the task and summarize the result."
});

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync("Create a report in artifacts/report.md", session);
Console.WriteLine(response);

string threadId = session.GetThreadId()!;
AgentSession resumed = await agent.CreateSessionAsync(threadId);
await agent.RunAsync("Add an executive summary to the report", resumed);
```

Use `RunStreamingAsync` for incremental messages. `CodexAgentOptions.OnRunEvent` reports supported execution activity with a readable `Summary` and typed details. Warnings, housekeeping, and unmapped notifications are excluded. `Method` identifies the underlying notification and `RawRepresentation` preserves its full JSON. Native notifications are also available through each streaming update's `RawRepresentation` (`JsonElement`). `response.GetFileChanges()` exposes completed file changes reported by Codex, including paths, operations, and diffs; the final streaming update provides the same list. Files remain in the workspace, and changes made through shell commands may not appear in this list. Token usage is included when reported by Codex.

Commentary is exposed through native notifications rather than answer text. Structured responses emit one final answer at turn completion, so progress messages cannot contaminate typed JSON deserialization. When message phases are absent, the last completed unlabelled message is used for structured output.

Sessions serialize their thread ID and AF state bag. Resume requires the corresponding runtime storage, login, and `CODEX_HOME` to remain available. Pass only new user input; Codex owns conversation history and its tool loop. Text and image content are supported. Background response continuation tokens are not supported.

`CodexAgentOptions.WorkingDirectory` is required. Empty or whitespace paths are rejected before startup.

The defaults are `ApprovalPolicy = CodexApprovalPolicy.Never` and `Permissions = CodexPermissions.WorkspaceWrite`; automatic execution does not bypass permission restrictions. Both are configurable. Service tier, reasoning effort, and reasoning summary also use provider-specific enums. Setting these nullable properties to null inherits the runtime configuration. `OnRunResponseRequired` receives a typed request. Use `request.Approve()`, `request.Decline()`, or `request.Cancel()` for command/file approvals, `request.AnswerQuestions(...)` for user input, and `request.CompleteTool(...)` for unregistered dynamic tool calls. `request.RespondWithNativeResult(...)` remains available for advanced native requests. Without a callback, command/file approval requests are accepted and other requests receive a protocol error instead of waiting indefinitely.

`ThreadStartParameters`, `ThreadResumeParameters`, and `TurnStartParameters` expose the complete native request surface and override mapped settings. Use `CodexAgentRunOptions.Parameters` for per-run turn overrides. The active session's thread ID is preserved. Experimental parameters require the appropriate `CodexConnection.InitializeParameters` capabilities.

Register C# functions through `Tools`; AFT enables the experimental dynamic-tool capability automatically before starting the client:

```csharp
await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
{
    WorkingDirectory = @"C:\projects\my-app",
    Tools = [Microsoft.Extensions.AI.AIFunctionFactory.Create(
        (string orderId) => new { OrderId = orderId, Status = "Shipped" },
        "get_order_status", "Gets the status of an order.")]
});

await agent.RunAsync("Check order 123.");
```

Functions are captured when the agent is created. Arguments are bound by `AIFunction`; string results are returned as text, and other results as JSON text. Function errors are returned as failed tool results so Codex can respond. `ToolCallingMiddleware` and `RawToolCallDetails` apply to registered C# functions; Codex's built-in tools execute inside the runtime. `Services` supplies optional invocation services. Cancellation is forwarded to functions that accept a `CancellationToken`. These functions execute in the calling .NET process, outside Codex's execution permissions.

Codex persists tool definitions with its thread. Supply the same functions when resuming with a new agent; delegates themselves are not serialized. Registered functions take precedence over `OnRunResponseRequired` for matching native tool calls; other requests still reach that callback. Native namespaced tools can be handled directly through the callback. If using an already-started raw client, its initialization must have enabled `capabilities.experimentalApi` before attaching an agent with tools.

For API-key authentication, construct the factory with `new CodexConnection { ApiKey = apiKey }`. `factory.Connection` exposes the full executable, environment, `CodexHome`, initialization, and timeout configuration directly. The key is passed only through the child process environment, using a process-local provider configuration; it does not replace the user's stored Codex login. Turn execution lasts until completion or caller cancellation.

Use `connection.GetClient()` to create a raw client without an agent. The caller owns and must asynchronously dispose that client.

`agent.Client.RequestAsync` exposes native operations such as `model/list`, `thread/read`, and `account/read`. Codex manages credentials for an existing local login. ChatGPT plan authentication has deployment-specific eligibility requirements; using this library does not grant a paid or hosted service access to subscription usage. Consult [OpenAI's authentication documentation](https://learn.chatgpt.com/docs/app-server).

Each factory-created agent owns its own process. A client must be dedicated to one agent's event stream. Runs on the same client are serialized. Cancelling or abandoning an active stream interrupts the turn and closes that client, so subsequent work must use a new agent and resume the saved thread ID. Disposing an owned client stops its process tree.

Register the factory using `services.AddCodexAgentFactory()` or `services.AddCodexAgentFactory(connection)`.
