using JetBrains.Annotations;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Evaluates typed decisions, choices, probabilities, and scores with a decision provider.</summary>
[PublicAPI]
public interface IDecisionFactory
{
    /// <summary>Creates a complete typed decision from text. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Text evidence and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    Task<DecisionResponse<T>> CreateDecisionAsync<T>(DecisionRequest request, CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>Creates a complete typed decision from images. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Inline images, optional text input, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    Task<DecisionResponse<T>> CreateDecisionAsync<T>(DecisionImageRequest request, CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    Task<TEnum> ChooseAsync<TEnum>(ChoiceRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum;

    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    Task<TEnum> ChooseAsync<TEnum>(ChoiceImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum;

    /// <summary>Checks whether a condition's probability meets the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the threshold is met. Refused or invalid answers throw.</returns>
    Task<bool> IsTrueAsync(ProbabilityRequest request, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a condition's probability meets the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the threshold is met. Refused or invalid answers throw.</returns>
    Task<bool> IsTrueAsync(ProbabilityImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a predicate probability whose IsTrue uses the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The probability and configured threshold. Refused or invalid answers throw.</returns>
    Task<Probability> ProbabilityAsync(ProbabilityRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a predicate probability whose IsTrue uses the request's inclusive threshold.</summary>
    /// <param name="request">The images, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The probability and configured threshold. Refused or invalid answers throw.</returns>
    Task<Probability> ProbabilityAsync(ProbabilityImageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Evaluates an ordered score question against text evidence.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum;

    /// <summary>Evaluates an ordered score question against inline images.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The images, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum;


}
