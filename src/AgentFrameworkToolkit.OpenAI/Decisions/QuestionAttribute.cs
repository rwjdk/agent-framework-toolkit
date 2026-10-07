using JetBrains.Annotations;

namespace AgentFrameworkToolkit.OpenAI.Decisions;

/// <summary>Defines a question on a decision result property.</summary>
/// <param name="question">The instructions for evaluating the question.</param>
[PublicAPI]
[AttributeUsage(AttributeTargets.Property)]
public abstract class QuestionAttribute(string question) : Attribute
{
    /// <summary>Gets the question instructions.</summary>
    public string Question { get; } = question;

    internal abstract string Kind { get; }
    internal virtual Type? EnumType => null;
}

/// <summary>Defines an enum choice question.</summary>
/// <typeparam name="T">The enum defining the choices.</typeparam>
/// <param name="question">The question instructions.</param>
[PublicAPI]
public sealed class ChoiceQuestionAttribute<T>(string question) : QuestionAttribute(question)
    where T : struct, Enum
{
    internal override string Kind => "choice";
    internal override Type EnumType => typeof(T);
}

/// <summary>Defines an ordered score question. Enum numeric order determines zero-based levels.</summary>
/// <typeparam name="T">The enum defining the ordered levels.</typeparam>
/// <param name="question">The question instructions.</param>
[PublicAPI]
public sealed class ScoreQuestionAttribute<T>(string question) : QuestionAttribute(question)
    where T : struct, Enum
{
    internal override string Kind => "score";
    internal override Type EnumType => typeof(T);
}

/// <summary>Defines a predicate question returning a probability or thresholded Boolean.</summary>
[PublicAPI]
public sealed class ProbabilityQuestionAttribute : QuestionAttribute
{
    /// <summary>Initializes a predicate question.</summary>
    /// <param name="question">The question instructions.</param>
    /// <param name="threshold">The inclusive threshold for Boolean results and Probability.IsTrue, between zero and one.</param>
    public ProbabilityQuestionAttribute(string question, double threshold = 0.5) : base(question)
    {
        Probability.ValidateThreshold(threshold);
        Threshold = threshold;
    }

    /// <summary>Gets the inclusive threshold for Boolean results and Probability.IsTrue.</summary>
    public double Threshold { get; }

    internal override string Kind => "predicate";
}
