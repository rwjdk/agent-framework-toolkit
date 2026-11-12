using System.Diagnostics.CodeAnalysis;
using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using OpenAI.Models;

namespace AgentFrameworkToolkit.Cohere;

/// <summary>
/// Represents a connection for Cohere
/// </summary>
[PublicAPI]
public class CohereConnection : OpenAIConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CohereConnection()
    {
        Endpoint = DefaultEndpoint;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">The API Key to be used</param>
    [SetsRequiredMembers]
    public CohereConnection(string apiKey) : base(apiKey)
    {
        Endpoint = DefaultEndpoint;
    }

    /// <summary>
    /// The Default Cohere Endpoint
    /// </summary>
    public const string DefaultEndpoint = "https://api.cohere.ai/compatibility/v1";

    /// <summary>Lists all models using Cohere's native models API, following every page.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The model names and native JSON metadata.</returns>
    public override async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        OpenAIModelClient client = GetClient().GetOpenAIModelClient();
#pragma warning disable OPENAI001 // Preserve endpoint overrides configured through OpenAIClientOptions.
        UriBuilder endpoint = new(client.Endpoint);
#pragma warning restore OPENAI001
        string path = endpoint.Path.TrimEnd('/');
        const string compatibilitySuffix = "/compatibility/v1";
        if (path.EndsWith(compatibilitySuffix, StringComparison.Ordinal))
        {
            path = path[..^compatibilitySuffix.Length] + "/v1";
        }

        endpoint.Path = path + "/models";
        string? pageToken = null;
        List<ModelInfo> models = [];
        do
        {
            UriBuilder pageEndpoint = new(endpoint.Uri);
            if (pageToken != null)
            {
                pageEndpoint.Query = string.IsNullOrEmpty(endpoint.Query)
                    ? "page_token=" + Uri.EscapeDataString(pageToken)
                    : endpoint.Query.TrimStart('?') + "&page_token=" + Uri.EscapeDataString(pageToken);
            }

            using PipelineMessage message = client.Pipeline.CreateMessage();
            message.Apply(new RequestOptions { CancellationToken = cancellationToken });
            message.Request.Method = "GET";
            message.Request.Uri = pageEndpoint.Uri;
            message.Request.Headers.Set("Accept", "application/json");
            await client.Pipeline.SendAsync(message);
            PipelineResponse response = message.Response!;
            if (response.Status is < 200 or >= 300)
            {
                throw new ClientResultException(response);
            }

            using JsonDocument document = JsonDocument.Parse(response.Content);
            foreach (JsonElement model in document.RootElement.GetProperty("models").EnumerateArray())
            {
                models.Add(new ModelInfo
                {
                    Name = model.GetProperty("name").GetString() ?? throw new JsonException("Cohere returned a model without a name."),
                    RawRepresentation = model.Clone()
                });
            }

            pageToken = document.RootElement.TryGetProperty("next_page_token", out JsonElement nextPageToken) ? nextPageToken.GetString() : null;
        } while (!string.IsNullOrEmpty(pageToken));

        return models;
    }
}
