using JetBrains.Annotations;
using Microsoft.Agents.AI;
using System.Text.Json.Nodes;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Native turn overrides for an individual Codex run.</summary>
[PublicAPI]
public class CodexAgentRunOptions : AgentRunOptions
{
    /// <summary>Native turn/start parameters overriding the agent defaults. The session's thread ID is always preserved.</summary>
    public JsonObject Parameters { get; set; } = [];
}
