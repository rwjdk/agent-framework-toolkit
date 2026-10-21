using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>The operation reported for a Codex file edit.</summary>
[PublicAPI]
public enum CodexFileChangeKind
{
    /// <summary>An operation not recognized by this version of the provider.</summary>
    Unknown,
    /// <summary>A file was created.</summary>
    Created,
    /// <summary>A file was modified, possibly moved to another path.</summary>
    Modified,
    /// <summary>A file was deleted.</summary>
    Deleted
}
