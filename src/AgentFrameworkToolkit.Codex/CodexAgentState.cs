using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The reported state of an agent targeted by a collaboration tool call.</summary>
[PublicAPI]
public sealed class CodexAgentState
{
    /// <summary>The collaborating agent's thread identifier.</summary>
    public required string ThreadId { get; init; }
    /// <summary>The native agent status. A completed tool call does not imply the agent has completed its task.</summary>
    public required string Status { get; init; }
    /// <summary>The agent's reported message or outcome, when supplied.</summary>
    public string? Message { get; init; }
}
