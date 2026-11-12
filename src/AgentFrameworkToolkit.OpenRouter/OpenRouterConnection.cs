using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace AgentFrameworkToolkit.OpenRouter;

/// <summary>
/// Represents a connection for OpenRouter
/// </summary>
[PublicAPI]
public class OpenRouterConnection : OpenAIConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public OpenRouterConnection()
    {
        Endpoint = DefaultEndpoint;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">The API Key to be used</param>
    [SetsRequiredMembers]
    public OpenRouterConnection(string apiKey) : base(apiKey)
    {
        Endpoint = DefaultEndpoint;
    }

    /// <summary>
    /// The Default OpenRouter Endpoint
    /// </summary>
    public const string DefaultEndpoint = "https://openrouter.ai/api/v1";
}
