using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A response created using the methods on the corresponding CodexServerRequest.</summary>
internal sealed class CodexServerResponse
{
    internal CodexServerResponse(CodexServerRequest request, JsonNode? result)
    {
        Request = request;
        Result = result?.DeepClone();
    }

    internal CodexServerRequest Request { get; }
    internal JsonNode? Result { get; }
}
