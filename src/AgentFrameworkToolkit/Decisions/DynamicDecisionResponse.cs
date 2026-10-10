using System.Collections.Frozen;
using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Contains runtime-defined answers accessible by case-insensitive question ID and token usage.</summary>
[PublicAPI]
public sealed class DecisionResponse
{
    private readonly FrozenDictionary<string, object> _answers;

    internal DecisionResponse(IEnumerable<KeyValuePair<string, object>> answers, string model, long inputTokens, long outputTokens, long totalTokens)
    {
        _answers = answers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        Model = model;
        InputTokenCount = inputTokens;
        OutputTokenCount = outputTokens;
        TotalTokenCount = totalTokens;
    }

    /// <summary>Gets the model used by the API.</summary>
    public string Model { get; }

    /// <summary>Gets input tokens consumed.</summary>
    public long InputTokenCount { get; }

    /// <summary>Gets output tokens consumed.</summary>
    public long OutputTokenCount { get; }

    /// <summary>Gets total tokens consumed.</summary>
    public long TotalTokenCount { get; }

    /// <summary>Gets a choice answer by case-insensitive question ID.</summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The full choice answer.</returns>
    /// <exception cref="KeyNotFoundException">The ID does not exist.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a choice.</exception>
    public ChoiceAnswer GetChoice(string id) => GetAnswer<ChoiceAnswer>(id);

    /// <summary>Gets a score answer by case-insensitive question ID.</summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The full score answer.</returns>
    /// <exception cref="KeyNotFoundException">The ID does not exist.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a score.</exception>
    public ScoreAnswer GetScore(string id) => GetAnswer<ScoreAnswer>(id);

    /// <summary>Gets a probability answer by case-insensitive question ID.</summary>
    /// <param name="id">The question ID.</param>
    /// <returns>The probability, threshold, and computed Boolean answer.</returns>
    /// <exception cref="KeyNotFoundException">The ID does not exist.</exception>
    /// <exception cref="InvalidOperationException">The answer is not a probability.</exception>
    public ProbabilityAnswer GetProbability(string id) => GetAnswer<ProbabilityAnswer>(id);

    private T GetAnswer<T>(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_answers.TryGetValue(id, out object? answer))
        {
            throw new KeyNotFoundException($"No decision answer exists for question ID '{id}'.");
        }
        if (answer is not T typedAnswer)
        {
            throw new InvalidOperationException($"Answer '{id}' is a {answer.GetType().Name}, not a {typeof(T).Name}.");
        }
        return typedAnswer;
    }
}

/// <summary>Contains a choice option and its probability.</summary>
/// <param name="Id">The option ID.</param>
/// <param name="Description">The option description.</param>
/// <param name="Probability">The probability assigned to this option.</param>
[PublicAPI]
public sealed record ChoiceOption(string Id, string Description, double Probability);

/// <summary>Contains a selected option, confidence, and all option probabilities.</summary>
/// <param name="Value">The selected option.</param>
/// <param name="Confidence">Confidence assigned by the API.</param>
/// <param name="Options">All options in question order.</param>
[PublicAPI]
public sealed record ChoiceAnswer(ChoiceOption Value, double Confidence, IReadOnlyList<ChoiceOption> Options)
{
    /// <summary>Gets an immutable snapshot of all options in question order.</summary>
    public IReadOnlyList<ChoiceOption> Options { get; } = Array.AsReadOnly(Options.ToArray());
}

/// <summary>Contains a score level and its probability.</summary>
/// <param name="Index">The zero-based level index.</param>
/// <param name="Description">The level description.</param>
/// <param name="Probability">The probability assigned to this level.</param>
[PublicAPI]
public sealed record ScoreLevel(int Index, string Description, double Probability);

/// <summary>Contains a probability-weighted score, confidence, and ordered level details.</summary>
/// <param name="Value">The probability-weighted zero-based score.</param>
/// <param name="Confidence">Confidence assigned by the API.</param>
/// <param name="Levels">All levels in zero-based index order.</param>
[PublicAPI]
public sealed record ScoreAnswer(double Value, double Confidence, IReadOnlyList<ScoreLevel> Levels)
{
    /// <summary>Gets an immutable snapshot of all levels in zero-based index order.</summary>
    public IReadOnlyList<ScoreLevel> Levels { get; } = Array.AsReadOnly(Levels.ToArray());
}

/// <summary>Contains a probability and its inclusive Boolean threshold.</summary>
[PublicAPI]
public sealed class ProbabilityAnswer
{
    private readonly Probability _probability;

    internal ProbabilityAnswer(double value, double threshold)
    {
        _probability = new(value, threshold);
    }

    /// <summary>Gets the probability between zero and one.</summary>
    public double Value => _probability.Value;

    /// <summary>Gets the configured inclusive threshold.</summary>
    public double Threshold => _probability.Threshold;

    /// <summary>Gets whether the probability meets the threshold.</summary>
    public bool IsTrue => _probability.IsTrue;
}
