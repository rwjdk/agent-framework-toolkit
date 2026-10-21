using JetBrains.Annotations;
using System.Text.Json;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Questions Codex needs the application to answer during a run.</summary>
[PublicAPI]
public sealed class CodexUserInputRequest
{
    private readonly CodexServerRequest _request;

    internal CodexUserInputRequest(CodexServerRequest request)
    {
        if (request.Kind != CodexServerRequestKind.UserInput)
        {
            throw new ArgumentException("A user-input request is required.", nameof(request));
        }
        _request = request;
    }

    /// <summary>The native thread identifier, when supplied.</summary>
    public string? ThreadId => _request.ThreadId;

    /// <summary>The native turn identifier, when supplied.</summary>
    public string? TurnId => _request.TurnId;

    /// <summary>The native item identifier, when supplied.</summary>
    public string? ItemId => _request.ItemId;

    /// <summary>The questions to answer.</summary>
    public IReadOnlyList<CodexUserInputQuestion> Questions => _request.Questions;

    /// <summary>The full native question request parameters.</summary>
    public JsonElement RawRepresentation => _request.RawRepresentation;

    /// <summary>Answers every question using its identifier and one or more answers.</summary>
    /// <param name="answers">Answers keyed by question identifier.</param>
    /// <returns>A response to this user-input request.</returns>
    public CodexUserInputResponse AnswerQuestions(IReadOnlyDictionary<string, string[]> answers)
    {
        return new(this, _request.AnswerQuestions(answers));
    }

    internal CodexServerResponse GetServerResponse(CodexUserInputResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!ReferenceEquals(response.Request, this))
        {
            throw new ArgumentException("The response must be created from the current user-input request.", nameof(response));
        }
        return response.ServerResponse;
    }
}
