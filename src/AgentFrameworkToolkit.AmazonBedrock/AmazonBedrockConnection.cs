using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.BedrockRuntime;
using JetBrains.Annotations;

namespace AgentFrameworkToolkit.AmazonBedrock;

/// <summary>
/// Represents a connection for Amazon Bedrock
/// </summary>
[PublicAPI]
public class AmazonBedrockConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public AmazonBedrockConnection()
    {
        //Empty
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="region">AWS region for Amazon Bedrock Runtime</param>
    /// <param name="apiKey">Amazon Bedrock API key</param>
    [SetsRequiredMembers]
    public AmazonBedrockConnection(RegionEndpoint region, string apiKey)
    {
        Region = region;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Amazon Bedrock API key (Bearer token). It will be applied via the
    /// <c>AWS_BEARER_TOKEN_BEDROCK</c> environment variable if not already defined
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// AWS region for Amazon Bedrock Runtime
    /// </summary>
    public required RegionEndpoint Region { get; set; }

    /// <summary>
    /// The timeout value of the LLM Call (if not defined the underlying infrastructure's default will be used)
    /// </summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>Lists the region's foundation models and inference profiles, following every profile page.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Foundation model IDs and inference profile IDs with provider metadata. Custom models and provisioned throughput are not included.</returns>
    /// <remarks>Requires bedrock:ListFoundationModels and bedrock:ListInferenceProfiles permissions. Catalog entries do not guarantee inference access.</remarks>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        ApplyApiKey();
        AmazonBedrockConfig config = new()
        {
            RegionEndpoint = Region
        };
        if (NetworkTimeout.HasValue)
        {
            config.Timeout = NetworkTimeout.Value;
        }

        using AmazonBedrockClient client = new(config);
        ListFoundationModelsResponse response = await client.ListFoundationModelsAsync(new ListFoundationModelsRequest(), cancellationToken);
        List<ModelInfo> models = [];
        foreach (FoundationModelSummary model in response.ModelSummaries ?? [])
        {
            models.Add(new ModelInfo
            {
                Name = model.ModelId,
                RawRepresentation = model
            });
        }

        string? nextToken = null;
        do
        {
            ListInferenceProfilesResponse profiles = await client.ListInferenceProfilesAsync(new ListInferenceProfilesRequest { NextToken = nextToken }, cancellationToken);
            foreach (InferenceProfileSummary profile in profiles.InferenceProfileSummaries ?? [])
            {
                models.Add(new ModelInfo
                {
                    Name = profile.InferenceProfileId,
                    RawRepresentation = profile
                });
            }

            nextToken = profiles.NextToken;
        } while (!string.IsNullOrEmpty(nextToken));

        return models;
    }

    /// <summary>
    /// Get a Raw Client
    /// </summary>
    /// <returns>The Raw Client</returns>
    public IAmazonBedrockRuntime GetClient()
    {
        ApplyApiKey();

        AmazonBedrockRuntimeConfig config = new()
        {
            RegionEndpoint = Region
        };

        if (NetworkTimeout.HasValue)
        {
            config.Timeout = NetworkTimeout.Value;
        }

        return new AmazonBedrockRuntimeClient(config);
    }

    private void ApplyApiKey()
    {
        const string variable = "AWS_BEARER_TOKEN_BEDROCK";
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)) && !string.IsNullOrWhiteSpace(ApiKey))
        {
            Environment.SetEnvironmentVariable(variable, ApiKey);
        }
    }
}
