using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A native app-server request with helpers for approvals, questions, and protocol results. Agent callbacks use CodexUserInputRequest instead.</summary>
internal sealed class CodexServerRequest
{
    internal CodexServerRequest(string method, JsonElement parameters)
    {
        Method = method;
        RawRepresentation = parameters.Clone();
        Kind = method switch
        {
            "item/commandExecution/requestApproval" => CodexServerRequestKind.CommandApproval,
            "item/fileChange/requestApproval" => CodexServerRequestKind.FileChangeApproval,
            "item/tool/requestUserInput" => CodexServerRequestKind.UserInput,
            _ => CodexServerRequestKind.Other
        };
        ThreadId = ReadString(parameters, "threadId");
        TurnId = ReadString(parameters, "turnId");
        ItemId = ReadString(parameters, "itemId");
        Command = ReadString(parameters, "command");
        WorkingDirectory = ReadString(parameters, "cwd");
        Reason = ReadString(parameters, "reason");
        RequestedWriteRoot = ReadString(parameters, "grantRoot");
        List<CodexUserInputQuestion> questions = [];
        if (Kind == CodexServerRequestKind.UserInput && parameters.TryGetProperty("questions", out JsonElement items))
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
    public CodexServerRequestKind Kind { get; }
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
    /// <summary>The questions to answer for a user-input request.</summary>
    public IReadOnlyList<CodexUserInputQuestion> Questions { get; }
    /// <summary>The full native request parameters, including fields without typed mappings.</summary>
    public JsonElement RawRepresentation { get; }

    /// <summary>Approves this command or file-edit request once.</summary>
    /// <returns>The approval response.</returns>
    public CodexServerResponse Approve() => Approval("accept");

    /// <summary>Declines this command or file-edit request.</summary>
    /// <returns>The decline response.</returns>
    public CodexServerResponse Decline() => Approval("decline");

    /// <summary>Cancels this command or file-edit approval request and its turn.</summary>
    /// <returns>The cancellation response.</returns>
    public CodexServerResponse Cancel() => Approval("cancel");

    /// <summary>Answers a user-input request using question identifiers and one or more answers per question.</summary>
    /// <param name="answers">Answers keyed by question identifier.</param>
    /// <returns>The user-input response.</returns>
    public CodexServerResponse AnswerQuestions(IReadOnlyDictionary<string, string[]> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        RequireKind(CodexServerRequestKind.UserInput);
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

    /// <summary>Returns a native result for advanced requests not covered by the typed helpers.</summary>
    /// <param name="result">The result object required by the native protocol.</param>
    /// <returns>The native response.</returns>
    public CodexServerResponse RespondWithNativeResult(JsonNode? result) => new(this, result);

    internal JsonNode? GetResult(CodexServerResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!ReferenceEquals(response.Request, this))
        {
            throw new ArgumentException("The response must be created from the current request.", nameof(response));
        }
        return response.Result?.DeepClone();
    }

    private CodexServerResponse Approval(string decision)
    {
        if (Kind is not (CodexServerRequestKind.CommandApproval or CodexServerRequestKind.FileChangeApproval))
        {
            throw new InvalidOperationException("Approve, Decline, and Cancel are only valid for command and file-edit approvals.");
        }
        return RespondWithNativeResult(new JsonObject { ["decision"] = decision });
    }

    private void RequireKind(CodexServerRequestKind expected)
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
