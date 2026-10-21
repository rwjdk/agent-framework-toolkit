using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A question Codex needs the application to answer.</summary>
[PublicAPI]
public sealed class CodexUserInputQuestion
{
    /// <summary>The identifier used when supplying the answer.</summary>
    public required string Id { get; init; }
    /// <summary>The question text.</summary>
    public required string Question { get; init; }
    /// <summary>Whether the answer is sensitive and should be collected privately.</summary>
    public bool IsSecret { get; init; }
    /// <summary>The suggested answer labels, if any. Full option details are available in the request's RawRepresentation.</summary>
    public IReadOnlyList<string> Options { get; init; } = [];
}
