using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The requested reasoning summary mode.</summary>
[PublicAPI]
public enum CodexReasoningSummary
{
    /// <summary>Automatically select the summary format.</summary>
    Auto,

    /// <summary>Provide a concise summary.</summary>
    Concise,

    /// <summary>Provide a detailed summary.</summary>
    Detailed,

    /// <summary>Disable reasoning summaries.</summary>
    None
}
