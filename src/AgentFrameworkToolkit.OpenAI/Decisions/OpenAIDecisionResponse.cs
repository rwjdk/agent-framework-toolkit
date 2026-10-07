using JetBrains.Annotations;
using System.Collections.Frozen;

namespace AgentFrameworkToolkit.OpenAI.Decisions;

/// <summary>Contains a complete typed decision and token usage.</summary>
/// <typeparam name="T">The result object type.</typeparam>
/// <param name="Result">The complete result.</param>
/// <param name="Model">The model used by the API.</param>
/// <param name="InputTokenCount">Input tokens consumed.</param>
/// <param name="OutputTokenCount">Output tokens consumed.</param>
/// <param name="TotalTokenCount">Total tokens consumed.</param>
[PublicAPI]
public sealed record OpenAIDecisionResponse<T>(T Result, string Model, long InputTokenCount, long OutputTokenCount, long TotalTokenCount);

/// <summary>Contains a choice and its probability distribution.</summary>
/// <typeparam name="T">The choices enum.</typeparam>
/// <param name="Value">The selected choice.</param>
/// <param name="Confidence">Confidence assigned by the API.</param>
/// <param name="Probabilities">Probability for every choice.</param>
[PublicAPI]
public sealed record Choice<T>(T Value, double Confidence, IReadOnlyDictionary<T, double> Probabilities)
    where T : struct, Enum
{
    /// <summary>Gets an immutable probability distribution.</summary>
    public IReadOnlyDictionary<T, double> Probabilities { get; } = Probabilities.ToFrozenDictionary();
}

/// <summary>Contains a probability-weighted score over zero-based enum levels.</summary>
/// <typeparam name="T">The levels enum.</typeparam>
/// <param name="Value">The probability-weighted level index.</param>
/// <param name="Confidence">Confidence assigned by the API.</param>
/// <param name="Probabilities">Probability for every level.</param>
/// <param name="LevelDescriptions">Description for every level.</param>
[PublicAPI]
public sealed record Score<T>(double Value, double Confidence, IReadOnlyDictionary<T, double> Probabilities, IReadOnlyDictionary<T, string> LevelDescriptions)
    where T : struct, Enum
{
    /// <summary>Gets an immutable probability distribution.</summary>
    public IReadOnlyDictionary<T, double> Probabilities { get; } = Probabilities.ToFrozenDictionary();

    /// <summary>Gets immutable level descriptions.</summary>
    public IReadOnlyDictionary<T, string> LevelDescriptions { get; } = LevelDescriptions.ToFrozenDictionary();
}

/// <summary>Contains the probability that a predicate is true.</summary>
/// <param name="Value">The probability between zero and one.</param>
[PublicAPI]
public sealed record Probability(double Value)
{
    /// <summary>Gets the probability between zero and one.</summary>
    public double Value { get; } = ValidateValue(Value);

    private static double ValidateValue(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A probability must be between zero and one.");
        }
        return value;
    }

    /// <summary>Initializes a probability with an inclusive threshold.</summary>
    /// <param name="value">The probability between zero and one.</param>
    /// <param name="threshold">A finite threshold between zero and one.</param>
    public Probability(double value, double threshold) : this(value)
    {
        ValidateThreshold(threshold);
        Threshold = threshold;
    }

    /// <summary>Gets the inclusive threshold used by IsTrue, defaulting to 0.5.</summary>
    public double Threshold { get; } = 0.5;

    /// <summary>Gets whether the probability meets its configured threshold.</summary>
    public bool IsTrue => IsAtLeast(Threshold);

    /// <summary>Checks an inclusive probability threshold.</summary>
    /// <param name="threshold">A finite threshold between zero and one.</param>
    /// <returns>Whether the threshold is met.</returns>
    public bool IsAtLeast(double threshold)
    {
        ValidateThreshold(threshold);
        return Value >= threshold;
    }

    internal static void ValidateThreshold(double threshold)
    {
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "A threshold must be between zero and one.");
        }
    }
}
