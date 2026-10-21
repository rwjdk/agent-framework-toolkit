using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The reasoning effort requested from a supporting Codex model.</summary>
[PublicAPI]
public enum CodexReasoningEffort
{
    /// <summary>No reasoning.</summary>
    None,

    /// <summary>Minimal reasoning.</summary>
    Minimal,

    /// <summary>Low reasoning effort.</summary>
    Low,

    /// <summary>Medium reasoning effort.</summary>
    Medium,

    /// <summary>High reasoning effort.</summary>
    High,

    /// <summary>Extra high reasoning effort.</summary>
    ExtraHigh,

    /// <summary>Maximum reasoning effort.</summary>
    Max
}
