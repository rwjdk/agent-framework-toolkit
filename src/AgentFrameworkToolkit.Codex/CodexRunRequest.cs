using JetBrains.Annotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A request from Codex with helpers that build correctly shaped responses.</summary>
[PublicAPI]
public sealed class CodexRunRequest
{
    internal CodexRunRequest(string method, JsonElement parameters)
    {
        Method = method;
        RawRepresentation = parameters.Clone();
        Kind = method switch
        {
            "item/commandExecution/requestApproval" => CodexRunRequestKind.CommandApproval,
            "item/fileChange/requestApproval" => CodexRunRequestKind.FileChangeApproval,
            "item/tool/requestUserInput" => CodexRunRequestKind.UserInput,
            "item/tool/call" => CodexRunRequestKind.ToolCall,
            _ => CodexRunRequestKind.Other
        };
        ThreadId = ReadString(parameters, "threadId");
        TurnId = ReadString(parameters, "turnId");
        ItemId = ReadString(parameters, "itemId");
        Command = ReadString(parameters, "command");
        WorkingDirectory = ReadString(parameters, "cwd");
        Reason = ReadString(parameters, "reason");
        RequestedWriteRoot = ReadString(parameters, "grantRoot");
        ToolName = ReadString(parameters, "tool");
        ToolNamespace = ReadString(parameters, "namespace") is { Length: > 0 } toolNamespace ? toolNamespace : null;
        if (Kind == CodexRunRequestKind.ToolCall && parameters.TryGetProperty("arguments", out JsonElement arguments) &&
            arguments.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
        {
            ToolArguments = arguments.Clone();
        }
        List<CodexUserInputQuestion> questions = [];
        if (Kind == CodexRunRequestKind.UserInput && parameters.TryGetProperty("questions", out JsonElement items))
        {
            foreach (JsonElement item in items.EnumerateArray())
            {
                List<string> labels = [];
                if (item.TryGetProperty("options", out JsonElement options) && options.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement option in options.EnumerateArray())
                    {
                        labels.Add(option.GetProperty("label").GetString()!);
                    }
                }
                questions.Add(new CodexUserInputQuestion
                {
                    Id = item.GetProperty("id").GetString()!,
                    Question = item.GetProperty("question").GetString()!,
                    IsSecret = item.TryGetProperty("isSecret", out JsonElement secret) && secret.ValueKind == JsonValueKind.True,
                    Options = labels.AsReadOnly()
                });
            }
        }
        Questions = questions.AsReadOnly();
    }

    /// <summary>The kind of response needed.</summary>
    public CodexRunRequestKind Kind { get; }
    /// <summary>The native method, for advanced requests.</summary>
    public string Method { get; }
    /// <summary>The native thread identifier, when supplied.</summary>
    public string? ThreadId { get; }
    /// <summary>The native turn identifier, when supplied.</summary>
    public string? TurnId { get; }
    /// <summary>The native item identifier, when supplied.</summary>
    public string? ItemId { get; }
    /// <summary>The command requiring approval, when supplied.</summary>
    public string? Command { get; }
    /// <summary>The command working directory, when supplied.</summary>
    public string? WorkingDirectory { get; }
    /// <summary>Codex's explanation of why approval is needed, when supplied.</summary>
    public string? Reason { get; }
    /// <summary>The requested write root for file-edit approval, when supplied.</summary>
    public string? RequestedWriteRoot { get; }
    /// <summary>The dynamic tool name, when supplied.</summary>
    public string? ToolName { get; }
    /// <summary>The dynamic tool namespace, or null for an unnamespaced tool.</summary>
    public string? ToolNamespace { get; }
    /// <summary>The dynamic tool arguments, preserving their JSON shape, or null when absent. The value remains valid independently of the original request document.</summary>
    public JsonElement? ToolArguments { get; }
    /// <summary>The questions to answer for a user-input request.</summary>
    public IReadOnlyList<CodexUserInputQuestion> Questions { get; }
    /// <summary>The full native request parameters, including fields without typed mappings.</summary>
    public JsonElement RawRepresentation { get; }

    /// <summary>Approves this command or file-edit request once.</summary>
    /// <returns>The approval response.</returns>
    public CodexRunResponse Approve() => Approval("accept");

    /// <summary>Declines this command or file-edit request.</summary>
    /// <returns>The decline response.</returns>
    public CodexRunResponse Decline() => Approval("decline");

    /// <summary>Cancels this command or file-edit approval request and its turn.</summary>
    /// <returns>The cancellation response.</returns>
    public CodexRunResponse Cancel() => Approval("cancel");

    /// <summary>Answers a user-input request using question identifiers and one or more answers per question.</summary>
    /// <param name="answers">Answers keyed by question identifier.</param>
    /// <returns>The user-input response.</returns>
    public CodexRunResponse AnswerQuestions(IReadOnlyDictionary<string, string[]> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        RequireKind(CodexRunRequestKind.UserInput);
        JsonObject result = new();
        foreach (CodexUserInputQuestion question in Questions)
        {
            if (!answers.TryGetValue(question.Id, out string[]? answer) || answer is null)
            {
                throw new ArgumentException("An answer is required for question " + question.Id, nameof(answers));
            }
            result[question.Id] = new JsonObject { ["answers"] = JsonSerializer.SerializeToNode(answer) };
        }
        return RespondWithNativeResult(new JsonObject { ["answers"] = result });
    }

    /// <summary>Completes a dynamic tool call. Registered C# functions normally produce these results automatically.</summary>
    /// <param name="text">The tool output.</param>
    /// <param name="success">Whether the tool succeeded.</param>
    /// <returns>The tool response.</returns>
    public CodexRunResponse CompleteTool(string text, bool success = true)
    {
        RequireKind(CodexRunRequestKind.ToolCall);
        return RespondWithNativeResult(CodexFunctionTools.Result(text, success));
    }

    /// <summary>Returns a native result for advanced requests not covered by the typed helpers.</summary>
    /// <param name="result">The result object required by the native protocol.</param>
    /// <returns>The native response.</returns>
    public CodexRunResponse RespondWithNativeResult(JsonNode? result) => new(this, result);

    internal JsonNode? GetResult(CodexRunResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!ReferenceEquals(response.Request, this))
        {
            throw new ArgumentException("The response must be created from the current request.", nameof(response));
        }
        return response.Result?.DeepClone();
    }

    private CodexRunResponse Approval(string decision)
    {
        if (Kind is not (CodexRunRequestKind.CommandApproval or CodexRunRequestKind.FileChangeApproval))
        {
            throw new InvalidOperationException("Approve, Decline, and Cancel are only valid for command and file-edit approvals.");
        }
        return RespondWithNativeResult(new JsonObject { ["decision"] = decision });
    }

    private void RequireKind(CodexRunRequestKind expected)
    {
        if (Kind != expected)
        {
            throw new InvalidOperationException("This response requires a " + expected + " request.");
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
