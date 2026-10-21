using JetBrains.Annotations;
using System.Text.Json;

namespace AgentFrameworkToolkit.Codex;

/// <summary>A file edit reported by Codex, rather than inferred from response text.</summary>
[PublicAPI]
public sealed class CodexFileChange
{
    /// <summary>The file path reported by Codex.</summary>
    public required string Path { get; init; }

    /// <summary>The operation performed on the file.</summary>
    public required CodexFileChangeKind Kind { get; init; }

    /// <summary>The native diff describing the change.</summary>
    public required string Diff { get; init; }

    /// <summary>The destination path when a modification moves the file; otherwise null.</summary>
    public string? MovePath { get; init; }

    internal static IReadOnlyList<CodexFileChange> FromItem(JsonElement item)
    {
        List<CodexFileChange> changes = [];
        foreach (JsonElement change in item.GetProperty("changes").EnumerateArray())
        {
            JsonElement kind = change.GetProperty("kind");
            changes.Add(new CodexFileChange
            {
                Path = change.GetProperty("path").GetString()!,
                Diff = change.GetProperty("diff").GetString()!,
                Kind = kind.GetProperty("type").GetString() switch
                {
                    "add" => CodexFileChangeKind.Created,
                    "update" => CodexFileChangeKind.Modified,
                    "delete" => CodexFileChangeKind.Deleted,
                    _ => CodexFileChangeKind.Unknown
                },
                MovePath = kind.TryGetProperty("move_path", out JsonElement movePath) ? movePath.GetString() : null
            });
        }
        return changes.AsReadOnly();
    }
}
