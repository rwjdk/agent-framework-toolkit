using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Codex;

/// <summary>Access to structured Codex response metadata.</summary>
[PublicAPI]
public static class CodexAgentResponseExtensions
{
    internal const string FileChangesKey = "AgentFrameworkToolkit.Codex.FileChanges";

    /// <summary>Gets completed file changes reported during this response, in event order. Shell changes may not be reported.</summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns>The reported changes, or an empty list.</returns>
    public static IReadOnlyList<CodexFileChange> GetFileChanges(this AgentResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return GetFileChanges(response.AdditionalProperties);
    }

    /// <summary>Gets the completed file changes attached to this update. The final update contains the full turn's list.</summary>
    /// <param name="update">The streaming update to inspect.</param>
    /// <returns>The reported changes, or an empty list.</returns>
    public static IReadOnlyList<CodexFileChange> GetFileChanges(this AgentResponseUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return GetFileChanges(update.AdditionalProperties);
    }

    private static IReadOnlyList<CodexFileChange> GetFileChanges(AdditionalPropertiesDictionary? properties)
    {
        return properties?.TryGetValue(FileChangesKey, out object? value) == true && value is IReadOnlyList<CodexFileChange> changes
            ? changes
            : [];
    }
}
