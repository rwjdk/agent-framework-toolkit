using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Mistral.SDK;

namespace AgentFrameworkToolkit.Mistral;

/// <summary>
/// Represents a connection for Mistral
/// </summary>
[PublicAPI]
public class MistralConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public MistralConnection()
    {
        //Empty
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">The API Key to be used</param>
    [SetsRequiredMembers]
    public MistralConnection(string apiKey)
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

    /// <summary>Lists models available to the user.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The models and their metadata.</returns>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        MistralClient client = GetClient();
        global::Mistral.SDK.DTOs.ModelList response = await client.Models.GetModelsAsync(cancellationToken);
        return response.Data.Select(model => new ModelInfo
        {
            Name = model.Id,
            RawRepresentation = model
        }).ToArray();
    }

    /// <summary>
    /// Get a Raw Client
    /// </summary>
    /// <param name="rawHttpCallDetails">An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM</param>
    /// <returns>The Raw Client</returns>
    public MistralClient GetClient(Action<RawCallDetails>? rawHttpCallDetails = null)
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

        return new MistralClient(new APIAuthentication(ApiKey), httpClient);
    }
}
