using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The category of native Codex activity.</summary>
[PublicAPI]
public enum CodexRunEventKind
{
    /// <summary>A run has started or finished.</summary>
    Run,
    /// <summary>A shell command.</summary>
    Command,
    /// <summary>A proposed or completed file edit.</summary>
    FileChange,
    /// <summary>Reasoning text or a reasoning summary exposed by the runtime.</summary>
    Reasoning,
    /// <summary>An assistant message, including commentary.</summary>
    Message,
    /// <summary>A plan.</summary>
    Plan,
    /// <summary>A native MCP, dynamic, or collaborating-agent tool call.</summary>
    ToolCall,
    /// <summary>A web search.</summary>
    WebSearch,
    /// <summary>Context compaction.</summary>
    Compaction,
    /// <summary>An execution error, including whether Codex will retry.</summary>
    Error
}
