using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentFrameworkToolkit.Decisions;
using JetBrains.Annotations;
using static AgentFrameworkToolkit.Decisions.DecisionQuestions;

namespace AgentFrameworkToolkit.Jev;

/// <summary>Evaluates shared decision contracts with the Jev API. Image requests are currently unsupported.</summary>
[PublicAPI]
public sealed class JevDecisionFactory : DecisionFactory
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly Uri _endpoint;
    private readonly TimeSpan? _networkTimeout;

    /// <summary>Gets the connection used at construction. Later changes do not reconfigure this factory.</summary>
    public JevConnection Connection { get; }

    /// <summary>Initializes decisions with an API key and an explicit model.</summary>
    /// <param name="apiKey">The TypeSafe AI API key.</param>
    /// <param name="model">The model used for all decisions.</param>
    public JevDecisionFactory(string apiKey, string model) : this(new JevConnection(apiKey), model)
    {
    }

    /// <summary>Initializes decisions with connection configuration and an explicit model.</summary>
    /// <param name="connection">Authentication, endpoint, and transport configuration.</param>
    /// <param name="model">The model used for all decisions.</param>
    public JevDecisionFactory(JevConnection connection, string model)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(connection.ApiKey);
        if (connection.Endpoint is null || !connection.Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("The Jev endpoint must be an absolute URI.", nameof(connection));
        }
        if (connection.NetworkTimeout is TimeSpan timeout && timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("NetworkTimeout must be positive.", nameof(connection));
        }
        Connection = connection;
        _apiKey = connection.ApiKey;
        _model = model;
        _endpoint = connection.Endpoint;
        _networkTimeout = connection.NetworkTimeout;
        _httpClient = connection.HttpClientFactory?.Invoke() ?? throw new ArgumentException("HttpClientFactory must return an HTTP client.", nameof(connection));
    }

    internal override async Task<DecisionEvaluation> EvaluateAsync(DecisionRequestBase request, List<QuestionDefinition> definitions, CancellationToken cancellationToken)
    {
        if (request is DecisionImageRequest or ChoiceImageRequest or ProbabilityImageRequest or ScoreImageRequest)
        {
            throw new NotSupportedException("The Jev Decisions API does not currently support images.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EvidenceInput);
        if (request.SafetyIdentifier is not null)
        {
            throw new NotSupportedException("The Jev Decisions API does not support SafetyIdentifier.");
        }
        if (definitions.Any(definition => definition.Attribute.Kind == "score" && definition.Names.Length > 10))
        {
            throw new InvalidOperationException("Jev score questions require between 2 and 10 enum levels.");
        }
        Dictionary<string, object> questions = KeyedDecisionProtocol.CreateQuestions(definitions);
        string requestJson = JsonSerializer.Serialize(new { model = _model, state = request.EvidenceInput, questions }, new JsonSerializerOptions { WriteIndented = true });
        using HttpRequestMessage message = new(HttpMethod.Post, _endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        message.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using CancellationTokenSource? timeoutSource = _networkTimeout is null ? null : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_networkTimeout is TimeSpan timeout)
        {
            timeoutSource!.CancelAfter(timeout);
        }
        CancellationToken requestToken = timeoutSource?.Token ?? cancellationToken;
        using HttpResponseMessage response = await _httpClient.SendAsync(message, requestToken).ConfigureAwait(false);
        string responseJson = await response.Content.ReadAsStringAsync(requestToken).ConfigureAwait(false);
        request.RawHttpCallDetails?.Invoke(new RawCallDetails
        {
            RequestUrl = _endpoint.AbsoluteUri,
            RequestData = requestJson,
            ResponseData = responseJson
        });
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(responseJson);
        return KeyedDecisionProtocol.ReadEvaluation(document.RootElement, definitions);
    }
}
