using System.Diagnostics.CodeAnalysis;
using Anthropic;
using Anthropic.Core;

namespace AgentFrameworkToolkit.Anthropic;

/// <summary>
/// Represents a connection for Anthropic
/// </summary>
public class AnthropicConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public AnthropicConnection()
    {
        //Empty
    }

    /// <summary>
    /// The API Key to be used
    /// </summary>
    /// <param name="apiKey"></param>
    [SetsRequiredMembers]
    public AnthropicConnection(string apiKey)
    {
        ApiKey = apiKey;
    }

    /// <summary>
    /// The API Key to be used
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The timeout value of the LLM Call (if not defined the underlying infrastructure's default will be used)
    /// </summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>
    /// Set the BaseUrl of the Client (if you need to the API from a non-default Endpoint; example Microsoft Foundry)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>Lists all available models, following every page returned by the provider.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The available models and their metadata.</returns>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        using AnthropicClient client = GetClient();
        global::Anthropic.Models.Models.ModelListPage page = await client.Models.List(cancellationToken: cancellationToken);
        List<ModelInfo> models = [];
        while (true)
        {
            foreach (global::Anthropic.Models.Models.ModelInfo model in page.Items)
            {
                models.Add(new ModelInfo
                {
                    Name = model.ID,
                    RawRepresentation = model
                });
            }

            if (!page.HasNext())
            {
                return models;
            }

            page = await page.Next(cancellationToken);
        }
    }

    /// <summary>
    /// Get a Raw Client
    /// </summary>
    /// <param name="rawHttpCallDetails">An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM</param>
    /// <returns>The Raw Client</returns>
    public AnthropicClient GetClient(Action<RawCallDetails>? rawHttpCallDetails = null)
    {
        HttpClient? httpClient = null;

        // ReSharper disable once InvertIf
        if (rawHttpCallDetails != null)
        {
            httpClient = new HttpClient(new RawCallDetailsHttpHandler(rawHttpCallDetails));
        }

        if (NetworkTimeout.HasValue)
        {
            httpClient ??= new HttpClient();
            httpClient.Timeout = NetworkTimeout.Value;
        }

        ClientOptions clientOptions = new()
        {
            ApiKey = ApiKey,
            Timeout = NetworkTimeout
        };

        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            clientOptions.BaseUrl = Endpoint;
        }

        if (httpClient != null)
        {
            clientOptions.HttpClient = httpClient;
        }

        return new AnthropicClient(clientOptions);
    }
}
