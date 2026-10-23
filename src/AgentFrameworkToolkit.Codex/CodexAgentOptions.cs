using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Configuration for a Codex-backed agent and its native threads and turns.</summary>
[PublicAPI]
public class CodexAgentOptions
{
    /// <summary>The agent identifier.</summary>
    public string? Id { get; set; }

    /// <summary>The agent name.</summary>
    public string? Name { get; set; }

    /// <summary>The agent description.</summary>
    public string? Description { get; set; }

    /// <summary>The model, or null for the configured Codex default.</summary>
    public string? Model { get; set; }

    /// <summary>The required working directory used for new and resumed threads.</summary>
    public required string WorkingDirectory { get; set; }

    /// <summary>Whether new sessions are persisted by Codex. Defaults to true. When false, sessions remain in memory and support follow-up runs only while the same Codex app-server is running.</summary>
    public bool PersistSessions { get; set; } = true;

    /// <summary>Additional developer instructions.</summary>
    public string? Instructions { get; set; }

    /// <summary>C# functions exposed as native Codex dynamic tools. Wrap a function in ApprovalRequiredAIFunction to require Agent Framework approval before execution. Re-supply the functions when resuming a thread with a new agent.</summary>
    public IList<AIFunction>? Tools { get; set; }

    /// <summary>Optional services supplied to registered function invocations.</summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>Inspects, changes, or short-circuits registered C# function invocations. Context.Messages contains the current run's input, not Codex's stored history. Native tools are not intercepted, and Context.Terminate does not control Codex's native turn loop.</summary>
    public MiddlewareDelegates.ToolCallingMiddlewareDelegate? ToolCallingMiddleware { get; set; }

    /// <summary>Observes registered C# function calls after the tool middleware returns successfully, including short-circuited calls.</summary>
    public Action<ToolCallingDetails>? RawToolCallDetails { get; set; }

    /// <summary>The model provider configured in Codex.</summary>
    public string? ModelProvider { get; set; }

    /// <summary>The service tier, or null for the runtime default.</summary>
    public CodexServiceTier? ServiceTier { get; set; }

    /// <summary>The reasoning effort for each turn.</summary>
    public CodexReasoningEffort? ReasoningEffort { get; set; }

    /// <summary>The reasoning summary mode for each turn.</summary>
    public CodexReasoningSummary? ReasoningSummary { get; set; }

    /// <summary>The native approval policy. Defaults to requesting approval when needed. Requested command/file approvals are returned as ToolApprovalRequestContent and continued with a ToolApprovalResponseContent using the same agent and session.</summary>
    public CodexApprovalPolicy? ApprovalPolicy { get; set; } = CodexApprovalPolicy.OnRequest;

    /// <summary>The thread's execution permissions. Defaults to workspace writes.</summary>
    public CodexPermissions? Permissions { get; set; } = CodexPermissions.WorkspaceWrite;

    /// <summary>Structured Codex configuration overrides for new and resumed threads.</summary>
    public JsonObject? Config { get; set; }

    /// <summary>A JSON Schema constraining the final response.</summary>
    public JsonNode? OutputSchema { get; set; }

    /// <summary>Additional thread/start parameters. Entries override the corresponding mapped settings.</summary>
    public JsonObject ThreadStartParameters { get; set; } = [];

    /// <summary>Additional thread/resume parameters. Entries override the corresponding mapped settings.</summary>
    public JsonObject ThreadResumeParameters { get; set; } = [];

    /// <summary>Additional turn/start parameters, including native input items and dynamic tool output.</summary>
    public JsonObject TurnStartParameters { get; set; } = [];

    /// <summary>Observes supported run activity, including commands, tools, file edits, reasoning, messages, plans, compaction, and errors. Internal status notifications, warnings, and unmapped events are excluded. Streaming updates retain native notifications for diagnostics.</summary>
    public Action<CodexRunEvent>? OnRunEvent { get; set; }

    /// <summary>Enables and answers user questions. When null, questions are disabled and unexpected requests are rejected immediately. Command and file approvals use Agent Framework ToolApprovalRequestContent and ToolApprovalResponseContent through RunAsync instead. Registered C# tool calls are handled separately.</summary>
    public Func<CodexUserInputRequest, CancellationToken, Task<CodexUserInputResponse>>? OnUserInputRequested { get; set; }
}
