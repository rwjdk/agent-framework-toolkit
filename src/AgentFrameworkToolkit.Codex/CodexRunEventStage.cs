using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The lifecycle stage of an execution progress notification.</summary>
[PublicAPI]
public enum CodexRunEventStage
{
    /// <summary>The item has started.</summary>
    Started,
    /// <summary>Incremental text or output is available.</summary>
    Delta,
    /// <summary>The item has finished; inspect Status for success or failure.</summary>
    Completed
}
