using Amazon.BedrockRuntime;
using Amazon;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFrameworkToolkit.AmazonBedrock;

/// <summary>
/// Factory for creating Amazon Bedrock Agents
/// </summary>
[PublicAPI]
public class AmazonBedrockAgentFactory
{
    /// <summary>
    /// Connection
    /// </summary>
    public AmazonBedrockConnection Connection { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="region">AWS region for Amazon Bedrock Runtime</param>
    /// <param name="apiKey">Amazon Bedrock API key (Bearer token)</param>
    public AmazonBedrockAgentFactory(RegionEndpoint region, string apiKey)
    {
        Connection = new AmazonBedrockConnection
        {
            ApiKey = apiKey,
            Region = region
        };
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="connection">Connection Details</param>
    public AmazonBedrockAgentFactory(AmazonBedrockConnection connection)
    {
        Connection = connection;
    }

    /// <summary>
    /// Create a simple Agent with default settings (For more advanced agents use the options overload)
    /// </summary>
    /// <param name="model">Name of the Model to use</param>
    /// <param name="instructions">Instructions for the Agent to follow (aka Developer Message)</param>
    /// <param name="name">Name of the Agent</param>
    /// <param name="tools">Tools for the Agent</param>
    /// <returns>An Agent</returns>
    public AmazonBedrockAgent CreateAgent(string model, string? instructions = null, string? name = null, IList<AITool>? tools = null)
    {
        return CreateAgent(new AmazonBedrockAgentOptions
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
    public AmazonBedrockAgent CreateAgent(AmazonBedrockAgentOptions options)
    {
        IAmazonBedrockRuntime runtimeClient = Connection.GetClient();
        IChatClient client = runtimeClient.AsIChatClient(options.Model);

        AIAgent innerAgent = new ChatClientAgent(client, CreateChatClientAgentOptions(options), options.LoggerFactory, options.Services);

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (options.ToolCallingMiddleware != null || options.OpenTelemetryMiddleware != null || options.LoggingMiddleware != null)
        {
            return new AmazonBedrockAgent(MiddlewareHelper.ApplyMiddleware(
                innerAgent,
                null,
                options.ToolCallingMiddleware,
                options.OpenTelemetryMiddleware,
                options.LoggingMiddleware,
                options.Services));
        }

        return new AmazonBedrockAgent(innerAgent);
    }

    internal static ChatClientAgentOptions CreateChatClientAgentOptions(AmazonBedrockAgentOptions options)
    {
        ChatOptions chatOptions = new()
        {
            ModelId = options.Model,
            Instructions = options.Instructions,
            MaxOutputTokens = options.MaxOutputTokens,
            Temperature = options.Temperature,
        };

        if (options.Tools != null)
        {
            chatOptions.Tools = options.Tools;
        }

        ChatClientAgentOptions chatClientAgentOptions = new()
        {
            Name = options.Name,
            Description = options.Description,
            Id = options.Id,
            ChatOptions = chatOptions,
            AIContextProviders = options.AIContextProviders,
            ChatHistoryProvider = options.ChatHistoryProvider,
        };

        options.AdditionalChatClientAgentOptions?.Invoke(chatClientAgentOptions);

        return chatClientAgentOptions;
    }
}
