using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The execution permissions applied to a Codex thread.</summary>
[PublicAPI]
public enum CodexPermissions
{
    /// <summary>Restrict execution to read-only access.</summary>
    ReadOnly,

    /// <summary>Permit workspace writes within the runtime's configured restrictions.</summary>
    WorkspaceWrite,

    /// <summary>Remove the runtime's filesystem and network isolation.</summary>
    FullAccess
}
