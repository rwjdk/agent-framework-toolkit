using JetBrains.Annotations;
using System.Text.Json;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A supported execution event with a readable summary and typed activity details. Completion does not necessarily mean success.</summary>
[PublicAPI]
public sealed class CodexRunEvent
{
    /// <summary>The activity category.</summary>
    public required CodexRunEventKind Kind { get; init; }
    /// <summary>A readable description of the activity, including its outcome when available.</summary>
    public required string Summary { get; init; }
    /// <summary>The native notification method.</summary>
    public required string Method { get; init; }
    /// <summary>The activity lifecycle stage, or null for notifications without a mapped stage.</summary>
    public CodexRunEventStage? Stage { get; init; }
    /// <summary>The native thread identifier, when supplied.</summary>
    public string? ThreadId { get; init; }
    /// <summary>The native turn identifier, when supplied.</summary>
    public string? TurnId { get; init; }
    /// <summary>The item identifier, shared by its start, delta, and completion events, when supplied.</summary>
    public string? ItemId { get; init; }
    /// <summary>The native item status, when supplied, such as completed, failed, or declined.</summary>
    public string? Status { get; init; }
    /// <summary>The incremental text for a delta, or the complete message or command output for a lifecycle event. Do not concatenate complete snapshots with deltas.</summary>
    public string? Text { get; init; }
    /// <summary>Additional diagnostic details, when supplied.</summary>
    public string? Details { get; init; }
    /// <summary>The error message for failed activities, when supplied.</summary>
    public string? Error { get; init; }
    /// <summary>Whether Codex will retry after an error, when supplied.</summary>
    public bool? WillRetry { get; init; }
    /// <summary>The shell command, when supplied.</summary>
    public string? Command { get; init; }
    /// <summary>The command working directory, when supplied.</summary>
    public string? WorkingDirectory { get; init; }
    /// <summary>The command exit code, when supplied.</summary>
    public int? ExitCode { get; init; }
    /// <summary>The operation duration, when supplied.</summary>
    public TimeSpan? Duration { get; init; }
    /// <summary>The tool name, when supplied.</summary>
    public string? ToolName { get; init; }
    /// <summary>The MCP server executing the tool, when supplied.</summary>
    public string? ToolServer { get; init; }
    /// <summary>The tool arguments, when supplied.</summary>
    public JsonElement? ToolArguments { get; init; }
    /// <summary>The complete native tool result, when supplied.</summary>
    public JsonElement? ToolResult { get; init; }
    /// <summary>The task or message sent to a collaborating agent, when supplied.</summary>
    public string? AgentTask { get; init; }
    /// <summary>The receiving agents' thread identifiers for a collaboration tool call.</summary>
    public IReadOnlyList<string> ReceivingThreadIds { get; init; } = [];
    /// <summary>The reported states and messages of collaborating agents. These may still indicate work in progress.</summary>
    public IReadOnlyList<CodexAgentState> AgentStates { get; init; } = [];
    /// <summary>The web search query, when supplied.</summary>
    public string? Query { get; init; }
    /// <summary>The assistant message phase, when supplied, including commentary or final_answer.</summary>
    public string? MessagePhase { get; init; }
    /// <summary>Proposed edits for started items or final edits for completed items. Check Status before treating edits as applied.</summary>
    public IReadOnlyList<CodexFileChange> FileChanges { get; init; } = [];
    /// <summary>The full native notification, preserving fields not mapped by this provider.</summary>
    public required JsonElement RawRepresentation { get; init; }

