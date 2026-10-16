using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Google.GenAI.Types;

namespace AgentFrameworkToolkit.Google;

/// <summary>
/// Represents a connection for Google
/// </summary>
public class GoogleConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public GoogleConnection()
    {
        //Empty
    }

    /// <summary>
    /// Constructor (Gemini)
    /// </summary>
    /// <param name="apiKey">Optional String for the <a href="https://ai.google.dev/gemini-api/docs/api-key">API key</a>. Gemini API only.</param>
    public GoogleConnection(string? apiKey)
    {
        ApiKey = apiKey;
    }

    /// <summary>
    /// Constructor (Vertex)
    /// </summary>
    /// <param name="project">Project ID</param>
    /// <param name="location">Location</param>
    public GoogleConnection(string project, string location)
    {
        VertexAI = true;
        Project = project;
        Location = location;
    }

    /// <summary>
    /// Optional String for the <a href="https://ai.google.dev/gemini-api/docs/api-key">API key</a>. Gemini API only.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Vertex Only: Optional Boolean for whether to use Vertex AI APIs. If not specified here nor in the environment variable, defaults to false.
    /// </summary>
    public bool? VertexAI { get; set; }

    /// <summary>
    /// Vertex Only: Optional - see:Google.Apis.Auth.OAuth2.GoogleCredential.
    /// </summary>
    public ICredential? Credential { get; set; }

    /// <summary>
    /// The timeout value of the LLM Call (if not defined the underlying infrastructure's default will be used)
    /// </summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>Lists base models exposed by the configured Gemini or Vertex AI endpoint, following every page.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The models and their metadata.</returns>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        using Client client = GetClient();
        Pager<Model, ListModelsConfig, ListModelsResponse> pager = await client.Models.ListAsync(cancellationToken: cancellationToken);
        List<ModelInfo> models = [];
        await foreach (Model model in pager.WithCancellation(cancellationToken))
        {
            models.Add(new ModelInfo
            {
                Name = model.Name ?? throw new AgentFrameworkToolkitException("Google returned a model without a name."),
                RawRepresentation = model
            });
        }

        return models;
    }

    /// <summary>
    /// Vertex Only: Optional String for the project ID. Find it here: https://cloud.google.com/resource-manager/docs/creating-managing-projects#identifying_projects
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Vertex Only: Optional String for the location. See: https://cloud.google.com/vertex-ai/generative-ai/docs/learn/locations
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Options for the HTTP Request
    /// </summary>
    public HttpOptions? HttpOptions { get; set; }

    /// <summary>
    /// Get a Raw Client
    /// <param name="rawHttpCallDetails">An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM</param>
    /// </summary>
    /// <returns>The Raw Client</returns>
    public Client GetClient(Action<RawCallDetails>? rawHttpCallDetails = null)
    {
        HttpOptions? httpOptions = HttpOptions;
        if (NetworkTimeout.HasValue)
        {
            if (httpOptions == null)
            {
                httpOptions = new HttpOptions
                {
                    Timeout = Convert.ToInt32(NetworkTimeout.Value.TotalMicroseconds)
                };
            }
            else
            {
                httpOptions.Timeout ??= Convert.ToInt32(NetworkTimeout.Value.TotalMicroseconds);
            }
        }


        ClientOptions? clientOptions = null;
        // ReSharper disable once InvertIf
        if (rawHttpCallDetails != null)
        {
            HttpClient inspectingHttpClient = new(new RawCallDetailsHttpHandler(rawHttpCallDetails));

            clientOptions = new ClientOptions
            {
                HttpClientFactory = () => inspectingHttpClient
            };
        }

        Client client = new(
            vertexAI: VertexAI,
            apiKey: ApiKey,
            credential: Credential,
            clientOptions: clientOptions,
            project: Project,
            location: Location,
            httpOptions: httpOptions);

        return client;
    }
}
