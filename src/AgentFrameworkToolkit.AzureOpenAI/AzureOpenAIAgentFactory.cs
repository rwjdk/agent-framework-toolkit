using AgentFrameworkToolkit.OpenAI;
using Azure.Core;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.AzureOpenAI;

/// <summary>
/// Factory for creating AzureOpenAI Agents
/// </summary>
[PublicAPI]
public class AzureOpenAIAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public AzureOpenAIConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Your AzureOpenAI Endpoint (not to be confused with a Microsoft Foundry Endpoint. format: 'https://YourName.openai.azure.com' or 'https://YourName.services.azure.com')</param>
    /// <param name="apiKey">Your AzureOpenAI API Key (if you need a more advanced connection use the constructor overload)</param>
    public AzureOpenAIAgentFactory(string endpoint, string apiKey)
    {
        Connection = new AzureOpenAIConnection
        {
            Endpoint = endpoint,
            ApiKey = apiKey
        };
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Your AzureOpenAI Endpoint (not to be confused with a Microsoft Foundry Endpoint. format: 'https://YourName.openai.azure.com' or 'https://YourName.services.azure.com')</param>
    /// <param name="credentials">Your RBAC Credentials (if you need a more advanced connection use the constructor overload)</param>
    public AzureOpenAIAgentFactory(string endpoint, TokenCredential credentials)
    {
        Connection = new AzureOpenAIConnection
        {
            Endpoint = endpoint,
            Credentials = credentials
        };
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public AzureOpenAIAgentFactory(AzureOpenAIConnection connection)
    {
        Connection = connection;
    }


    /// <summary>
    /// Create a simple Agent with default settings (For more advanced agents use the options overloads)
    /// </summary>
    /// <param name="model">Name of the model to use</param>
    /// <param name="instructions">Instructions for the Agent to follow (aka Developer Message)</param>
    /// <param name="name">Name of the Agent</param>
    /// <param name="tools">Tools for the Agent</param>
    /// <returns>An Agent</returns>
    public AzureOpenAIAgent CreateAgent(string model, string? instructions = null, string? name = null, IList<AITool>? tools = null)
    {
        return CreateAgent(new AgentOptions
        {
            Model = model,
            Name = name,
            Instructions = instructions,
            Tools = tools
        });
    }

    /// <summary>
    /// Create a new Agent
    /// </summary>
    /// <param name="options">Options for the agent</param>
    /// <returns>The Agent</returns>
    public AzureOpenAIAgent CreateAgent(AgentOptions options)
    {
        global::OpenAI.OpenAIClient client = Connection.GetClient(options.RawHttpCallDetails);
        ChatClientAgent innerAgent = OpenAIAgentFactory.GetChatClientAgent(options, client, options.Model, Connection.DefaultClientType);
        return new AzureOpenAIAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }
}
