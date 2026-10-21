using JetBrains.Annotations;
using Microsoft.Agents.AI;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Extensions for Codex agent sessions.</summary>
[PublicAPI]
public static class AgentSessionExtensions
{
    /// <summary>Gets the native Codex thread identifier, or null before the thread is created.</summary>
    /// <param name="session">The Codex agent session.</param>
    /// <returns>The thread identifier, populated when Codex creates or resumes the thread.</returns>
    /// <exception cref="ArgumentNullException">The session is null.</exception>
    /// <exception cref="ArgumentException">The session is not a Codex session.</exception>
    public static string? GetThreadId(this AgentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session is not CodexAgentSession codexSession)
        {
            throw new ArgumentException("The session must be a CodexAgentSession.", nameof(session));
        }
        return codexSession.ThreadId;
    }
}
