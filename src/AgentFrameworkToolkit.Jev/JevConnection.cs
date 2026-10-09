using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Jev;

/// <summary>Configures authentication and HTTP transport for the Jev Decisions API.</summary>
[PublicAPI]
public sealed class JevConnection
{
    private static readonly HttpClient SharedHttpClient = new();

    /// <summary>Initializes connection configuration.</summary>
    public JevConnection()
    {
    }

    /// <summary>Initializes a connection with an API key.</summary>
    /// <param name="apiKey">The TypeSafe AI API key.</param>
    [SetsRequiredMembers]
    public JevConnection(string apiKey)
    {
        ApiKey = apiKey;
    }

    /// <summary>Gets or sets the TypeSafe AI API key.</summary>
    public required string ApiKey { get; set; }

    /// <summary>Gets or sets the complete Jev decision endpoint.</summary>
    public Uri Endpoint { get; set; } = new("https://api.typesafe.ai/v1/systemone");

    /// <summary>Gets or sets a factory supplying a caller-owned HTTP client. The factory does not dispose it.</summary>
    public Func<HttpClient> HttpClientFactory { get; set; } = () => SharedHttpClient;

    /// <summary>Gets or sets an optional timeout for each decision request.</summary>
    public TimeSpan? NetworkTimeout { get; set; }
}
