using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using AgentFrameworkToolkit.OpenAI.Decisions;
using JetBrains.Annotations;
using OpenAI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.OpenAI;

/// <summary>Evaluates attribute-defined questions with the OpenAI Decisions API.</summary>
[PublicAPI]
public partial class OpenAIDecisionFactory
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    /// <summary>Gets the connection used to create the retained client, or null when supplied an SDK client. Later connection changes do not reconfigure the client.</summary>
    public OpenAIConnection? Connection { get; }

    internal static string? ResolveAgentDecisionModel(string agentModel, string? decisionApiModel)
    {
        // Explicit models may include newly supported models or custom provider names.
        if (decisionApiModel is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(decisionApiModel);
            return decisionApiModel;
        }
        // Only fall back to model names confirmed by the Decisions API documentation.
        return agentModel == OpenAIChatModels.Gpt6Luna ? agentModel : null;
    }

    /// <summary>Initializes the factory with an API key.</summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="model">The model used for all decisions.</param>
    public OpenAIDecisionFactory(string apiKey, string model) : this(new OpenAIConnection(apiKey), model)
    {
    }

    /// <summary>Initializes the factory with connection configuration.</summary>
    /// <param name="connection">The connection configuration.</param>
    /// <param name="model">The model used for all decisions.</param>
    public OpenAIDecisionFactory(OpenAIConnection connection, string model)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        _model = model;
        Connection = connection;
        _client = connection.GetClient();
    }

    /// <summary>Initializes decisions using an existing SDK client and its transport.</summary>
    /// <param name="client">The shared SDK client. This factory does not own or dispose it.</param>
    /// <param name="model">The model used for all decisions.</param>
    public OpenAIDecisionFactory(OpenAIClient client, string model)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        _model = model;
        _client = client;
    }

    /// <summary>Creates a complete typed decision from text. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Text evidence and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    public Task<OpenAIDecisionResponse<T>> CreateDecisionAsync<T>(DecisionRequest request, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return EvaluateCoreAsync<T>(BuildRequestInput(request), request, cancellationToken);
    }

    /// <summary>Creates a complete typed decision from images. Refused or invalid answers throw.</summary>
    /// <typeparam name="T">An attributed result class with a public parameterless constructor.</typeparam>
    /// <param name="request">Inline images, optional text input, and request configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete typed result and token usage.</returns>
    public Task<OpenAIDecisionResponse<T>> CreateDecisionAsync<T>(DecisionImageRequest request, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return EvaluateCoreAsync<T>(BuildRequestInput(request), request, cancellationToken);
    }

    private static object BuildImageInput(IEnumerable<DataContent> images, string? input, ImageDetail detail)
    {
        ArgumentNullException.ThrowIfNull(images);
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
        int count = 0;
        foreach (DataContent image in images)
        {
            ArgumentNullException.ThrowIfNull(image);
            if (++count > 128)
            {
                throw new ArgumentException("At most 128 images are supported.", nameof(images));
            }
            if (!image.HasTopLevelMediaType("image") || image.Data.IsEmpty)
            {
                throw new ArgumentException("Images must contain nonempty inline data and an image MIME type.", nameof(images));
            }
            string dataUrl = $"data:{image.MediaType};base64,{Convert.ToBase64String(image.Data.Span)}";
            content.Add(new { type = "input_image", image_url = dataUrl, detail = imageDetail });
        }
        if (content.Count == 0)
        {
            throw new ArgumentException("Provide at least one image or shared text evidence.", nameof(images));
        }
        return new[] { new { role = "user", content } };
    }

    private async Task<OpenAIDecisionResponse<T>> EvaluateCoreAsync<T>(object input, DecisionRequestBase options, CancellationToken cancellationToken)
        where T : class, new()
    {
        List<QuestionDefinition> definitions = BuildDefinitions<T>();
        DecisionPayload response = await SendDecisionAsync(input, options, definitions, cancellationToken).ConfigureAwait(false);
        object[] values = ConvertAnswers(definitions, response.Answers);
        T result = new();
        for (int i = 0; i < definitions.Count; i++)
        {
            definitions[i].Property!.SetValue(result, values[i]);
        }
        return new(result, response.Model, response.InputTokens, response.OutputTokens, response.TotalTokens);
    }

    private sealed record DecisionPayload(JsonElement Answers, string Model, long InputTokens, long OutputTokens, long TotalTokens);

    private async Task<DecisionPayload> SendDecisionAsync(object input, DecisionRequestBase options, List<QuestionDefinition> definitions, CancellationToken cancellationToken)
    {
        if (options.SafetyIdentifier?.Length > 128)
        {
            throw new ArgumentException("SafetyIdentifier must be at most 128 characters.", nameof(options));
        }
        Dictionary<string, object> payload = new()
        {
            ["model"] = _model,
            ["input"] = input,
            ["questions"] = definitions.Select(BuildQuestion).ToArray()
        };
        if (options.SafetyIdentifier is not null)
        {
            payload["safety_identifier"] = options.SafetyIdentifier;
        }
        OpenAIClient client = _client;
        ClientPipeline pipeline = client.Pipeline;
        using PipelineMessage message = pipeline.CreateMessage();
        message.Apply(new RequestOptions { CancellationToken = cancellationToken });
        message.Request.Method = "POST";
#pragma warning disable OPENAI001 // The SDK endpoint is needed to preserve custom connection configuration.
        message.Request.Uri = new Uri(client.Endpoint.AbsoluteUri.TrimEnd('/') + "/decisions");
#pragma warning restore OPENAI001
        message.Request.Headers.Set("Content-Type", "application/json");
        message.Request.Headers.Set("Accept", "application/json");
        BinaryData requestData = BinaryData.FromObjectAsJson(payload);
        Action<RawCallDetails>? rawHttpCallDetails = options.RawHttpCallDetails;
        message.Request.Content = BinaryContent.Create(requestData);
        await pipeline.SendAsync(message).ConfigureAwait(false);
        PipelineResponse response = message.Response!;
        rawHttpCallDetails?.Invoke(new RawCallDetails
        {
            RequestUrl = message.Request.Uri.AbsoluteUri,
            RequestData = FormatRawBody(requestData.ToString()),
            ResponseData = FormatRawBody(response.Content.ToString())
        });
        if (response.Status is < 200 or >= 300)
        {
            throw new ClientResultException(response);
        }
        using JsonDocument document = JsonDocument.Parse(response.Content);
        JsonElement root = document.RootElement;
        JsonElement usage = root.GetProperty("usage");
        string model = root.GetProperty("model").GetString() ?? throw new JsonException("Missing response model.");
        long inputTokens = usage.GetProperty("input_tokens").GetInt64();
        long outputTokens = usage.GetProperty("output_tokens").GetInt64();
        long totalTokens = usage.GetProperty("total_tokens").GetInt64();
        return new(root.GetProperty("answers").Clone(), model, inputTokens, outputTokens, totalTokens);
    }

    private static string FormatRawBody(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return body;
        }
    }

}
