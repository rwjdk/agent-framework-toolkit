using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Indicates that the API refused a question. No partial decision is returned.</summary>
/// <param name="questionName">The name of the refused question.</param>
[PublicAPI]
public sealed class DecisionRefusalException(string questionName)
    : InvalidOperationException($"The decision API refused question '{questionName}'. No partial decision result is returned.")
{
    /// <summary>Gets the name of the refused question.</summary>
    public string QuestionName { get; } = questionName;
}
