using AgentFrameworkToolkit.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using JetBrains.Annotations;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Connection for Microsoft Foundry
/// </summary>
[PublicAPI]
public class MicrosoftFoundryConnection
{
    /// <summary>
    /// Endpoint of Microsoft Foundry Project
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// TokenProvider used for credentials; if not provided DefaultAzureCredential will be used
    /// </summary>
    public AuthenticationTokenProvider? AuthenticationTokenProvider { get; set; }

    /// <summary>
    /// An Action that allow you to set additional options on the ProjectClientOptions
    /// </summary>
    public Action<AIProjectClientOptions>? AdditionalProjectClientOptions { get; set; }

    /// <summary>
    /// The Default ClientType (ChatClient or ResponsesAPI) to use for Agents (Responses API is default)
    /// </summary>
    public ClientType DefaultClientType { get; set; } = ClientType.ResponsesApi;
    
    /// <summary>
    /// The timeout value of the LLM Call (if not defined the underlying infrastructure's default will be used)
    /// </summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>Lists all model deployments in the project, following every page.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Deployment names and underlying model metadata.</returns>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        AIProjectClient client = GetClient();
        List<ModelInfo> models = [];
        await foreach (AIProjectDeployment deployment in client.Deployments.GetDeploymentsAsync(cancellationToken: cancellationToken))
        {
            if (deployment is ModelDeployment model)
            {
                models.Add(new ModelInfo
                {
                    Name = string.IsNullOrEmpty(model.ConnectionName) ? model.Name : $"{model.ConnectionName}/{model.Name}",
                    RawRepresentation = model
                });
            }
        }

        return models;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public MicrosoftFoundryConnection()
    {
        //Empty
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Endpoint of Microsoft Foundry Project</param>
    /// <param name="authenticationTokenProvider">Optional TokenProvider used for credentials; if not provided DefaultAzureCredential will be used</param>
    [SetsRequiredMembers]
    public MicrosoftFoundryConnection(string endpoint, AuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        Endpoint = endpoint;
        AuthenticationTokenProvider = authenticationTokenProvider;
    }

    /// <summary>
    /// Get a Raw Client
    /// </summary>
    /// <param name="rawHttpCallDetails">An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM</param>
    /// <returns>The Raw Client</returns>
    public AIProjectClient GetClient(Action<RawCallDetails>? rawHttpCallDetails = null)
    {
        return GetClient(rawHttpCallDetails, null);
    }

    internal AIProjectClient GetClient(Action<RawCallDetails>? rawHttpCallDetails, AgentOptions? agentOptions)
    {
        AIProjectClientOptions options = new()
        {
            NetworkTimeout = NetworkTimeout
        };

        // ReSharper disable once InvertIf
        if (rawHttpCallDetails != null)
        {
            HttpClient inspectingHttpClient = new(new RawCallDetailsHttpHandler(rawHttpCallDetails));
            options.Transport = new HttpClientPipelineTransport(inspectingHttpClient);
        }

        AdditionalProjectClientOptions?.Invoke(options);

        if (agentOptions != null)
        {
            AzureImageGenerationSupport.Configure(options, agentOptions, DefaultClientType);
        }

        return new AIProjectClient(
            new Uri(Endpoint),
            AuthenticationTokenProvider ?? new DefaultAzureCredential(), options);
    }
}
