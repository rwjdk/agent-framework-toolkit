using AgentFrameworkToolkit.OpenAI;
using Azure.Core;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using OpenAI;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.AzureOpenAI;

/// <summary>
/// Represents a connection for Azure OpenAI
/// </summary>
[PublicAPI]
public class AzureOpenAIConnection
{
    /// <summary>
    /// Constructor
    /// </summary>
    public AzureOpenAIConnection()
    {
        //Empty
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">The Endpoint of your Azure OpenAI Resource</param>
    /// <param name="apiKey">The API Key</param>
    [SetsRequiredMembers]
    public AzureOpenAIConnection(string endpoint, string apiKey)
    {
        Endpoint = endpoint;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">The Endpoint of your Azure OpenAI Resource</param>
    /// <param name="credentials">Credentials for Role-Based Access Control (Example 'DefaultAzureCredential' or 'AzureCliCredential')</param>
    [SetsRequiredMembers]
    public AzureOpenAIConnection(string endpoint, TokenCredential credentials)
    {
        Endpoint = endpoint;
        Credentials = credentials;
    }

    /// <summary>
    /// The Default ClientType (ChatClient or ResponsesAPI) to use for Agents (ChatClient is default)
    /// </summary>
    public ClientType DefaultClientType { get; set; } = ClientType.ChatClient;

    /// <summary>
    /// The Endpoint of your Azure OpenAI Resource
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Autocorrect of the given endpoint if the pattern 'https://[name].services.ai.azure.com/api/projects/[project]' is detected to the valid 'https://[name].services.ai.azure.com' (Default = true)
    /// </summary>
    public bool AutoCorrectFoundryEndpoint { get; set; } = true;

    /// <summary>
    /// The API Key (or use Credentials instead for RBAC)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Credentials for Role-Based Access Control (Example 'DefaultAzureCredential' or 'AzureCliCredential') [or use ApiKey instead]
    /// </summary>
    public TokenCredential? Credentials { get; set; }

    /// <summary>
    /// An Action that allows you to set additional options on the OpenAIClientOptions
    /// </summary>
    public Action<OpenAIClientOptions>? AdditionalOpenAIClientOptions { get; set; }

    /// <summary>
    /// The timeout value of the LLM Call (if not defined the underlying infrastructure's default will be used)
    /// </summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>
    /// Get a Raw Client
    /// </summary>
    /// <param name="rawHttpCallDetails">An Action, if set, will attach an HTTP Message Handler so you can see the raw HTTP Calls that are sent to the LLM</param>
    /// <returns>The Raw Client</returns>
    public OpenAIClient GetClient(Action<RawCallDetails>? rawHttpCallDetails = null)
    {
        OpenAIClientOptions openAIClientOptions = new()
        {
            Endpoint = new Uri($"{GetEndpointUrl().TrimEnd('/')}/openai/v1/"),
            NetworkTimeout = NetworkTimeout
        };

        // ReSharper disable once InvertIf
        if (rawHttpCallDetails != null)
        {
            HttpClient inspectingHttpClient = new(new RawCallDetailsHttpHandler(rawHttpCallDetails));
            openAIClientOptions.Transport = new HttpClientPipelineTransport(inspectingHttpClient);
        }

        AdditionalOpenAIClientOptions?.Invoke(openAIClientOptions);

        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            return new OpenAIClient(new ApiKeyCredential(ApiKey!), openAIClientOptions);
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (Credentials != null)
        {
            return new OpenAIClient(
                new BearerTokenPolicy(Credentials, "https://ai.azure.com/.default"),
                openAIClientOptions);
        }

        throw new AgentFrameworkToolkitException("Neither APIKey nor TokenCredentials was provided in the AzureConnection");
    }

    private string GetEndpointUrl()
    {
        string endpointUrl = Endpoint;
        if (AutoCorrectFoundryEndpoint)
        {
            endpointUrl = AzureAiUrlHelper.RemoveSuffixIfMatches(AzureAiUrlHelper.ProjectPattern, endpointUrl);
            endpointUrl = AzureAiUrlHelper.RemoveSuffixIfMatches(AzureAiUrlHelper.OpenAiPattern, endpointUrl);
        }

        return endpointUrl;
    }

    internal static class AzureAiUrlHelper
    {
        internal static readonly Regex ProjectPattern = new(
            @"^https://.+?\.services\.ai\.azure\.com(?<suffix>/api/projects/.+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static readonly Regex OpenAiPattern = new(
            @"^https://.+?\.openai\.azure\.com(?<suffix>/openai/v1(?:/.+)?)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        
        public static string RemoveSuffixIfMatches(Regex regex, string input)
        {
            Match match = regex.Match(input);
            if (!match.Success)
            {
                return input;
            }

            string suffix = match.Groups["suffix"].Value;
            return input[..^suffix.Length];
        }
    }
}
