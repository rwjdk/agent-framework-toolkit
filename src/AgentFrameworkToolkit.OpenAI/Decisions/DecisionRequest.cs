using JetBrains.Annotations;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.OpenAI.Decisions;

/// <summary>Contains shared Decisions API request configuration.</summary>
[PublicAPI]
public abstract class DecisionRequestBase
{
    /// <summary>Gets or sets an optional callback receiving this decision's raw HTTP request and response bodies.</summary>
    public Action<RawCallDetails>? RawHttpCallDetails { get; set; }

    /// <summary>Gets or sets an optional opaque end-user identifier, at most 128 characters.</summary>
    public string? SafetyIdentifier { get; set; }

    internal abstract string? EvidenceInput { get; }
    internal virtual IList<DataContent>? EvidenceImages => null;
    internal virtual ImageDetail EvidenceImageDetail => ImageDetail.Auto;
}

/// <summary>Evaluates a choice question against text evidence.</summary>
[PublicAPI]
public sealed class ChoiceRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets the required text evidence.</summary>
    public required string Input { get; set; }

    internal override string EvidenceInput => Input;
}

/// <summary>Evaluates a choice question against inline image evidence.</summary>
[PublicAPI]
public sealed class ChoiceImageRequest : DecisionRequestBase
{
    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override ImageDetail EvidenceImageDetail => ImageDetail;

    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets required inline image data and MIME types, with at most 128 images.</summary>
    public required IList<DataContent> Images { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    internal override string? EvidenceInput => Input;
    internal override IList<DataContent> EvidenceImages => Images;
}

/// <summary>Evaluates a predicate question against text evidence.</summary>
[PublicAPI]
public sealed class ProbabilityRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets the inclusive threshold used by IsTrueAsync and ProbabilityAsync.</summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>Gets or sets the required text evidence.</summary>
    public required string Input { get; set; }

    internal override string EvidenceInput => Input;
}

/// <summary>Evaluates a predicate question against inline image evidence.</summary>
[PublicAPI]
public sealed class ProbabilityImageRequest : DecisionRequestBase
{
    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override ImageDetail EvidenceImageDetail => ImageDetail;

    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets the inclusive threshold used by IsTrueAsync and ProbabilityAsync.</summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>Gets or sets required inline image data and MIME types, with at most 128 images.</summary>
    public required IList<DataContent> Images { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    internal override string? EvidenceInput => Input;
    internal override IList<DataContent> EvidenceImages => Images;
}

/// <summary>Evaluates an ordered score question against text evidence.</summary>
[PublicAPI]
public sealed class ScoreRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets the required text evidence.</summary>
    public required string Input { get; set; }

    internal override string EvidenceInput => Input;
}

/// <summary>Evaluates an ordered score question against inline images.</summary>
[PublicAPI]
public sealed class ScoreImageRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets required inline image data and MIME types, with at most 128 images.</summary>
    public required IList<DataContent> Images { get; set; }

    /// <summary>Gets or sets optional text input.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<DataContent> EvidenceImages => Images;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}

/// <summary>Evaluates attribute-defined questions against text evidence.</summary>
[PublicAPI]
public sealed class DecisionRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the required text evidence.</summary>
    public required string Input { get; set; }

    internal override string EvidenceInput => Input;
}

/// <summary>Evaluates attribute-defined questions against inline images.</summary>
[PublicAPI]
public sealed class DecisionImageRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the required inline image evidence, with at most 128 images.</summary>
    public required IList<DataContent> Images { get; set; }

    /// <summary>Gets or sets optional text input.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<DataContent> EvidenceImages => Images;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}
