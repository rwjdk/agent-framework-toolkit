using JetBrains.Annotations;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A response created using the methods on the corresponding CodexRunRequest.</summary>
[PublicAPI]
public sealed class CodexRunResponse
{
    internal CodexRunResponse(CodexRunRequest request, JsonNode? result)
    {
        Request = request;
        Result = result?.DeepClone();
    }

    internal CodexRunRequest Request { get; }
    internal JsonNode? Result { get; }
}
