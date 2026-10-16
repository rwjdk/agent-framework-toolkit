using System.Diagnostics.CodeAnalysis;
using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;

namespace AgentFrameworkToolkit.XAI;

/// <summary>
/// Represents a connection for XAI
/// </summary>
[PublicAPI]
public class XAIConnection : OpenAIConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public XAIConnection()
    {
        Endpoint = DefaultEndpoint;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">The API Key to be used</param>
    [SetsRequiredMembers]
    public XAIConnection(string apiKey) : base(apiKey)
    {
        Endpoint = DefaultEndpoint;
    }
    /// <summary>
    /// The Default XAI Endpoint
    /// </summary>
    public const string DefaultEndpoint = "https://api.x.ai/v1";
}
