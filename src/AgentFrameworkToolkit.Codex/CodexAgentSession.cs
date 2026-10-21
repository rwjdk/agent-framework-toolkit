using JetBrains.Annotations;
using Microsoft.Agents.AI;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A reference to a native Codex thread stored by the Codex runtime.</summary>
[PublicAPI]
public sealed class CodexAgentSession : AgentSession
{
    /// <summary>Creates a session whose thread is allocated on its first run.</summary>
    public CodexAgentSession()
    {
    }

    internal CodexAgentSession(AgentSessionStateBag stateBag) : base(stateBag)
    {
    }

    /// <summary>The native thread identifier, populated after the first run.</summary>
    public string? ThreadId { get; set; }
}
