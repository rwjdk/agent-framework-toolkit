using JetBrains.Annotations;
using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.Decisions;

/// <summary>Provides shared typed and single-question decision operations for provider implementations.</summary>
[PublicAPI]
public abstract class DecisionFactory : IDecisionFactory
{
    /// <summary>Creates a complete typed decision from text. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Text evidence and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    public Task<DecisionResponse<T>> CreateDecisionAsync<T>(DecisionRequest request, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return EvaluateCoreAsync<T>(request, cancellationToken);
    }

    /// <summary>Creates a complete typed decision from images. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Inline images, optional text input, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    public Task<DecisionResponse<T>> CreateDecisionAsync<T>(DecisionImageRequest request, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return EvaluateCoreAsync<T>(request, cancellationToken);
    }

    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    public Task<TEnum> ChooseAsync<TEnum>(ChoiceRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(request);
        return EvaluateSingleAsync<TEnum>(request, new ChoiceQuestionAttribute<TEnum>(request.Question), cancellationToken);
    }

    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    public Task<TEnum> ChooseAsync<TEnum>(ChoiceImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(request);
        return EvaluateSingleAsync<TEnum>(request, new ChoiceQuestionAttribute<TEnum>(request.Question), cancellationToken);
    }

    /// <summary>Checks whether a condition's probability meets the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the threshold is met. Refused or invalid answers throw.</returns>
    public async Task<bool> IsTrueAsync(ProbabilityRequest request, CancellationToken cancellationToken = default)
    {
        return (await ProbabilityAsync(request, cancellationToken).ConfigureAwait(false)).IsTrue;
    }

    /// <summary>Checks whether a condition's probability meets the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the threshold is met. Refused or invalid answers throw.</returns>
    public async Task<bool> IsTrueAsync(ProbabilityImageRequest request, CancellationToken cancellationToken = default)
    {
        return (await ProbabilityAsync(request, cancellationToken).ConfigureAwait(false)).IsTrue;
    }

    /// <summary>Gets a predicate probability whose IsTrue uses the request's inclusive threshold.</summary>
    /// <param name="request">The evidence, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The probability and configured threshold. Refused or invalid answers throw.</returns>
    public async Task<Probability> ProbabilityAsync(ProbabilityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        double threshold = request.Threshold;
        Probability.ValidateThreshold(threshold);
        return await EvaluateSingleAsync<Probability>(request, new ProbabilityQuestionAttribute(request.Question, threshold), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets a predicate probability whose IsTrue uses the request's inclusive threshold.</summary>
    /// <param name="request">The images, question, threshold, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The probability and configured threshold. Refused or invalid answers throw.</returns>
    public async Task<Probability> ProbabilityAsync(ProbabilityImageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        double threshold = request.Threshold;
        Probability.ValidateThreshold(threshold);
        return await EvaluateSingleAsync<Probability>(request, new ProbabilityQuestionAttribute(request.Question, threshold), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Evaluates an ordered score question against text evidence.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    public Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(request);
        return EvaluateSingleAsync<Score<TEnum>>(request, new ScoreQuestionAttribute<TEnum>(request.Question), cancellationToken);
    }

    /// <summary>Evaluates an ordered score question against inline images.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The images, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    public Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(request);
        return EvaluateSingleAsync<Score<TEnum>>(request, new ScoreQuestionAttribute<TEnum>(request.Question), cancellationToken);
    }
    internal abstract Task<DecisionEvaluation> EvaluateAsync(DecisionRequestBase request, List<QuestionDefinition> definitions, CancellationToken cancellationToken);

    private async Task<DecisionResponse<T>> EvaluateCoreAsync<T>(DecisionRequestBase request, CancellationToken cancellationToken)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(request);
        List<QuestionDefinition> definitions = BuildDefinitions<T>();
        DecisionEvaluation response = await EvaluateAsync(request, definitions, cancellationToken).ConfigureAwait(false);
        T result = new();
        for (int i = 0; i < definitions.Count; i++)
        {
            definitions[i].Property!.SetValue(result, response.Values[i]);
        }
        return new(result, response.Model, response.InputTokens, response.OutputTokens, response.TotalTokens);
    }

    private async Task<TValue> EvaluateSingleAsync<TValue>(DecisionRequestBase request, QuestionAttribute attribute, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(attribute.Question);
        List<QuestionDefinition> definitions = [new("Value", attribute, typeof(TValue), attribute.EnumType is Type enumType ? GetEnumNames(enumType) : [])];
        DecisionEvaluation response = await EvaluateAsync(request, definitions, cancellationToken).ConfigureAwait(false);
        return (TValue)response.Values[0];
    }
}
