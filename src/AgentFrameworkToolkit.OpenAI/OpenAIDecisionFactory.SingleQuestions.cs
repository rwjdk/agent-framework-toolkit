using AgentFrameworkToolkit.OpenAI.Decisions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    public Task<TEnum> ChooseAsync<TEnum>(ChoiceRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        object input = BuildRequestInput(request);
        return EvaluateSingleAsync<TEnum>(input, new ChoiceQuestionAttribute<TEnum>(request.Question), request, cancellationToken);
    }

    /// <summary>Selects an enum choice from text and/or images. Refused or invalid answers throw.</summary>
    /// <typeparam name="TEnum">The enum defining choices and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected enum value.</returns>
    public Task<TEnum> ChooseAsync<TEnum>(ChoiceImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        object input = BuildRequestInput(request);
        return EvaluateSingleAsync<TEnum>(input, new ChoiceQuestionAttribute<TEnum>(request.Question), request, cancellationToken);
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
        object input = BuildRequestInput(request);
        return await EvaluateSingleAsync<Probability>(input, new ProbabilityQuestionAttribute(request.Question, threshold), request, cancellationToken).ConfigureAwait(false);
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
        object input = BuildRequestInput(request);
        return await EvaluateSingleAsync<Probability>(input, new ProbabilityQuestionAttribute(request.Question, threshold), request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Evaluates an ordered score question against text evidence.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The evidence, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    public Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        object input = BuildRequestInput(request);
        return EvaluateSingleAsync<Score<TEnum>>(input, new ScoreQuestionAttribute<TEnum>(request.Question), request, cancellationToken);
    }

    /// <summary>Evaluates an ordered score question against inline images.</summary>
    /// <typeparam name="TEnum">The enum defining levels in numeric order and optional descriptions.</typeparam>
    /// <param name="request">The images, question, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weighted score, confidence, distribution, and level descriptions. Refused or invalid answers throw.</returns>
    public Task<Score<TEnum>> ScoreAsync<TEnum>(ScoreImageRequest request, CancellationToken cancellationToken = default)
        where TEnum : struct, Enum
    {
        object input = BuildRequestInput(request);
        return EvaluateSingleAsync<Score<TEnum>>(input, new ScoreQuestionAttribute<TEnum>(request.Question), request, cancellationToken);
    }

    private static object BuildRequestInput(DecisionRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request is ChoiceImageRequest or ProbabilityImageRequest or ScoreImageRequest or DecisionImageRequest)
        {
            ArgumentNullException.ThrowIfNull(request.EvidenceImages);
            if (request.EvidenceImages.Count == 0)
            {
                throw new ArgumentException("Provide at least one image.", nameof(request));
            }
            return BuildImageInput(request.EvidenceImages, request.EvidenceInput, request.EvidenceImageDetail);
        }
        if (string.IsNullOrWhiteSpace(request.EvidenceInput))
        {
            throw new ArgumentException("Provide text input.", nameof(request));
        }
        return request.EvidenceInput;
    }

    private async Task<TValue> EvaluateSingleAsync<TValue>(object input, QuestionAttribute attribute, DecisionRequestBase options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attribute.Question);
        List<QuestionDefinition> definitions = [new("Value", attribute, typeof(TValue), attribute.EnumType is Type enumType ? GetEnumNames(enumType) : [])];
        DecisionPayload response = await SendDecisionAsync(input, options, definitions, cancellationToken).ConfigureAwait(false);
        return (TValue)ConvertAnswers(definitions, response.Answers)[0];
    }
}
