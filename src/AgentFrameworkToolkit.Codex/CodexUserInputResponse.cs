using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Answers created by the corresponding CodexUserInputRequest.</summary>
[PublicAPI]
public sealed class CodexUserInputResponse
{
    internal CodexUserInputResponse(CodexUserInputRequest request, CodexServerResponse response)
    {
        Request = request;
        ServerResponse = response;
    }

    internal CodexUserInputRequest Request { get; }
    internal CodexServerResponse ServerResponse { get; }
}
