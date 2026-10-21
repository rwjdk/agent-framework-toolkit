using JetBrains.Annotations;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Authentication and runtime configuration for creating Codex clients.</summary>
[PublicAPI]
public class CodexConnection
{
    /// <summary>Creates a connection using the existing Codex installation and login.</summary>
    public CodexConnection()
    {
    }

    /// <summary>The Codex executable or its absolute path. Defaults to codex on PATH.</summary>
    public string ExecutablePath { get; set; } = "codex";

    /// <summary>Arguments placed before app-server, such as CLI configuration overrides.</summary>
    public IList<string> Arguments { get; set; } = [];

    /// <summary>An optional API key. When absent, Codex uses its configured authentication.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The Responses API base URL used when ApiKey is supplied.</summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>An optional CODEX_HOME directory for credentials, configuration, and thread storage.</summary>
    public string? CodexHome { get; set; }

    /// <summary>Environment overrides applied only to the child process.</summary>
    public IDictionary<string, string?> Environment { get; set; } = new Dictionary<string, string?>();

    /// <summary>The timeout for an individual protocol request, excluding the duration of a running turn.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The client name sent during protocol initialization.</summary>
    public string ClientName { get; set; } = "agent_framework_toolkit";

    /// <summary>Additional initialization parameters, including protocol capabilities.</summary>
    public JsonObject InitializeParameters { get; set; } = [];

    /// <summary>Observes stderr lines. This callback must not throw.</summary>
    public Action<string>? OnStandardError { get; set; }

    /// <summary>Creates a new client. The caller owns and must asynchronously dispose it.</summary>
    /// <returns>A client that starts the runtime on first use.</returns>
    internal CodexClient GetClient()
    {
        return new CodexClient(this);
    }
}
