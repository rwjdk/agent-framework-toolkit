using JetBrains.Annotations;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Defines a named question supplied at runtime. Only the built-in question types are supported.</summary>
[PublicAPI]
public interface IQuestion
{
    /// <summary>Gets the question ID, unique within a request ignoring case.</summary>
    string Id { get; }

    /// <summary>Gets the question instructions.</summary>
    string Question { get; }
}

/// <summary>Defines a choice option supplied at runtime.</summary>
[PublicAPI]
public sealed class ChoiceQuestionOption
{
    /// <summary>Gets or sets the option ID, unique within the question ignoring case.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the option description.</summary>
    public required string Description { get; set; }
}

/// <summary>Selects one of the supplied string options.</summary>
[PublicAPI]
public sealed class ChoiceQuestion : IQuestion
{
    /// <inheritdoc />
    public required string Id { get; set; }

    /// <inheritdoc />
    public required string Question { get; set; }

    /// <summary>Gets or sets at least two distinct choice options.</summary>
    public required IList<ChoiceQuestionOption> Choices { get; set; }
}

/// <summary>Evaluates a probability-weighted score over ordered levels.</summary>
[PublicAPI]
public sealed class ScoreQuestion : IQuestion
{
    /// <inheritdoc />
    public required string Id { get; set; }

    /// <inheritdoc />
    public required string Question { get; set; }

    /// <summary>Gets or sets at least two level descriptions in zero-based score order.</summary>
    public required IList<string> Levels { get; set; }
}

/// <summary>Evaluates the probability of a condition.</summary>
[PublicAPI]
public sealed class ProbabilityQuestion : IQuestion
{
    /// <inheritdoc />
    public required string Id { get; set; }

    /// <inheritdoc />
    public required string Question { get; set; }

    /// <summary>Gets or sets the inclusive threshold used by the answer's IsTrue, defaulting to 0.5.</summary>
    public double Threshold { get; set; } = 0.5;
}

/// <summary>Evaluates runtime-defined questions against text evidence.</summary>
[PublicAPI]
public sealed class DynamicDecisionRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the required text evidence.</summary>
    public required string Input { get; set; }

    /// <summary>Gets or sets the questions, with nonempty IDs unique ignoring case.</summary>
    public required IList<IQuestion> Questions { get; set; }

    internal override string EvidenceInput => Input;
}

/// <summary>Evaluates runtime-defined questions against inline image evidence.</summary>
[PublicAPI]
public sealed class DynamicDecisionImageRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the required inline image evidence, with at most 128 images.</summary>
    public required IList<DataContent> Images { get; set; }

    /// <summary>Gets or sets optional text evidence.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    /// <summary>Gets or sets the questions, with nonempty IDs unique ignoring case.</summary>
    public required IList<IQuestion> Questions { get; set; }

    internal override string? EvidenceInput => Input;
    internal override IList<DataContent> EvidenceImages => Images;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}