    internal static CodexRunEvent? FromEvent(JsonElement message)
    {
        string method = message.GetProperty("method").GetString()!;
        message.TryGetProperty("params", out JsonElement parameters);
        if (parameters.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        bool lifecycle = method is "item/started" or "item/completed";
        CodexRunEventKind? kind;
        CodexRunEventStage? stage = null;
        JsonElement item = parameters;
        if (lifecycle)
        {
            item = parameters.GetProperty("item");
            kind = GetString(item, "type") switch
            {
                "commandExecution" => CodexRunEventKind.Command,
                "fileChange" => CodexRunEventKind.FileChange,
                "reasoning" => CodexRunEventKind.Reasoning,
                "agentMessage" => CodexRunEventKind.Message,
                "plan" => CodexRunEventKind.Plan,
                "mcpToolCall" or "dynamicToolCall" or "collabAgentToolCall" => CodexRunEventKind.ToolCall,
                "webSearch" => CodexRunEventKind.WebSearch,
                "contextCompaction" => CodexRunEventKind.Compaction,
                _ => null
            };
            stage = method == "item/started" ? CodexRunEventStage.Started : CodexRunEventStage.Completed;
        }
        else
        {
            kind = method switch
            {
                "item/commandExecution/outputDelta" => CodexRunEventKind.Command,
                "item/fileChange/outputDelta" => CodexRunEventKind.FileChange,
                "item/reasoning/summaryTextDelta" or "item/reasoning/textDelta" => CodexRunEventKind.Reasoning,
                "item/agentMessage/delta" => CodexRunEventKind.Message,
                "item/plan/delta" or "turn/plan/updated" => CodexRunEventKind.Plan,
                "item/mcpToolCall/progress" => CodexRunEventKind.ToolCall,
                "turn/started" or "turn/completed" => CodexRunEventKind.Run,
                "error" => CodexRunEventKind.Error,
                _ => null
            };
            if (kind == CodexRunEventKind.Run)
            {
                item = parameters.GetProperty("turn");
                stage = method == "turn/started" ? CodexRunEventStage.Started : CodexRunEventStage.Completed;
            }
            else if (kind is not (null or CodexRunEventKind.Error) && method != "turn/plan/updated")
            {
                stage = CodexRunEventStage.Delta;
            }
        }
        if (kind is null)
        {
            return null;
        }
        string? status = GetString(item, "status");
        string? error = item.TryGetProperty("error", out JsonElement errorValue)
            ? GetString(errorValue, "message") ?? (errorValue.ValueKind == JsonValueKind.String ? errorValue.GetString() : null)
            : null;
        bool? willRetry = parameters.TryGetProperty("willRetry", out JsonElement retry) && retry.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? retry.GetBoolean() : null;
        string? command = GetString(item, "command");
        string? tool = GetString(item, "tool");
        string? query = GetString(item, "query");
        string? text = stage == CodexRunEventStage.Delta ? GetString(item, "delta")
            : GetString(item, "text") ?? GetString(item, "aggregatedOutput");
        if (method == "turn/plan/updated")
        {
            List<string> steps = [];
            if (GetString(item, "explanation") is { Length: > 0 } explanation)
            {
                steps.Add(explanation);
            }
            foreach (JsonElement step in item.GetProperty("plan").EnumerateArray())
            {
                steps.Add($"{GetString(step, "status")}: {GetString(step, "step")}");
            }
            text = string.Join(Environment.NewLine, steps);
        }
        JsonElement? toolResult = null;
        string? agentTask = null;
        List<string> receivingThreadIds = [];
        List<CodexAgentState> agentStates = [];
        if (kind == CodexRunEventKind.Reasoning && lifecycle)
        {
            text = ReadTextArray(item, "summary") ?? ReadTextArray(item, "content");
        }
        if (kind == CodexRunEventKind.ToolCall)
        {
            text ??= GetString(item, "message");
            toolResult = GetValue(item, "result") ?? GetValue(item, "contentItems") ?? GetValue(item, "agentsStates");
            if (toolResult is { } result)
            {
                text = result.ValueKind == JsonValueKind.Object ? ReadTextArray(result, "content") : ReadTextArray(result);
            }
        }
        if (GetString(item, "type") == "collabAgentToolCall")
        {
            agentTask = GetString(item, "prompt");
            if (GetValue(item, "receiverThreadIds") is { ValueKind: JsonValueKind.Array } receivers)
            {
                foreach (JsonElement receiver in receivers.EnumerateArray())
                {
                    if (receiver.ValueKind == JsonValueKind.String && receiver.GetString() is { } threadId)
                    {
                        receivingThreadIds.Add(threadId);
                    }
                }
            }
            List<string> descriptions = [];
            if (!string.IsNullOrWhiteSpace(agentTask))
            {
                descriptions.Add("Task: " + agentTask);
            }
            if (receivingThreadIds.Count > 0)
            {
                descriptions.Add("Receiving agents: " + string.Join(", ", receivingThreadIds));
            }
            if (GetValue(item, "agentsStates") is { ValueKind: JsonValueKind.Object } states)
            {
                foreach (JsonProperty state in states.EnumerateObject())
                {
                    CodexAgentState agent = new()
                    {
                        ThreadId = state.Name,
                        Status = GetString(state.Value, "status") ?? "Status not reported",
                        Message = GetString(state.Value, "message")
                    };
                    agentStates.Add(agent);
                    descriptions.Add($"Agent {agent.ThreadId}: {agent.Status}" +
                        (string.IsNullOrWhiteSpace(agent.Message) ? "" : $" — {agent.Message}"));
                }
            }
            text = descriptions.Count == 0 ? null : string.Join(Environment.NewLine, descriptions);
        }
        if ((stage == CodexRunEventStage.Delta || kind is CodexRunEventKind.Message or CodexRunEventKind.Reasoning or CodexRunEventKind.Plan) &&
            string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        IReadOnlyList<CodexFileChange> fileChanges = kind == CodexRunEventKind.FileChange && lifecycle ? CodexFileChange.FromItem(item) : [];
        int? code = item.TryGetProperty("exitCode", out JsonElement exitCode) && exitCode.ValueKind == JsonValueKind.Number && exitCode.TryGetInt32(out int number) ? number : null;
        string action = stage == CodexRunEventStage.Started ? "started" : status ?? "completed";
        string summary = kind switch
        {
            CodexRunEventKind.Run => error is null ? $"Run {action}." : $"Run {action}: {error}",
            CodexRunEventKind.Command => stage == CodexRunEventStage.Delta ? text! :
                $"Command {action}: {command ?? "shell command"}" + (code is null ? "" : $" (exit code {code})"),
            CodexRunEventKind.FileChange => stage == CodexRunEventStage.Delta ? text! :
                $"File edits {action}: " + string.Join(", ", fileChanges.Select(change => $"{change.Kind}: {change.Path}" + (change.MovePath is null ? "" : $" -> {change.MovePath}"))),
            CodexRunEventKind.ToolCall => stage == CodexRunEventStage.Delta ? text! : $"Tool {tool ?? "call"} {action}." + (error is null ? "" : $" {error}"),
            CodexRunEventKind.WebSearch => $"Web search {action}: {query ?? "search query unavailable"}",
            CodexRunEventKind.Compaction => stage == CodexRunEventStage.Started ? "Compacting conversation history." : "Conversation history compacted.",
            CodexRunEventKind.Error => (error ?? "Codex reported an execution error.") + (willRetry == true ? " Retrying." : ""),
            _ => text!
        };
        if (GetString(item, "type") == "collabAgentToolCall" && !string.IsNullOrWhiteSpace(text))
        {
            summary += Environment.NewLine + text;
        }
        return new CodexRunEvent
        {
            Kind = kind.Value,
            Summary = summary,
            Method = method,
            Stage = stage,
            ThreadId = GetString(parameters, "threadId"),
            TurnId = GetString(parameters, "turnId") ?? (kind == CodexRunEventKind.Run ? GetString(item, "id") : null),
            ItemId = GetString(item, lifecycle ? "id" : "itemId"),
            Status = status,
            Text = text,
            Details = GetString(item, "details") ?? GetString(errorValue, "additionalDetails"),
            Error = error,
            WillRetry = willRetry,
            Command = command,
            WorkingDirectory = GetString(item, "cwd"),
            ExitCode = code,
            Duration = item.TryGetProperty("durationMs", out JsonElement duration) && duration.ValueKind == JsonValueKind.Number && duration.TryGetInt64(out long milliseconds) ? TimeSpan.FromMilliseconds(milliseconds) : null,
            ToolName = tool,
            ToolServer = GetString(item, "server"),
            ToolArguments = kind == CodexRunEventKind.ToolCall ? GetValue(item, "arguments") : null,
            ToolResult = toolResult,
            AgentTask = agentTask,
            ReceivingThreadIds = receivingThreadIds.AsReadOnly(),
            AgentStates = agentStates.AsReadOnly(),
            Query = query,
            MessagePhase = GetString(item, "phase"),
            FileChanges = fileChanges,
            RawRepresentation = message.Clone()
        };
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static JsonElement? GetValue(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? value.Clone() : null;
    }

    private static string? ReadTextArray(JsonElement element, string name)
    {
        return GetValue(element, name) is { } value ? ReadTextArray(value) : null;
    }

    private static string? ReadTextArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }
        List<string> parts = [];
        foreach (JsonElement value in element.EnumerateArray())
        {
            string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : GetString(value, "text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(text);
            }
        }
        return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
    }
}
