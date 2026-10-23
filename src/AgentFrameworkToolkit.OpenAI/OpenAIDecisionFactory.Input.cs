using AgentFrameworkToolkit.Decisions;

namespace AgentFrameworkToolkit.OpenAI;

public partial class OpenAIDecisionFactory
{
    private static object BuildRequestInput(DecisionRequestBase request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request is ChoiceImageUrlRequest or ProbabilityImageUrlRequest or ScoreImageUrlRequest or DecisionImageUrlRequest or DynamicDecisionImageUrlRequest)
        {
            ArgumentNullException.ThrowIfNull(request.EvidenceImageUrls);
            return BuildImageUrlInput(request.EvidenceImageUrls, request.EvidenceInput, request.EvidenceImageDetail);
        }
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

    private static object BuildImageUrlInput(IList<Uri> imageUrls, string? input, ImageDetail detail)
    {
        if (imageUrls.Count is < 1 or > 128)
        {
            throw new ArgumentException("Provide between 1 and 128 image URLs.", nameof(imageUrls));
        }
        string imageDetail = detail switch
        {
            ImageDetail.Auto => "auto",
            ImageDetail.Low => "low",
            ImageDetail.High => "high",
            ImageDetail.Original => "original",
            _ => throw new ArgumentOutOfRangeException(nameof(detail), detail, "Unsupported image detail.")
        };
        List<object> content = [];
        if (input is not null)
        {
            content.Add(new { type = "input_text", text = input });
        }
        foreach (Uri imageUrl in imageUrls)
        {
            ArgumentNullException.ThrowIfNull(imageUrl);
            if (!imageUrl.IsAbsoluteUri || (imageUrl.Scheme != Uri.UriSchemeHttp && imageUrl.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Image URLs must be absolute HTTP or HTTPS URLs.", nameof(imageUrls));
            }
            content.Add(new { type = "input_image", image_url = imageUrl.AbsoluteUri, detail = imageDetail });
        }
        return new[] { new { role = "user", content } };
    }
}
