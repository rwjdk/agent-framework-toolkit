using AgentFrameworkToolkit.Decisions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    private static object BuildRequestInput(DecisionRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request is ChoiceImageRequest or ProbabilityImageRequest or ScoreImageRequest or DecisionImageRequest or DynamicDecisionImageRequest)
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
}
