using AgentFrameworkToolkit.OpenAI;
using Azure.AI.Projects;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.ClientModel;

#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.MicrosoftFoundry;

/// <summary>
/// Factory for creating Microsoft Foundry Agents
/// </summary>
[PublicAPI]
public class MicrosoftFoundryAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public MicrosoftFoundryConnection Connection { get; }

    /// <summary>
    /// Methods for working with Declarative Agents (aka Agents that 'live' as definitions in ai.azure.com)
    /// </summary>
    public MicrosoftFoundryDeclarativeAgentFactory DeclarativeAgentFactory { get; }

    /// <summary>
    /// Methods for working with Declarative Agents (aka Agents that 'live' as definitions in ai.azure.com)
    /// </summary>
    public MicrosoftFoundryHostedAgentFactory HostedAgentFactory { get; }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public MicrosoftFoundryAgentFactory(MicrosoftFoundryConnection connection)
    {
        Connection = connection;
        DeclarativeAgentFactory = new MicrosoftFoundryDeclarativeAgentFactory(connection);
        HostedAgentFactory = new MicrosoftFoundryHostedAgentFactory(connection);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="endpoint">Endpoint of Microsoft Foundry Project</param>
    /// <param name="authenticationTokenProvider">Optional TokenProvider used for credentials; if not provided DefaultAzureCredential will be used</param>
    public MicrosoftFoundryAgentFactory(string endpoint, AuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        Connection = new MicrosoftFoundryConnection(endpoint, authenticationTokenProvider);
        DeclarativeAgentFactory = new MicrosoftFoundryDeclarativeAgentFactory(Connection);
        HostedAgentFactory = new MicrosoftFoundryHostedAgentFactory(Connection);
    }

    /// <summary>
    /// Create a simple Agent with default settings (For more advanced agents use the options overloads)
    /// </summary>
    /// <param name="model">Name of the model to use</param>
    /// <param name="instructions">Instructions for the Agent to follow (aka Developer Message)</param>
    /// <param name="name">Name of the Agent</param>
    /// <param name="tools">Tools for the Agent</param>
    /// <returns>An Agent</returns>
    public MicrosoftFoundryAgent CreateAgent(string model, string? instructions = null, string? name = null, IList<AITool>? tools = null)
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
    public MicrosoftFoundryAgent CreateAgent(AgentOptions options)
    {
        AIProjectClient client = Connection.GetClient(options.RawHttpCallDetails);
        ChatClientAgent innerAgent = OpenAIAgentFactory.GetChatClientAgent(options, client.ProjectOpenAIClient, options.Model, Connection.DefaultClientType);
        return new MicrosoftFoundryAgent(MiddlewareHelper.ApplyMiddleware(
            innerAgent,
            options.RawToolCallDetails,
            options.ToolCallingMiddleware,
            options.OpenTelemetryMiddleware,
            options.LoggingMiddleware,
            options.Services));
    }
}