using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The requested processing tier, subject to model and account availability.</summary>
[PublicAPI]
public enum CodexServiceTier
{
    /// <summary>Normal processing.</summary>
    Normal,

    /// <summary>Fast processing, mapped to priority by Codex.</summary>
    Fast,

    /// <summary>Access-controlled ultrafast processing.</summary>
    Ultrafast
}
