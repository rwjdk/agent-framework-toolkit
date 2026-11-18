using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using AgentFrameworkToolkit.Decisions;
using AgentFrameworkToolkit.OpenAI;
using Azure.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using OpenAI;
using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.AzureOpenAI;

/// <summary>Evaluates decisions with the selected Jev or OpenAI Decisions API in Azure.</summary>
[PublicAPI]
public sealed class AzureOpenAIDecisionFactory : DecisionFactory
{
    private readonly OpenAIClient _client;
    private readonly string _deployment;
    private readonly Uri _endpoint;
    private readonly OpenAIDecisionFactory? _openAIDecisions;

    /// <summary>Gets the connection used at construction. Later changes do not reconfigure the client.</summary>
    public AzureOpenAIConnection Connection { get; }

    /// <summary>Initializes decisions with an Azure resource endpoint, API key, deployment name, and protocol.</summary>
    /// <param name="endpoint">The Azure resource endpoint.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="apiKey">The resource API key.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    public AzureOpenAIDecisionFactory(string endpoint, DecisionProtocol protocol, string apiKey, string deployment)
        : this(new AzureOpenAIConnection(endpoint, apiKey), protocol, deployment)
    {
    }

    /// <summary>Initializes decisions with an Azure resource endpoint, RBAC credentials, deployment name, and protocol.</summary>
    /// <param name="endpoint">The Azure resource endpoint.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="credentials">Credentials authorized to call the deployment.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    public AzureOpenAIDecisionFactory(string endpoint, DecisionProtocol protocol, TokenCredential credentials, string deployment)
        : this(new AzureOpenAIConnection(endpoint, credentials), protocol, deployment)
    {
    }

    /// <summary>Initializes decisions with existing Azure connection and transport configuration.</summary>
    /// <param name="connection">Authentication, endpoint, timeout, and SDK transport configuration.</param>
    /// <param name="protocol">The underlying decision protocol.</param>
    /// <param name="deployment">The deployment name of a model supported by the selected protocol.</param>
    public AzureOpenAIDecisionFactory(AzureOpenAIConnection connection, DecisionProtocol protocol, string deployment)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        if (!Enum.IsDefined(protocol))
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported decision protocol.");
        }
        Connection = connection;
        _deployment = deployment;
        _client = connection.GetClient();
        _endpoint = GetDecisionEndpoint(_client);
        if (protocol == DecisionProtocol.OpenAI)
        {
            _openAIDecisions = new(_client, deployment);
        }
    }

    private static Uri GetDecisionEndpoint(OpenAIClient client)
    {
#pragma warning disable OPENAI001 // Preserve the configured SDK endpoint and transport.
        string endpoint = client.Endpoint.AbsoluteUri.TrimEnd('/');
#pragma warning restore OPENAI001
        const string decisionPath = "/providers/microsoft/v1/systemone";
        const string openAIPath = "/openai/v1";
        while (endpoint.EndsWith(openAIPath, StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint[..^openAIPath.Length];
        }
        if (endpoint.EndsWith(decisionPath, StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(endpoint);
        }
        return new Uri(endpoint + decisionPath);
    }

    internal override async Task<DecisionEvaluation> EvaluateAsync(DecisionRequestBase request, List<QuestionDefinition> definitions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_openAIDecisions is not null)
        {
            return await _openAIDecisions.EvaluateAsync(request, definitions, cancellationToken).ConfigureAwait(false);
        }
        if (request is DecisionImageUrlRequest or ChoiceImageUrlRequest or ProbabilityImageUrlRequest or ScoreImageUrlRequest or DynamicDecisionImageUrlRequest)
        {
            throw new NotSupportedException("The Microsoft decision API requires inline image data.");
        }
        if (request.SafetyIdentifier is not null)
        {
            throw new NotSupportedException("The Microsoft decision API does not support SafetyIdentifier.");
        }
        Dictionary<string, object> questions = KeyedDecisionProtocol.CreateQuestions(definitions);
        BinaryData requestData = CreateRequestData(request, questions);
        using PipelineMessage message = _client.Pipeline.CreateMessage();
        message.Apply(new RequestOptions { CancellationToken = cancellationToken });
        message.Request.Method = "POST";
        message.Request.Uri = _endpoint;
        message.Request.Headers.Set("Content-Type", "application/json");
        message.Request.Headers.Set("Accept", "application/json");
        message.Request.Content = BinaryContent.Create(requestData);
        await _client.Pipeline.SendAsync(message).ConfigureAwait(false);
        PipelineResponse response = message.Response!;
        request.RawHttpCallDetails?.Invoke(new RawCallDetails
        {
            RequestUrl = _endpoint.AbsoluteUri,
            RequestData = requestData.ToString(),
            ResponseData = response.Content.ToString()
        });
        if (response.Status is < 200 or >= 300)
        {
            throw new ClientResultException(response);
        }
        using JsonDocument document = JsonDocument.Parse(response.Content);
        return KeyedDecisionProtocol.ReadEvaluation(document.RootElement, definitions);
    }

    private BinaryData CreateRequestData(DecisionRequestBase request, Dictionary<string, object> questions)
    {
        if (request is not (DecisionImageRequest or ChoiceImageRequest or ProbabilityImageRequest or ScoreImageRequest or DynamicDecisionImageRequest))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.EvidenceInput);
            return BinaryData.FromObjectAsJson(new { model = _deployment, state = request.EvidenceInput, questions });
        }

        IList<DataContent>? images = request.EvidenceImages;
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count is < 1 or > 8)
        {
            throw new ArgumentException("Provide between 1 and 8 images.", nameof(request));
        }
        if (request.EvidenceImageDetail != ImageDetail.Auto)
        {
            throw new NotSupportedException("The Microsoft decision API does not support setting image detail.");
        }

        Dictionary<string, object> assets = new();
        List<string> stateParts = [];
        if (!string.IsNullOrWhiteSpace(request.EvidenceInput))
        {
            stateParts.Add(request.EvidenceInput);
        }
        for (int index = 0; index < images.Count; index++)
        {
            DataContent image = images[index];
            ArgumentNullException.ThrowIfNull(image);
            if (!image.HasTopLevelMediaType("image") || image.Data.IsEmpty)
            {
                throw new ArgumentException("Images must contain nonempty inline data and an image MIME type.", nameof(request));
            }
            string format = image.MediaType.Split(';', 2)[0]["image/".Length..].Trim().ToLowerInvariant();
            string name = FormattableString.Invariant($"toolkit-image-{index}.{format}");
            assets.Add(name, new { type = "image", format, data_base64 = Convert.ToBase64String(image.Data.Span) });
            stateParts.Add($"image:{name}");
        }

        return BinaryData.FromObjectAsJson(new { model = _deployment, state = string.Join("\n", stateParts), questions, assets });
    }
}
