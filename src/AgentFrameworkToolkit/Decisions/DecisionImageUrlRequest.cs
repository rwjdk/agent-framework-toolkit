using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Evaluates a choice question against publicly accessible image URLs.</summary>
[PublicAPI]
public sealed class ChoiceImageUrlRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets between 1 and 128 publicly accessible absolute HTTP or HTTPS image URLs.</summary>
    public required IList<Uri> ImageUrls { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<Uri> EvidenceImageUrls => ImageUrls;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}

/// <summary>Evaluates a predicate question against publicly accessible image URLs.</summary>
[PublicAPI]
public sealed class ProbabilityImageUrlRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets the inclusive threshold used by IsTrueAsync and ProbabilityAsync.</summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>Gets or sets between 1 and 128 publicly accessible absolute HTTP or HTTPS image URLs.</summary>
    public required IList<Uri> ImageUrls { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<Uri> EvidenceImageUrls => ImageUrls;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}

/// <summary>Evaluates a score question against publicly accessible image URLs.</summary>
[PublicAPI]
public sealed class ScoreImageUrlRequest : DecisionRequestBase
{
    /// <summary>Gets or sets the question instructions.</summary>
    public required string Question { get; set; }

    /// <summary>Gets or sets between 1 and 128 publicly accessible absolute HTTP or HTTPS image URLs.</summary>
    public required IList<Uri> ImageUrls { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<Uri> EvidenceImageUrls => ImageUrls;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}

/// <summary>Evaluates attribute-defined questions against publicly accessible image URLs.</summary>
[PublicAPI]
public sealed class DecisionImageUrlRequest : DecisionRequestBase
{
    /// <summary>Gets or sets between 1 and 128 publicly accessible absolute HTTP or HTTPS image URLs.</summary>
    public required IList<Uri> ImageUrls { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    internal override string? EvidenceInput => Input;
    internal override IList<Uri> EvidenceImageUrls => ImageUrls;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}

/// <summary>Evaluates runtime-defined questions against publicly accessible image URLs.</summary>
[PublicAPI]
public sealed class DynamicDecisionImageUrlRequest : DecisionRequestBase
{
    /// <summary>Gets or sets between 1 and 128 publicly accessible absolute HTTP or HTTPS image URLs.</summary>
    public required IList<Uri> ImageUrls { get; set; }

    /// <summary>Gets or sets optional text input for the images.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets image detail for all images.</summary>
    public ImageDetail ImageDetail { get; set; } = ImageDetail.Auto;

    /// <summary>Gets or sets the questions, with nonempty IDs unique ignoring case.</summary>
    public required IList<IQuestion> Questions { get; set; }

    internal override string? EvidenceInput => Input;
    internal override IList<Uri> EvidenceImageUrls => ImageUrls;
    internal override ImageDetail EvidenceImageDetail => ImageDetail;
}
