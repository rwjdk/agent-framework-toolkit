using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Controls when Codex may request approval beyond its configured permissions.</summary>
[PublicAPI]
public enum CodexApprovalPolicy
{
    /// <summary>Do not prompt; actions requiring approval remain blocked.</summary>
    Never,

    /// <summary>Allow Codex to request approval when needed.</summary>
    OnRequest
}
