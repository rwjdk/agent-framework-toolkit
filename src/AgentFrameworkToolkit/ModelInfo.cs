using System.Diagnostics;
using JetBrains.Annotations;

namespace AgentFrameworkToolkit;

/// <summary>Describes a model or deployment returned by a provider's discovery API.</summary>
[PublicAPI]
[DebuggerDisplay("{Name}")]
public sealed class ModelInfo
{
    /// <summary>Gets the model identifier or deployment name returned by the provider.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the provider's SDK model, or a detached <see cref="System.Text.Json.JsonElement"/> for JSON-based discovery, containing additional metadata.</summary>
    public object? RawRepresentation { get; init; }
}
